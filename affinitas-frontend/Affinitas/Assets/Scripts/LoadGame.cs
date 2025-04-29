using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class LoadGameData
{
    public int day_no;
    public int remaining_ap;
    public LoadGameJournalData journal_data;
    public List<string> item_list;
    public List<LoadGameNpc> npcs;
}

[Serializable]
public class LoadGameJournalData
{
    public List<object> additionalProp1;
    public List<object> additionalProp2;
    public List<object> additionalProp3;
}

[Serializable]
public class LoadGameNpc
{
    public int npc_name;
    public int affinitas;
    public List<LoadGameQuestStatus> quests;
    public LoadGameNpcMeta npc_meta;
    public List<object> chat_history;
}

[Serializable]
public class LoadGameQuestStatus
{
    public LoadGameQuestMeta quest_meta;
    public bool started;
    public string status;
}

[Serializable]
public class LoadGameQuestMeta
{
    public string _id;
    public string name;
    public string description;
    public List<string> rewards;
}

[Serializable]
public class LoadGameNpcMeta
{
    public string _id;
    public string name;
    public int age;
    public string occupation;
    public List<string> personality;
    public List<string> likes;
    public List<string> dislikes;
    public List<string> motivations;
    public string backstory;
    public string minigame;
    public LoadGameAffinitasMeta affinitas_meta;
    public List<string> endings;
    public List<LoadGameQuestMeta> quests;
    public List<string> dialogue_unlocks;
}

[Serializable]
public class LoadGameAffinitasMeta
{
    public int initial;
    public int increase;
    public int decrease;
}

public static class LoadGame
{
    //public static async void GetLoadGameInfo(string uuid_string)
    //{
    //    UuidRequest uuid = new UuidRequest { x_client_uuid = uuid_string };
    //    LoadGameRootResponse rootResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<UuidRequest, LoadGameRootResponse>(uuid, "/game/load");

    //    GameManager.Instance.dayNo = rootResponse.data.day_no;
    //    GameManager.Instance.dailyActionPoints = rootResponse.data.remaining_ap;

    //    foreach (var npcUiGameObject in GameManager.Instance.npcUiList)
    //    {
    //        npcUiGameObject.InitializeNpc(new Npc());
    //    }
        

        
    //}
}
