from datetime import datetime, UTC
from functools import partial

import pymongo
from beanie import Document, Indexed
from pydantic import Field
from pymongo import IndexModel

from affinitas_backend.models.game_data import GameData


class Save(Document, GameData):
    name: str
    saved_at: datetime = Field(default_factory=partial(datetime.now, tz=UTC))
    client_uuid: Indexed(str)

    class Settings:
        name = "save"
        indexes = [
            IndexModel([("client_uuid", pymongo.DESCENDING)]),
        ]
