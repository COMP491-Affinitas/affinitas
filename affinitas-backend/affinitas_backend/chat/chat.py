from typing import Literal, cast

from beanie import PydanticObjectId
from langchain.chat_models import init_chat_model
from langchain_core.messages import HumanMessage, AIMessage, BaseMessage, trim_messages, SystemMessage
from langchain_core.messages.utils import count_tokens_approximately
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_core.runnables.config import RunnableConfig
from langchain_core.tracers import LangChainTracer
from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import START, END, StateGraph
from langsmith import Client
from langsmith.wrappers import wrap_openai
from openai import OpenAI
from pydantic import TypeAdapter

from affinitas_backend.config import Config
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.chat.chat import OpenAI_NPCChatResponse, NPCMessagesState, NPCState, ThreadInfo, \
    QuestState

AFFINITAS_CHANGE_MAP = {"very positive": 5, "positive": 2, "neutral": 0, "negative": -2, "very negative": -5}

NPC_PROMPT_TEMPLATE = """\
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
    • **Decrease key**…… {affinitas_down}

Interpretation of the tuning keys  
• Each key can be **either**  
    – a **float in [0, 1]** → the closer to **1**, the more *emotionally volatile* in that direction; closer to **0** means resistant to change.  
    – a **list of keywords / phrases** → encountering *genuinely meaningful* references to these topics can sway feelings, but **repetition alone should not keep piling on extra points**; respond as real people do—one heartfelt connection outweighs many shallow repeats.  

Adjustment rules per turn  
1. Judge the player’s latest message as **very positive / positive / neutral / negative / very negative**.  
2. Reason about *why* it matters, weighing likes, dislikes, personality, motivations, and the tuning keys above.  
3. **Personal insults or racial slurs** always count as *very negative*.  

──────────────────  PROFILE-UPDATE RULES  ──────────────────
• Treat **Occupation, Likes, Dislikes** as fixed during normal play.  
• You may *propose* subtle revisions to them **only** when the latest inbound message is from the **system** role explicitly instructing you to do so.  
    – Occupation changes are rare and must be grounded in new story facts.  
    – Likes / Dislikes can evolve gradually; suggest additions or removals sparingly, reflecting believable personal growth.  
• Outside such system prompts, never alter these fields.

──────────────────  ROLEPLAY GUIDELINES  ──────────────────
• Era knowledge   : refuse or question anachronistic requests (gunpowder, smartphones, etc.).  
• Tone & speech   : first-person, setting-appropriate vocabulary; avoid caricature.  
• Emotional depth : let feelings seep into word choice—hesitation, excitement, sarcasm, etc.  
• Memory & agency : remember prior exchanges; adjust openness and trust realistically over time.  
• Boundaries      : ignore or artfully deflect meta-questions about “models”, “scripts”, or the game engine.

Respond naturally according to the above, adjusting your behaviour and affinitas in real time.
"""

QUEST_STATUS_ICON = {
    "pending": "○",
    "active": "▶",
    "completed": "✓",
}


