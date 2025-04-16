from beanie import PydanticObjectId
from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import BaseNPC


class NPCResponse(BaseNPC):
    id: PydanticObjectId = Field(alias="_id")

class NPCsResponse(BaseModel):
    npcs: list[NPCResponse] = Field(default_factory=list)


class NPCChatResponse(BaseModel):
    npc_name: str
    affinitas_change: int
    response: str
