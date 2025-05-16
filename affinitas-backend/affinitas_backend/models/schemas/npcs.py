from typing import Literal

from beanie import PydanticObjectId
from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import QuestSaveData


class QuestSaveDataResponse(QuestSaveData):
    name: str
    description: str
    rewards: list[str] = Field(default_factory=list)


class NPCResponse(BaseModel):
    npc_id: PydanticObjectId
    name: str
    affinitas: int
    quests: list[QuestSaveDataResponse]
    chat_history: list[tuple[Literal["user", "system", "ai"], str]] = Field(default_factory=list)


class NPCQuestRequest(BaseModel):
    shadow_save_id: PydanticObjectId


class NPCQuestResponse(BaseModel):
    quest_id: PydanticObjectId
    response: str


class NPCQuestResponses(BaseModel):
    quests: list[NPCQuestResponse] = Field(default_factory=list)


class NPCQuest(BaseModel):
    quest_id: PydanticObjectId
    description: str
    linked_npc: PydanticObjectId | None = None
