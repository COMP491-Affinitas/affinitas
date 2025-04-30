using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // singleton
    public static GameManager Instance { get; private set; }

    public string gameId;

    //ServerResponse serverResponse;

    public Dictionary<int, Npc> npcDict = new();
    public int dailyActionPoints;
    public int dayNo;

    public Dictionary<string, bool> dialoguesDict = new();
    public Dictionary<string, bool> questDict = new();

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
        GetAuthenticationUUID();
    }

    // Get New UUID from server
    public async void GetAuthenticationUUID()
    {
        UuidRequest uuid = new UuidRequest { x_client_uuid = "" };
        UuidResponse uuidResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<UuidRequest, UuidResponse>(uuid, "/auth/uuid");
        gameId = uuidResponse.uuid;
    }

    // Get game information from server
    public async void LoadGameWithUUID()
    {
        //LoadGame.GetLoadGameInfo(gameId);
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

        //var alice = new Npc(1, "Alice", 10);
        //var bob = new Npc(2, "Bob", 20);

        //npcDict[alice.npcId] = alice;
        //npcDict[bob.npcId] = bob;

    }

    void InitializeInteractionDicts()
    {
        foreach (Npc npc in npcDict.Values)
        {
            dialoguesDict[npc.npcName] = false;
            questDict[npc.npcName] = false;
        }
    }

    // TODO: This info should come from server
    void InitializeGame()
    {
        dailyActionPoints = 15;
        dayNo = 1;
    }

    public async void SendAndReceiveFromServer(ClientResponse message, string directory)
    {
        // Send player input message to server
        ServerResponse serverResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<ClientResponse, ServerResponse>(message, directory);

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

    //Call when End Day button is pressed
    public void EndDay()
    {
        dayNo += 1;
        dailyActionPoints = 15;
    }

    // calculate how mnay actşon points left
    void CalculateActionPoints()
    {
        int actionPointsUsed = 0;

    }

    // If not enough actions points left, do not let player do things
    void CheckActionPoints()
    {

    }

}
