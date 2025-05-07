using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }
    
    public event Action OnNpcDataLoaded;
    private bool npcDataReady = false;

    public string gameId;
    public string shadowSaveId;
    //ServerResponse serverResponse;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await GetAuthenticationUUID();
        await LoadGameWithUUID();
    }

    // Get New UUID from server
    public async Task GetAuthenticationUUID()
    {
        UuidRequest uuid = new UuidRequest { x_client_uuid = "" };
        UuidResponse uuidResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<UuidRequest, UuidResponse>(uuid, "/auth/uuid", HttpMethod.Post);
        gameId = uuidResponse.uuid;
        Debug.Log(gameId);
    }

    // Get game information from server
    public async Task LoadGameWithUUID()
    {
        await LoadGame.GetLoadGameInfo(gameId);
        MainGameManager.Instance.InitializeNpcsUisAndVariables();
        npcDataReady = true;
        OnNpcDataLoaded?.Invoke();
    }

    public void SubscribeToNpcDataLoaded(Action listener)
    {
        if (npcDataReady)
            listener?.Invoke();
        else
            OnNpcDataLoaded += listener;
    }


    public async Task<string> CreateMessageForSendPlayerInput(string playerInput, string dbNpcId)
    {
        string url = $"/npcs/{dbNpcId}/chat";   
         
        ClientResponse message = new ClientResponse(
            role: "user",
            shadow_save_id: shadowSaveId,
            content: playerInput
        );

        
        ServerResponse serverResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<ClientResponse, ServerResponse>(
                message, url, HttpMethod.Post
            );
        

        if (serverResponse == null)
        {
            Debug.LogError("Server returned null response.");
            return null; 
        }

        // Update NPC data 
        Npc npc = MainGameManager.Instance.npcList.Find(n => n.dbNpcId == dbNpcId);

        if (npc != null)
        {
            int oldAffinitas = npc.affinitasValue;
            npc.affinitasValue = serverResponse.affinitas_new;
            npc.dialogueSummary.Add(serverResponse.response);

            if (oldAffinitas != npc.affinitasValue)
            {
                MainGame.MainGameUiManager.Instance.UpdateNpcAffinitasUi(npc);
                Debug.Log("old affinitas was:" + oldAffinitas + "updated affinitas is: " + npc.affinitasValue);
            }
        }

        Debug.Log(serverResponse.response);
        return serverResponse.response; 
    }

    //public void InitializeNpc(string ncp_id) // int npcId
    //{
    //    // npcId should be int



    //    //TODO: Get Npc information from Unity connection
    //    //npcList = new Npc[3];
    //    //Npc currNpc;

    //    //for (int i = 0; i < npcList.Length; i++)
    //    //{
    //    //    // Random affinitas values for now
    //    //    currNpc = new Npc(i, npcNames[i], i * 10);
    //    //    string[] questList = { "Say hello to the world." };
    //    //    currNpc.AddQuestList(questList);

    //    //    npcUiList[i].InitializeNpc(currNpc);

    //    //    npcList[i] = currNpc;
    //    //}


    //    //var alice = new Npc(1, "Alice", 10, new List<Npc.Quest>());
    //    //var bob = new Npc(2, "Bob", 20, new List<Npc.Quest>());


    //    //npcDict[alice.npcId] = alice;
    //    //npcDict[bob.npcId] = bob;

    //}

    //void InitializeInteractionDicts()
    //{
    //    foreach (Npc npc in npcDict.Values)
    //    {
    //        dialoguesDict[npc.npcName] = false;
    //        questDict[npc.npcName] = false;
    //    }
    //}

    //// TODO: This info should come from server
    //void InitializeGame()
    //{
    //    dailyActionPoints = 15;
    //    dayNo = 1;
    //}

    //public async void SendAndReceiveFromServer(ClientResponse message, string directory)
    //{
    //    // Send player input message to server
    //    ServerResponse serverResponse = await ServerConnection.Instance.SendAndGetMessageFromServer<ClientResponse, ServerResponse>(message, directory);

    //    if (serverResponse == null)
    //    {
    //        return;
    //    }

    //    // TODO: Write code to add NPC dialogue box on screen
    //    // TODO: Update journal page with summary

    //    // Update everything
    //    Npc npc = npcDict[serverResponse.npcId];
    //    npc.affinitasValue = serverResponse.affinitasChange;

    //}

    



}
