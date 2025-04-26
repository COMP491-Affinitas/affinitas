from datetime import datetime

import pymongo
from beanie import Document, Indexed, PydanticObjectId
from pymongo import IndexModel

from affinitas_backend.models.game_data import GameData


class Save(Document, GameData):
    name: str
    saved_at: datetime
    client_uuid: Indexed(str)

    class Settings:
        name = "save"
        indexes = [
            IndexModel([("saved_at", pymongo.DESCENDING)]),
        ]


class ShadowSave(Document, GameData):
    client_uuid: Indexed(str)

    class Settings:
        name = "shadow_save"
        indexes = [
            IndexModel(
                [("client_uuid", pymongo.ASCENDING)],
                unique=True,
            )
        ]