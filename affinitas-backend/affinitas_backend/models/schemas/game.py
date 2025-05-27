from datetime import datetime

from beanie import PydanticObjectId
from pydantic import BaseModel, Field

from affinitas_backend.config import Config
from affinitas_backend.models.game_data import GameData
from affinitas_backend.models.schemas.npcs import NPCResponse

config = Config()  # noqa


class SaveSessionRequest(BaseModel):
    name: str
    shadow_save_id: PydanticObjectId


class GameSessionData(GameData):
    npcs: list[NPCResponse]


class GameSessionResponse(BaseModel):
    data: GameSessionData
    shadow_save_id: PydanticObjectId


class SaveIdRequest(BaseModel):
    save_id: PydanticObjectId


class DeleteSessionRequest(SaveIdRequest):
    pass  # TODO: Would be nice if this had shadow_save_id instead of save_id


class GameSaveSummary(BaseModel):
    save_id: PydanticObjectId
    name: str
    saved_at: datetime

    class Settings:
        projection = {"save_id": "$_id", "name": 1, "saved_at": 1, "_id": 0}


class GameSavesResponse(BaseModel):
    saves: list[GameSaveSummary] = Field(default_factory=list)


class ShadowSaveIdRequest(BaseModel):
    shadow_save_id: PydanticObjectId


class GameEndingResponse(BaseModel):
    ending: str


class GiveItemRequest(BaseModel):
    item_name: str
    shadow_save_id: PydanticObjectId
