using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Linq;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    public string playerId;
    public string shadowSaveId;

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
        //PlayerPrefs.SetString("player_id", ""); //TODO: COMMENT OUT WHEN BUILDING GAME

        playerId = PlayerPrefs.GetString("player_id");
        Debug.Log("Current player id: " + playerId);
        await GetAuthenticationUUID();
    }

    [ContextMenu("Reset Player ID")]
    private void ResetPlayerID()
    {
        PlayerPrefs.SetString("player_id", "");
        Debug.Log("Player ID cleared: " + PlayerPrefs.GetString("player_id"));
    }

    // Get New UUID from server or authenticate UUID
    public async Task GetAuthenticationUUID()
    {
        UuidRequest uuid = new UuidRequest { x_client_uuid = playerId };
        UuidResponse uuidResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<UuidRequest, UuidResponse>(
                uuid,
                "/auth/uuid",
                HttpMethod.Post
            );

        playerId = uuidResponse.uuid;
        PlayerPrefs.SetString("player_id", playerId);
        PlayerPrefs.Save();
    }

    // Get new game information from server
    public async Task LoadNewGame()
    {
        LoadGameRootResponse rootResponse = await LoadGame.GetNewGameInfo();
        LoadGame.InitializeGameInfo(rootResponse);
        MainGameManager.Instance.InitializeNpcsUisAndVariables();
    }

    // Get saved game information from server
    public async Task LoadSavedGame(string saveIdToLoad)
    {
        LoadGameRootResponse rootResponse = await LoadGame.GetSavedGameInfo(saveIdToLoad);
        LoadGame.InitializeGameInfo(rootResponse);
        MainGameManager.Instance.InitializeNpcsUisAndVariables();
    }

    public async Task<List<(string, string)>> CreateGameSavesList()
    {
        List<Save> gameSaves = await GetGameSaves();
        List<(string, string)> gameSavesTexts = new();

        string saveText;
        foreach (Save save in gameSaves)
        {
            DateTime dateTime = DateTime.Parse(save.saved_at);
            saveText = "Save name: " + save.name + "\nSave time: " + dateTime.AddHours(3).ToString("dd.MM.yyyy HH:mm");
            gameSavesTexts.Add((save.save_id, saveText));
        }
        return gameSavesTexts;
    }

    public async Task<List<Save>> GetGameSaves()
    {
        GetSavesRequest getSavesRequest = new();

        GetSavesResponse getSavesResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<GetSavesRequest, GetSavesResponse>(
                getSavesRequest,
                "/saves/",
                HttpMethod.Get
            );

        return getSavesResponse.saves;
    }

    public async Task DeleteGameSave(string saveIdToDelete)
    {
        DeleteSaveRequest deleteSaveRequest = new();

        BaseResponse deleteSaveResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<DeleteSaveRequest, BaseResponse>(
                deleteSaveRequest,
                $"/saves/{saveIdToDelete}",
                HttpMethod.Delete
            );
    }

    public async Task<string> CreateMessageForEndGame()
    {
        EndingRequest endRequest = new()
        {
            shadow_save_id = shadowSaveId
        };

        EndingResponse endResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<EndingRequest, EndingResponse>(
                endRequest,
                "/session/generate-ending",
                HttpMethod.Post
            );

        if (endResponse == null)
            return null;

        return endResponse.ending;
    }

    public async Task<string> CreateMessageForSendPlayerInput(string playerInput, string dbNpcId)
    {
        PlayerRequest message = new()
        {
            role = "user",
            shadow_save_id = shadowSaveId,
            content = playerInput
        };

        NpcResponse npcResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<PlayerRequest, NpcResponse>(
                message,
                $"/npcs/{dbNpcId}/chat",
                HttpMethod.Post
            );

        if (npcResponse == null)
        {
            Debug.LogError("Server returned null response.");
            return null;
        }

        // Update NPC data
        Npc npc = MainGameManager.Instance.npcDict.Values.FirstOrDefault(n => n.dbNpcId == dbNpcId);

        if (npc != null)
        {
            int oldAffinitas = npc.affinitasValue;
            npc.affinitasValue = npcResponse.affinitas_new;
            if (oldAffinitas != npc.affinitasValue)
            {
                MainGame.MainGameUiManager.Instance.UpdateNpcAffinitasUi(npc);
                MainGame.MainGameUiManager.Instance.EmphasizeAffinitas(npc);
            }

            if (npcResponse.completed_quests.Count != 0)
            {
                List<string> completeQuestIds = null;
                for (int i = 0; i < npcResponse.completed_quests.Count; i++)
                {
                    if (MainGameManager.Instance.questDict.TryGetValue(npcResponse.completed_quests[i], out Quest quest) && quest != null)
                    {
                        // Mora lysa
                        if (quest.linkedNpcId == 1)
                        {
                            if (MainGame.MainGameUiManager.Instance.AddItemToInventory(quest.item.itemName))
                                await NotifyForItemTakenFromNpcOrMinigame(quest.item.itemName);
                        }
                        else
                        {
                            if (MainGameManager.Instance.npcDict.TryGetValue(quest.linkedNpcId, out Npc npcMatchedToQuest) && npcMatchedToQuest != null)
                            {
                                completeQuestIds = MainGameManager.Instance.UpdateQuestComplete(npcMatchedToQuest, quest.questId);
                                if (completeQuestIds != null)
                                {
                                    foreach (string completeQuestId in completeQuestIds)
                                    {
                                        Debug.Log("completedQuestIds: " + completeQuestId + " of quest name: " + MainGameManager.Instance.questDict[completeQuestId].name + " of npc: " + npcMatchedToQuest.npcName);
                                        await NotifyForQuestComplete(npcMatchedToQuest, completeQuestId);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Debug.Log(npcResponse.response);
        return npcResponse.response;
    }

    public async Task<List<string>> CreateMessageForGetQuest(string dbNpcId, int npcId)
    {
        QuestRequest request = new()
        {
            shadow_save_id = shadowSaveId
        };

        // Response returns quest list. Each quest has quest ID and quest description
        QuestListResponse questResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<QuestRequest, QuestListResponse>(
                request,
                $"/npcs/{dbNpcId}/quest",
                HttpMethod.Post
            );

        List<string> questDescriptions = new();
        foreach (QuestEntry questEntry in questResponse.quests)
        {
            if (MainGameManager.Instance.questDict.TryGetValue(questEntry.quest_id, out Quest quest) && quest != null)
            {
                quest.description = questEntry.response;
                quest.status = MainGameManager.Instance.questStatusDict[QuestStatus.InProgress];
                questDescriptions.Add(quest.description);
            }
        }
        MainGameManager.Instance.HandleGivenQuests(npcId);
        return questDescriptions;
    }

    public async Task<bool> NotifyForEndDay()
    {
        // Send message to all npcs to notify that day has ended
        string systemMessage = "A new day has begun.";

        PlayerRequest message = new()
        {
            role = "system",
            shadow_save_id = shadowSaveId,
            content = systemMessage
        };

        foreach (Npc npc in MainGameManager.Instance.npcDict.Values)
        {
            NpcResponse serverResponse = await ServerConnection.Instance
                .SendAndGetMessageFromServer<PlayerRequest, NpcResponse>(
                    message,
                    $"/npcs/{npc.dbNpcId}/chat",
                    HttpMethod.Post
                );
        }
        return true;
    }

    public async Task<bool> SendDayNoAndActionPointInfo()
    {
        string url = $"/session?day-no={MainGameManager.Instance.dayNo}&ap={MainGameManager.Instance.dailyActionPoints}";

        DayNoInfoRequest message = new()
        {
            shadow_save_id = shadowSaveId
        };

        BaseResponse serverResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<DayNoInfoRequest, BaseResponse>(
                message,
                url,
                HttpMethod.Patch
            );
        return true;
    }

    public async Task<bool> NotifyForQuestComplete(Npc npc, string questId)
    {
        // Send message to notify npc with quest completed
        QuestCompleteRequest message = new()
        {
            quest_id = questId,
            shadow_save_id = shadowSaveId
        };

        QuestCompleteResponse serverResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<QuestCompleteRequest, QuestCompleteResponse>(
                message,
                $"/npcs/{npc.dbNpcId}/quest/complete",
                HttpMethod.Post
            );

        Debug.Log("updated affinitas: " + serverResponse.affinitas);
        npc.affinitasValue = serverResponse.affinitas;
        MainGame.MainGameUiManager.Instance.UpdateNpcAffinitasUi(npc);
        return true;
    }

    public async Task<bool> NotifyForItemTakenFromNpcOrMinigame(string itemName)
    {
        // Send message that player has taken an item from an npc or minigame
        TakeItemRequest message = new()
        {
            item_name = itemName,
            shadow_save_id = shadowSaveId
        };

        BaseResponse npcResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<TakeItemRequest, BaseResponse>(
                message,
                $"/session/item",
                HttpMethod.Post
            );
        return true;
    }

    public async Task<string> NotifyForItemGivenToNpc(string dbNpcId, string itemName)
    {
        // Send message that player has given an item to an npc, and return npc response
        Npc npc = MainGameManager.Instance.npcDict.Values.FirstOrDefault(n => n.dbNpcId == dbNpcId);

        GiveItemRequest message = new()
        {
            item_name = itemName,
            shadow_save_id = shadowSaveId
        };

        GiveItemResponse npcResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<GiveItemRequest, GiveItemResponse>(
                message,
                $"/npcs/{npc.dbNpcId}/item",
                HttpMethod.Post
            );
        return npcResponse.response;
    }

    // Call from Save Game button inside the Save Game panel
    public async void SaveGame()
    {
        await SendGameSave();
    }

    async Task<bool> SendGameSave()
    {
        string saveName = MainGame.MainGameUiManager.Instance.GetSaveNameFromPanel();

        if (saveName == null || saveName == "")
            return false;

        SaveRequest saveRequest = new()
        {
            name = saveName,
            shadow_save_id = shadowSaveId
        };

        SaveResponse saveResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<SaveRequest, SaveResponse>(
                saveRequest,
                "/session/save",
                HttpMethod.Post
            );
        return true;
    }

    // Call from go to menu button in main panel and from go to menu in ending panel
    public async void EndCurrentGame()
    {
        ServerConnection.Instance.canSendMessage = true;
        await EndGameSession();
    }

    // Call from quit game button in menu panel
    public void QuitGame()
    {
        ServerConnection.Instance.CloseServerConnection();
    }

    async Task EndGameSession()
    {
        // Delete the shadow save
        string url = "/session?id=" + shadowSaveId;

        QuitRequest quitRequest = new();

        BaseResponse quitResponse = await ServerConnection.Instance
            .SendAndGetMessageFromServer<QuitRequest, BaseResponse>(
                quitRequest,
                url,
                HttpMethod.Delete
            );

        shadowSaveId = "";
    }

    void OnApplicationQuit()
        {
            QuitGame();
        }

}
