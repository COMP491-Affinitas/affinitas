from datetime import datetime
from typing import Annotated

import pymongo
from beanie import Document, Indexed
from pydantic import UUID4, Field
from pymongo import IndexModel

from affinitas_backend.models.game_data import GameData


class Save(Document, GameData):
    client_uuid: Annotated[UUID4, Indexed()]  # directly using `Indexed(UUID4)` does not work here
    chat_id: UUID4

    name: str | None
    saved_at: datetime | None

    class Settings:
        name = "save"
        indexes = [
            IndexModel(
                [("saved_at", pymongo.DESCENDING)],
                name="saved_at_desc",
            ),
            IndexModel(
                [("client_uuid", pymongo.ASCENDING)],
                name="client_uuid_asc",
            ),
        ]


class ShadowSave(Document, GameData):
    client_uuid: Annotated[UUID4, Indexed(unique=True)]  # We want only one active game per user
    chat_id: UUID4

    class Settings:
        name = "shadow_save"
        indexes = [
            IndexModel(
                [("client_uuid", pymongo.ASCENDING)],
                name="client_uuid_asc",
                unique=True,
            ),
        ]


class DefaultSave(Document, GameData):
    id: int = Field(..., alias="_id")

    class Settings:
        name = "default_save"
