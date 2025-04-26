# TODO: Class will be removed and its functionality will be performed using functions
#  The thread ID for the chat states is generated and passed by the client.
#  The unity client should have a list of messages, not the whole history but the unanswered ones.
#  When a user message is added, the API call should be made and then the list should be cleared.
#  The new game is implemented by loading the data of a game that is saved right after being started.
#  The active game data is stored in the database in a "shadow" saves collection. Updated after each action.
#  When a game is saved, the data is copied to the main saves collection. When a game is quit, the shadow save is deleted.
#  Implement new endpoints related to this.
import os
from typing import Literal, cast

from beanie import PydanticObjectId
from beanie.odm.operators.update.array import Push
from beanie.odm.operators.update.general import Set
from bson import json_util
from dotenv import load_dotenv
from langchain.chat_models import init_chat_model
from langchain_core.messages import HumanMessage, AIMessage, BaseMessage, trim_messages
from langchain_core.messages.utils import count_tokens_approximately
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_core.runnables.config import RunnableConfig
from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import START, END, StateGraph
from pydantic import TypeAdapter

from affinitas_backend.db.mongo import init_db
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.chat.chat import NPCChatResponse, NPCMessagesState, NPCState

load_dotenv()


def call_model(state: NPCMessagesState):
    npc_json = json_util.dumps(state["npc"])
    trimmed_messages = trimmer.invoke(state["messages"])
    prompt = prompt_template.format_prompt(messages=trimmed_messages, npc_json=npc_json)
    res = model.invoke(prompt)

    response = res.response
    affinitas_change = res.affinitas_change

    occupation = res.delta.occupation
    likes = res.delta.likes
    dislikes = res.delta.dislikes

    return {
        "messages": [AIMessage(response)],
        "npc": update_npc(state["npc"], affinitas_change=AFFINITAS_CHANGE_MAP[affinitas_change], occupation=occupation,
                          likes=likes, dislikes=dislikes),
    }


def append_message(state: NPCMessagesState):
    return {"messages": [], "npc": state["npc"]}


def get_next_node(state: NPCMessagesState) -> Literal["call", "__end__"]:
    if state["invoke_model"]:
        return "call"
    else:
        return END


model = init_chat_model(
    model=os.getenv("OPENAI_MODEL_NAME"),
    model_provider="openai",
).with_structured_output(NPCChatResponse)

workflow = StateGraph(state_schema=NPCMessagesState)
workflow.add_node("call", call_model)
workflow.add_node("append", append_message)
workflow.add_edge(START, "append")
workflow.add_conditional_edges("append", get_next_node)

memory = MemorySaver()
app = workflow.compile(checkpointer=memory)

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


async def get_response(
        message: BaseMessage, npc_id: PydanticObjectId, client_id: str,
        shadow_save_id: PydanticObjectId, chat_id: str
):
    thread_id = f"{client_id}:{chat_id}:{npc_id}"

    state = get_state(thread_id)

    if state:
        npc = state["npc"]
    else:
        npc = await get_npc_state(client_id, shadow_save_id, npc_id)  # TODO: Should the chat_history be taken from here
        if npc is None:
            raise ValueError(f"NPC with ID {npc_id} not found")

    res = app.invoke({
        "messages": [message],
        "npc": npc,
        "invoke_model": isinstance(message, HumanMessage),
    }, config=cast(RunnableConfig, {"configurable": {"thread_id": thread_id}}))

    (await ShadowSave  # TODO: Do this in the route.
     .find(ShadowSave.id == shadow_save_id)
     .find({"npcs.npc_meta._id": npc_id})
     .update(Push({"npcs.$.chat_history": res["messages"][-1]}))
     .update(Set({"npcs.$.affinitas": res["npc"]["affinitas"]}))
     .update(Set({"npcs.$.npc_meta.occupation": res["npc"]["npc_meta"]["occupation"]}))
     .update(Set({"npcs.$.npc_meta.likes": res["npc"]["npc_meta"]["likes"]}))
     .update(Set({"npcs.$.npc_meta.dislikes": res["npc"]["npc_meta"]["dislikes"]})))

    return res


def update_npc(npc: NPCState, *, affinitas_change: int = 0, occupation: str | None = None, likes: list[str] = None,
               dislikes: list[str] = None) -> NPCState:
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
            "npc_meta._id": 0,
            "npc_meta.quests._id": 0,
            "quests.quest_meta._id": 0,
        }}
    ]).to_list(1)

    if npc_data:
        if os.getenv("ENV") == "dev":  # TODO: Remove this after confirming the npc_data has the correct structure
            npc_state_validator = TypeAdapter(NPCState)
            npc_state_validator.validate_python(npc_data[0], strict=True)
        return cast(NPCState, npc_data[0])

    return None


if __name__ == "__main__":
    import asyncio

    loop = asyncio.get_event_loop()
    loop.run_until_complete(init_db())

    uid = "ebc1f20c-af94-45ea-8ce3-11ca1428418d"
    ssid = PydanticObjectId("680b76ddcb00481b1e7d8163")
    npc_id = PydanticObjectId("6809505281ea296e5f1daa59")

    print(loop.run_until_complete(
        get_response(HumanMessage("Hello, what is your name?"), npc_id, uid, ssid, "test"))
    )
