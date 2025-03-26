from pymongo import MongoClient
from datetime import datetime

# DB setup ??
client = MongoClient("mongodb://localhost:27017/")
db = client["affinitas"]

# Insert Characters only once 
characters = [

    {
        "_id": "mora_lysa",
        "name": "Mora Lysa",
        "age": 34,
        "occupation": ["Muralist", "painter", "revolutionary artist"],
        "personality": ["passionate", "radical", "feminist", "revolutionist", "sharp-witted", "intellectual", "rebellious"],
        "likes": ["deep conversations", "honest people", "philosophical debates", "art", "people asking philosophical questions"],
        "dislikes": ["elitists", "materialists", "authority lovers", "rude people", "people who ask her to paint something"],
        "motivations": "make people love art",
        "affinitas": {
            "start": "neutral",
            "change": {
            "increase": "hard",
            "decrease": "easy"
            }
        },
        "backstory": " Mora Lysa was once a respected painter in the royal palace, creating murals for the nobility. But she refused to paint empty praises—she used her art to tell the truth, hiding symbols of oppression and lost rebellions in her work. One day, she painted a forbidden mural that exposed the corruption of the rulers. When they discovered its meaning, she was banished, her masterpiece destroyed, and her name erased from court records. Now, she lives in a small town, painting for the people instead of kings. Even in exile, she believes art can change the world.",
        "quests": ["reconstruct_painting"],
        "extras": {
            "reward_on_completion": True,
            "painting_pieces_hidden": True,
            "affinitas_boost_on_completion": True
        }
    },
    {
        "_id": "gus_tider",
        "name": "Gus Tider",
        "age": 54,
        "occupation": "Fisherman",
        "personality": ["cold to strangers", "kind inside", "blunt", "quiet", "gruff", "stubborn", "patient"],
        "likes": ["the sea", "fishing", "his wife Mira", "silence", "sunrise", "cats"],
        "dislikes": ["strangers", "overly familiar people", "loudmouths", "show-offs", "impatience"],
        "motivations": ["preserve traditional fishing", "pass knowledge to someone"],
        "affinitas": {
            "start": "low",
            "change": {
            "increase": "moderate",
            "decrease": "moderate"
            }
        },
        "backstory": "He was born in the town and spent his entire life near the sea. He learned how to fish from his father and met his wife while she was collecting clams on the beach. They lived happily in their little house near the beach but it weighed on them both that they were never able to have children. Spends his days fishing on the beach and watching his wife try to preserve the view with amateur sketches.",
        "quests": ["fishing_minigame", "gift_for_mira"],
        "extras": {
            "minigame": "fishing",
            "bonus_affinitas_if_painting_given": True
        }
    },
    {
        "_id": "bart_ender",
        "name": "Bart Ender",
        "age": 37,
        "occupation": ["bartender", "Tavern Owner"],
        "personality": ["smart", "wise", "talkative", "neutral", "secretive", "observant", "Gives good advice", "Hard to earn his trust", "Stays Neutral to events", "Does not take sides", "Knows everyone a lot"],
        "likes": ["drinking", "chatting", "Order and Peace", "Playing Dart really good at it people call him the_Dart_Ender", "Staying out of trouble"],
        "dislikes": ["chaos", "liars", "disloyalty", "Disloyalty"],
        "motivations": ["maintain order", "not take sides"],
        "affinitas": {
            "start": "neutral",
            "change": {
            "increase": "hard",
            "decrease": "easy"
            },
            "global_influence": True
        },
        "backstory": "Ender family has been running the main Tavern/Inn of the town for many generations. Everyone in town tries not to get on bad terms with them. They are well respected. Bart Ender is the current owner of the tavern. He knows a lot about everyone but not many people know a lot about his personal life, and no one dares asking.",
        "quests": [],
        "extras": {
            "shop": True,
            "minigame": "dart",
            "dialogue_unlocks": ["town_history", "npc_tips", "quest_advice"],
            "nickname": "Dart Ender"
        }
    },
    {
        "_id": "jonathan_tainly",
        "name": "Sir Jonathan Tainly",
        "age": 23,
        "occupation": "Unknown",
        "personality": ["indecisive"],
        "likes": ["mind games", ""],
        "dislikes": ["slow thinkers", "opinited people"],
        "motivations": ["discover self", "choose destiny"],
        "affinitas": {
            "start": "high",
            "change": {
            "increase": "easy",
            "decrease": "easy"
            }
        },
        "backstory": "Sir Jonathan Tainly is the youngest of the royal Tainly family. They are known for their iron will, solid leadership and decisiveness. However, Jonathan is different from the other Tainlys. He finds it hard to decide on things. He agrees with anything, and he is easy to manipulate. Because of this, he has been abandoned by his family to this town. Many people love Jonathan and they try to protect him by not letting him get manipulated by the others. But some people say he is a shame to this town just like he is to his family. Will he be able to find his true personality and uncover his destiny?",
        "quests": ["self_discovery"],
        "extras": {
            "dynamic_personality": True,
            "branching_endings": [
            "becomes noble knight",
            "settles with town friends",
            "seeks revenge against family"
            ]
        }
    },
    {
        "_id": "al_zaymar",
        "name": "al Zaymar",
        "age": 58,
        "occupation": "Wandering Salesman",
        "personality": ["memory loss", "friendly (maybe)"],
        "likes": [],
        "dislikes": [],
        "motivations": ["find home", "remember destination"],
        "affinitas": {
            "start": "normal",
            "change": {
            "increase": "normal",
            "decrease": "normal"
            }
        },
        "backstory": " al Zaymar was a wandering traveller headed to his destination from his hometown. Something must have happened on the way so he is missing his memories of his home now. He also does not remember where he was heading towards. When he first came to town, people thought he was crazy. But after they saw that he had memory loss, they helped him settle in.",
        "quests": ["memory_path"],
        "extras": {
            "minigame": "memory_match",
            "branching_endings": [
            "returns to home",
            "stays in town",
            "completes forgotten journey"
            ]
        }
    },
    {
        "_id": "cher_lock",
        "name": "Cher Lock",
        "age": 38,
        "occupation": "Investigator (former police)",
        "personality": ["analytical", "sarcastic", "moral", "emotionally distant", "observant"],
        "likes": ["mysteries", "books", "puzzles", "coffee", "critical thinkers"],
        "dislikes": ["liars", "small talk", "bureaucracy", "being interrupted", "people who don’t think critically"],
        "motivations": ["expose corruption", "uncover truth"],
        "affinitas": {
            "start": "neutral",
            "change": {
            "increase_conditions": ["provide useful info", "logical dialogue"],
            "decrease_conditions": ["waste time", "baseless claims"]
            }
        },
        "backstory": "Cher Lock was a respected detective in the town's police force. She was known for solving cases others couldn’t. Her pursuit of justice made her hero to some and a threat to others. After she took on a major case to expose corruption within the police force, she got suspended, slandered and stripped of her badge. Now, she operates independently in a quiet town, taking on cases no one else will touch. Even though it was painful, she never gave up on her belief that truth had to come out. Cher spends most of their days in an old office filled with case files, scribbled notes, and cups of coffee. Despite their isolation, she is always watching, always listening. If someone in town has a secret, chances are, Cher already knows.",
        "quests": ["solve_the_town_mystery"],
        "extras": {
            "riddle_rewards": True,
            "coded_messages": True,
            "final_theory_unlockable": True
        }
    }
]


