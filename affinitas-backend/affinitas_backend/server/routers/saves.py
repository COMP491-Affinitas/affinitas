import logging

from fastapi import HTTPException
from fastapi.requests import Request
from fastapi.routing import APIRouter
from pymongo.errors import DuplicateKeyError
from starlette import status

from affinitas_backend.config import Config
from affinitas_backend.db.utils import get_aggregate_pipeline
from affinitas_backend.models.beanie.save import Save, ShadowSave
from affinitas_backend.models.schemas.game import GameSavesResponse, GameLoadResponse, GameLoadRequest, \
    GameSaveResponse, \
    GameDataResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.utils import throw_500

router = APIRouter(prefix="/saves", tags=["saves"])

config = Config()  # noqa


@router.get(
    "/",
    response_model=GameSavesResponse,
    summary="Lists all game saves",
    description="Returns a list of all game saves for the client. "
                "The response includes the save ID, name, and saved date. "
                "The `X-Client-UUID` header must be provided.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("3/minute")
async def list_game_saves(request: Request, x_client_uuid: XClientUUIDHeader):
    saves = await Save.aggregate([
        {"$match": {"client_uuid": x_client_uuid}},
        {"$sort": {"saved_at": -1}},
        {"$project": {
            "_id": 1,
            "name": 1,
            "saved_at": 1,
        }},
        {"$set": {"save_id": "$_id"}},
        {"$unset": ["_id"]},
    ]).to_list()

    return GameSavesResponse(saves=[
        GameSaveResponse.model_validate(save) for save in saves
    ])


@router.post(
    "/",
    response_model=GameLoadResponse,
    summary="Loads a game save",
    description="Loads a game save by ID. "
                "The `X-Client-UUID` header must be provided. If a game save "
                "with the given ID belonging to the client is not found, a 404 "
                "status is returned. A shadow save entry matching the returned data "
                "is created and returned.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("3/minute")
async def load_game_save(request: Request, payload: GameLoadRequest, x_client_uuid: XClientUUIDHeader):
    save = await Save.aggregate(
        get_aggregate_pipeline({"_id": payload.save_id})
    ).to_list(1)

    if not save:
        logging.info(f"Save with ID {payload.save_id} not found")
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Save not found. Save ID: {payload.save_id}"
        )

    save = save[0]

    shadow_save = ShadowSave(
        **save
    )
    try:
        res = await shadow_save.insert()  # noqa
    except DuplicateKeyError:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail=f"A game save for this client already exists.",
        )

    if res is None:
        throw_500(
            "Failed to load game: Failed to insert shadow save",
            f"Save ID: {payload.save_id}",
            f"Save data: {save}",
        )

    try:
        save.pop("client_uuid", None)
        save.pop("chat_id", None)
        save.pop("saved_at", None)

        for npc in save["npcs"]:
            npc.pop("likes", None)
            npc.pop("dislikes", None)
            npc.pop("occupation", None)

        return GameLoadResponse(
            data=GameDataResponse(**save),
            shadow_save_id=res.id,
        )
    except Exception:
        await res.delete()
        raise
