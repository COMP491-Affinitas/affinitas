import logging

from beanie import PydanticObjectId
from beanie.odm.operators.update.array import Push
from beanie.odm.operators.update.general import Set
from fastapi import HTTPException, Response
from fastapi.background import BackgroundTasks
from fastapi.requests import Request
from fastapi.routing import APIRouter
from starlette import status

from affinitas_backend.chat.chat import get_message, get_response
from affinitas_backend.models.beanie.npc import NPC
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.schemas.chat import NPCChatRequest, NPCChatResponse
from affinitas_backend.models.schemas.npcs import NPCResponse, NPCsResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.uuid import validate_uuid

router = APIRouter(prefix="/npcs", tags=["npcs"])


@router.get(
    "/",
    response_model=NPCsResponse,
    summary="Returns all NPCs",
    description="Returns all NPCs in the database. This is a read-only endpoint and does not modify any data. "
                "Needs a valid UUID in the `X-Client-UUID` header.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("3/minute")
async def get_npcs(request: Request, x_client_uuid: XClientUUIDHeader):
    validate_uuid(x_client_uuid)

    npcs = await NPC.find().to_list()

    return NPCsResponse(npcs=npcs)


@router.get(
    "/{npc_id}",
    response_model=NPCResponse,
    summary="Returns an NPC by ID",
    description="Returns an NPC by ID. This is a read-only endpoint and does not modify any data. "
                "Needs a valid UUID in the `X-Client-UUID` header.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("18/minute")  # Would be better if the limit was 3 * #NPCs / minute
async def get_npc_by_id(request: Request, npc_id: PydanticObjectId, x_client_uuid: XClientUUIDHeader):
    validate_uuid(x_client_uuid)

    npc = (
        await NPC
        .find(NPC.id == npc_id)
        .first_or_none()
    )

    if not npc:
        logging.info(f"NPC with ID {npc_id} not found")
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"NPC not found. NPC ID: {npc_id}")

    return NPCResponse(npc=npc)


# Chat with the LLM
@router.post(
    "/{npc_id}/chat",
    response_model=None,
    # summary=None,
    # description=None,
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
    validate_uuid(x_client_uuid)

    message = get_message(payload.role, payload.content)
    shadow_save_id = payload.shadow_save_id

    update_query = (
        ShadowSave
        .find(ShadowSave.id == shadow_save_id)
        .find({"npcs.npc_meta._id": npc_id})
    )

    res = await get_response(
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
                    "npcs.$.npc_meta.occupation": updated_npc_data["occupation"],
                    "npcs.$.npc_meta.likes": updated_npc_data["likes"]
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
            headers={"Cache-Control": "no-store, no-cache, must-revalidate, max-age=0"}
        )
        update_query = (
            update_query
            .update(Push({"npcs.$.chat_history": {"$each": [(payload.role, payload.content)]}}))
        )

    background_tasks.add_task(_update_npc, update_query)

    return response


async def _update_npc(update_query):
    try:
        await update_query
    except Exception as e:
        logging.error(f"Background update failed: {e}")