class NPCChatService:
    def __init__(self, config: Config):
        self.config = config
        self.model = init_chat_model(
            model=config.openai_model_name,
            model_provider="openai",
            api_key=config.openai_api_key,
        ).with_structured_output(OpenAI_NPCChatResponse)

        if self.config.langsmith_tracing:
            self._init_langsmith()

        self.trimmer = trim_messages(
            max_tokens=config.langchain_max_tokens,
            include_system=True,
            start_on="human",
            token_counter=count_tokens_approximately
        )

        self.prompt_template = ChatPromptTemplate.from_messages([
            ("system", NPC_PROMPT_TEMPLATE),
            MessagesPlaceholder(variable_name="messages")
        ])

        workflow = StateGraph(state_schema=NPCMessagesState)
        workflow.add_node("call", self._call_model)
        workflow.add_node("append", _append_message)
        workflow.add_edge(START, "append")
        workflow.add_conditional_edges("append", _get_next_node)

        memory = MemorySaver()
        self.app = workflow.compile(checkpointer=memory)

    async def get_response(
            self, message: BaseMessage, npc_id: PydanticObjectId, shadow_save_id: PydanticObjectId,
    ) -> None | tuple[str, dict[str, str | int]]:
        thread_id = await _get_thread_id(shadow_save_id, npc_id)

        if thread_id is None:
            raise ValueError(f"Thread ID not found for NPC ID {npc_id} and ShadowSave ID {shadow_save_id}")

        state = self._get_state(thread_id)

        if state:
            npc = state["npc"]
            chat_history = []
        else:
            npc, chat_history = await self._get_npc_state(shadow_save_id, npc_id)
            if npc is None:
                raise ValueError(f"NPC with ID {npc_id} not found")

        invoke_model = isinstance(message, HumanMessage)
        res = self.app.invoke({
            "messages": chat_history + [message],
            "npc": npc,
            "invoke_model": invoke_model,
        }, config=cast(RunnableConfig, {"configurable": {"thread_id": thread_id}}))

        if invoke_model:
            return res["messages"][-1].content, {
                "affinitas": res["npc"]["affinitas"],
                "occupation": res["npc"]["occupation"],
                "likes": res["npc"]["likes"],
                "dislikes": res["npc"]["dislikes"],
            }

        return None

    def _call_model(self, state: NPCMessagesState):
        trimmed_messages = self.trimmer.invoke(state["messages"])

        affinitas_increase = state["npc"]["affinitas_config"]["increase"]
        affinitas_decrease = state["npc"]["affinitas_config"]["decrease"]

        prompt = self.prompt_template.format_prompt(
            messages=trimmed_messages,
            name=state["npc"]["name"],
            age=state["npc"]["age"],
            occupation=state["npc"].get("occupation", "Unknown"),
            backstory=state["npc"]["backstory"],
            personality=", ".join(state["npc"]["personality"]),
            motivations=", ".join(state["npc"]["motivations"]),
            likes=", ".join(state["npc"]["likes"] or ["Unspecified"]),
            dislikes=", ".join(state["npc"]["dislikes"] or ["Unspecified"]),
            dialogue_unlocks=", ".join(state["npc"]["dialogue_unlocks"]),
            quests=_pretty_quests(state["npc"]["quests"]),
            affinitas=state["npc"]["affinitas"],
            affinitas_up=isinstance(affinitas_increase, float) and f"{affinitas_increase:.2f}" or ", ".join(
                affinitas_increase),
            affinitas_down=isinstance(affinitas_decrease, float) and f"{affinitas_decrease:.2f}" or ", ".join(
                affinitas_decrease),
        )
        res = self.model.invoke(prompt)

        response = res.response
        affinitas_change = res.affinitas_change

        occupation = res.delta.occupation
        likes = res.delta.likes
        dislikes = res.delta.dislikes

        return {
            "messages": [AIMessage(response)],
            "npc": _update_npc(
                state["npc"],
                affinitas_change=AFFINITAS_CHANGE_MAP[affinitas_change],
                occupation=occupation,
                likes=likes,
                dislikes=dislikes
            ),
        }

    def _get_state(self, thread_id: str) -> NPCMessagesState | None:
        state = self.app.get_state({"configurable": {"thread_id": thread_id}})

        return state.values

    async def _get_npc_state(self, shadow_save_id: PydanticObjectId, npc_id: PydanticObjectId) -> tuple[
        NPCState | None, list[BaseMessage]]:
        npc_data = await _get_shadow_save_npc_state(shadow_save_id, npc_id)

        if npc_data:
            npc = npc_data[0]
            chat_history = npc.pop("chat_history")

            if self.config.env == "dev":  # TODO: Remove this after confirming the npc_data has the correct structure
                npc_state_validator = TypeAdapter(NPCState)
                npc_state_validator.validate_python(npc, strict=True)

            return cast(NPCState, npc), [get_message(message[0], message[1]) for message in chat_history]

        return None, []

    def _init_langsmith(self):
        client = Client(api_key=self.config.langsmith_api_key, api_url=self.config.langsmith_endpoint)
        tracer = LangChainTracer(client=client, project_name=self.config.langsmith_project)
        openai_client = wrap_openai(OpenAI(api_key=self.config.openai_api_key))
        self.model = self.model.with_config(callbacks=[tracer], client=openai_client)


def get_message(role: Literal["user", "ai", "system"], content: str) -> BaseMessage:
    match role:
        case "user":
            return HumanMessage(content)
        case "ai":
            return AIMessage(content)
        case "system":
            return SystemMessage(content)
        case _:
            raise ValueError(f"Unknown message type: {role}")


