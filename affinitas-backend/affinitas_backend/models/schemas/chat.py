from typing import Literal

from pydantic import BaseModel


class ChatRequest(BaseModel):
    query: str
    respond: bool
    role: Literal["user", "system"]


class MasterLLMChatRequest(BaseModel):
    pass


class NPCChatRequest(ChatRequest):
    npc_id: str




