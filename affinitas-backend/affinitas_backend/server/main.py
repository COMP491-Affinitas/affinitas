import logging

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from slowapi import _rate_limit_exceeded_handler
from slowapi.errors import RateLimitExceeded
from slowapi.middleware import SlowAPIMiddleware

from affinitas_backend.config import Config
from affinitas_backend.server.lifespan import lifespan
from affinitas_backend.server.limiter import limiter
from affinitas_backend.server.routers.auth import router as auth_router
from affinitas_backend.server.routers.npcs import router as npcs_router
from affinitas_backend.server.routers.saves import router as saves_router
from affinitas_backend.server.routers.session import router as session_router

config = Config()  # noqa

logging.basicConfig(level=config.log_level)

DESCRIPTION = """
Backend API for **Affinitas: A Ten Day Tale**

This FastAPI service handles player authentication, game‐state persistence, session control, and dynamic NPC interactions powered by LLM-driven dialogue and quest logic.

---

### Core Responsibilities
* **Authentication** – Issue and validate `X-Client-UUID` headers for client identification.  
* **Game Save Management** –  
  * List existing saves, load a save into a *shadow* session, persist a shadow session to a permanent save, and delete saves.  
* **Session Control** –  
  * Start new games, update action points & day number, give items to the player’s inventory, generate narrative endings, and quit sessions (clean up shadow saves).  
* **NPC Workflows** –  
  * Chat (user or system messages), retrieve & activate quests, complete quests (with affinitas rewards), and give items to NPCs.  
  * All interactions return LLM-generated responses, updated **affinitas** scores, and are logged asynchronously.

---

### Public Endpoints & Rate Limits

| Method & Path                       | Purpose                                                   | Limit    |
|-------------------------------------|-----------------------------------------------------------|----------|
| **POST /auth/uuid**                 | Issue or validate client UUID                            | 100/min  |
| **GET  /saves/**                    | List all saved games for the client                       | 10/min   |
| **POST /saves/**                    | Load a save → returns shadow-save ID & trimmed game data  | 10/min   |
| **DELETE /saves/{save_id}**         | Permanently delete a saved game                           | 10/min   |
| **POST /session/new**               | Create a new shadow save for a fresh run                  | 10/min   |
| **PATCH /session?day-no=&ap=**      | Update action points & advance day number                 | 30/min   |
| **POST /session/item**              | Give an item to the player (activate in shadow save)      | 10/min   |
| **POST /session/save**              | Persist the active shadow save as a permanent slot        | 10/min   |
| **DELETE /session?id={shadow_id}**  | Quit game → delete the shadow save                        | 10/min   |
| **POST /session/generate-ending**   | Generate a narrative game ending based on NPC states      | 10/min   |
| **POST /npcs/{npc_id}/chat**        | Chat with an NPC → returns reply & updated affinitas      | 10/min   |
| **POST /npcs/{npc_id}/quest**       | Retrieve and activate quests for an NPC                   | 10/min   |
| **POST /npcs/{npc_id}/quest/complete** | Complete a quest and reward affinitas                | 10/min   |
| **POST /npcs/{npc_id}/item**        | Give an item to an NPC → receive narrative response       | 10/min   |

---

### Data-Flow Highlights
* **Shadow-save pattern** – All gameplay modifications occur on an isolated session copy; commits only on explicit save.  
* **MongoDB Aggregation** – Merges save, NPC, and quest data at query time for schema flexibility.  
* **Background Tasks** – Performs chat logging and state updates asynchronously to keep API responses low-latency.

---

### Typical Client Flow
1. `POST /auth/uuid` → obtain your client UUID.  
2. `POST /session/new` *or* `POST /saves/` → start a new run or load an existing save (get shadow-save ID + game data).  
3. `POST /npcs/{npc_id}/chat`, `/quest`, `/quest/complete`, `/item` → interact with NPCs; affinitas and history update in background.  
4. `PATCH /session?day-no=&ap=` to advance days or adjust action points.  
5. `POST /session/save` to commit progress, or `DELETE /session?id=` to quit and discard.  
6. Optionally generate the ending with `POST /session/generate-ending`.

Licensed under MIT.
"""


app = FastAPI(
    title="Affinitas Backend",
    description=DESCRIPTION,
    version="1.3.1",
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
    allow_methods=["GET", "POST", "PATCH", "DELETE", "HEAD", "OPTIONS"],
    allow_headers=["*"],
)
app.add_middleware(SlowAPIMiddleware)  # noqa

app.include_router(auth_router)
app.include_router(npcs_router)
app.include_router(session_router)
app.include_router(saves_router)
