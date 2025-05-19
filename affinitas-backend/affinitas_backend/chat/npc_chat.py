import copy
from typing import Literal, cast, TypedDict

from beanie import PydanticObjectId
from langchain.chat_models import init_chat_model
from langchain_core.messages import HumanMessage, AIMessage, BaseMessage, trim_messages
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

from affinitas_backend.chat.utils import NPC_PROMPT_TEMPLATE, AFFINITAS_CHANGE_MAP, get_message, \
    _pretty_quests
from affinitas_backend.config import Config
from affinitas_backend.db.utils import get_shadow_save_npc_state
from affinitas_backend.models.beanie.save import ShadowSave
from affinitas_backend.models.chat.chat import OpenAI_NPCChatResponse, NPCMessagesState, NPCState, ThreadInfo


class UpdatedNPCData(TypedDict):
    affinitas: int
    occupation: str | None
    likes: list[str]
    dislikes: list[str]


class GetResponse(TypedDict):
    message: str
    updated_npc_data: UpdatedNPCData
    completed_quests: list[str]


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
            *, invoke_model: bool = False
    ) -> GetResponse | None:
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

        invoke_model = invoke_model or isinstance(message, HumanMessage)
        res = self.app.invoke({
            "messages": chat_history + [message],
            "npc": npc,
            "invoke_model": invoke_model,
        }, config=cast(RunnableConfig, {"configurable": {"thread_id": thread_id}}))

        if invoke_model:
            return cast(GetResponse, {
                "message": res["messages"][-1].content,
                "updated_npc_data": {
                    "affinitas": res["npc"]["affinitas"],
                    "occupation": res["npc"]["occupation"],
                    "likes": res["npc"]["likes"],
                    "dislikes": res["npc"]["dislikes"],
                },
                "completed_quests": res["npc"]["completed_quests"][len(npc["completed_quests"]):],
            })

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
                dislikes=dislikes,
                completed_quests=res.completed_quests
            ),
        }

    def _get_state(self, thread_id: str) -> NPCMessagesState | None:
        state = self.app.get_state({"configurable": {"thread_id": thread_id}})

        return state.values

    async def _get_npc_state(self, shadow_save_id: PydanticObjectId, npc_id: PydanticObjectId) -> tuple[
        NPCState | None, list[BaseMessage]]:
        npc_data = await get_shadow_save_npc_state(shadow_save_id, npc_id)

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


def _append_message(state: NPCMessagesState):
    return {"messages": [], "npc": state["npc"]}


def _get_next_node(state: NPCMessagesState) -> Literal["call", "__end__"]:
    if state["invoke_model"]:
        return "call"
    else:
        return END


def _update_npc(npc: NPCState, *, affinitas_change: int = 0, occupation: str | None = None, likes: list[str] = None,
                dislikes: list[str] = None, completed_quests: list[str] = None) -> NPCState:
    npc = copy.deepcopy(npc)

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

    if completed_quests:
        npc["completed_quests"].extend(completed_quests)
        npc["completed_quests"] = list(set(npc["completed_quests"]))

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
