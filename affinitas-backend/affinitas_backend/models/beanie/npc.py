from beanie import Document

from affinitas_backend.models.game_data import BaseNPC


class NPC(Document, BaseNPC):
    class Settings:
        name = "npcs"
