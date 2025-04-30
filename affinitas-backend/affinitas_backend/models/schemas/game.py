from datetime import datetime

from beanie import PydanticObjectId
from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import GameData
from affinitas_backend.models.schemas.npcs import NPCResponse


class GameSaveRequest(BaseModel):
    name: str
    shadow_save_id: PydanticObjectId


class GameDataResponse(GameData):
    npcs: list[NPCResponse]


class GameLoadResponse(BaseModel):
    data: GameDataResponse
    shadow_save_id: PydanticObjectId


class GameLoadRequest(BaseModel):
    save_id: PydanticObjectId


class GameQuitRequest(BaseModel):
    save_id: PydanticObjectId


class GameSaveResponse(BaseModel):
    save_id: PydanticObjectId = Field(..., alias="_id")
    name: str
    saved_at: datetime


class GameSavesResponse(BaseModel):
    saves: list[GameSaveResponse] = Field(default_factory=list)
