from typing import Literal, TypedDict

from beanie import PydanticObjectId
from langgraph.graph.message import MessagesState
from pydantic import BaseModel, Field


class NPCDataDelta(BaseModel):
    occupation: str | None = Field(None, description="The NPC's occupation")
    likes: list[str] = Field(default_factory=list, description="List of things the NPC likes")
    dislikes: list[str] = Field(default_factory=list, description="List of things the NPC dislikes")


class OpenAI_NPCChatResponse(BaseModel):
    response: str = Field(..., description="Response from the NPC")
    affinitas_change: Literal["very positive", "positive", "neutral", "negative", "very negative"] = Field(
        "neutral",
        description="The NPC's evaluation of the player's input, categorized into one of the following categories: "
                    "`very positive`, `positive`, `neutral`, `negative`, `very negative`."
    )
    delta: NPCDataDelta = Field(...,
                                description="Optional changes to the NPC's likes, dislikes and occupation. Occupation is only changed when the NPC is missing it.")


class QuestMeta(TypedDict):
    name: str
    description: str
    rewards: list[str]


class QuestState(TypedDict):
    quest_meta: QuestMeta
    started: bool
    status: str


class NPCState(TypedDict):
    affinitas: int
    quests: list[QuestState]
    npc_meta: TypedDict("BaseNPCState", {
        "name": str,
        "age": int,
        "occupation": str | None,
        "personality": list[str],
        "likes": list[str],
        "dislikes": list[str],
        "motivations": list[str],
        "backstory": str,
        "endings": list[str],
        "quests": list[QuestMeta],
        "affinitas_meta": TypedDict("NPCAffinitasMetadata", {
            "initial": int,
            "increase": float | list[str],
            "decrease": float | list[str],
        }),
        "dialogue_unlocks": list[str],
    })


class NPCMessagesState(MessagesState):
    npc: NPCState
    invoke_model: bool


class ThreadInfo(BaseModel):
    chat_id: PydanticObjectId
    client_uuid: str
