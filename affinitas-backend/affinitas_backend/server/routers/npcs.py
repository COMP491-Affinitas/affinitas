import logging

from fastapi import HTTPException
from fastapi.requests import Request
from fastapi.routing import APIRouter
from starlette import status
from starlette.status import HTTP_200_OK

from affinitas_backend.models.beanie.npc import NPC
from affinitas_backend.models.schemas.npcs import NPCResponse
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.uuid import validate_uuid

router = APIRouter(prefix="/npcs", tags=["npcs"])


@router.get(
    "/",
    response_model=list[NPCResponse],
    summary="Returns all NPCs",
    description="Returns all NPCs in the database. This is a read-only endpoint and does not modify any data. "
                "Needs a valid UUID in the `X-Client-UUID` header.",
    status_code=HTTP_200_OK,
)
@limiter.limit("3/minute")
async def get_npcs(request: Request):
    validate_uuid(request.headers.get("X-Client-UUID"))


    npcs = await NPC.find().project(NPCResponse).to_list()

    return npcs


@router.get(
    "/{npc_id}",
    response_model=NPCResponse,
    summary="Returns an NPC by ID",
    description="Returns an NPC by ID. This is a read-only endpoint and does not modify any data. "
                "Needs a valid UUID in the `X-Client-UUID` header.",
    status_code=HTTP_200_OK,
)
@limiter.limit("18/minute")  # Would be better if the limit was 3 * #NPCs / minute
async def get_npc_by_id(request: Request, npc_id: str):
    validate_uuid(request.headers.get("X-Client-UUID"))

    npc = await NPC.find_one(id == npc_id).project(NPCResponse)

    if not npc:
        logging.info(f"NPC with ID {npc_id} not found")
        raise HTTPException(status_code=404, detail=f"NPC not found. NPC ID: {npc_id}")

    return npc


# Chat with the LLM
@router.post(
    "/{npc_id}/chat",
    # response_model=None,
    # summary=None,
    # description=None,
    status_code=HTTP_200_OK,
)
@limiter.limit("10/minute")
async def npc_chat(request: Request, npc_id: str, payload):
    client_uuid = request.headers.get("X-Client-UUID")
    validate_uuid(client_uuid)

    raise HTTPException(status_code=status.HTTP_501_NOT_IMPLEMENTED, detail="Not implemented yet")
