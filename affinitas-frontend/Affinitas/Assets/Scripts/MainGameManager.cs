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
    public string questId;
    public string name;
    public string description;
    public string status;
    public int affinitasReward;
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

    public List<Npc> npcList = new(6);
    //public Dictionary<string, Npc> npcDict = new();
    public int dailyActionPoints;
    public int dayNo = -1;
    public bool journalActive;
    public string journalTownInfo;

    public Dictionary<QuestStatus, string> questDict = new()
    {
        { QuestStatus.Pending, "pending" },
        { QuestStatus.InProgress, "active" },
        { QuestStatus.Completed, "complete" }
    };

    public List<Npc> currentNpcQuests = new();

    // Temporary
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
        foreach (Npc npc in npcList)
        {
            MainGame.MainGameUiManager.Instance.InitializeNpcUIs(npc);
        }
        CheckBartEnderQuest();
        CheckJournalActive();
    }

    public void CheckBartEnderQuest()
    {
        if (dayNo > 1)
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);

        // if Bart Ender quest has not begun, then that means first day has not ended
        else if (npcList[2].questList[0].status.Equals(questDict[QuestStatus.Pending]))
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(false);
        else
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);
    }

    // TODO: WHEN TO MAKE JOURNAL ACTIVE?
    public void CheckJournalActive()
    {
        MainGame.MainGameUiManager.Instance.ToggleJournalButtonActive(journalActive);
    }

    //Call when End Day button is pressed
    public async void EndDay()
    {
        // If Bart Ender Quest not completed, do not let the day pass
        if (!npcList[2].questList[0].status.Equals(questDict[QuestStatus.Completed]))
        {
            MainGame.MainGameUiManager.Instance.OpenWarningPanel("You should complete Bart Ender's quest first!");
        }
        else if (dayNo > 10)
        {
            UIManager.Instance.OpenEndingPanel();
            string endingText = await GameManager.Instance.CreateMessageForEndGame();
            Debug.Log(endingText);
            UIManager.Instance.PutEndingTextToPanel(endingText);
        }
        else
        {
            dayNo += 1;
            dailyActionPoints = 15;
            await GameManager.Instance.NotifyForEndDay();
        }
    }

    // Call from any house button pressed
    public void ReduceActionPointsForDialogue()
    {
        if (dayNo == 1)
            return;
        dailyActionPoints -= 1;
    }
    // Call from anu minigame button pressed
    public void ReduceActionPointsForMinigame()
    {
        if (dayNo == 1)
            return;
        dailyActionPoints -= 2;
    }
    // Call from any get quest button pressed
    public void ReduceActionPointsForGetQuest()
    {
        if (dayNo == 1)
            return;
        dailyActionPoints -= 3;
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
    public void ReturnFromGusMinigame(int gusMinigameScoreVal)
    {
        gusMinigameScore = gusMinigameScoreVal;
    }

    // Call from GoToMap function in CherMinigameScene
    public void ReturnFromCherMinigame(int cherMinigameScoreVal)
    {
        cherMinigameScore = cherMinigameScoreVal;
    }

    public void HandleGivenQuests(int npcId)
    {
        List<Quest> npcQuests = npcList[npcId - 1].questList;

        if (npcQuests.Count < 1)
        {
            Debug.Log("Npc has no quests.");
            return;
        }
            
        string questText = $@"<b><size=30>{npcList[npcId - 1].npcName}'s Quest:\n{npcQuests[0].name}</size></b>";
        MainGame.MainGameUiManager.Instance.AddQuestToQuestPanel(npcQuests[0].questId, questText, npcQuests[0].status);

        for (int i = 1; i < npcQuests.Count; i++)
        {
            questText = $@"\n• <size=24>{npcQuests[i].name}</size>";
            MainGame.MainGameUiManager.Instance.AddQuestToQuestPanel(npcQuests[i].questId, questText, npcQuests[0].status);
        }

        questText = $@"<b><size=30>{npcList[npcId - 1].npcName}'s Quest:\n{npcQuests[0].name}</size></b>";
        questText += $@"\n<size=24>{npcQuests[0].description}</size>\n";
        for (int i = 1; i < npcQuests.Count; i++)
        {
            questText += $@"\n<b>• <size=26>{npcQuests[i].name}</size></b>";
            questText += $@"\n<size=24>{npcQuests[i].description}</size>";
        }
        MainGame.MainGameUiManager.Instance.AddQuestToQuestDetails(npcId, questText + "\n\n");

        // This is <s>crossed out</s>. This is <b>bold</b> text.
    }

    public List<string> UpdateQuestStatus(Npc npc, string questId, string newStatus)
    {
        Debug.Log("hey");

        List<string> completeQuestIds = new();
        Quest questToUpdate = null;

        foreach (Quest quest in npc.questList)
        {
            if (quest.questId.Equals(questId))
                questToUpdate = quest;
        }

        if (questToUpdate == null)
        {
            Debug.Log("Quest with id: " + questId + " does not exist");
            return null;
        }

        Debug.Log("hey again");

        completeQuestIds.Add(questToUpdate.questId);
        questToUpdate.status = newStatus;
        MainGame.MainGameUiManager.Instance.UpdateQuestInQuestPanel(questId, newStatus);


        // Check main quest completion as well
        bool allSubquestsCompleted = true;
        if (npc.questList.Count > 1)
        {
            for (int i = 1; i < npc.questList.Count; i++)
            {
                if (!npc.questList[i].status.Equals(questDict[QuestStatus.Completed]))
                {
                    allSubquestsCompleted = false;
                }
            }
            if (allSubquestsCompleted)
            {
                npc.questList[0].status = newStatus;
                MainGame.MainGameUiManager.Instance.UpdateQuestInQuestPanel(npc.questList[0].questId, newStatus);
                completeQuestIds.Add(npc.questList[0].questId);
            }
        }

        return completeQuestIds;
    }

    public Npc MatchQuestToNpc(string questId)
    {
        foreach (Npc npc in npcList)
        {
            foreach (Quest quest in npc.questList)
            {
                if (quest.questId.Equals(questId))
                    return npc;
            }
        }
        return null;
    }

    public void LoadSavedQuestsToQuestPanel()
    {
        currentNpcQuests = new();
        MainGame.MainGameUiManager.Instance.EmptyQuestPanel();
        foreach (Npc npc in npcList)
        {
            bool handledQuests = false;
            Debug.Log("npc name: " + npc.npcName);
            foreach (Quest quest in npc.questList)
            {
                Debug.Log("quest name: " + quest.name + " with status: " + quest.status.ToString());
                if (!quest.status.Equals(questDict[QuestStatus.Pending]))
                {
                    if (!handledQuests)
                    {
                        currentNpcQuests.Add(npc);
                        HandleGivenQuests(npc.npcId);
                        handledQuests = true;
                    }
                    if (quest.status.Equals(questDict[QuestStatus.Completed]))
                    {
                        Debug.Log("pls");
                        UpdateQuestStatus(npc, quest.questId, questDict[QuestStatus.Completed]);
                    }
                }
            }
        }
    }

    public List<string> CreateJournalText()
    {
        List<string> journalTexts = new();

        string townInfoText = $@"<b><size=30>Information About the Town</size></b>";
        townInfoText += $@"\n<size=24>{journalTownInfo}</size>";
        journalTexts.Add(townInfoText);

        string npcInfoText = "";
        foreach (Npc npc in npcList)
        {
            npcInfoText += $@"<b><size=30>{npc.npcName}</size></b>";
            npcInfoText += $@"\n<size=24>{npc.description}</size>\n\n";
        }
        journalTexts.Add(npcInfoText);

        return journalTexts;
    }

    public void ActivateJournal()
    {
        journalActive = true;
        MainGame.MainGameUiManager.Instance.ToggleJournalButtonActive(true);
    }

}
