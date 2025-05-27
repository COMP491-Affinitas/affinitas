from beanie import Document, PydanticObjectId
from pydantic import Field, BaseModel, field_validator


class NPCAffinitasConfig(BaseModel):
    initial: int
    increase: float | list[str] = Field(
        ...,
        description="Affinitas increase factor or a list of positive triggers. "
                    "If a list is provided, the phrases in the list are used as "
                    "triggers for increasing affinitas. Otherwise, "
                    "values closer to 1 make it easier to gain affinity; "
                    "values closer to 0 make it harder; 0.5 is neutral."
    )
    decrease: float | list[str] = Field(
        ...,
        description="Affinitas decrease factor or a list of negative triggers. "
                    "If a list is provided, the phrases in the list are used as "
                    "triggers for decreasing affinitas. Otherwise, "
                    "values closer to 1 make it easier to lose affinity; "
                    "values closer to 0 make it harder; 0.5 is neutral."
    )

    @field_validator("increase")  # noqa: Again, this is not a bug with the code but with the linter
    @classmethod
    def validate_increase(cls, value):
        if isinstance(value, float):
            if not (0.0 <= value <= 1.0):
                raise ValueError("increase must be between 0 and 1 if given as a float")
        return value

    @field_validator("decrease")  # noqa
    @classmethod
    def validate_decrease(cls, value):
        if isinstance(value, float):
            if not (0.0 <= value <= 1.0):
                raise ValueError("decrease must be between 0 and 1 if given as a float")
        return value


class Quest(BaseModel):
    id: PydanticObjectId = Field(..., alias="_id")
    name: str
    description: str | None
    affinitas_reward: int
    linked_npc: PydanticObjectId | None = None
    triggers: list[str] = Field(default_factory=list)


class NPC(Document):
    id: PydanticObjectId = Field(..., alias="_id")
    name: str
    age: int
    occupation: str | None = None
    personality: list[str] = Field(default_factory=list)
    likes: list[str] = Field(default_factory=list)
    dislikes: list[str] = Field(default_factory=list)
    motivations: list[str] = Field(default_factory=list)
    backstory: str
    affinitas_config: NPCAffinitasConfig
    endings: list[str] = Field(default_factory=list)
    quests: list[Quest] = Field(default_factory=list)
    dialogue_unlocks: list[str] = Field(default_factory=list)
    order_no: int

    class Settings:
        name = "npcs"
