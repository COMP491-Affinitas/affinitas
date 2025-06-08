import logging
from typing import Awaitable

from beanie import PydanticObjectId
from beanie.odm.operators.find.array import ElemMatch
from beanie.odm.operators.update.array import Push
from beanie.odm.operators.update.general import Inc, Set
from beanie.odm.queries.update import UpdateResponse
from fastapi import Response, HTTPException, status
from fastapi.background import BackgroundTasks
from fastapi.requests import Request
from fastapi.routing import APIRouter
from pydantic import TypeAdapter

from affinitas_backend.chat import get_message
from affinitas_backend.chat import npc_chat_service, master_llm_service
from affinitas_backend.db.utils import get_npc_quests_pipeline
from affinitas_backend.models.beanie.npc import NPC
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.schemas.chat import NPCChatRequest, NPCChatResponse
from affinitas_backend.models.schemas.npcs import NPCQuestResponses, NPCQuestRequest, NPCQuestCompleteRequest, \
    NPCQuestCompleteResponse, NPCGiveItemRequest
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.utils import throw_500

router = APIRouter(prefix="/npcs", tags=["npcs"])


@router.post(
    "/{npc_id}/chat",
    response_model=NPCChatResponse,
    status_code=status.HTTP_200_OK,
    summary="Send a message to an NPC and receive a response",
    description="Handles messaging between the player and a specific NPC.\n\n"
                "**Behavior:**\n"
                "- If the message is from the user (`role='user'`):\n"
                "  - The NPC will generate a reply using the LLM.\n"
                "  - The updated `affinitas`, possible quest completions, and NPC profile changes are returned.\n"
                "- If the message is a system instruction (`role='system'`):\n"
                "  - No response is generated, and the endpoint returns HTTP 204 (No Content).\n\n"
                "**Additional Notes:**\n"
                "- The message, NPC response, and quest changes are recorded in the `ShadowSave` document.\n"
                "- Updates are handled asynchronously using background tasks to minimize latency.\n"
                "**Rate Limit:** 10 requests per minute per client.",
    responses={
        status.HTTP_200_OK: {
            "description": "NPC response with updated affinitas and completed quests.",
            "content": {
                "application/json": {
                    "example": {
                        "response": "The stars have shifted. Be wary tonight.",
                        "affinitas_new": 78,
                        "completed_quests": ["664fe3e382ac71b15bda3ec9"]
                    }
                }
            }
        },
        status.HTTP_204_NO_CONTENT: {
            "description": "No content. System messages are acknowledged silently.",
            "headers": {
                "Cache-Control": {
                    "description": "Indicates that the response should not be cached.",
                    "schema": {
                        "type": "string",
                        "example": "no-store, no-cache, must-revalidate, max-age=0"
                    }
                }
            }
        }
    }
)
@limiter.limit("10/minute")
async def npc_chat(
        request: Request,
        npc_id: PydanticObjectId,
        payload: NPCChatRequest,
        x_client_uuid: XClientUUIDHeader,
        background_tasks: BackgroundTasks,
):
    """
    Handles a chat interaction with a given NPC.

    - User messages will receive an AI-generated response.
    - System messages return 204 and are logged without triggering AI logic.
    """
    message = get_message(payload.role, payload.content)
    shadow_save_id = payload.shadow_save_id

    update_query = (
        ShadowSave
        .find(ShadowSave.id == shadow_save_id)
        .find(ShadowSave.npcs.npc_id == npc_id)  # noqa
    )

    if payload.role == "user":
        res = await npc_chat_service.get_response(
            message=message,
            npc_id=npc_id,
            shadow_save_id=shadow_save_id,
        )

        npc_response = res["message"]
        updated_npc_data = res["updated_npc_data"]
        completed_quests = res["completed_quests"]

        chat = [(payload.role, payload.content), ("ai", npc_response)]
        update_query = (
            update_query
            .update(
                Set({
                    "npcs.$.affinitas": updated_npc_data["affinitas"],
                    "npcs.$.occupation": updated_npc_data["occupation"],
                    "npcs.$.likes": updated_npc_data["likes"],
                    "npcs.$.dislikes": updated_npc_data["dislikes"],
                    "journal_active": True,
                    "journal_data.npcs.$[npc].active": True,
                    "journal_data.town_info.active": True,
                }),
                Push({
                    "npcs.$.chat_history": {"$each": chat},
                    "npcs.$.completed_quests": {"$each": completed_quests},
                    "journal_data.chat_history.$[group].chat_history": {"$each": chat},
                }),
                array_filters=[
                    {"group.npc_id": npc_id},
                    {"npc.npc_id": npc_id}
                ],
            )
        )

        response = NPCChatResponse(
            response=npc_response,
            affinitas_new=updated_npc_data["affinitas"],
            completed_quests=TypeAdapter(list[PydanticObjectId]).validate_python(completed_quests)
        )
    else:
        response = Response(
            status_code=status.HTTP_204_NO_CONTENT,
            headers={"Cache-Control": "no-store, no-cache, must-revalidate, max-age=0"},
            background=background_tasks
        )
        update_query = (
            update_query
            .update(Push({"npcs.$.chat_history": {"$each": [(payload.role, payload.content)]}}))
        )

    background_tasks.add_task(await_coroutine, update_query)
    return response


