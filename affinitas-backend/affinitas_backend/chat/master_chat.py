import asyncio
from typing import Any

import bson.json_util
from beanie import PydanticObjectId
from langchain.chat_models import init_chat_model
from pydantic import TypeAdapter

from affinitas_backend.chat.utils import NPC_DATA_TEMPLATE, pretty_quests, QUEST_PROMPT_TEMPLATE, \
    ENDING_PROMPT_TEMPLATE, with_tracing, get_npc_data
from affinitas_backend.config import Config
from affinitas_backend.models.chat.chat import NPCData


class MasterLLM:
    def __init__(self, config: Config):
        self.config = config
        self.model = init_chat_model(
            model=config.openai_model_name,
            model_provider="openai",
            api_key=config.openai_api_key,
        )

        if self.config.langsmith_tracing:
            self.model = with_tracing(self.model, self.config)

    async def get_quest_responses(self, quests: list[dict], shadow_save_id: PydanticObjectId,
                                  npc_id: PydanticObjectId) -> list[dict]:
        npc = await get_npc_data(
            shadow_save_id,
            npc_id,
            include_chat_history=False,
            include_static_data=True,
        )

        if not npc:
            raise ValueError(f"NPC with ID {npc_id} not found in shadow save {shadow_save_id}.")

        if self.config.env == "dev":
            npc_data_validator = TypeAdapter(NPCData)
            npc_data_validator.validate_python(npc, strict=True)

        affinitas_increase = npc["affinitas_config"]["increase"]
        affinitas_decrease = npc["affinitas_config"]["decrease"]

        messages = [
            self.model.ainvoke(
                QUEST_PROMPT_TEMPLATE.format(
                    npc_data=NPC_DATA_TEMPLATE.format(
                        name=npc["name"],
                        age=npc["age"],
                        occupation=npc.get("occupation", "Unknown"),
                        backstory=npc["backstory"],
                        personality=", ".join(npc["personality"]),
                        motivations=", ".join(npc["motivations"]),
                        likes=", ".join(npc["likes"] or ["Unspecified"]),
                        dislikes=", ".join(npc["dislikes"] or ["Unspecified"]),
                        dialogue_unlocks=", ".join(npc["dialogue_unlocks"]),
                        quests=pretty_quests(npc["quests"]),
                        affinitas=npc["affinitas"],
                        affinitas_up=isinstance(affinitas_increase, float) and f"{affinitas_increase:.2f}" or ", ".join(
                            affinitas_increase),
                        affinitas_down=isinstance(affinitas_decrease,
                                                  float) and f"{affinitas_decrease:.2f}" or ", ".join(
                            affinitas_decrease),
                    ),
                    quest_description=quest["description"]
                )
            ) for quest in quests if quest["description"] is not None
        ]

        res = await asyncio.gather(*messages)

        return [
            {
                "quest_id": quest["quest_id"],
                "response": message.content,
            }
            for quest, message in zip(quests, res)
        ]

    def generate_ending(self, npc_infos: list[dict[str, Any]], *, stream: bool = False):
        meth = self.model.invoke if not stream else self.model.astream
        return meth(
            ENDING_PROMPT_TEMPLATE.format(game_state=bson.json_util.dumps(npc_infos)),
            stream=stream
        )
