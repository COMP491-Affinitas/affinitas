import datetime
import logging
import uuid
from typing import Any

from fastapi import HTTPException
from fastapi.requests import Request
from fastapi.routing import APIRouter
from pymongo.errors import DuplicateKeyError
from starlette import status

from affinitas_backend.config import Config
from affinitas_backend.models.beanie.save import Save, ShadowSave, DefaultSave
from affinitas_backend.models.schemas.game import GameSavesResponse, GameLoadResponse, GameLoadRequest, GameSaveRequest, \
    GameSaveResponse, GameQuitRequest, GameDataResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter

router = APIRouter(prefix="/game", tags=["game"])

config = Config()  # noqa

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
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to save game"
        )

    return GameSaveResponse(
        save_id=save_res.id,
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
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Shadow save not found. shadow_save_id: {payload.save_id}"
        )

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
    save = await DefaultSave.aggregate(
        get_aggregate_pipeline({"_id": config.default_save_version})
    ).to_list(1)

    if not save:
        throw_500(
            "Failed to create new game: Default save not found",
            f"Default save version no: {config.default_save_version}",
        )

    save = save[0]

    shadow_save = ShadowSave(
        client_uuid=x_client_uuid,
        chat_id=uuid.uuid4(),
        **save,
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
            "Failed to create new game: Failed to insert shadow save",
            f"Save data: {save}",
        )

    try:
        for npc in save["npcs"]:
            npc.pop("likes", None)  # Not needed in the response data
            npc.pop("dislikes", None)
            npc.pop("occupation", None)

        return GameLoadResponse(
            data=GameDataResponse(**save),
            shadow_save_id=res.id,
        )
    except Exception:
        await res.delete()  # Delete the shadow save if the merge fails since we don't want to keep it now
        raise


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


def throw_500(detail: str, *msgs: str):
    logging.error(detail)
    for msg in msgs:
        logging.error(msg)

    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=detail)


def get_aggregate_pipeline(match: dict[str, Any], ):
    """
    Returns the aggregation pipeline for the game save data.
    The pipeline is used to transform the save data stored in ShadowSave, DefaultSave, and Save documents.
    The dynamic npc data (likes, dislikes, and occupation) is embedded into the npc data, and the static quest data
    stored in npcs.quests is embedded into the quest save data.
    :param match: Match stage to filter the save data. The match stage is used to filter the save data by ID.
    :return: The aggregation pipeline for fetching save data.
    """
    return [
        {"$match": match},
        {"$lookup": {
            "from": "npcs",
            "localField": "npcs.npc_id",
            "foreignField": "_id",
            "as": "npc_configs"
        }},
        {"$set": {
            "npcs": {
                "$map": {
                    "input": "$npcs",
                    "as": "npc_save",
                    "in": {
                        "$let": {
                            "vars": {
                                "npc_config": {
                                    "$arrayElemAt": [
                                        {
                                            "$filter": {
                                                "input": "$npc_configs",
                                                "as": "cfg",
                                                "cond": {
                                                    "$eq": ["$$cfg._id", "$$npc_save.npc_id"]
                                                }
                                            }
                                        },
                                        0
                                    ]
                                }
                            },
                            "in": {
                                "$mergeObjects": [
                                    "$$npc_save",
                                    {
                                        "affinitas": "$$npc_config.affinitas",
                                        "likes": "$$npc_config.likes",
                                        "dislikes": "$$npc_config.dislikes",
                                        "occupation": "$$npc_config.occupation",
                                        "quests": {
                                            "$map": {
                                                "input": "$$npc_save.quests",
                                                "as": "quest_save",
                                                "in": {
                                                    "$let": {
                                                        "vars": {
                                                            "quest_config": {
                                                                "$arrayElemAt": [
                                                                    {
                                                                        "$filter": {
                                                                            "input": "$$npc_config.quests",
                                                                            "as": "qcfg",
                                                                            "cond": {
                                                                                "$eq": ["$$qcfg._id",
                                                                                        "$$quest_save.quest_id"]
                                                                            }
                                                                        }
                                                                    },
                                                                    0
                                                                ]
                                                            }
                                                        },
                                                        "in": {
                                                            "$mergeObjects": [
                                                                "$$quest_save",
                                                                {
                                                                    "name": "$$quest_config.name",
                                                                    "description": "$$quest_config.description",
                                                                    "rewards": "$$quest_config.rewards"
                                                                }
                                                            ]
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                ]
                            }
                        }
                    }
                }
            }
        }},
        {"$unset": ["npc_configs", "_id"]}
    ]
