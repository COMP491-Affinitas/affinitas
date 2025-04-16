import logging

from beanie import PydanticObjectId
from fastapi import HTTPException
from fastapi.requests import Request
from fastapi.routing import APIRouter
from starlette import status

from affinitas_backend.models.beanie.save import Save
from affinitas_backend.models.schemas.game import GameSavesResponse, GameLoadResponse, GameLoadRequest, GameSaveRequest, \
    GameSaveResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.uuid import validate_uuid

router = APIRouter(prefix="/game", tags=["game"])


@router.get(
    "/load",
    response_model=GameSavesResponse,
    summary="Lists all game saves",
    description="Returns a list of all game saves for the client. "
                "The response includes the save ID, name, and saved date. "
                "The `X-Client-UUID` header must be provided.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("3/minute")
async def list_game_saves(request: Request, x_client_uuid: XClientUUIDHeader):
    validate_uuid(x_client_uuid)

    saves = (
        await Save
            .find(Save.client_uuid == x_client_uuid)
            .sort(-Save.saved_at)  # noqa
            .project(GameSaveResponse)
            .to_list()
    )

    return GameSavesResponse(saves=saves)


@router.post(
    "/load",
    response_model=GameLoadResponse,
    summary="Loads a game save",
    description="Loads a game save by ID. "
                "The `X-Client-UUID` header must be provided. "
                "If a game save with the given ID belonging to the client is not found, "
                "a 404 status is returned.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("3/minute")
async def load_game(request: Request, payload: GameLoadRequest, x_client_uuid: XClientUUIDHeader):
    validate_uuid(x_client_uuid)

    save_id = payload.id

    save = (
        await Save
            .find(Save.client_uuid == x_client_uuid)
            .find(Save.id == PydanticObjectId(save_id))
            .project(GameLoadResponse)
            .first_or_none()
    )

    if not save:
        logging.info(f"Save with ID {save_id} not found")
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Save not found. Save ID: {save_id}")

    return save


@router.post(
    "/save",
    response_model=GameSaveResponse,
    summary="Saves a game to the database",
    description="Saves a game to the database and returns the save id, name and the save date. "
                "The `X-Client-UUID` header must be provided.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("10/minute")
async def save_game(request: Request, payload: GameSaveRequest, x_client_uuid: XClientUUIDHeader):
    validate_uuid(x_client_uuid)

    save = Save(
        name=payload.name,
        saved_at=payload.saved_at,
        client_uuid=x_client_uuid,
        day_no=payload.day_no,
        remaining_ap=payload.remaining_ap,
        journal_data=payload.journal_data,
        item_list=payload.item_list,
        npcs=payload.npcs,
    )

    save_res = await save.insert()  # noqa: ignore[no-untyped-call]: A bug with the linter. No issue with the code.

    if save_res is None:
        logging.error("Failed to save game")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Failed to save game")

    return GameSaveResponse(
        id=save_res.id,
        name=save_res.name,
        saved_at=save_res.saved_at,
    )


@router.post(
    "/chat",
    # response_model=None,
    # summary=None,
    # description=None,
    status_code=status.HTTP_200_OK,
)
@limiter.limit("20/minute")
async def master_chat(request: Request, payload, x_client_uuid: XClientUUIDHeader):
    validate_uuid(x_client_uuid)

    raise HTTPException(status_code=status.HTTP_501_NOT_IMPLEMENTED, detail="Not implemented")
