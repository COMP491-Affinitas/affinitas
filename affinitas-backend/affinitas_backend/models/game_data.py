from typing import Literal

from beanie import PydanticObjectId
from pydantic import BaseModel, Field

from affinitas_backend.models.journal_data import Journal


class Item(BaseModel):
    name: str
    active: bool = False


class QuestSaveData(BaseModel):
    quest_id: PydanticObjectId
    status: str


class NPCSaveData(BaseModel):
    npc_id: PydanticObjectId
    affinitas: int
    likes: list[str] = Field(default_factory=list)
    dislikes: list[str] = Field(default_factory=list)
    occupation: str | None = None
    quests: list[QuestSaveData] = Field(default_factory=list)
    chat_history: list[tuple[Literal["user", "system", "ai"], str]] = Field(default_factory=list)
    # Quests completed by this NPC; the quest does not have to belong to this NPC
    completed_quests: list[PydanticObjectId] = Field(default_factory=list)


class GameData(BaseModel):
    day_no: int
    remaining_ap: int
    journal_data: Journal
    journal_active: bool
    item_list: list[Item] = Field(default_factory=list)
    npcs: list[NPCSaveData] = Field(default_factory=list)
