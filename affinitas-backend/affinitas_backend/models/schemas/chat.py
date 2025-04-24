from typing import Literal

from pydantic import BaseModel

from affinitas_backend.models.game_data import NPCSaveData


class NPCUserChatRequest(BaseModel):
    role: Literal["user"]
    content: str
    npc: NPCSaveData


class NPCSystemChatRequest(BaseModel):
    role: Literal["system"]
    content: str


class NPCChatResponse(BaseModel):
    response: str
    updated_npc: NPCSaveData
