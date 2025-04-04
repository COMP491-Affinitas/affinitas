import logging

from fastapi import APIRouter

from ..message import Message

router = APIRouter(prefix="/hello")

@router.get("/")
async def say_hello():
    return {"message": "Hello, world!"}

@router.post("/")
async def hello_world(message: Message):
    logging.info(message)
    return {
        "message": "Hello client!",
        "error": "",
        "id": "0"
    }
