import datetime
import logging
import os
import uuid
from typing import Any

from beanie import PydanticObjectId
from fastapi import HTTPException
from fastapi.requests import Request
from fastapi.routing import APIRouter
from starlette import status

from affinitas_backend.models.beanie.save import Save, ShadowSave, DefaultSave
from affinitas_backend.models.schemas.game import GameSavesResponse, GameLoadResponse, GameLoadRequest, GameSaveRequest, \
    GameSaveResponse, GameQuitRequest, GameDataResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter

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
                "The `X-Client-UUID` header must be provided. If a game save "
                "with the given ID belonging to the client is not found, a 404 "
                "status is returned. A shadow save entry matching the returned data "
                "is created and returned.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("3/minute")
async def load_game(request: Request, payload: GameLoadRequest, x_client_uuid: XClientUUIDHeader):
    save = (
        await Save
        .find(Save.client_uuid == x_client_uuid)
        .find(Save.id == PydanticObjectId(payload.save_id))
        .first_or_none()
    )

    if not save:
        logging.info(f"Save with ID {payload.save_id} not found")
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Save not found. Save ID: {payload.save_id}")

    shadow_save = ShadowSave(
        **save.model_dump(exclude={"_id", "saved_at", "name"}),
    )

    res = await shadow_save.insert()  # noqa

    if res is None:
        logging.error("Failed to load game: Failed to insert shadow save")
        logging.error(f"Save ID: {payload.save_id}")
        logging.error(f"Save data: {save}")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                            detail="Failed to load game: Failed to insert shadow save")

    return GameLoadResponse(
        data=GameDataResponse(**res.model_dump(exclude={"_id", "client_uuid", "chat_id"})),
        shadow_save_id=res.id,
    )


@router.post(
    "/save",
    response_model=GameSaveResponse,
    summary="Saves a game to the database",
    description="Saves a game to the database and returns the save id, name and the save date. "
                "The `X-Client-UUID` header must be provided. Save data is taken from the shadow "
                "save entry. If a shadow save entry with the given ID is not found, a 404 status "
                "is returned. The shadow save entry is not deleted after saving and must be deleted "
                "afterwards if necessary.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("10/minute")
async def save_game(request: Request, payload: GameSaveRequest, x_client_uuid: XClientUUIDHeader):
    shadow_save = (
        await ShadowSave
        .find(ShadowSave.client_uuid == x_client_uuid)
        .find(ShadowSave.id == payload.shadow_save_id)
        .first_or_none()
    )

    if not shadow_save:
        logging.info(f"Shadow save with ID {payload.shadow_save_id} not found")
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND,
                            detail=f"Shadow save not found. shadow_save_id: {payload.shadow_save_id}")

    save = Save(
        name=payload.name,
        saved_at=datetime.datetime.now(datetime.UTC),
        **shadow_save.model_dump(exclude={"_id"}),
    )

    save_res = await save.insert()  # noqa: A bug with the linter. No issue with the code.

    if save_res is None:
        logging.error("Failed to save game")
        logging.error(f"Shadow save ID: {payload.shadow_save_id}")
        logging.error(f"Save data: {save}")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Failed to save game")

    return GameSaveResponse(
        id=save_res.id,
        name=save_res.name,
        saved_at=save_res.saved_at,
    )


@router.post(
    "/quit",
    response_model=None,
    summary="Used to quit a game",
    description="Deletes the shadow save entry to remove redundant data before quitting. If the "
                "shadow save entry with the given ID is not found, a 404 status is returned. The "
                "`X-Client-UUID` header must be provided. No data is returned.",
    status_code=status.HTTP_204_NO_CONTENT,
)
@limiter.limit("3/minute")
async def quit_game(request: Request, payload: GameQuitRequest, x_client_uuid: XClientUUIDHeader):
    shadow_save = await ShadowSave.get(payload.save_id)
    if not shadow_save:
        logging.info(f"Shadow save with ID {payload.save_id} not found")
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND,
                            detail=f"Shadow save not found. shadow_save_id: {payload.save_id}")

    await shadow_save.delete()  # noqa


@router.get(
    "/new",
    response_model=GameLoadResponse,
    summary="Creates a new game",
    description="Creates a new game and returns the shadow save entry. "
                "The `X-Client-UUID` header must be provided. The shadow save entry "
                "is created with default values.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("3/minute")
async def new_game(request: Request, x_client_uuid: XClientUUIDHeader):
    # 4 AM coding. Don't ask anything about this.

    save = (await DefaultSave.aggregate([
        {"$match": {
            "_id": int(os.getenv("DEFAULT_SAVE_VERSION"))
        }},
        {"$lookup": {
            "from": "npcs",
            "localField": "npcs.npc_id",
            "foreignField": "_id",
            "as": "npc_configs"
        }}
    ]).to_list(1))[0]

    npc_configs = save.pop("npc_configs")
    del save["_id"]

    if not save:
        logging.error("Failed to create new game: Default save not found")
        logging.error("Default save version no: %s", os.getenv("DEFAULT_SAVE_VERSION"))
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                            detail="Failed to create new game: Default save not found")

    shadow_save = ShadowSave(
        client_uuid=x_client_uuid,
        chat_id=uuid.uuid4(),
        **save,
    )

    res = await shadow_save.insert()  # noqa
    if res is None:
        logging.error("Failed to create new game: Failed to insert shadow save")
        logging.error(f"Save data: {save}")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                            detail="Failed to create new game: Failed to insert shadow save")

    dump = res.model_dump(exclude={"id", "client_uuid", "chat_id"})

    merge_quest_config(dump["npcs"], npc_configs)

    return GameLoadResponse(
        data=GameDataResponse(**dump),
        shadow_save_id=res.id,
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
    raise HTTPException(status_code=status.HTTP_501_NOT_IMPLEMENTED, detail="Not implemented")


def merge_quest_config(npcs: list[dict[str, Any]], npc_configs: list[dict[str, Any]]):
    for npc_save_data in npcs:
        npc_config = next((npc for npc in npc_configs if npc["_id"] == npc_save_data["npc_id"]), None)
        if not npc_config:
            logging.error("Failed to create new game: NPC config not found")
            logging.error(f"NPC ID: {npc_save_data["npc_id"]}")
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                                detail="Failed to create new game: NPC config not found")

        for i, quest in enumerate(npc_save_data["quests"]):
            quest_config = next(
                (quest_config for quest_config in npc_config["quests"] if quest_config["_id"] == quest["quest_id"]),
                None)
            if not quest_config:
                logging.error("Failed to create new game: Quest config not found")
                logging.error(f"Quest ID: {quest["quest_id"]}")
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                                    detail="Failed to create new game: Quest config not found")

            npc_save_data["quests"][i] = {
                **quest,
                "name": quest_config["name"],
                "description": quest_config["description"],
                "rewards": quest_config["rewards"],
            }
