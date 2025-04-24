from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import BaseNPC


class NPCResponse(BaseModel):
    npc: BaseNPC


class NPCsResponse(BaseModel):
    npcs: list[BaseNPC] = Field(default_factory=list)


class NPCChatResponse(BaseModel):
    npc_name: str
    affinitas_change: int
    response: str
