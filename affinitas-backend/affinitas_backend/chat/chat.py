# TODO: Class will be removed and its functionality will be performed using functions
#  The thread ID for the chat states is generated and passed by the client.
#  NPC data is also given by the client.
#  The unity client should have a list of messages, not the whole history but the unanswered ones.
#  When a user message is added, the API call should be made and then the list should be cleared.
#  The new game is implemented by loading the data of a game that is saved right after being started.
import os
import uuid
from typing import Literal

from langchain_core.messages import HumanMessage, SystemMessage, AIMessage, BaseMessage, trim_messages
from langchain.chat_models import init_chat_model
from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import START, StateGraph
from langchain_core.runnables.config import RunnableConfig
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_core.messages.utils import count_tokens_approximately

from affinitas_backend.models.game_data import NPCSaveData
from affinitas_backend.models.chat.chat import NPCChatResponse, NPCMessagesState


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
    prompt = prompt_template.format_prompt(messages=trimmed_messages, npc_json=state["npc_json"])
    response = model.invoke(prompt)


    return {
        "messages": [AIMessage(response.response)],
        "affinitas_change": response.affinitas_change,
        "delta": response.delta
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

        npc_response = output["messages"][-1].content
        affinitas_change = AFFINITAS_CHANGE_MAP[output["affinitas_change"]]
        delta = output["delta"]

        # Update NPC data
        self.npc_data.affinitas += affinitas_change
        self.npc_data.affinitas = max(0, min(100, self.npc_data.affinitas))

        if delta.occupation and not self.npc_data.npc_meta.occupation:
            self.npc_data.npc_meta.occupation = delta["occupation"]

        if delta.likes:
            self.npc_data.npc_meta.likes.extend(delta.likes)
            self.npc_data.npc_meta.likes = list(set(self.npc_data.npc_meta.likes))

        if delta.dislikes:
            self.npc_data.npc_meta.dislikes.extend(delta.dislikes)
            self.npc_data.npc_meta.dislikes = list(set(self.npc_data.npc_meta.dislikes))

        return {
            "response": npc_response,
            "affinitas_change": affinitas_change,
        }

    @staticmethod
    def _generate_thread_id():
        return uuid.uuid4().hex

    @property
    def default_config(self) -> RunnableConfig:
        cfg = self.DEFAULT_CONFIG.copy()
        cfg["configurable"]["thread_id"] = self._generate_thread_id()

        return cfg
