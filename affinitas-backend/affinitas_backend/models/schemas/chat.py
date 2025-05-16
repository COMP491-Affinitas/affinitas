from typing import Literal

from beanie import PydanticObjectId
from pydantic import BaseModel, Field


class NPCChatRequest(BaseModel):
    role: Literal["user", "system"]
    content: str
    shadow_save_id: PydanticObjectId


class NPCChatResponse(BaseModel):
    response: str
    affinitas_new: int
    completed_quests: list[PydanticObjectId] = Field(default_factory=list)
