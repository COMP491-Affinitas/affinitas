from typing import Literal, TypedDict

from langgraph.graph.message import MessagesState
from pydantic import BaseModel, Field, UUID4


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
    completed_quests: list[str] = Field(default_factory=list,
                                        description="List of completed quest IDs that are completed in the current turn")


class QuestState(TypedDict):
    status: Literal["pending", "active", "completed"]
    name: str
    description: str | None
    rewards: list[str]


class NPCState(TypedDict):
    affinitas: int
    likes: list[str]
    dislikes: list[str]
    occupation: str | None
    quests: list[QuestState]
    name: str
    age: int
    personality: list[str]
    motivations: list[str]
    backstory: str
    affinitas_config: TypedDict("NPCAffinitasConfig", {
        "initial": int,
        "increase": float | list[str],
        "decrease": float | list[str],
    })
    endings: list[str]
    dialogue_unlocks: list[str]


class NPCMessagesState(MessagesState):
    npc: NPCState
    invoke_model: bool
    completed_quests: list[str]


class ThreadInfo(BaseModel):
    chat_id: UUID4
    client_uuid: UUID4