@router.post(
    "/{npc_id}/quest",
    response_model=NPCQuestResponses,
    status_code=status.HTTP_200_OK,
    summary="Retrieve active quests for a given NPC",
    description="Returns the main quest and associated subquests for the specified NPC.\n\n"
                "**Behavior:**\n"
                "- The first item in the `quests` list is always the main quest.\n"
                "- Remaining items, if any, are treated as subquests.\n"
                "- The `X-Client-UUID` header is required for request tracking and authentication.\n"
                "- All quests are marked as `active` in the database upon retrieval.\n"
                "- If a quest is linked to another NPC, a system message is logged for that NPC in the background.\n"
                "- Quest responses are generated via the master LLM and logged to both the NPC and journal chat history.\n\n"
                "**Rate Limit:** 10 requests per minute per client.",
    responses={
        status.HTTP_200_OK: {
            "description": "List of active quests and LLM-generated responses for the NPC.",
            "content": {
                "application/json": {
                    "example": {
                        "quests": [
                            {
                                "quest_id": "664fe3e382ac71b15bda3ec9",
                                "response": "The sacred trees whisper your name already. The grove awaits you."
                            }
                        ]
                    }
                }
            }
        },
        status.HTTP_404_NOT_FOUND: {
            "description": "NPC not found or no quests available for the given NPC and save ID.",
            "content": {
                "application/json": {
                    "example": {"detail": "NPC not found"}
                }
            }
        }
    }
)
@limiter.limit("10/minute")
async def get_quest(
        request: Request,
        npc_id: PydanticObjectId,
        payload: NPCQuestRequest,
        x_client_uuid: XClientUUIDHeader,
        background_tasks: BackgroundTasks
):
    """
    Retrieves and activates quest data for a specific NPC.

    - Updates quest statuses to "active".
    - Triggers system messages for linked NPCs.
    - Logs LLM responses to both NPC and journal histories.
    """
    quests = (
        await ShadowSave
        .aggregate(get_npc_quests_pipeline(npc_id, payload.shadow_save_id))
        .to_list()
    )

    if not quests:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="NPC not found")

    quests = quests[0].get("quests", [])

    update_query = (
        ShadowSave
        .find(ShadowSave.id == payload.shadow_save_id)
        .update(
            Set({
                "npcs.$[npc].quests.$[].status": "active",
                "journal_data.quests.$[group].quests.$[].status": "active"
            }),
            array_filters=[
                {"npc.npc_id": npc_id},
                {"group.npc_id": npc_id}
            ],
        )
    )
    background_tasks.add_task(await_coroutine, update_query)

    for quest in quests:
        linked_npc_id = quest.get("linked_npc")
        if linked_npc_id:
            msg = TAKE_QUEST_PROMPT_TEMPLATE.format(
                quest_id=quest.get("quest_id"),
                quest_name=quest.get("name"),
                quest_description=quest.get("description"),
                keywords=", ".join(map(repr, quest.get("triggers", []))),
            )

            update_query = (
                ShadowSave
                .find(ShadowSave.id == payload.shadow_save_id)
                .update(
                    Push({"npcs.$[npc].chat_history": ("system", msg)}),
                    array_filters=[{"npc.npc_id": linked_npc_id}],
                )
            )
            background_tasks.add_task(await_coroutine, update_query)

    res = await master_llm_service.get_quest_responses(
        quests,
        shadow_save_id=payload.shadow_save_id,
        npc_id=npc_id,
    )

    npc_responses = [("ai", quest["response"]) for quest in res]
    update_query2 = (
        ShadowSave
        .find(ShadowSave.id == payload.shadow_save_id)
        .update(
            Push({
                "npcs.$[npc].chat_history": {"$each": npc_responses},
                "journal_data.chat_history.$[group].chat_history": {"$each": npc_responses},
            }),
            array_filters=[
                {"npc.npc_id": npc_id},
                {"group.npc_id": npc_id}
            ],
        )
    )
    background_tasks.add_task(await_coroutine, update_query2)

    return TypeAdapter(NPCQuestResponses).validate_python({"quests": res})


