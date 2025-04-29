from datetime import datetime, UTC
from functools import partial

from beanie import PydanticObjectId
from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import GameData, QuestSaveData, NPCSaveData


class GameSaveRequest(BaseModel):
    name: str
    saved_at: datetime = Field(default_factory=partial(datetime.now, tz=UTC))
    shadow_save_id: PydanticObjectId


class QuestSaveDataResponse(QuestSaveData):
    name: str
    description: str
    rewards: list[str] = Field(default_factory=list)


class NPCSaveDataResponse(NPCSaveData):
    quests: list[QuestSaveDataResponse]


class GameDataResponse(GameData):
    npcs: list[NPCSaveDataResponse]


class GameLoadResponse(BaseModel):
    data: GameDataResponse
    shadow_save_id: PydanticObjectId


class GameLoadRequest(BaseModel):
    save_id: PydanticObjectId


class GameQuitRequest(BaseModel):
    save_id: PydanticObjectId


class GameSaveResponse(BaseModel):
    id: PydanticObjectId
    name: str
    saved_at: datetime


class GameSavesResponse(BaseModel):
    saves: list[GameSaveResponse] = Field(default_factory=list)
