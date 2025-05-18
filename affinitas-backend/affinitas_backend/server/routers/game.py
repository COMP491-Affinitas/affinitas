from fastapi import status
from fastapi.requests import Request
from fastapi.routing import APIRouter

from affinitas_backend.models.schemas.game import GameSavesResponse, GameSessionResponse, LoadGameRequest, \
    SaveSessionRequest, \
    GameSaveSummary, DeleteSessionRequest, GameEndingResponse, GenerateGameEndingRequest
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.routers.saves import list_game_saves, load_game_save
from affinitas_backend.server.routers.session import quit_game, save_game, new_game, generate_ending

router = APIRouter(prefix="/game", tags=["game"])


@router.get(
    "/load",
    deprecated=True,
    response_model=GameSavesResponse,
    summary="Deprecated -- Lists all game saves",
    description="Deprecated. Use `GET /saves/` instead.",
    status_code=status.HTTP_200_OK,
)
@limiter.limit("3/minute")
async def _list_game_saves(request: Request, x_client_uuid: XClientUUIDHeader):
    return await list_game_saves(request, x_client_uuid)

@router.post(
    "/load",
    deprecated=True,
    response_model=GameSessionResponse,
    summary="Deprecated -- Loads a game save",
    description="Deprecated. Use `POST /saves/` instead.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("3/minute")
async def _load_game(request: Request, payload: LoadGameRequest, x_client_uuid: XClientUUIDHeader):
    return await load_game_save(request, payload, x_client_uuid)


@router.post(
    "/save",
    deprecated=True,
    response_model=GameSaveSummary,
    summary="Deprecated -- Saves a game to the database",
    description="Deprecated. Use `POST /session/save` instead.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("10/minute")
async def _save_game(request: Request, payload: SaveSessionRequest, x_client_uuid: XClientUUIDHeader):
    return await save_game(request, payload, x_client_uuid)


@router.post(
    "/quit",
    deprecated=True,
    response_model=None,
    summary="Deprecated -- Used to quit a game",
    description="Deprecated. Use `DELETE /session/` instead.",
    status_code=status.HTTP_204_NO_CONTENT,
)
@limiter.limit("3/minute")
async def _quit_game(request: Request, payload: DeleteSessionRequest, x_client_uuid: XClientUUIDHeader):
    return await quit_game(request, payload, x_client_uuid)


@router.get(
    "/new",
    deprecated=True,
    response_model=GameSessionResponse,
    summary="Deprecated -- Creates a new game",
    description="Deprecated. Use `GET /session/new` instead.",
    status_code=status.HTTP_201_CREATED,
)
@limiter.limit("3/minute")
async def _new_game(request: Request, x_client_uuid: XClientUUIDHeader):
    return await new_game(request, x_client_uuid)


@router.post(
    "/end",
    deprecated=True,
    response_model=GameEndingResponse,
    summary="Deprecated -- Generates a game ending.",
    description="Deprecated. Use `POST /session/generate-ending` instead.",
    status_code=status.HTTP_200_OK,
)
async def _end_game(request: Request, payload: GenerateGameEndingRequest, x_client_uuid: XClientUUIDHeader):
    return await generate_ending(request, payload, x_client_uuid)
