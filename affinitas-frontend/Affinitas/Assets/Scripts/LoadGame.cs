using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Http;

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
    public List<string> item_list;
    public List<LoadGameNpcData> npcs;
}

[Serializable]
public class LoadGameJournalData
{
    public List<object> additionalProp1;
    public List<object> additionalProp2;
    public List<object> additionalProp3;
}

[Serializable]
public class LoadGameNpcData
{
    public string npc_id;
    public int affinitas;
    public List<LoadGameQuestMeta> quests; // first quest is the main quest
    public List<string> chat_history;
}

[Serializable]
public class LoadGameQuestMeta
{ 
    public string quest_id;
    public string name;
    public string description;
    public List<string> rewards;
    public bool started;
    public string status;
}

public static class LoadGame
{
    public static async void GetLoadGameInfo(string uuid_string)
    {
        UuidRequest uuid = new UuidRequest { x_client_uuid = uuid_string };
        LoadGameRootResponse rootResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<UuidRequest, LoadGameRootResponse>(uuid, "/game/new", HttpMethod.Get);

        GameManager.Instance.dayNo = rootResponse.data.day_no;
        GameManager.Instance.dailyActionPoints = rootResponse.data.remaining_ap;
        Debug.Log("Game Info");
        Debug.Log(GameManager.Instance.dailyActionPoints);
        Debug.Log(GameManager.Instance.dayNo);
        Debug.Log(rootResponse.data.npcs[0]);

        var npcDatas = rootResponse.data.npcs;
        // for (int i = 0; i < npcDatas.Count; i++)
        // {
        //     LoadGameNpcData npcData = npcDatas[i];
        //     int npcId = i + 1;

        //     var questList = new List<Npc.Quest>();
        //     foreach (var questMeta in npcData.quests)
        //     {
        //         var quest = new Npc.Quest
        //         {
        //             name = questMeta.name,
        //             status = questMeta.status,
        //             description = questMeta.description,
        //             reward = questMeta.reward
        //         };
        //         questList.Add(quest);
        //     }

        //     var npc = new Npc(npcId, npcData.npc_name, npcData.affinitas, questList);

        //     GameManager.Instance.npcUiList[i].InitializeNpc(npc);
        // }

        // TODO: Initialize journal info
        // TODO: Initialize item list 

    }
}