@router.post(
    "/{npc_id}/quest/complete",
    response_model=NPCQuestCompleteResponse,
    status_code=status.HTTP_200_OK,
    summary="Complete a quest for a specific NPC",
    description="Marks a quest as completed for a given NPC.\n\n"
                "**Behavior:**\n"
                "- Requires `npc_id`, `quest_id`, and `shadow_save_id` in the request body.\n"
                "- Verifies the quest exists and is active under the specified NPC.\n"
                "- Marks the quest as completed in both the NPC and journal records.\n"
                "- Increments the NPC’s `affinitas` score by the quest’s reward amount.\n"
                "- Sends a system message to the NPC via LLM for contextual reaction.\n\n"
                "**Rate Limit:** 10 requests per minute per client.",
    responses={
        status.HTTP_200_OK: {
            "description": "Quest successfully completed. Returns updated affinitas value.",
            "content": {
                "application/json": {
                    "example": {"affinitas": 87}
                }
            }
        },
        status.HTTP_404_NOT_FOUND: {
            "description": "Quest not found or not active for the given NPC.",
            "content": {
                "application/json": {
                    "example": {"detail": "Quest not found for this NPC"}
                }
            }
        }
    }
)
@limiter.limit("10/minute")
async def complete_quest(
        request: Request,
        npc_id: PydanticObjectId,
        payload: NPCQuestCompleteRequest,
        x_client_uuid: XClientUUIDHeader,
):
    """
    Completes an active quest for the specified NPC.

    - Updates quest status to 'completed'.
    - Adds affinitas reward to the NPC.
    - Logs system message and triggers NPC response.
    """
    shadow_save_id = payload.shadow_save_id

    quest_data = await (
        NPC
        .aggregate([
            {"$match": {"_id": npc_id}},
            {"$unwind": "$quests"},
            {"$match": {"quests._id": payload.quest_id}},
            {"$project": {
                "_id": 0,
                "quest_id": "$quests._id",
                "name": "$quests.name",
                "description": "$quests.description",
                "affinitas_reward": "$quests.affinitas_reward"
            }}
        ]).to_list()
    )

    if not quest_data:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Quest not found for this NPC"
        )

    quest_data = quest_data[0]
    quest_id = quest_data["quest_id"]
    quest_name = quest_data["name"]
    quest_description = quest_data["description"]
    quest_reward = quest_data["affinitas_reward"]

    sys_msg = COMPLETE_QUEST_PROMPT_TEMPLATE.format(
        quest_id=quest_id,
        quest_name=quest_name,
        quest_description=quest_description
    )

    res: ShadowSave = await (
        ShadowSave
        .find_one(ShadowSave.id == shadow_save_id)
        .update_one(
            Inc({"npcs.$[npc].affinitas": quest_reward}),
            Set({
                "npcs.$[npc].quests.$[quest].status": "completed",
                "journal_data.quests.$[group].quests.$[quest].status": "completed"
            }),
            Push({"npcs.$[npc].chat_history": ("system", sys_msg)}),
            array_filters=[
                {"npc.npc_id": npc_id},
                {"quest.quest_id": quest_id, "quest.status": "active"},
                {"group.npc_id": npc_id},
            ],
            response_type=UpdateResponse.NEW_DOCUMENT
        )
    )

    if not res:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Active quest not found"
        )

    npc = next(npc for npc in res.npcs if npc.npc_id == npc_id)
    return NPCQuestCompleteResponse(affinitas=npc.affinitas)


