from pydantic import BaseModel, Field, field_validator


class NPCAffinitasMetadata(BaseModel):
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
    id: str
    name: str
    rewards: list[str] = Field(default_factory=list)  # This needs some rework


class QuestSaveData(Quest):
    started: bool
    status: str


class BaseNPC(BaseModel):
    id: str = Field(..., alias="_id")
    name: str
    age: int
    occupation: str | None = None
    personality: list[str] = Field(default_factory=list)
    likes: list[str] = Field(default_factory=list)
    dislikes: list[str] = Field(default_factory=list)
    motivations: list[str] = Field(default_factory=list)
    backstory: str
    minigame: str | None = None
    affinitas: NPCAffinitasMetadata
    global_influence: bool
    endings: list[str] = Field(default_factory=list)
    quests: list[Quest] = Field(default_factory=list)
    dialogue_unlocks: list[str] = Field(default_factory=list)


class NPCSaveData(BaseNPC):
    affinitas: int
    quests: list[QuestSaveData] = Field(default_factory=list)


class GameData(BaseModel):
    day_no: int
    remaining_ap: int
    journal_data: dict[str, list[str | dict[str, str]]] = Field(default_factory=dict)
    item_list: list[str] = Field(default_factory=list)
    npcs: list[NPCSaveData] = Field(default_factory=list)
