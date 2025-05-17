from typing import Any


def get_aggregate_pipeline(match: dict[str, Any], ):
    """
    Returns the aggregation pipeline for the game save data.
    The pipeline is used to transform the save data stored in ShadowSave, DefaultSave, and Save documents.
    The dynamic npc data (likes, dislikes, and occupation) is embedded into the npc data, and the static quest data
    stored in npcs.quests is embedded into the quest save data.
    :param match: Match stage to filter the save data. The match stage is used to filter the save data by ID.
    :return: The aggregation pipeline for fetching save data.
    """
    return [
        {"$match": match},
        {"$lookup": {
            "from": "npcs",
            "localField": "npcs.npc_id",
            "foreignField": "_id",
            "as": "npc_configs"
        }},
        {"$set": {
            "npcs": {
                "$map": {
                    "input": "$npcs",
                    "as": "npc_save",
                    "in": {
                        "$let": {
                            "vars": {
                                "npc_config": {
                                    "$arrayElemAt": [
                                        {
                                            "$filter": {
                                                "input": "$npc_configs",
                                                "as": "cfg",
                                                "cond": {
                                                    "$eq": ["$$cfg._id", "$$npc_save.npc_id"]
                                                }
                                            }
                                        },
                                        0
                                    ]
                                }
                            },
                            "in": {
                                "$mergeObjects": [
                                    "$$npc_save",
                                    {
                                        "name": "$$npc_config.name",
                                        "affinitas": "$$npc_config.affinitas",
                                        "likes": "$$npc_config.likes",
                                        "dislikes": "$$npc_config.dislikes",
                                        "occupation": "$$npc_config.occupation",
                                        "order_no": "$$npc_config.order_no",
                                        "quests": {
                                            "$map": {
                                                "input": "$$npc_save.quests",
                                                "as": "quest_save",
                                                "in": {
                                                    "$let": {
                                                        "vars": {
                                                            "quest_config": {
                                                                "$arrayElemAt": [
                                                                    {
                                                                        "$filter": {
                                                                            "input": "$$npc_config.quests",
                                                                            "as": "qcfg",
                                                                            "cond": {
                                                                                "$eq": ["$$qcfg._id",
                                                                                        "$$quest_save.quest_id"]
                                                                            }
                                                                        }
                                                                    },
                                                                    0
                                                                ]
                                                            }
                                                        },
                                                        "in": {
                                                            "$mergeObjects": [
                                                                "$$quest_save",
                                                                {
                                                                    "name": "$$quest_config.name",
                                                                    "description": "$$quest_config.description",
                                                                    "rewards": "$$quest_config.rewards"
                                                                }
                                                            ]
                                                        }
                                                    }
                                                }
                                            }
                                        },
                                        # "endings": "$$npc_config.dialogue_unlocks",
                                    }
                                ]
                            }
                        }
                    }
                }
            }
        }},
        {"$set": {
            "npcs": {
                "$sortArray": {
                    "input": "$npcs",
                    "sortBy": {
                        "order_no": 1
                    }
                }
            }
        }},
        {"$unset": ["npc_configs", "_id", "npcs.order_no"]}
    ]
