import asyncio
from typing import Any

import bson.json_util
from beanie import PydanticObjectId
from langchain.chat_models import init_chat_model
from langchain_core.tracers import LangChainTracer
from langsmith import Client
from langsmith.wrappers import wrap_openai
from openai import OpenAI
from pydantic import TypeAdapter

from affinitas_backend.chat.utils import _get_shadow_save_npc_state
from affinitas_backend.config import Config
from affinitas_backend.models.chat.chat import NPCState


class MasterLLM:
    def __init__(self, config: Config):
        self.config = config
        self.model = init_chat_model(
            model=config.openai_model_name,
            model_provider="openai",
            api_key=config.openai_api_key,
        )

        if self.config.langsmith_tracing:
            self._init_langsmith()

    async def get_quest_responses(self, quests: list[dict], shadow_save_id: PydanticObjectId,
                                  npc_id: PydanticObjectId) -> list[dict]:
        npc = await _get_shadow_save_npc_state(
            shadow_save_id,
            npc_id,
        )

        if npc:
            npc = npc[0]

            if self.config.env == "dev":
                npc_state_validator = TypeAdapter(NPCState)
                npc_state_validator.validate_python(npc, strict=True)

        messages = [
            self.model.ainvoke(
                "Paraphrase the following text like this person would speak:\n"
                f"{bson.json_util.dumps(npc)}\n"
                "---\n"
                f"{quest["description"]!r}\n"
                "---\n"
                "Only include the paraphrased text and nothing else.\n"
            ) for quest in quests
        ]

        res = await asyncio.gather(*messages)

        return [
            {
                "quest_id": quest["quest_id"],
                "response": message.content,
            }
            for quest, message in zip(quests, res)
        ]

    def generate_ending(self, npc_infos: list[dict[str, Any]]):
        return self.model.invoke(
            "Generate an ending for the following game state:\n"
            f"{bson.json_util.dumps(npc_infos)}\n"
            "---\n"
            "Only include the ending text and nothing else.\n"
            "The ending should adequately reflect the game state and the choices made by the player. "
            "High affinitas value for an NPC means that the player has a good relationship with them. "
            "Low affinitas value means that the player has a bad relationship with them. "
            "How well the player interacted with the NPCs should be reflected in the ending. "
            "If the endings array is provided for an NPC, their ending is based on "
            "the ending descriptions in the array. Higher affinitas shall result in "
            "a better ending for the NPCs. If a quest is not marked `completed` in the game state, "
            "the NPC should not mention it positively in the ending. The may choose to skip the quest or "
            "mention it negatively. "
            "The endings should not necessarily be optimistic and should reflect the history and the affinitas "
            "values of the NPCs. The endings should be unique and not repeated. "
        )

    def _init_langsmith(self):
        client = Client(api_key=self.config.langsmith_api_key, api_url=self.config.langsmith_endpoint)
        tracer = LangChainTracer(client=client, project_name=self.config.langsmith_project)
        openai_client = wrap_openai(OpenAI(api_key=self.config.openai_api_key))
        self.model = self.model.with_config(callbacks=[tracer], client=openai_client)
