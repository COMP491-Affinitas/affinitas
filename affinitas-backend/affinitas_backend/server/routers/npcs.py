import logging

from beanie import PydanticObjectId
from beanie.odm.operators.update.array import Push
from beanie.odm.operators.update.general import Inc
from beanie.odm.operators.update.general import Set
from beanie.odm.queries.update import UpdateMany
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
    NPCQuestCompleteResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter

router = APIRouter(prefix="/npcs", tags=["npcs"])


@router.post(
    "/{npc_id}/chat",
    response_model=NPCChatResponse,
    responses={
        status.HTTP_204_NO_CONTENT: {
            "model": None,
            "description": "Returns no content when the request is a system message",
        }
    },
    summary="Chat with an NPC",
    description="Send a system message or a user message to an NPC. If the request is a system message, "
                "204 No Content will be returned. If the request is a user message, the response "
                "will include the NPC's reply as well as the new affinitas value. The message is logged to the "
                "`ShadowSave` document in the background to ensure that the data is not lost.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("10/minute")
async def npc_chat(
        request: Request,
        npc_id: PydanticObjectId,
        payload: NPCChatRequest,
        x_client_uuid: XClientUUIDHeader,
        background_tasks: BackgroundTasks,
):
    message = get_message(payload.role, payload.content)
    shadow_save_id = payload.shadow_save_id

    update_query = (
        ShadowSave
        .find(ShadowSave.id == shadow_save_id)
        .find(ShadowSave.npcs.npc_id == npc_id)  # noqa
    )

    res = await npc_chat_service.get_response(
        message=message,
        npc_id=npc_id,
        shadow_save_id=shadow_save_id,
    )

    if res:
        npc_response = res["message"]
        updated_npc_data = res["updated_npc_data"]
        completed_quests = res["completed_quests"]
        update_query = (
            update_query
            .update(
                Set({
                    "npcs.$.affinitas": updated_npc_data["affinitas"],
                    "npcs.$.occupation": updated_npc_data["occupation"],
                    "npcs.$.likes": updated_npc_data["likes"]
                }),
                Push({
                    "npcs.$.chat_history": {"$each": [(payload.role, payload.content), ("ai", npc_response)]},
                    "npcs.$.completed_quests": {"$each": completed_quests},
                }),
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

    background_tasks.add_task(_update_document, update_query)

    return response


@router.post(
    "/{npc_id}/quest",
    response_model=NPCQuestResponses,
    summary="Get NPC quest and subquest data",
    description="Returns the quest and subquest data for an NPC. The first item in the list will be the main quest, "
                "and the subsequent items will be subquests. The `X-Client-UUID` header must be provided.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("10/minute")
async def get_quest(request: Request, npc_id: PydanticObjectId, payload: NPCQuestRequest,
                    x_client_uuid: XClientUUIDHeader, background_tasks: BackgroundTasks):
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
            Set({"npcs.$[npc].quests.$[].status": "active"}),
            array_filters=[{"npc.npc_id": npc_id}]
        )
    )

    background_tasks.add_task(_update_document, update_query)

    for quest in quests:
        linked_npc_id = quest.get("linked_npc")
        if linked_npc_id:
            msg = QUEST_PROMPT_TEMPLATE.format(
                quest_id=quest.get("quest_id"),
                quest_name=quest.get("name"),
                quest_description=quest.get("description"),
                keywords=", ".join(map(repr, quest.get("triggers", []))),
            )
            await npc_chat_service.get_response(
                message=get_message(
                    role="system",
                    content=msg
                ),
                npc_id=linked_npc_id,
                shadow_save_id=payload.shadow_save_id,
            ),

            update_query = (
                ShadowSave
                .find(ShadowSave.id == payload.shadow_save_id)
                .update(
                    Push({"npcs.$[npc].chat_history": ("system", msg)}),
                    array_filters=[{"npc.npc_id": linked_npc_id}],
                )
            )
            background_tasks.add_task(_update_document, update_query)

    res = await master_llm_service.get_quest_responses(
        quests,
        shadow_save_id=payload.shadow_save_id,
        npc_id=npc_id,
    )

    return TypeAdapter(NPCQuestResponses).validate_python({
        "quests": res
    })


@router.post(
    "/{npc_id}/quest/complete",
    summary="Complete an NPC quest",
    description="Completes an NPC quest. The quest with the given ID will be marked as completed. "
                "Returns the new affinitas value if the quest is completed successfully. "
                "The `X-Client-UUID` header must be provided.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("10/minute")
async def complete_quest(
        request: Request,
        npc_id: PydanticObjectId,
        payload: NPCQuestCompleteRequest,
        x_client_uuid: XClientUUIDHeader,
):
    quest_id = payload.quest_id
    shadow_save_id = payload.shadow_save_id

    sys_msg = f"Quest with ID `{quest_id!r}` completed."

    affinitas_reward = await (
        NPC
        .get_motor_collection()
        .find_one({"_id": npc_id, "quests._id": quest_id}, {"quests.$": 1, "_id": 0})
    )

    if not affinitas_reward:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Quest not found")

    affinitas_reward = affinitas_reward["quests"][0]["affinitas_reward"]

    res: ShadowSave = await (
        ShadowSave
        .find_one(ShadowSave.id == shadow_save_id)
        .update_one(
            Inc({"npcs.$[npc].affinitas": affinitas_reward}),
            Set({"npcs.$[npc].quests.$[quest].status": "completed"}),
            Push({"npcs.$[npc].chat_history": {"$each": [("system", sys_msg)]}}),
            array_filters=[
                {"npc.npc_id": npc_id},
                {"quest.quest_id": quest_id},
                {"quest.status": "active"}
            ],
            response_type=UpdateResponse.NEW_DOCUMENT
        )
    )

    if not res:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Active quest not found")

    await npc_chat_service.get_response(
        message=get_message("system", sys_msg),
        npc_id=npc_id,
        shadow_save_id=shadow_save_id,
    )

    npc = next(npc for npc in res.npcs if npc.npc_id == npc_id)
    return NPCQuestCompleteResponse(affinitas=npc.affinitas)


async def _update_document(update_query: UpdateMany):
    try:
        await update_query
    except Exception as e:
        logging.error(f"Background update failed: {e}")


QUEST_PROMPT_TEMPLATE = """\
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
