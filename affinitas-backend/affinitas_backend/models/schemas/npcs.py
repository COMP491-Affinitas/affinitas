from beanie import PydanticObjectId
from langchain_core.messages import MessageLikeRepresentation
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
    chat_history: list[MessageLikeRepresentation] = Field(default_factory=list)
