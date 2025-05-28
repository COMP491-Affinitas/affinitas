using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

[Serializable]
public class LoadGameRootResponse
{
    public LoadGameData data;
    public string shadow_save_id; 
}

[Serializable]
public class LoadGameData
{
    public int day_no;
    public int remaining_ap;
    public LoadGameJournalData journal_data;
    public bool journal_active;
    public List<LoadGameItemMeta> item_list;
    public List<LoadGameNpcData> npcs;
}

[Serializable]
public class LoadGameItemMeta
{
    public string name;
    public bool active;
}

[Serializable]
public class LoadGameJournalData
{
    public List<object> quests;
    public List<LoadGameJournalNpcMeta> npcs;
    public LoadGameJournalTownInfoMeta town_info;
    public List<object> chat_history;
}

[Serializable]
public class LoadGameJournalNpcMeta
{
    public string npc_id;
    public string description;
    public bool active;
}

[Serializable]
public class LoadGameJournalTownInfoMeta
{
    public string description;
    public bool active;
}

[Serializable]
public class LoadGameNpcData
{
    public string npc_id;
    public string name;
    public int affinitas;
    public List<LoadGameQuestMeta> quests; // first quest is the main quest
    public List<List<string>> chat_history;
}

[Serializable]
public class LoadGameQuestMeta
{
    public string quest_id;
    public string status;
    public string name;
    public string description;
    public int affinitas_reward;
}

public static class LoadGame
{
    public static async Task<LoadGameRootResponse> GetNewGameInfo()
    {
        UuidRequest uuid = new()
        {
            x_client_uuid = GameManager.Instance.playerId
        };

        LoadGameRootResponse rootResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<UuidRequest, LoadGameRootResponse>(
                uuid,
                "/session/new",
                HttpMethod.Get
            );
        Debug.Log("New game root response: " + rootResponse);
        Debug.Log("New game shadow_save_id: " +  rootResponse.shadow_save_id);
        return rootResponse;
    }

    public static async Task<LoadGameRootResponse> GetSavedGameInfo(string saveIdToLoad)
    {
        LoadSaveRequest loadSaveRequest = new()
        {
            save_id = saveIdToLoad
        };

        LoadGameRootResponse rootResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<LoadSaveRequest, LoadGameRootResponse>(
                loadSaveRequest,
                "/game/load",
                HttpMethod.Post
            );

        return rootResponse;
    }

    public static void InitializeGameInfo(LoadGameRootResponse rootResponse)
    {
        GameManager.Instance.shadowSaveId = rootResponse.shadow_save_id;

        MainGameManager.Instance.dayNo = rootResponse.data.day_no;
        MainGameManager.Instance.dailyActionPoints = rootResponse.data.remaining_ap;
        MainGameManager.Instance.journalActive = rootResponse.data.journal_active;
        MainGameManager.Instance.journalTownInfo = rootResponse.data.journal_data.town_info.description;

        Debug.Log("journal town info: " + rootResponse.data.journal_data.town_info);
        Debug.Log("Game Info");
        Debug.Log("action points: " + MainGameManager.Instance.dailyActionPoints);
        Debug.Log("day no:" + MainGameManager.Instance.dayNo);

        List<LoadGameNpcData> npcDatas = rootResponse.data.npcs;
        MainGameManager.Instance.npcDict = new();
        int npcIdInGame = 0;
        foreach (LoadGameNpcData npcData in npcDatas)
        {
            npcIdInGame++;
            Npc newNpc = new()
            {
                npcId = npcIdInGame,
                dbNpcId = npcData.npc_id,
                npcName = npcData.name,
                affinitasValue = npcData.affinitas,
                chatHistory = npcData.chat_history
            };

            Debug.Log("npc infos: " + rootResponse.data.journal_data.npcs);
            if (rootResponse.data.journal_data.npcs != null)
            {
                Debug.Log("npc infos not null");
                foreach (LoadGameJournalNpcMeta npcInfo in rootResponse.data.journal_data.npcs)
                {
                    if (npcInfo.npc_id.Equals(newNpc.dbNpcId))
                    {
                        newNpc.description = npcInfo.description;
                        Debug.Log("npc matched description: " + newNpc.description);
                    }
                    Debug.Log("npc description: " + npcInfo.description);
                }
            }

            foreach (LoadGameQuestMeta questData in npcData.quests)
            {
                Quest newQuest = new()
                {
                    linkedNpcId = npcIdInGame,
                    questId = questData.quest_id,
                    name = questData.name,
                    description = questData.description,
                    status = questData.status,
                    affinitasReward = questData.affinitas_reward,
                    item = null
                };
                newNpc.questList.Add(newQuest);
                MainGameManager.Instance.questDict[newQuest.questId] = newQuest;

                Debug.Log("quest name: " + newQuest.name + ", status: [" + newQuest.status + "]");
            }
            MainGameManager.Instance.npcDict[npcIdInGame] = newNpc;

            Debug.Log("Npc no " + newNpc.npcId.ToString() + ": " + newNpc.npcName + " with Affinitas " + newNpc.affinitasValue.ToString());
        }

        Debug.Log("item_list.Count: " + rootResponse.data.item_list.Count);

        int questIndex = 1;
        foreach (LoadGameItemMeta itemInfo in rootResponse.data.item_list)
        {
            Item newItem = new()
            {
                itemName = itemInfo.name,
                active = itemInfo.active
            };
            MainGameManager.Instance.itemDict[newItem.itemName] = newItem;

            Debug.Log("item name: " + newItem.itemName + ", active:" + newItem.active);

            Quest linkedQuest = null;
            // Match gus fish to gus quest
            if (newItem.itemName.Equals("gus_fish"))
                linkedQuest = MainGameManager.Instance.npcDict[2].questList[0];
            // Match mora pieces to mora subquests
            else
            {
                linkedQuest = MainGameManager.Instance.npcDict[1].questList[questIndex];
                questIndex++;
            }
            if (linkedQuest != null)
            {
                newItem.linkedQuestId = linkedQuest.questId;
                linkedQuest.item = newItem;
            }    
        }
        return;        
    }
}