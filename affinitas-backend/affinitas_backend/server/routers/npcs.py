import logging

from beanie import PydanticObjectId
from beanie.odm.operators.update.array import Push
from beanie.odm.operators.update.general import Set
from beanie.odm.queries.update import UpdateMany
from fastapi import Response
from fastapi.background import BackgroundTasks
from fastapi.requests import Request
from fastapi.routing import APIRouter
from starlette import status

from affinitas_backend.chat.chat import get_message, NPCChatService
from affinitas_backend.config import Config
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.schemas.chat import NPCChatRequest, NPCChatResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter

router = APIRouter(prefix="/npcs", tags=["npcs"])

chat_service = NPCChatService(config=Config())

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

    background_tasks.add_task(_update_npc, update_query)

    return response


async def _update_npc(update_query: UpdateMany):
    try:
        await update_query
    except Exception as e:
        print("EXCEPTION")
        logging.error(f"Background update failed: {e}")
