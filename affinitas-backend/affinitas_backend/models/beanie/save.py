from datetime import datetime

import pymongo
from beanie import Document, Indexed
from pymongo import IndexModel

from affinitas_backend.models.game_data import GameData


class Save(Document, GameData):
    name: str
    saved_at: datetime
    client_uuid: Indexed(str)
    chat_id: Indexed(str)

    class Settings:
        name = "save"
        indexes = [
            IndexModel(
                [("saved_at", pymongo.DESCENDING)]
            ),
            IndexModel(
                [("chat_id", pymongo.DESCENDING)],
                unique=True,
            )
        ]


class ShadowSave(Document, GameData):
    client_uuid: Indexed(str)
    chat_id: Indexed(str)

    class Settings:
        name = "shadow_save"
        indexes = [
            IndexModel(
                [("client_uuid", pymongo.ASCENDING)],
                unique=True,
            ),
            IndexModel(
                [("chat_id", pymongo.ASCENDING)],
                unique=True,
            )
        ]
