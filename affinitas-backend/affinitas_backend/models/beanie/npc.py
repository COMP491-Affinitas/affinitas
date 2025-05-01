from beanie import Document, PydanticObjectId
from pydantic import Field

from affinitas_backend.models.game_data import NPCAffinitasConfig, Quest


class NPC(Document):
    id: PydanticObjectId = Field(..., alias="_id")
    name: str
    age: int
    occupation: str | None = None
    personality: list[str] = Field(default_factory=list)
    likes: list[str] = Field(default_factory=list)
    dislikes: list[str] = Field(default_factory=list)
    motivations: list[str] = Field(default_factory=list)
    backstory: str
    affinitas_config: NPCAffinitasConfig
    endings: list[str] = Field(default_factory=list)
    quests: list[Quest] = Field(default_factory=list)
    dialogue_unlocks: list[str] = Field(default_factory=list)
    order_no: int

    class Settings:
        name = "npcs"
