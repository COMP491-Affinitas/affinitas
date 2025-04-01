from fastapi import APIRouter

from .message import Message


router = APIRouter()


@router.get("/hello")
async def say_hello():
    return "Hello, world!"


@router.post("/hello")
async def hello_world(message: Message):
    print(message)

    return {
        "message": "Hello client!",
        "error": "",
    }
