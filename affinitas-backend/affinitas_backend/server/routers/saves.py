import logging

from beanie import SortDirection, PydanticObjectId
from fastapi import HTTPException, status
from fastapi.requests import Request
from fastapi.routing import APIRouter

from affinitas_backend.config import Config
from affinitas_backend.db.utils import get_save_pipeline
from affinitas_backend.models.beanie.save import Save, ShadowSave
from affinitas_backend.models.schemas.game import GameSavesResponse, GameSessionResponse, SaveIdRequest, \
    GameSessionData, GameSaveSummary
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.utils import throw_500

router = APIRouter(prefix="/saves", tags=["saves"])

config = Config()  # noqa


@router.get(
    "/",
    response_model=GameSavesResponse,
    status_code=status.HTTP_200_OK,
    summary="List all saved games for the client",
    description=(
            "Retrieves all saved game entries associated with the client UUID.\n\n"
            "**Behavior:**\n"
            "- Requires a valid `X-Client-UUID` header.\n"
            "- Returns a chronologically sorted list of saves (most recent first).\n"
            "- Each save includes its unique `save_id`, `name`, and `saved_at` timestamp.\n\n"
            "**Rate Limit:** 10 requests per minute per client."
    ),
    responses={
        status.HTTP_200_OK: {
            "description": "List of saved games for the client.",
            "content": {
                "application/json": {
                    "example": {
                        "saves": [
                            {
                                "save_id": "6650d7f354ec3374bdb3f1a1",
                                "name": "Day 4 - After Market Visit",
                                "saved_at": "2025-05-30T01:20:15.457Z"
                            },
                            {
                                "save_id": "664fbd5ecf473c6f3a5a8d78",
                                "name": "Start of Day 3",
                                "saved_at": "2025-05-29T13:45:02.210Z"
                            }
                        ]
                    }
                }
            }
        }
    }
)
@limiter.limit("10/minute")
async def list_game_saves(
        request: Request,
        x_client_uuid: XClientUUIDHeader
):
    """
    Lists all game saves for the requesting client.

    - Sorted by most recent `saved_at` timestamp.
    - Only includes saves belonging to the client UUID.
    """
    saves = await (
        Save
        .find(Save.client_uuid == x_client_uuid)
        .project(GameSaveSummary)
        .sort(("saved_at", SortDirection.DESCENDING))
        .to_list()
    )

    return GameSavesResponse(saves=saves)


@router.post(
    "/",
    response_model=GameSessionResponse,
    status_code=status.HTTP_201_CREATED,
    summary="Load a game save",
    description=(
            "Loads a game save by ID and starts a new gameplay session.\n\n"
            "**Behavior:**\n"
            "- Requires a valid `X-Client-UUID` header.\n"
            "- Finds the persistent save with the given ID owned by the client.\n"
            "- If the save is not found, returns `404 Not Found`.\n"
            "- If a shadow save already exists, returns `409 Conflict`.\n"
            "- Creates and returns a new shadow save stripped of non-runtime metadata.\n\n"
            "**Rate Limit:** 10 requests per minute per client."
    ),
    responses={
        status.HTTP_201_CREATED: {
            "description": "Game save successfully loaded. Returns the gameplay session and new shadow save ID.",
            "content": {
                "application/json": {
                    "example": {
                        "shadow_save_id": "6651acb12f41d99fc2f91a87",
                        "data": {
                            "day_no": 4,
                            "journal_active": False,
                            "npcs": [
                                {
                                    "npc_id": "664fbd5ecf473c6f3a5a8d78",
                                    "name": "Mora Lysa",
                                    "affinitas": 45,
                                    "quests": [],
                                    "chat_history": []
                                }
                            ]
                        }
                    }
                }
            }
        },
        status.HTTP_404_NOT_FOUND: {
            "description": "No save with the specified ID was found for this client.",
            "content": {
                "application/json": {
                    "example": {
                        "detail": "Save not found. Save ID: 6651ab98ef8b9f5b94a1330c"
                    }
                }
            }
        },
        status.HTTP_409_CONFLICT: {
            "description": "A shadow save already exists for this client and session.",
            "content": {
                "application/json": {
                    "example": {
                        "detail": "A game save for this client already exists."
                    }
                }
            }
        },
        status.HTTP_500_INTERNAL_SERVER_ERROR: {
            "description": "Unexpected server error during shadow save creation."
        }
    }
)
@limiter.limit("10/minute")
async def load_game_save(
        request: Request,
        payload: SaveIdRequest,
        x_client_uuid: XClientUUIDHeader
):
    """
    Loads a saved game by ID and creates a shadow save for gameplay.

    - Returns game data if successful.
    - Raises 404 if save is not found.
    - Raises 409 if a conflicting shadow save exists.
    """
    save = await Save.aggregate(
        get_save_pipeline({"_id": payload.save_id})
    ).to_list(1)

    if not save:
        logging.info(f"Save with ID {payload.save_id} not found")
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Save not found. Save ID: {payload.save_id}"
        )

    save = save[0]
    shadow_save = ShadowSave(**save)

    await ShadowSave.find(ShadowSave.client_uuid == x_client_uuid).delete()

    res = await shadow_save.insert()  # noqa

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

        return GameSessionResponse(
            data=GameSessionData(**save),
            shadow_save_id=res.id,
        )
    except Exception:
        await res.delete()
        raise



@router.delete(
    "/{save_id}",
    summary="Delete a game save",
    description="Deletes a game save by its unique ID.\n\n"
                "**Behavior:**\n"
                "- Requires a valid `X-Client-UUID` header.\n"
                "- If a save matching the given ID and client UUID is found, it is deleted.\n"
                "- If no such save exists, returns `404 Not Found`.\n"
                "- If deletion fails unexpectedly, returns `500 Internal Server Error`.\n\n"
                "**Rate Limit:** 10 requests per minute per client.",
    status_code=status.HTTP_204_NO_CONTENT,
    responses={
        status.HTTP_204_NO_CONTENT: {
            "description": "Game save successfully deleted. No content is returned."
        },
        status.HTTP_404_NOT_FOUND: {
            "description": "No save found with the given ID for the client.",
            "content": {
                "application/json": {
                    "example": {
                        "detail": "Save not found. Save ID: 6651ab98ef8b9f5b94a1330c"
                    }
                }
            }
        },
        status.HTTP_500_INTERNAL_SERVER_ERROR: {
            "description": "Failed to delete the save due to a server error."
        }
    }
)
@limiter.limit("10/minute")
async def delete_game_save(
        request: Request,
        save_id: PydanticObjectId,
        x_client_uuid: XClientUUIDHeader
):
    """
    Deletes a saved game if it exists and belongs to the client.

    - Returns 204 on success.
    - Returns 404 if no such save exists.
    - Returns 500 if deletion fails.
    """
    save = await (
        Save
        .find(Save.id == save_id)
        .find(Save.client_uuid == x_client_uuid)
        .first_or_none()
    )

    if not save:
        logging.info(f"Save with ID {save_id} not found")
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Save not found. Save ID: {save_id}"
        )

    res = await save.delete()  # noqa

    if res.deleted_count != 1:
        throw_500(
            "Failed to delete game save",
            f"Save ID: {save_id}",
        )
