import logging
from contextlib import asynccontextmanager
from typing import AsyncIterator

from fastapi import FastAPI

from affinitas_backend.db.mongo import init_db


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    client = await init_db()
    app.db = client.account

    logging.info("Startup complete")
    yield
    client.close()
    logging.info("Shutdown complete")
