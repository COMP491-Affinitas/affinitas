import datetime
import logging
import uuid
from typing import Annotated

from beanie import PydanticObjectId
from beanie.odm.operators.update.array import Push
from fastapi import HTTPException, APIRouter, Request, status, BackgroundTasks, Query
from pymongo.errors import DuplicateKeyError

from affinitas_backend.chat import master_llm_service
from affinitas_backend.config import Config
from affinitas_backend.db.utils import get_save_pipeline
from affinitas_backend.models.beanie.save import DefaultSave, ShadowSave, Save
from affinitas_backend.models.schemas.game import GameSessionResponse, GameSessionData, GameSaveSummary, \
    SaveSessionRequest, GameEndingResponse, ShadowSaveIdRequest, GiveItemRequest, SetAPRequest
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.utils import throw_500

router = APIRouter(prefix="/session", tags=["session"])

config = Config()  # noqa


@router.get(
    "/new",
    response_model=GameSessionResponse,
    summary="Creates a new game",
    description="Creates a new game and returns the shadow save entry. "
                "The `X-Client-UUID` header must be provided. The shadow save entry "
                "is created with default values.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("10/minute")
async def new_game(request: Request, x_client_uuid: XClientUUIDHeader):
    save = await DefaultSave.aggregate(
        get_save_pipeline({"_id": config.default_save_version})
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

        return GameSessionResponse(
            data=GameSessionData(**save),
            shadow_save_id=res.id,
        )
    except Exception:
        await res.delete()
        raise


@router.post(
    "/item",
    response_model=None,
    summary="Gives an item to the player.",
    description="Gives an item to the player. The `X-Client-UUID` header must be provided. ",
    status_code=status.HTTP_204_NO_CONTENT,
)
@limiter.limit("10/minute")
async def give_item(request: Request, payload: GiveItemRequest, x_client_uuid: XClientUUIDHeader,
                    background_tasks: BackgroundTasks):
    shadow_save_id = payload.shadow_save_id
    item_name = payload.item_name

    update_res = await (
        ShadowSave
        .find(ShadowSave.id == shadow_save_id)
        .find(ShadowSave.client_uuid == x_client_uuid)
        .update(
            Push({"item_list": item_name}),
        )
    )

    if update_res.modified_count == 0:
        raise HTTPException(
            detail=f"Shadow save not found. shadow_save_id: {shadow_save_id}",
            status_code=status.HTTP_404_NOT_FOUND,
        )


@router.post(
    "/save",
    response_model=GameSaveSummary,
    summary="Saves a game to the database",
    description="Saves a game to the database and returns the save id, name and the save date. "
                "The `X-Client-UUID` header must be provided. Save data is taken from the shadow "
                "save entry. If a shadow save entry with the given ID is not found, a 404 status "
                "is returned. The shadow save entry is not deleted after saving and must be deleted "
                "afterwards if necessary.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("10/minute")
async def save_game(request: Request, payload: SaveSessionRequest, x_client_uuid: XClientUUIDHeader):
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

    return GameSaveSummary(
        save_id=save_res.id,
        name=save_res.name,
        saved_at=save_res.saved_at,
    )


@router.delete(
    "",
    response_model=None,
    summary="Used to quit a game",
    description="Deletes the shadow save entry to remove redundant data before quitting. If the "
                "shadow save entry with the given ID is not found, a 404 status is returned. The "
                "`X-Client-UUID` header must be provided. No data is returned.",
    status_code=status.HTTP_204_NO_CONTENT,
)
@limiter.limit("10/minute")
async def quit_game(request: Request, shadow_save_id: Annotated[PydanticObjectId, Query(alias="id")],
                    x_client_uuid: XClientUUIDHeader):
    shadow_save = await ShadowSave.get(shadow_save_id)
    if not shadow_save:
        logging.info(f"Shadow save with ID {shadow_save_id} not found")
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Shadow save not found. shadow_save_id: {shadow_save_id}"
        )

    await shadow_save.delete()  # noqa


@router.post(
    "/generate-ending",
    response_model=GameEndingResponse,
    summary="Generates a game ending.",
    description="Generates a game ending from all the NPC info. "
                "The `X-Client-UUID` header must be provided. The shadow save entry "
                "is not deleted after generating the ending is returned. Thus, /game/quit "
                "must be called to delete the shadow save entry.",
    status_code=status.HTTP_200_OK,
)
async def generate_ending(request: Request, payload: ShadowSaveIdRequest, x_client_uuid: XClientUUIDHeader):
    npc_infos = (
        await ShadowSave
        .aggregate(
            get_save_pipeline({"_id": payload.shadow_save_id})
            + [{"$project": {"npcs": 1}}]
        )
        .to_list()
    )

    if not npc_infos:
        logging.info(f"Shadow save with ID {payload.shadow_save_id} not found")
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Shadow save not found. shadow_save_id: {payload.shadow_save_id}"
        )

    npc_infos = npc_infos[0].get("npcs", [])

    res = master_llm_service.generate_ending(npc_infos)

    if res is None:
        throw_500(
            "Failed to generate game ending",
            f"Shadow save ID: {payload.shadow_save_id}",
            f"NPC data: {npc_infos}",
        )

    return GameEndingResponse(ending=res.content)


@router.patch(
    "/action-points",
    response_model=None,
    summary="Sets the action points for the given shadow save.",
    description="Sets the action points for the given shadow save. "
                "The action points are set to the given value without any checks. "
                "The `X-Client-UUID` header must be provided. The shadow save entry ",
    status_code=status.HTTP_204_NO_CONTENT,
)
@limiter.limit("30/minute")
async def set_ap(request: Request, payload: SetAPRequest, x_client_uuid: XClientUUIDHeader):
    shadow_save = await ShadowSave.find_one(
        ShadowSave.id == payload.shadow_save_id,
        ShadowSave.client_uuid == x_client_uuid,
    )

    if not shadow_save:
        logging.info(f"Shadow save with ID {payload.shadow_save_id} not found")
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Shadow save not found. shadow_save_id: {payload.shadow_save_id}"
        )

    await shadow_save.set({ShadowSave.remaining_ap: payload.action_points})
