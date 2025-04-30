import logging

from beanie import init_beanie
from motor.motor_asyncio import AsyncIOMotorClient

from affinitas_backend.config import Config
from affinitas_backend.models.beanie.npc import NPC
from affinitas_backend.models.beanie.save import Save, ShadowSave, DefaultSave


async def init_db():
    config = Config()  # noqa
    client = AsyncIOMotorClient(config.mongodb_uri, uuidRepresentation="standard")
    await init_beanie(database=client[config.mongodb_dbname], document_models=[NPC, Save, ShadowSave, DefaultSave])
    await test_connection(client)

    return client


# Verify connection
async def test_connection(client: AsyncIOMotorClient):
    try:
        await client.admin.command('ping')
        logging.info("MongoDB connected successfully.")
    except Exception as e:
        logging.fatal("MongoDB connection failed:", e)
        raise
