from datetime import datetime, UTC
from functools import partial

from beanie import PydanticObjectId
from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import GameData


class GameSaveRequest(BaseModel):
    name: str
    saved_at: datetime = Field(default_factory=partial(datetime.now, tz=UTC))
    data: GameData

class GameLoadResponse(BaseModel):
    data: GameData


class GameLoadRequest(BaseModel):
    id: PydanticObjectId


class GameSaveResponse(BaseModel):
    id: PydanticObjectId
    name: str
    saved_at: datetime


class GameSavesResponse(BaseModel):
    saves: list[GameSaveResponse] = Field(default_factory=list)
