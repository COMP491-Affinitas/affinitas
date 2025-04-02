from fastapi import FastAPI

from .routers.hello import router as hello_world_router


app = FastAPI()
app.include_router(hello_world_router)

@app.get("/")
async def root():
    return {
        "message": "Hello World"
    }