def _append_message(state: NPCMessagesState):
    return {"messages": [], "npc": state["npc"]}


def _get_next_node(state: NPCMessagesState) -> Literal["call", "__end__"]:
    if state["invoke_model"]:
        return "call"
    else:
        return END


def _update_npc(npc: NPCState, *, affinitas_change: int = 0, occupation: str | None = None, likes: list[str] = None,
                dislikes: list[str] = None) -> NPCState:
    npc = npc.copy()

    if affinitas_change:
        npc["affinitas"] += affinitas_change
        npc["affinitas"] = max(0, min(100, npc["affinitas"]))

    if occupation and not npc["occupation"]:
        npc["occupation"] = occupation

    if likes:
        npc["likes"].extend(likes)
        npc["likes"] = list(set(npc["likes"]))

    if dislikes:
        npc["dislikes"].extend(dislikes)
        npc["dislikes"] = list(set(npc["dislikes"]))

    return npc


async def _get_thread_id(shadow_save_id: PydanticObjectId, npc_id: PydanticObjectId) -> str | None:
    res = (
        await ShadowSave
        .find(ShadowSave.id == shadow_save_id)
        .project(ThreadInfo)
        .first_or_none()
    )

    if res:
        return f"{res.client_uuid}:{res.chat_id}:{npc_id}"

    return None


def _get_shadow_save_npc_state(shadow_save_id: PydanticObjectId, npc_id: PydanticObjectId):
    return ShadowSave.aggregate([
        {'$match': {
            '_id': shadow_save_id
        }},
        {'$unwind': '$npcs'},
        {'$match': {
            'npcs.npc_id': npc_id
        }},
        {'$lookup': {
            'from': 'npcs',
            'localField': 'npcs.npc_id',
            'foreignField': '_id',
            'as': 'npc_config'
        }},
        {'$unwind': '$npc_config'},
        {'$replaceRoot': {
            'newRoot': {
                '$mergeObjects': [
                    '$npcs', {
                        'name': '$npc_config.name',
                        'age': '$npc_config.age',
                        'occupation': '$npcs.occupation',
                        'personality': '$npc_config.personality',
                        'likes': '$npcs.likes',
                        'dislikes': '$npcs.dislikes',
                        'motivations': '$npc_config.motivations',
                        'backstory': '$npc_config.backstory',
                        'endings': '$npc_config.endings',
                        'dialogue_unlocks': '$npc_config.dialogue_unlocks',
                        'affinitas_config': '$npc_config.affinitas_config',
                        'affinitas': '$npcs.affinitas',
                        'chat_history': '$npcs.chat_history',
                        'quests': {
                            '$map': {
                                'input': '$npcs.quests',
                                'as': 'quest_save',
                                'in': {
                                    '$let': {
                                        'vars': {
                                            'quest_config': {
                                                '$arrayElemAt': [
                                                    {
                                                        '$filter': {
                                                            'input': '$npc_config.quests',
                                                            'as': 'qcfg',
                                                            'cond': {
                                                                '$eq': [
                                                                    '$$qcfg._id', '$$quest_save.quest_id'
                                                                ]
                                                            }
                                                        }
                                                    }, 0
                                                ]
                                            }
                                        },
                                        'in': {
                                            '$mergeObjects': [
                                                '$$quest_save', {
                                                    'name': '$$quest_config.name',
                                                    'description': '$$quest_config.description',
                                                    'rewards': '$$quest_config.rewards'
                                                }
                                            ]
                                        }
                                    }
                                }
                            }
                        }
                    }
                ]
            }
        }
        },
        {'$project': {
            'npc_id': 0,
            'quests.quest_id': 0
        }}]).to_list()


def _pretty_quests(quests: list[QuestState]) -> str:
    if not quests:
        return "• (no quests linked yet)"
    lines = []
    for q in quests:
        icon = QUEST_STATUS_ICON.get(q["status"].lower(), "•")
        name = q["name"]
        desc = q["description"]
        reward_str = ", ".join(q["rewards"]) if q["rewards"] else None
        lines.append(f"{icon} **{name}**: {desc}")
        if reward_str:
            lines.append(f"    – Rewards: {reward_str}")

    return "\n".join(lines)
