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
    public List<string> item_list;
    public List<LoadGameNpcData> npcs;
}

[Serializable]
public class LoadGameJournalData
{
    public List<object> quests;
    public List<LoadGameJournalNpcMeta> additionalProp2;
    public string town_info;
    public List<object> chat_history;
}

[Serializable]
public class LoadGameJournalNpcMeta
{
    public string npc_id;
    public string description;
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
        UuidRequest uuid = new UuidRequest { x_client_uuid = GameManager.Instance.playerId };
        LoadGameRootResponse rootResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<UuidRequest, LoadGameRootResponse>(uuid, "/session/new", HttpMethod.Get);
        Debug.Log("New game shadow_save_id: " +  rootResponse.shadow_save_id);
        return rootResponse;
    }

    public static async Task<LoadGameRootResponse> GetSavedGameInfo(string saveIdToLoad)
    {
        LoadSaveRequest loadSaveRequest = new() { save_id = saveIdToLoad };
        LoadGameRootResponse rootResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<LoadSaveRequest, LoadGameRootResponse>(loadSaveRequest, "/game/load", HttpMethod.Post);
        return rootResponse;
    }

    public static void InitializeGameInfo(LoadGameRootResponse rootResponse)
    {
        Debug.Log("root response: " + rootResponse);
        Debug.Log("root response.data: " + rootResponse.data);

        GameManager.Instance.shadowSaveId = rootResponse.shadow_save_id;

        MainGameManager.Instance.dayNo = rootResponse.data.day_no;
        MainGameManager.Instance.dailyActionPoints = rootResponse.data.remaining_ap;

        Debug.Log("Game Info");
        Debug.Log("action points: " + MainGameManager.Instance.dailyActionPoints);
        Debug.Log(MainGameManager.Instance.dayNo);

        List<LoadGameNpcData> npcDatas = rootResponse.data.npcs;
        MainGameManager.Instance.npcList = new(6);
        int i = 1;
        foreach (LoadGameNpcData npcData in npcDatas)
        {
            Npc newNpc = new()
            {
                dbNpcId = npcData.npc_id,
                npcId = i,
                npcName = npcData.name,
                affinitasValue = npcData.affinitas,
                chatHistory = npcData.chat_history
            };
            i++;



            //TODO: DELETE LATER
            if (npcData.chat_history == null)
                Debug.Log("chat history null ");
            else if (npcData.chat_history.Count > 0)
            {
                Debug.Log("chat history USER OR AI: " + npcData.chat_history[0][0]);
                Debug.Log("chat history CONTENT: " + npcData.chat_history[0][1]);
            }
            else
                Debug.Log("count: " + npcData.chat_history.Count.ToString());


            foreach (LoadGameQuestMeta questData in npcData.quests)
            {
                Quest newQuest = new()
                {
                    questId = questData.quest_id,
                    status = questData.status,
                    name = questData.name,
                    description = questData.description,
                    affinitasReward = questData.affinitas_reward
                };
                newNpc.questList.Add(newQuest);
                Debug.Log("quest name: " + newQuest.name + ", status: [" + newQuest.status + "]");
            }
            MainGameManager.Instance.npcList.Add(newNpc);

            Debug.Log("Npc no " + newNpc.npcId.ToString() + ": " + newNpc.npcName + " with Affinitas " + newNpc.affinitasValue.ToString());
        }

        // TODO: Initialize journal info
        // journal data from json LoadGameJournalNpc info and town info etc do it

        // TODO: Chat history code!

        // TODO: DELETE LATER!!!! Completes Bart Ender's quest 
        //MainGameManager.Instance.npcList[2].questList[0].status = "completed";

        return;

        
    }
}