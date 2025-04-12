from pydantic import BaseModel, Field

from affinitas_backend.models.game_data import BaseNPC


class NPCResponse(BaseNPC):
    id: str = Field(..., alias="_id")


class NPCChatResponse(BaseModel):
    npc_name: str
    affinitas_change: int
    response: str
