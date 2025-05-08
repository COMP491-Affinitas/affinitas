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

    public async Task<string> EndGame()
    {
        EndingRequest endRequest = new EndingRequest { shadow_save_id = shadowSaveId };


        EndingResponse endResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<EndingRequest, EndingResponse>(endRequest, "/game/end", HttpMethod.Post);

        if (endResponse == null)
        {
            Debug.LogError("endResponse is null.");
            return null;
        }

        return endResponse.ending_text;
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

    public async Task<List<string>> CreateMessageForGetQuest(string dbNpcId, int npcId)
    {
        string url = $"/npcs/{dbNpcId}/quest";

        // Quest Request
        QuestRequest request = new QuestRequest
        {
            shadow_save_id = shadowSaveId
        };

        // Response returns quest list. Each quest has quest ID and quest description
        QuestListResponse questResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<QuestRequest, QuestListResponse>(
                request,url, HttpMethod.Post
        );

        if (questResponse == null)
            Debug.LogError("Server returned null response.");

        List<string> questDescriptions = new();
        // Match quest_id from get quest to update descriptions in Quest instances
        foreach (QuestEntry questEntry in questResponse.quests)
        {
            foreach (Quest quest in MainGameManager.Instance.npcList[npcId - 1].questList)
            {
                Debug.Log("Quest from server no: " + questEntry.quest_id + ", description: " + questEntry.response);
                Debug.Log("Quest from game no: " + quest.questId + ", description: " + quest.description);

                if (quest.questId.Equals(questEntry.quest_id))
                {
                    quest.description = questEntry.response;
                    quest.status = QuestStatus.InProgress;

                    questDescriptions.Add(quest.description);

                    Debug.Log("Quest no: " + quest.questId + ", name: " + quest.name + ", description: " + quest.description);
                }
            }            
        }
        MainGameManager.Instance.HandleGivenQuests(npcId);

        return questDescriptions;
    }

    public async Task<bool> CreateMessageForEndDay()
    {
        // Send message to all npcs to notify that day has ended
        string systemMessage = "A new day has begun.";

        ClientResponse message = new ClientResponse(
            role: "system",
            shadow_save_id: shadowSaveId,
            content: systemMessage
        );

        foreach (Npc npc in MainGameManager.Instance.npcList)
        {
            ServerResponse serverResponse = await ServerConnection.Instance
                .SendAndGetMessageFromServer<ClientResponse, ServerResponse>(
                    message,
                    $"/npcs/{npc.dbNpcId}/chat",
                    HttpMethod.Post
                );
        }

        return true;
    }
}
