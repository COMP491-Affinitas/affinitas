using UnityEngine;
using System.Collections.Generic;

public class Npc
{
    public int npcId;
    public string dbNpcId; // this comes from database
    public string npcName;
    public int affinitasValue;
    public List<Quest> questList = new(); // First quest is main quest, others subquests
    public List<List<string>> chatHistory = new();
    public string description;
}

public class Quest
{
    public int linkedNpcId;
    public string questId;
    public string name;
    public string description;
    public string status;
    public int affinitasReward;
    public Item item;
}

public class Item
{
    public string linkedQuestId;
    public string itemName;
    public bool active;
}

public enum QuestStatus
{
    Pending,
    InProgress,
    Completed
}

public class MainGameManager : MonoBehaviour
{
    // Singleton
    public static MainGameManager Instance { get; private set; }

    public SortedDictionary<int, Npc> npcDict = new();
    public Dictionary<string, Quest> questDict = new();
    public Dictionary<string, Item> itemDict = new();

    public int dailyActionPoints;
    public int dayNo = -1;
    public bool journalActive;
    public string journalTownInfo;

    public Dictionary<QuestStatus, string> questStatusDict = new()
    {
        { QuestStatus.Pending, "pending" },
        { QuestStatus.InProgress, "active" },
        { QuestStatus.Completed, "completed" }
    };

    public int gusMinigameScore;
    public int cherMinigameScore;

