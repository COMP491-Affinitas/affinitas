from typing import cast, TypedDict

from beanie import PydanticObjectId
from langchain.chat_models import init_chat_model
from langchain_core.messages import HumanMessage, AIMessage, BaseMessage, trim_messages
from langchain_core.messages.utils import count_tokens_approximately
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from pydantic import TypeAdapter

from affinitas_backend.chat.utils import (
    NPC_PROMPT_TEMPLATE,
    AFFINITAS_CHANGE_MAP,
    get_message,
    pretty_quests,
    with_tracing, get_npc_data
)
from affinitas_backend.config import Config
from affinitas_backend.db.utils import get_thread_id
from affinitas_backend.models.chat.chat import OpenAI_NPCChatResponse, NPCChatState


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
            self.model = with_tracing(self.model, self.config)

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

    async def get_response(
            self,
            message: BaseMessage,
            npc_id: PydanticObjectId,
            shadow_save_id: PydanticObjectId,
            *, invoke_model: bool = False
    ) -> GetResponse | None:
        thread_id = await get_thread_id(shadow_save_id, npc_id)

        if thread_id is None:
            raise ValueError(f"Thread ID not found for NPC ID {npc_id} and ShadowSave ID {shadow_save_id}")

        npc, chat_history = await self._get_npc_state(shadow_save_id, npc_id)
        prev_completed_quests = npc["completed_quests"].copy() if npc else []
        if npc is None:
            raise ValueError(f"NPC with ID {npc_id} not found")

        invoke_model = invoke_model or isinstance(message, HumanMessage)

        res = self.call_model(chat_history + [message], npc)

        if invoke_model:
            return cast(GetResponse, {
                "message": res["messages"][-1].content,
                "updated_npc_data": {
                    "affinitas": npc["affinitas"],
                    "occupation": npc["occupation"],
                    "likes": npc["likes"],
                    "dislikes": npc["dislikes"],
                },
                "completed_quests": list(
                    set(npc["completed_quests"]) - set(prev_completed_quests)
                )
            })

        return None

    def call_model(self, messages: list[BaseMessage], npc: NPCChatState):
        trimmed_messages = messages

        prompt = self.prompt_template.format_prompt(
            messages=trimmed_messages,
            occupation=npc["occupation"] or "Unknown",
            likes=", ".join(npc["likes"] or ["Unspecified"]),
            dislikes=", ".join(npc["dislikes"] or ["Unspecified"]),
            quests=pretty_quests(npc["quests"]),
            affinitas=npc["affinitas"],
        )

        res = self.model.invoke(prompt)

        response = res.response
        affinitas_change = res.affinitas_change

        occupation = res.delta.occupation
        likes = res.delta.likes
        dislikes = res.delta.dislikes

        completed_quests = res.completed_quests

        _update_npc(
            npc,
            affinitas_change=AFFINITAS_CHANGE_MAP.get(affinitas_change, 0),
            occupation=occupation,
            likes=likes,
            dislikes=dislikes,
            completed_quests=completed_quests
        )

        return {
            "messages": [AIMessage(response)],
        }

    async def _get_npc_state(
            self,
            shadow_save_id: PydanticObjectId,
            npc_id: PydanticObjectId,
    ) -> tuple[NPCChatState | None, list[BaseMessage]]:
        npc = await get_npc_data(
            shadow_save_id,
            npc_id,
            include_chat_history=True,
            include_static_data=False,
        )

        if npc:
            if self.config.env == "dev":
                npc_state_validator = TypeAdapter(NPCChatState)
                npc_state_validator.validate_python(npc, strict=True)

            return cast(NPCChatState, npc), [get_message(msg_type, msg_content) for msg_type, msg_content in
                                             npc.pop("chat_history")]

        return None, []


def _update_npc(
        npc: NPCChatState, *,
        affinitas_change: int = 0,
        occupation: str | None = None,
        likes: list[str] = None,
        dislikes: list[str] = None,
        completed_quests: list[str] = None
):
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
