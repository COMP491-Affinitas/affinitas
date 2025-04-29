from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from slowapi import _rate_limit_exceeded_handler
from slowapi.errors import RateLimitExceeded
from slowapi.middleware import SlowAPIMiddleware

from .lifespan import lifespan
from .limiter import limiter
from .routers.auth import router as auth_router
from .routers.game import router as game_router
from .routers.npcs import router as npcs_router

DESCRIPTION = """\
# Backend server for the *Affinitas: A Ten Day Tale* game.
This server handles user authentication, game saves, and NPC data.
## Features
- User authentication by assigning them the clients UUID
- Game saves management, including loading and saving game states
- NPC data management, including loading the entire list of NPCs or loading a specific NPC
## Rate Limiting
To prevent abuse, the server implements rate limiting:
- **POST /auth/uuid**: 5 requests per minute
- **GET /game/load**: 3 requests per minute
- **POST /game/load**: 3 requests per minute
- **POST /game/save**: 10 requests per minute
- **GET /npcs/**: 3 requests per minute
- **GET /npcs/{npc_id}**: 18 requests per minute
## TODO
- Implement a more sophisticated rate limiting 
- Add example data to route schemas\
"""

app = FastAPI(
    title="Affinitas Backend",
    description=DESCRIPTION,
    version="0.1.0",
    license_info={
        "name": "MIT",
        "url": "https://github.com/COMP491-Affinitas/affinitas/blob/main/LICENSE"
    },
    lifespan=lifespan,
)

app.state.limiter = limiter  # noqa: I'm just following the docs

app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)  # noqa

app.add_middleware(
    CORSMiddleware,  # noqa
    allow_origins=["*"],
    allow_credentials=False,
    allow_methods=["GET", "POST", "HEAD", "OPTIONS"],
    allow_headers=["*"],
)
app.add_middleware(SlowAPIMiddleware)  # noqa

app.include_router(auth_router)
app.include_router(game_router)
app.include_router(npcs_router)
