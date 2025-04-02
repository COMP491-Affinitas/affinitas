from fastapi import APIRouter

from ..message import Message

router = APIRouter(prefix="/hello")

@router.get("/")
async def say_hello():
    return "Hello, world!"


@router.post("/")
async def hello_world(message: Message):
    print(message)

    return {
        "message": "Hello client!",
        "error": "",
        "id": "0"
    }
