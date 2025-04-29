from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import NPCSaveData


class NPCResponse(BaseModel):
    npc: NPCSaveData


class NPCsResponse(BaseModel):
    npcs: list[NPCSaveData] = Field(default_factory=list)
