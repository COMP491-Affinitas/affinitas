import logging
from contextlib import asynccontextmanager
from typing import AsyncIterator

from dotenv import load_dotenv
from fastapi import FastAPI

from affinitas_backend.db.mongo import init_db


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    load_dotenv()
    client = await init_db()
    app.db = client.account

    logging.info("Startup complete")
    yield
    await client.close()
    logging.info("Shutdown complete")
