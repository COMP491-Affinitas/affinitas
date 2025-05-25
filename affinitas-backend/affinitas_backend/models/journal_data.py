from typing import Literal

from beanie import PydanticObjectId
from pydantic import BaseModel


class JournalNPCEntry(BaseModel):
    npc_id: PydanticObjectId
    description: str | None


class JournalQuestEntry(BaseModel):
    quest_id: PydanticObjectId
    status: str
    name: str


class JournalQuestGroup(BaseModel):
    npc_id: PydanticObjectId
    quests: list[JournalQuestEntry]


class JournalChatHistoryEntry(BaseModel):
    npc_id: PydanticObjectId
    chat_history: list[tuple[Literal["user", "ai"], str]]


class TownInfoEntry(BaseModel):
    description: str
    active: bool


class Journal(BaseModel):
    quests: list[JournalQuestGroup]
    npcs: list[JournalNPCEntry]
    town_info: TownInfoEntry
    chat_history: list[JournalChatHistoryEntry]
