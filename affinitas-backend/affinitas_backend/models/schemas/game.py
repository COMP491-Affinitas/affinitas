from datetime import datetime, UTC
from functools import partial

from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import GameData


class GameSaveRequest(GameData):
    name: str
    saved_at: datetime = Field(default_factory=partial(datetime.now, tz=UTC))


class GameLoadResponse(GameData):
    pass


class GameLoadRequest(BaseModel):
    id: str


class GameSaveResponse(BaseModel):
    id: str
    name: str
    saved_at: datetime


class GameSavesResponse(GameData):
    saves: list[GameSaveResponse] = Field(default_factory=list)
