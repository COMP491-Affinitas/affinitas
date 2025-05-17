from typing import Any

from beanie import PydanticObjectId

from affinitas_backend.models.beanie.save import ShadowSave


def get_save_pipeline(match: dict[str, Any]):
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
                                                                    "affinitas_reward": "$$quest_config.affinitas_reward"
                                                                }
                                                            ]
                                                        }
                                                    }
                                                }
                                            }
                                        },
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


def get_npc_quests_pipeline(npc_id: PydanticObjectId, shadow_save_id: PydanticObjectId):
    return [
        {"$match": {
            "_id": shadow_save_id,
            "npcs.npc_id": npc_id
        }},
        {"$lookup": {
            "from": "npcs",
            "let": {"targetNpc": npc_id},
            "pipeline": [
                {"$match": {
                    "$expr": {"$eq": ["$_id", "$$targetNpc"]}
                }}
            ],
            "as": "npc_config"
        }},
        {"$set": {
            "npcs": {"$map": {
                "input": {
                    "$filter": {
                        "input": "$npcs",
                        "as": "ns",
                        "cond": {"$eq": ["$$ns.npc_id", npc_id]}
                    }
                },
                "as": "ns",
                "in": {"$let": {
                    "vars": {"cfg": {"$arrayElemAt": ["$npc_config", 0]}},
                    "in": {"$mergeObjects": [
                        "$$ns",
                        {
                            "quests": {"$map": {
                                "input": "$$ns.quests",
                                "as": "qs",
                                "in": {"$mergeObjects": [
                                    "$$qs",
                                    {"$let": {
                                        "vars": {
                                            "qcfg": {"$arrayElemAt": [
                                                {"$filter": {
                                                    "input": "$$cfg.quests",
                                                    "as": "c",
                                                    "cond": {"$eq": ["$$c._id",
                                                                     "$$qs.quest_id"]}
                                                }
                                                },
                                                0
                                            ]
                                            }
                                        },
                                        "in": {
                                            "description": "$$qcfg.description",
                                            "name": "$$qcfg.name",
                                            "linked_npc": "$$qcfg.linked_npc",
                                            "triggers": "$$qcfg.triggers"
                                        }
                                    }}
                                ]}
                            }}
                        }
                    ]}
                }}
            }}
        }},
        {"$project": {
            "_id": 0,
            "quests": {"$arrayElemAt": ["$npcs.quests", 0]}
        }}
    ]


def get_shadow_save_npc_state(shadow_save_id: PydanticObjectId, npc_id: PydanticObjectId):
    return ShadowSave.aggregate([
        {'$match': {
            '_id': shadow_save_id
        }},
        {'$unwind': '$npcs'},
        {'$match': {
            'npcs.npc_id': npc_id
        }},
        {'$lookup': {
            'from': 'npcs',
            'localField': 'npcs.npc_id',
            'foreignField': '_id',
            'as': 'npc_config'
        }},
        {'$unwind': '$npc_config'},
        {'$replaceRoot': {
            'newRoot': {
                '$mergeObjects': [
                    '$npcs', {
                        'name': '$npc_config.name',
                        'age': '$npc_config.age',
                        'occupation': '$npcs.occupation',
                        'personality': '$npc_config.personality',
                        'likes': '$npcs.likes',
                        'dislikes': '$npcs.dislikes',
                        'motivations': '$npc_config.motivations',
                        'backstory': '$npc_config.backstory',
                        'endings': '$npc_config.endings',
                        'dialogue_unlocks': '$npc_config.dialogue_unlocks',
                        'affinitas_config': '$npc_config.affinitas_config',
                        'affinitas': '$npcs.affinitas',
                        'chat_history': '$npcs.chat_history',
                        'completed_quests': '$npcs.completed_quests',
                        'quests': {
                            '$map': {
                                'input': '$npcs.quests',
                                'as': 'quest_save',
                                'in': {
                                    '$let': {
                                        'vars': {
                                            'quest_config': {
                                                '$arrayElemAt': [
                                                    {
                                                        '$filter': {
                                                            'input': '$npc_config.quests',
                                                            'as': 'qcfg',
                                                            'cond': {
                                                                '$eq': [
                                                                    '$$qcfg._id', '$$quest_save.quest_id'
                                                                ]
                                                            }
                                                        }
                                                    }, 0
                                                ]
                                            }
                                        },
                                        'in': {
                                            '$mergeObjects': [
                                                '$$quest_save', {
                                                    'name': '$$quest_config.name',
                                                    'description': '$$quest_config.description',
                                                    'affinitas_reward': '$$quest_config.affinitas_reward'
                                                }
                                            ]
                                        }
                                    }
                                }
                            }
                        }
                    }
                ]
            }
        }
        },
        {'$project': {
            'npc_id': 0,
            'quests.quest_id': 0
        }}]).to_list()
