using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class LoadGameRootResponse
{
    public LoadGameData data;
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
    public string npc_name;
    public int affinitas;
    public List<LoadGameQuestMeta> quests; // first quest is the main quest
}

[Serializable]
public class LoadGameQuestMeta
{ 
    public string name;
    public int status; // 0 for incomplete, 1 for complete
    public string description;
    public string reward; 
}

public static class LoadGame
{
    public static async void GetLoadGameInfo(string uuid_string)
    {
        UuidRequest uuid = new UuidRequest { x_client_uuid = uuid_string };
        LoadGameRootResponse rootResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<UuidRequest, LoadGameRootResponse>(uuid, "/game/load");

        GameManager.Instance.dayNo = rootResponse.data.day_no;
        GameManager.Instance.dailyActionPoints = rootResponse.data.remaining_ap;

        var npcDatas = rootResponse.data.npcs;
        for (int i = 0; i < npcDatas.Count; i++)
        {
            LoadGameNpcData npcData = npcDatas[i];
            int npcId = i + 1;

            var questList = new List<Npc.Quest>();
            foreach (var questMeta in npcData.quests)
            {
                var quest = new Npc.Quest
                {
                    name = questMeta.name,
                    status = questMeta.status,
                    description = questMeta.description,
                    reward = questMeta.reward
                };
                questList.Add(quest);
            }

            var npc = new Npc(npcId, npcData.npc_name, npcData.affinitas, questList);

            GameManager.Instance.npcUiList[i].InitializeNpc(npc);
        }

        // TODO: Initialize journal info
        // TODO: Initialize item list 

    }
}