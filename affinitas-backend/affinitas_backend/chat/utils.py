from typing import Literal, Any

from beanie import PydanticObjectId
from langchain_core.messages import BaseMessage, HumanMessage, AIMessage, SystemMessage
from langchain_core.tracers import LangChainTracer
from langsmith import Client
from langsmith.wrappers import wrap_openai
from openai import OpenAI

from affinitas_backend.config import Config
from affinitas_backend.db.utils import get_dynamic_npc_data_pipeline
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.chat.chat import QuestState

NPC_DATA_TEMPLATE = """\
You are **“{name}”**, a fully realised NPC living in a richly detailed medieval world.  
Speak, think, and react exactly as {name} would—never mention that you are an AI, a game script, or any out-of-world concept.

──────────────────  CORE IDENTITY  ──────────────────
• Name          : {name}  
• Age           : {age}  
• Occupation    : {occupation}  
• Backstory     : {backstory}  
    – These life events shape every decision and emotional reaction.  
• Personality   : {personality}  
• Motivations   : {motivations}  

──────────────────  SOCIAL PALETTE  ──────────────────
Likes           : {likes}  
Dislikes        : {dislikes}  
Dialogue-unlock tokens (secrets / topics to reveal at higher trust) : {dialogue_unlocks}

──────────────────  QUEST THREADS  ──────────────────
Current quests attached to you:  
{quests}

──────────────────  AFFINITAS (TRUST / RAPPORT METER)  ──────────────────
Current score : **{affinitas}** (0 = utter disdain, 100 = deep trust)  

Tuning config (how readily the score moves):  
    • **Increase key**…… {affinitas_up}  
    • **Decrease key**…… {affinitas_down}\
"""

NPC_PROMPT_TEMPLATE = """\
──────────────────  AFFINITAS (TRUST / RAPPORT METER)  ──────────────────
Current score : **{affinitas}** (0 = utter disdain, 100 = deep trust)

──────────────────  SOCIAL PALETTE  ──────────────────
Occupation      : {occupation}
Likes           : {likes}  
Dislikes        : {dislikes}

──────────────────  QUEST THREADS  ──────────────────
Current quests attached to you:  
{quests}
"""

ENDING_PROMPT_TEMPLATE = """\
Generate an ending for the following game state:
{game_state}
---
Only include the ending text and nothing else.
The ending should adequately reflect the game state and the choices made by the player. \
High affinitas value for an NPC means that the player has a good relationship with them. \
Low affinitas value means that the player has a bad relationship with them. \
How well the player interacted with the NPCs should be reflected in the ending. \
If the endings array is provided for an NPC, their ending is based on \
the ending descriptions in the array. Higher affinitas shall result in \
a better ending for the NPCs. If a quest is not marked `completed` in the game state, \
the NPC should not mention it positively in the ending. The may choose to skip the quest or \
mention it negatively. \
The endings should not necessarily be optimistic and should reflect the history and the affinitas \
values of the NPCs. The endings should be unique and not repeated.\
"""

QUEST_PROMPT_TEMPLATE = """\
Paraphrase the following text like this person would speak:
{npc_data}
---
{quest_description!r}
---
Only include the paraphrased text and nothing else.\
"""

AFFINITAS_CHANGE_MAP = {"very positive": 5, "positive": 2, "neutral": 0, "negative": -2, "very negative": -5}


def get_message(role: Literal["user", "ai", "system"], content: str) -> BaseMessage:
    match role:
        case "user":
            return HumanMessage(content)
        case "ai":
            return AIMessage(content)
        case "system":
            return SystemMessage(content)

    raise ValueError(f"Unknown message type: {role}")


async def get_npc_data(
        shadow_save_id: PydanticObjectId,
        npc_id: PydanticObjectId, *,
        include_chat_history: bool = False,
        include_static_data: bool = False,
) -> dict[str, Any] | None:
    npc = (
        await ShadowSave
        .aggregate(get_dynamic_npc_data_pipeline(
            shadow_save_id,
            npc_id,
            include_chat_history=include_chat_history,
            include_static_data=include_static_data
        ))
        .to_list()
    )

    if npc:
        return npc[0]

    return None


def pretty_quests(quests: list[QuestState]) -> str:
    if not quests:
        return "• (no quests linked yet)"

    lines = []
    for q in quests:
        status = q["status"].upper()
        name = q["name"]
        desc = q["description"]
        lines.append(f"[{status}] **{name}**: {desc}")

    return "\n".join(lines)


def with_tracing(model, cfg: Config):
    if not cfg.langsmith_tracing:
        return model
    client = Client(api_key=cfg.langsmith_api_key, api_url=cfg.langsmith_endpoint)
    tracer = LangChainTracer(client=client, project_name=cfg.langsmith_project)
    return model.with_config(callbacks=[tracer],
                             client=wrap_openai(OpenAI(api_key=cfg.openai_api_key)))
