import logging
import os

from beanie import init_beanie
from motor.motor_asyncio import AsyncIOMotorClient

from affinitas_backend.models.beanie.npc import NPC
from affinitas_backend.models.beanie.save import Save, ShadowSave


# Load variables
def load_config():
    return os.getenv("MONGODB_URI"), os.getenv("MONGODB_DBNAME")


async def init_db():
    uri, dbname = load_config()
    client = AsyncIOMotorClient(uri)
    await init_beanie(database=client[dbname], document_models=[NPC, Save, ShadowSave])
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
