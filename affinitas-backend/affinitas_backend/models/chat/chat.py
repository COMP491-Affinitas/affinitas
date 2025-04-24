from typing import Annotated, Literal, TypedDict, Any

from langgraph.graph.message import MessagesState

from pydantic import BaseModel, Field


class NPCDataDelta(BaseModel):
    occupation: str | None = Field(None, description="The NPC's occupation")
    likes: list[str] = Field(default_factory=list, description="List of things the NPC likes")
    dislikes: list[str] = Field(default_factory=list, description="List of things the NPC dislikes")


class NPCChatResponse(BaseModel):
    response: str =  Field(..., description="Response from the NPC")
    affinitas_change: Literal["very positive", "positive", "neutral", "negative", "very negative"] = Field(
        "neutral",
        description="The NPC's evaluation of the player's input, categorized into one of the following categories: "
                    "`very positive`, `positive`, `neutral`, `negative`, `very negative`."
    )
    delta: NPCDataDelta = Field(..., description="Optional changes to the NPC's likes, dislikes and occupation. Occupation is only changed when the NPC is missing it.")


class NPCMessagesState(MessagesState):
    npc_json: str

    affinitas_change: str
    delta: NPCDataDelta
