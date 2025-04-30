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
    // public int npc_no;
    //public string npc_name;
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
    public static async Task GetLoadGameInfo(string uuid_string)
    {
        UuidRequest uuid = new UuidRequest { x_client_uuid = uuid_string };
        LoadGameRootResponse rootResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<UuidRequest, LoadGameRootResponse>(uuid, "/game/new", HttpMethod.Get);
        MainGameManager manager = MainGameManager.Instance;

        manager.dayNo = rootResponse.data.day_no;
        manager.dailyActionPoints = rootResponse.data.remaining_ap;


        Debug.Log("Game Info");
        Debug.Log(manager.dailyActionPoints);
        Debug.Log(manager.dayNo);

        List<LoadGameNpcData> npcDatas = rootResponse.data.npcs;
        int i = 1;
        foreach (LoadGameNpcData npcData in npcDatas)
        {
            Npc newNpc = new()
            {
                //npc_id = npcData.npc_id,
                npcId = i,
                npcName = "NPC wowowow" + i.ToString(),
                affinitasValue = npcData.affinitas
            };
            i++;

            foreach (LoadGameQuestMeta questData in npcData.quests)
            {
                Quest newQuest = new()
                {
                    name = questData.name,
                    description = questData.description,
                    started = questData.started,
                    status = questData.status
                };
                newNpc.questList.Add(newQuest);
            }

            manager.npcList.Add(newNpc);

            Debug.Log("hello " + newNpc.npcId.ToString() + newNpc.affinitasValue.ToString() + newNpc.npcName);
        }

        return;
       



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