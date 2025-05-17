from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from slowapi import _rate_limit_exceeded_handler
from slowapi.errors import RateLimitExceeded
from slowapi.middleware import SlowAPIMiddleware

from affinitas_backend.server.lifespan import lifespan
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.routers.auth import router as auth_router
from affinitas_backend.server.routers.game import router as game_router
from affinitas_backend.server.routers.npcs import router as npcs_router
from affinitas_backend.server.routers.saves import router as saves_router
from affinitas_backend.server.routers.session import router as session_router

DESCRIPTION = """
Backend API for **Affinitas: A Ten Day Tale**

This FastAPI service handles player identity, game-state persistence, and dialogue with
LLM-driven NPCs.

---

### Core Responsibilities
* **Authentication** – Issues or validates a `X-Client-UUID` so the game data can be identified.
* **Game** –  
  * Create a *shadow save* for a new run.  
  * Promote a shadow save to a permanent save-slot.  
  * List or restore existing saves.  
  * Cleanly discard a shadow save on quit.
* **NPC Interaction** – Sends user or system messages to any NPC, returns the NPC’s reply
  plus the updated **affinitas** score, and logs the exchange in the background.

---

### Public Endpoints & Rate Limits

| Method & Path                | Purpose                                                     | Limit |
|------------------------------|-------------------------------------------------------------|-------|
| **POST /auth/uuid**          | Issue a new UUID or echo back a valid one                  | 5/min |
| **GET  /game/load**          | List all saves for the caller                              | 3/min |
| **POST /game/load**          | Load a save → returns shadow-save & trimmed game data      | 3/min |
| **GET  /game/new**           | Bootstrap a fresh game from the default template           | 3/min |
| **POST /game/save**          | Persist the active shadow save as a permanent slot         | 10/min|
| **POST /game/quit**          | Delete the shadow save when the player exits               | 3/min |
| **POST /npcs/{npc_id}/chat** | Talk to an NPC<br>• User msg → 200 with reply & affinity<br>• System msg → 204 No Content | 10/min |

*(A master `/game/chat` route is reserved for future global narrative events.)*

---

### Data-Flow Highlights
* **Shadow-save pattern** – Gameplay occurs on an isolated copy; only committed to
  `Save` when the player explicitly saves.
* **MongoDB aggregation** – NPC and quest configuration documents are merged into
  save data at query time to keep the schema lean.
* **Background tasks** – Chat logs and affinity updates run asynchronously so the client
  never waits on disk I/O.

---

### Typical Client Flow
1. `POST /auth/uuid` → obtain your UUID header.  
2. `GET  /game/new` *or* `POST /game/load` → receive initial data & shadow-save ID.  
3. `POST /npcs/{npc}/chat` → converse; affinity and history update in the background.  
4. `POST /game/save` to commit progress, or `POST /game/quit` to discard.

---

### Roadmap
* Finish the reserved `/game/chat` route.
  * May be better to separate into `GET /game/quest/:quest_id` and `POST /game/ending`
* Add OpenAPI examples for every schema.

#### Non-Urgent
* Add `DELETE /game/load` to delete a save.
* Rename game routes.
* Remove the shadow save ID dependency from game routes.
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
app.include_router(session_router)
app.include_router(saves_router)
app.include_router(game_router)
