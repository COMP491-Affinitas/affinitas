using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // singleton
    public static GameManager Instance { get; private set; }

    ServerResponse serverResponse;

    [SerializeField]
    NpcUi[] npcUiList;

    public Dictionary<int, Npc> npcDict = new();

    public int dailyActionPoint;

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        InitializeNpcs();
    }

    void InitializeNpcs()
    {
        //TODO: Get Npc information from Unity connection
        //npcList = new Npc[3];
        //Npc currNpc;

        //for (int i = 0; i < npcList.Length; i++)
        //{
        //    // Random affinitas values for now
        //    currNpc = new Npc(i, npcNames[i], i * 10);
        //    string[] questList = { "Say hello to the world." };
        //    currNpc.AddQuestList(questList);

        //    npcUiList[i].InitializeNpc(currNpc);

        //    npcList[i] = currNpc;
        //}

        var alice = new Npc(1, "Alice", 10);
        var bob = new Npc(2, "Bob", 20);

        npcDict[alice.idNo] = alice;
        npcDict[bob.idNo] = bob;

    }

    public async void SendAndReceiveFromServer(string playerInput, bool requestQuest)
    {
        // Send player input message to server
        serverResponse = await ServerConnection.Instance.SendAndGetMessageFromServer(playerInput, requestQuest);

        if (serverResponse == null)
        {
            return; 
        }

        // TODO: Write code to add NPC dialogue box on screen
        // TODO: Update journal page with summary

        // Update everything
        Npc npc = npcDict[serverResponse.npcId];
        npc.affinitasValue = serverResponse.affinitasChange;

    }
}