    private void Awake()
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
    }

    public void InitializeNpcsUisAndVariables()
    {
        foreach (Npc npc in npcDict.Values)
            MainGame.MainGameUiManager.Instance.InitializeNpcUIs(npc);
        CheckBartEnderQuest();
        CheckJournalActive();
    }

    public void CheckBartEnderQuest()
    {
        if (dayNo > 1)
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);

        // if Bart Ender quest has not begun, then that means first day has not ended
        else if (npcDict[3].questList[0].status.Equals(questStatusDict[QuestStatus.Pending]))
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(false);
        else
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);
    }

    public void CheckJournalActive()
    {
        MainGame.MainGameUiManager.Instance.ToggleJournalButtonActive(journalActive);
    }

    //Call when End Day button is pressed
    public async void EndDay()
    {
        // If Bart Ender Quest not completed, do not let the day pass
        if (!npcDict[3].questList[0].status.Equals(questStatusDict[QuestStatus.Completed]))
            MainGame.MainGameUiManager.Instance.OpenWarningPanel("You should complete Bart Ender's quest first!");
        else if (dayNo >= 10)
        {
            UIManager.Instance.OpenEndingPanel();
            string endingText = await GameManager.Instance.CreateMessageForEndGame();
            UIManager.Instance.PutEndingTextToPanel(endingText);
        }
        else
        {
            dayNo += 1;
            dailyActionPoints = 15;
            MainGame.MainGameUiManager.Instance.ActionAfterGameSaved();
            await GameManager.Instance.SendDayNoAndActionPointInfo();
            await GameManager.Instance.NotifyForEndDay();
        }
    }

    // Call from any house button pressed
    public async void ReduceActionPointsForDialogue()
    {
        if (dayNo == 1)
            return;
        dailyActionPoints -= 1;
        await GameManager.Instance.SendDayNoAndActionPointInfo();
    }
    // Call from anu minigame button pressed
    public async void ReduceActionPointsForMinigame()
    {
        if (dayNo == 1)
            return;
        dailyActionPoints -= 2;
        await GameManager.Instance.SendDayNoAndActionPointInfo();
    }
    // Call from any get quest button pressed
    public async void ReduceActionPointsForGetQuest()
    {
        if (dayNo == 1)
            return;
        dailyActionPoints -= 3;
        await GameManager.Instance.SendDayNoAndActionPointInfo();
    }

    // Call this from dialogues, minigames, quests etc. before an action is done
    // If not enough actions points left, do not let player do things
    public bool EnoughActionPointsForDialogue()
    {
        if (1 <= dailyActionPoints)
            return true;
        return false;
    }
    public bool EnoughActionPointsForMinigame()
    {
        if (2 <= dailyActionPoints)
            return true;
        return false;
    }
    public bool EnoughActionPointsForGetQuest()
    {
        if (3 <= dailyActionPoints)
            return true;
        return false;
    }

    // Call from GoToMap function in GusMinigameScene
    public async void ReturnFromGusMinigame(int gusMinigameScoreVal, int obtainedFish)
    {
        gusMinigameScore = gusMinigameScoreVal;
        if (obtainedFish > 0)
            if (MainGame.MainGameUiManager.Instance.AddItemToInventory("gus_fish"))
            {
                MainGame.MainGameUiManager.Instance.ActionAfterGameSaved();
                await GameManager.Instance.NotifyForItemTakenFromNpcOrMinigame("gus_fish");
            }    
    }

    // Call from GoToMap function in CherMinigameScene
    public void ReturnFromCherMinigame(int cherMinigameScoreVal)
    {
        cherMinigameScore = cherMinigameScoreVal;
    }

    public void HandleGivenQuests(int npcId)
    {
        Npc npc = npcDict[npcId];
        List<Quest> npcQuests = npcDict[npcId].questList;

        if (npcQuests.Count < 1)
            return;

        // Add main npc quest as title to quest panel
        string questText = $@"<b><size=30>{npc.npcName}'s Quest:\n{npcQuests[0].name}</size></b>";
        MainGame.MainGameUiManager.Instance.AddQuestToQuestPanel(npcQuests[0].questId, questText, npcQuests[0].status);

        // Add subquests to quest panel
        for (int i = 1; i < npcQuests.Count; i++)
        {
            questText = $@"\n• <size=24>{npcQuests[i].name}</size>";
            MainGame.MainGameUiManager.Instance.AddQuestToQuestPanel(npcQuests[i].questId, questText, npcQuests[i].status);
        }

        // Add all of quest details to quest details panel
        questText = $@"<b><size=30>{npc.npcName}'s Quest:\n{npcQuests[0].name}</size></b>";
        questText += $@"\n<size=24>{npcQuests[0].description}</size>\n";
        for (int i = 1; i < npcQuests.Count; i++)
        {
            questText += $@"\n<b>• <size=26>{npcQuests[i].name}</size></b>";
            questText += $@"\n<size=24>{npcQuests[i].description}</size>";
        }
        MainGame.MainGameUiManager.Instance.AddQuestToQuestDetails(npcId, questText + "\n\n");
    }

    public List<string> UpdateQuestComplete(Npc npc, string questId)
    {
        List<string> completeQuestIds = new();

        if (questDict.TryGetValue(questId, out Quest quest) && quest != null)
        {
            quest.status = questStatusDict[QuestStatus.Completed];
            completeQuestIds.Add(quest.questId);
            
            MainGame.MainGameUiManager.Instance.UpdateQuestInQuestPanel(questId, questStatusDict[QuestStatus.Completed]);

            // Check main quest completion (check all subquests complete)
            if (npc.questList.Count > 1)
            {
                bool allSubquestsCompleted = true;
                for (int i = 1; i < npc.questList.Count; i++)
                {
                    if (!npc.questList[i].status.Equals(questStatusDict[QuestStatus.Completed]))
                        allSubquestsCompleted = false;
                }
                if (allSubquestsCompleted)
                {
                    npc.questList[0].status = questStatusDict[QuestStatus.Completed];
                    completeQuestIds.Add(npc.questList[0].questId);

                    MainGame.MainGameUiManager.Instance.UpdateQuestInQuestPanel(npc.questList[0].questId, questStatusDict[QuestStatus.Completed]);
                }
            }
            return completeQuestIds;
        }
        return null;
    }

    public void LoadSavedQuestsToQuestPanel()
    {
        foreach (Npc npc in npcDict.Values)
        {
            if (npc.questList == null || npc.questList.Count < 1)
                continue;

            if (npc.questList[0].status != questStatusDict[QuestStatus.Pending])
            {
                Debug.Log("LoadSavedQuestsToQuestPanel, " + npc.questList[0].name + ", status: " + npc.questList[0].status);
                HandleGivenQuests(npc.npcId);
                foreach (Quest quest in npc.questList)
                {
                    if (quest.status == questStatusDict[QuestStatus.Completed])
                        MainGame.MainGameUiManager.Instance.UpdateQuestInQuestPanel(quest.questId, questStatusDict[QuestStatus.Completed]);
                }
            }
        }
    }

    public List<string> CreateJournalText()
    {
        List<string> journalTexts = new();

        // Create town info text
        string townInfoText = $@"<b><size=30>Information About the Town</size></b>";
        townInfoText += $@"\n<size=24>{journalTownInfo}</size>";
        journalTexts.Add(townInfoText);

        // Create npc info texts
        string npcInfoText;
        foreach (Npc npc in npcDict.Values)
        {
            npcInfoText = $@"<b><size=30>{npc.npcName}</size></b>";
            npcInfoText += $@"\n<size=24>{npc.description}</size>\n\n";
            journalTexts.Add(npcInfoText);
        }
        return journalTexts;
    }

    public void ActivateJournal()
    {
        journalActive = true;
        MainGame.MainGameUiManager.Instance.ToggleJournalButtonActive(true);
    }

    public bool CheckGetQuest(int npcId)
    {
        if (npcDict[npcId].questList == null || npcDict[npcId].questList.Count < 1)
            return true;

        if (npcDict[npcId].questList[0].status != questStatusDict[QuestStatus.Pending])
            return true;
        return false;
    }

}
