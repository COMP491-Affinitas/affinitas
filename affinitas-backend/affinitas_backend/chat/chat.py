from affinitas_backend.chat.master_chat import MasterLLM
from affinitas_backend.chat.npc_chat import NPCChatService
from affinitas_backend.config import Config

config = Config()

npc_chat_service = NPCChatService(config=config)
master_llm_service = MasterLLM(config=config)
