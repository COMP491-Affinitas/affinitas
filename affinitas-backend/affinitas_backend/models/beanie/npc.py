from beanie import Document
from pydantic import Field

from affinitas_backend.models.game_data import BaseNPC


class NPC(Document, BaseNPC):
    id: str = Field(..., alias="_id")

    class Settings:
        name = "characters"
