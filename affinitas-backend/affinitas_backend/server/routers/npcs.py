import logging

from beanie import PydanticObjectId
from beanie.odm.operators.update.array import Push
from beanie.odm.operators.update.general import Set
from beanie.odm.queries.update import UpdateMany
from fastapi import Response, HTTPException
from fastapi.background import BackgroundTasks
from fastapi.requests import Request
from fastapi.routing import APIRouter
from starlette import status

from affinitas_backend.chat.chat import NPCChatService
from affinitas_backend.chat.master_chat import MasterLLM
from affinitas_backend.chat.utils import get_message
from affinitas_backend.config import Config
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.schemas.chat import NPCChatRequest, NPCChatResponse
from affinitas_backend.models.schemas.npcs import NPCQuestResponses, NPCQuestRequest
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter

router = APIRouter(prefix="/npcs", tags=["npcs"])

config = Config()

chat_service = NPCChatService(config=config)
master_llm_service = MasterLLM(config=config)

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

    res = await chat_service.get_response(
        message=message,
        npc_id=npc_id,
        shadow_save_id=shadow_save_id,
    )

    if res:
        npc_response, updated_npc_data = res
        update_query = (
            update_query
            .update(
                Set({
                    "npcs.$.affinitas": updated_npc_data["affinitas"],
                    "npcs.$.occupation": updated_npc_data["occupation"],
                    "npcs.$.likes": updated_npc_data["likes"]
                }),
                Push({"npcs.$.chat_history": {"$each": [(payload.role, payload.content), ("ai", npc_response)]}})
            )
        )

        response = NPCChatResponse(
            response=npc_response,
            affinitas_new=updated_npc_data["affinitas"],
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
)
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
            msg = f"If the player asks about this quest, mark the quest with id {quest["quest_id"]!r} completed.\n" \
                  "---\n" \
                  f"The quest is: {quest["description"]!r}"
            await chat_service.get_response(
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

    return NPCQuestResponses(
        quests=res
    )


async def _update_document(update_query: UpdateMany):
    try:
        await update_query
    except Exception as e:
        logging.error(f"Background update failed: {e}")


def get_npc_quests_pipeline(npc_id: PydanticObjectId, shadow_save_id: PydanticObjectId):
    return [
        {"$match": {"_id": shadow_save_id}},
        {"$unwind": "$npcs"},
        {"$match": {"npcs.npc_id": npc_id}},
        {"$lookup": {
            "from": "npcs",
            "localField": "npcs.npc_id",
            "foreignField": "_id",
            "as": "npcDoc",
        }},
        {"$unwind": "$npcDoc"},
        {"$project": {
            "_id": 0,
            "quests": {
                "$map": {
                    "input": "$npcs.quests",
                    "as": "sq",
                    "in": {
                        "quest_id": "$$sq.quest_id",
                        "description": {"$arrayElemAt": [
                            {"$map": {
                                "input": {"$filter": {
                                    "input": "$npcDoc.quests",
                                    "as": "dq",
                                    "cond": {
                                        "$eq": ["$$dq._id", "$$sq.quest_id"]
                                    }
                                }},
                                "as": "match",
                                "in": "$$match.description"
                            }},
                            0
                        ]},
                        "linked_npc": {"$arrayElemAt": [
                            {"$map": {
                                "input": {
                                    "$filter": {
                                        "input": "$npcDoc.quests",
                                        "as": "dq",
                                        "cond": {"$eq": ["$$dq._id", "$$sq.quest_id"]}
                                    }
                                },
                                "as": "match",
                                "in": "$$match.linked_npc"
                            }},
                            0
                        ]}
                    }
                }
            }
        }}
    ]