@router.post(
    "/{npc_id}/item",
    response_model=NPCChatResponse,
    status_code=status.HTTP_200_OK,
    summary="Give an item to an NPC",
    description="Transfers an item from the player's inventory to a specific NPC and generates an LLM-based response.\n\n"
                "**Behavior:**\n"
                "- Requires a valid, active item in the player's inventory (`item_name`).\n"
                "- The item is marked inactive after the transaction.\n"
                "- A system message is constructed to inform the NPC about the item.\n"
                "- The NPC generates a reply through the LLM, impersonating the player giving the item.\n"
                "- The interaction is logged in both the NPC’s and journal’s chat histories.\n\n"
                "**Rate Limit:** 10 requests per minute per client.",
    responses={
        status.HTTP_200_OK: {
            "description": "NPC successfully received the item and responded. Returns the NPC's message, new affinitas, and any completed quests.",
            "content": {
                "application/json": {
                    "example": {
                        "response": "Oh… this was my brother's ring. I thought it was lost forever.",
                        "affinitas_new": 92,
                        "completed_quests": ["6650fbd5ecf473c6f3a5a8d7"]
                    }
                }
            }
        },
        status.HTTP_404_NOT_FOUND: {
            "description": "The specified item does not exist or is not active in the player's current session.",
            "content": {
                "application/json": {
                    "example": {"detail": "Item not found in inventory for the current session"}
                }
            }
        },
        status.HTTP_500_INTERNAL_SERVER_ERROR: {
            "description": "Unexpected error when processing the NPC's response.",
            "content": {
                "application/json": {
                    "example": {"detail": "Failed to get NPC response after giving item"}
                }
            }
        }
    }
)
@limiter.limit("10/minute")
async def give_item(
        request: Request,
        npc_id: PydanticObjectId,
        payload: NPCGiveItemRequest,
        x_client_uuid: XClientUUIDHeader,
        background_tasks: BackgroundTasks,
):
    """
    Handles the logic for giving an item to an NPC.

    - Verifies item ownership and activity.
    - Triggers LLM response as if the player gave the item.
    - Logs interaction and disables the item.
    """
    shadow_save_id = payload.shadow_save_id
    item_name = payload.item_name

    item_exists = await ShadowSave.find_one(
        ShadowSave.id == shadow_save_id,
        ShadowSave.client_uuid == x_client_uuid,
        ElemMatch(ShadowSave.item_list, {"name": item_name, "active": True})
    )

    if not item_exists:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Item not found in inventory for the current session"
        )

    sys_msg = GIVE_ITEM_TEMPLATE.format(item_name=item_name)

    npc_response = await npc_chat_service.get_response(
        message=get_message("system", sys_msg),
        npc_id=npc_id,
        shadow_save_id=shadow_save_id,
        invoke_model=True
    )

    if not npc_response:
        throw_500(
            "Failed to get NPC response after giving item",
            f"NPC ID: {npc_id}, Shadow Save ID: {shadow_save_id}, Item Name: {item_name}"
        )

    query = (
        ShadowSave
        .find(ShadowSave.id == shadow_save_id)
        .update(
            Push({
                "npcs.$[npc].chat_history": {"$each": [("system", sys_msg), ("ai", npc_response["message"])]},
                "journal_data.chat_history.$[group].chat_history": ("ai", npc_response["message"]),
            }),
            Set({"item_list.$[item].active": False}),
            array_filters=[
                {"npc.npc_id": npc_id},
                {"group.npc_id": npc_id},
                {"item.name": item_name}
            ],
        )
    )

    background_tasks.add_task(await_coroutine, query)

    return NPCChatResponse(
        response=npc_response["message"],
        affinitas_new=npc_response["updated_npc_data"]["affinitas"],
        completed_quests=npc_response["completed_quests"]
    )


async def await_coroutine(coroutine: Awaitable):
    try:
        await coroutine
    except Exception as e:
        logging.error(f"Background update failed: {e}")


TAKE_QUEST_PROMPT_TEMPLATE = """\
The player has accepted this quest:

Quest ID: {quest_id}
Quest Name: {quest_name}
Quest Description: {quest_description}
---
Make use of the keywords below and the quest name and description (if non-null) \
to decide whether the quest is completed.
{keywords}
---
If the player completes the quest, append the quest ID to the `completed_quests` array.\
"""

COMPLETE_QUEST_PROMPT_TEMPLATE = """\
The player has completed this quest:
Quest ID: {quest_id}
Quest Name: {quest_name}
Quest Description: {quest_description}
---
This quest is already completed and should not be included in the `completed_quests` array.
Keep this in mind in future conversations.\
"""

GIVE_ITEM_TEMPLATE = """\
The player has given you the following item:
Item Name: {item_name}
---
On your next response, please acknowledge the item. \
Do not modify the affinitas value, the NPC's state or mark any quest completed. \
Only return a response.\
"""
