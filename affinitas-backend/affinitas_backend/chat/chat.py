# TODO: Class will be removed and its functionality will be performed using functions
#  The thread ID for the chat states is generated and passed by the client.
#  The unity client should have a list of messages, not the whole history but the unanswered ones.
#  When a user message is added, the API call should be made and then the list should be cleared.
#  The new game is implemented by loading the data of a game that is saved right after being started.
#  The active game data is stored in the database in a "shadow" saves collection. Updated after each action.
#  When a game is saved, the data is copied to the main saves collection. When a game is quit, the shadow save is deleted.
#  Implement new endpoints related to this.
import os
import uuid
from typing import Literal

from beanie import PydanticObjectId
from langchain_core.messages import HumanMessage, SystemMessage, AIMessage, BaseMessage, trim_messages
from langchain.chat_models import init_chat_model
from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import START, StateGraph
from langchain_core.runnables.config import RunnableConfig
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_core.messages.utils import count_tokens_approximately

from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.game_data import NPCSaveData
from affinitas_backend.models.chat.chat import NPCChatResponse, NPCMessagesState, NPCState

MESSAGE_TYPES = {"user": HumanMessage, "system": SystemMessage, "assistant": AIMessage}
AFFINITAS_CHANGE_MAP = {"very positive": 5, "positive": 2, "neutral": 0, "negative": -2, "very negative": -5}

NPC_PROMPT_TEMPLATE = """\
You are an NPC in a video game that takes place in a medieval time setting. You have an affinity score called \
`affinitas` with the player. After each interaction, you will evaluate and categorize the player's input into one of the following \
categories depending on your character information: very positive, positive, neutral, negative, very negative. \
The change in affinitas is determined by the fields in the affinitas. Items, actions and concepts that are out of your character's \
knowledge and the medieval setting must be refused since they are invalid. Finally, at all times, stay in character and do not break the fourth wall.\
Act like a real person talking to another real person You may like, dislike, and even love or hate them. You are simulating the NPC with the following data:
{npc_json}\
"""

trimmer = trim_messages(
    max_tokens=int(os.getenv("LANGCHAIN_MAX_TOKENS") or 30_000),
    include_system=True,
    start_on="human",
    token_counter=count_tokens_approximately
)

prompt_template = ChatPromptTemplate.from_messages([
    ("system", NPC_PROMPT_TEMPLATE),
    MessagesPlaceholder(variable_name="messages")
])


def call_model(state: NPCMessagesState):
    trimmed_messages = trimmer.invoke(state["messages"])
    prompt = prompt_template.format_prompt(messages=trimmed_messages, npc=state["npc"])
    res = model.invoke(prompt)

    response = res.response
    affinitas_change = res.affinitas_change

    occupation = res.delta.occupation
    likes = res.delta.likes
    dislikes = res.delta.dislikes

    return {
        "messages": [AIMessage(response)],
        "npc": update_npc(state["npc"], affinitas_change=affinitas_change, occupation=occupation, likes=likes, dislikes=dislikes),
    }


model = init_chat_model(
    model=os.getenv("OPENAI_MODEL_NAME"),
    model_provider="openai",
).with_structured_output(NPCChatResponse)


workflow = StateGraph(state_schema=NPCMessagesState)
workflow.add_edge(START, "model")
workflow.add_node("model", call_model)

memory = MemorySaver()
app = workflow.compile(checkpointer=memory)


def get_response(thread_id: str):
    state = app.get_state({"configurable": {"thread_id": thread_id}})

    if state is None:
        raise ValueError("Thread ID not found")

class NPCChat:
    DEFAULT_CONFIG: RunnableConfig = {"configurable": {"thread_id": None}}
    affinitas_change_map = {"very positive": 5, "positive": 2, "neutral": 0, "negative": -2, "very negative": -5}

    def __init__(self, npc: NPCSaveData):
        self._messages: list[BaseMessage] = []
        self.npc_data = npc
        self._model_config = self.default_config

    def reset(self):
        self._messages.clear()
        self._model_config = self.default_config

    def add_message(self, message: str, role: Literal["user", "system", "assistant"]):
        self._messages.append(MESSAGE_TYPES[role](content=message))

    def get_response(self):
        output = app.invoke({
            "messages": self._messages,
            "npc_json": self.npc_data.model_dump_json(exclude={
                "npc_meta": {
                    "id": True,
                    "quests": {
                        "quest_meta": {
                            "id": True
                        }
                    }, "minigame": True
                },
                "chat_history": True
            })
        }, self._model_config)

        self._messages.clear()


        return "I hate nigas"

    @staticmethod
    def _generate_thread_id():
        return uuid.uuid4().hex

    @property
    def default_config(self) -> RunnableConfig:
        cfg = self.DEFAULT_CONFIG.copy()
        cfg["configurable"]["thread_id"] = self._generate_thread_id()

        return cfg


def update_npc(npc: NPCState, *, affinitas_change: int = 0, occupation: str | None = None, likes: list[str] = None, dislikes: list[str] = None) -> NPCState:
    npc = npc.copy()

    if affinitas_change:
        npc["affinitas"] += affinitas_change
        npc["affinitas"] = max(0, min(100, npc["affinitas"]))

    if occupation and not npc["npc_meta"]["occupation"]:
        npc["npc_meta"]["occupation"] = occupation

    if likes:
        npc["npc_meta"]["likes"].extend(likes)
        npc["npc_meta"]["likes"] = list(set(npc["npc_meta"]["likes"]))

    if dislikes:
        npc["npc_meta"]["dislikes"].extend(dislikes)
        npc["npc_meta"]["dislikes"] = list(set(npc["npc_meta"]["dislikes"]))

    return npc


def get_state(thread_id: str) -> NPCMessagesState | None:
    state = app.get_state({"configurable": {"thread_id": thread_id}})
    if state is None:
        return None

    return state.values


async def get_npc_state(client_id: str, shadow_save_id: PydanticObjectId, npc_id: PydanticObjectId) -> NPCState | None:
    npc_data = await ShadowSave.aggregate([
        {"$match": {
            "_id": shadow_save_id,
            "client_uuid": client_id,
        }},
        {"$unwind": "$npcs"},
        {"$match": {"npcs.npc_meta._id": npc_id}},
        {"$replaceRoot": {"newRoot": "$npcs"}},
        {"$project": {
            "chat_history": 0,
        }}
    ]).to_list(1)

    print("NPC Data:", npc_data)
    if npc_data:
        return npc_data[0]

    return None