# Dynamic insert

if db.characters.count_documents({}) == 0:
    db.characters.insert_many(characters)
    print("Characters inserted into MongoDB!")
else:
    print("Characters already exist in the database.")

# Log interaction with full LLM context
def log_interaction(player_id, npc_id, day, interaction_type, player_input, npc_response, affinitas_change=0, quest_triggered=None):
    interaction = {
        "player_id": player_id,
        "npc_id": npc_id,
        "day": day,
        "interaction_type": interaction_type,
        "player_input": player_input,
        "npc_response": npc_response,
        "affinitas_change": affinitas_change,
        "quest_triggered": quest_triggered,
        "timestamp": datetime.utcnow()
    }
    db.player_npc_interactions.insert_one(interaction)



# update memory summary per NPC
def update_memory_summary(player_id, npc_id, summary_text):
    summary = {
        "player_id": player_id,
        "npc_id": npc_id,
        "summary": summary_text,
        "last_updated": datetime.utcnow()
    }
    db.memory_summaries.update_one(
        {"player_id": player_id, "npc_id": npc_id},
        {"$set": summary},
        upsert=True
    )



# current game state
def update_game_state(player_id, player_name, current_day, days_remaining, inventory,
                      visited_npcs, affinitas_scores, quest_progress, ending_unlocked=None):
    state = {
        "_id": player_id,
        "player_name": player_name,
        "current_day": current_day,
        "days_remaining": days_remaining,
        "inventory": inventory,
        "visited_npcs": visited_npcs,
        "affinitas_scores": affinitas_scores,  
        "quest_progress": quest_progress, 
        "ending_unlocked": ending_unlocked,
        "last_saved": datetime.utcnow().isoformat()
    }
    db.game_state.update_one(
        {"_id": player_id},
        {"$set": state},
        upsert=True
    )

def insert_quest(quest_id, name, npc_id, description, quest_type, is_main_quest,
                 affinitas_reward=0, item_reward=None, status_effect=None):
    quest = {
        "_id": quest_id,
        "name": name,
        "npc_id": npc_id,
        "description": description,
        "type": quest_type,
        "affinitas_reward": affinitas_reward,
        "item_reward": item_reward,
        "status_effect": status_effect,
        "is_main_quest": is_main_quest,
    }

    db.quests.update_one(
        {"_id": quest_id},
        {"$set": quest},
        upsert=True
    )
