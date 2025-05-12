using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Npc
{
    public int npcId;
    public string dbNpcId; // this comes from database
    public string npcName;
    public int affinitasValue;
    public List<Quest> questList = new(); // First quest is main quest, others subquests
    public List<string> dialogueSummary = new(); // One dialogue summary for each day
}

public enum QuestStatus
{
    Pending,
    InProgress,
    Completed
}

public class Quest
{
    public string questId;
    public string name;
    public string description;
    public QuestStatus status;
}

public class MainGameManager : MonoBehaviour
{
    // Singleton
    public static MainGameManager Instance { get; private set; }

    public List<Npc> npcList = new(6);
    //public Dictionary<string, Npc> npcDict = new();
    public int dailyActionPoints;
    public int dayNo;

    public Dictionary<string, bool> hadDialogueDict = new();
    public Dictionary<string, bool> gotQuestDict = new();
    // 0: GusMinigame, 1: CherMinigame, 2: MonaMinigame
    public bool[] minigameList = { false, false, false };

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

    //Call when End D ay button is pressed
    public async void EndDay()
    {
        if (dayNo > 9)
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
            ResetVariables();
            //MainGame.MainGameUiManager.Instance.UpdateDaysLeftPanel();
            await GameManager.Instance.CreateMessageForEndDay();
        }
    }

    // Call this from dialogues, minigames, quests etc. after an action is done
    // Check if action already done, if so do nothing, if not decrease action points
    public void ReduceActionPointsForDialogue(string itemKey)
    {
        if (hadDialogueDict.ContainsKey(itemKey))
        {
            if (hadDialogueDict[itemKey] == false)
            {
                hadDialogueDict[itemKey] = true;
                dailyActionPoints -= 1;
            }
        }
    }
    public void ReduceActionPointsForMinigame(int minigameNo)
    {
        if (minigameNo < minigameList.Length) //number of minigames is 3
        {
            if (minigameList[minigameNo] == false)
            {
                minigameList[minigameNo] = true;
                dailyActionPoints -= 2;
            }
        }
    }
    public void ReduceActionPointsForGetQuest(string itemKey)
    {
        if (gotQuestDict.ContainsKey(itemKey))
        {
            if (gotQuestDict[itemKey] == false)
            {
                gotQuestDict[itemKey] = true;
                dailyActionPoints -= 3;
            }
        }
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

    // Call this at the end of each day
    void ResetVariables()
    {
        foreach (string key in gotQuestDict.Keys.ToList())
            gotQuestDict[key] = false;

        foreach (string key in hadDialogueDict.Keys.ToList())
            hadDialogueDict[key] = false;

        for (int i = 0; i < minigameList.Length; i++)
            minigameList[i] = false;
    }

    public void InitializeNpcsUisAndVariables()
    {
        foreach (Npc npc in npcList)
        {
            MainGame.MainGameUiManager.Instance.InitializeNpcUIs(npc);
            gotQuestDict.Add(npc.npcName, false);
            hadDialogueDict.Add(npc.npcName, false);
        }
        for (int i = 0; i < minigameList.Length; i++)
            minigameList[i] = false;
        MainGame.MainGameUiManager.Instance.UpdateDaysLeftPanel();
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
        MainGame.MainGameUiManager.Instance.AddQuestToQuestPanel(npcQuests[0].questId, questText);

        for (int i = 1; i < npcQuests.Count; i++)
        {
            questText = $@"\n• <size=24>{npcQuests[i].name}</size>";
            MainGame.MainGameUiManager.Instance.AddQuestToQuestPanel(npcQuests[i].questId, questText);
        }

        questText = $@"<b><size=30>{npcList[npcId - 1].npcName}'s Quest:\n{npcQuests[0].name}</size></b>";
        questText += $@"\n<size=24>{npcQuests[0].description}</size>\n";
        for (int i = 1; i < npcQuests.Count; i++)
        {
            questText += $@"\n<b>• <size=26>{npcQuests[i].name}</size></b>";
            questText += $@"\n<size=24>{npcQuests[i].description}</size>";
        }
        MainGame.MainGameUiManager.Instance.AddQuestToJournal(npcId, questText + "\n\n");

        // This is <s>crossed out</s>. This is <b>bold</b> text.
    }

    void UpdateQuestStatus(int npcId, string questId, QuestStatus newStatus)
    {
        // TODO: Quest completion game logic code etc. here
        // ...

        Quest questToUpdate = null;
        foreach (Quest quest in npcList[npcId - 1].questList)
        {
            if (quest.questId.Equals(questId))
                questToUpdate = quest;
        }

        if (questToUpdate == null)
        {
            Debug.Log("Quest with id: " + questId + " does not exist for NPC" + npcId);
            return;
        }

        questToUpdate.status = newStatus;
        MainGame.MainGameUiManager.Instance.UpdateQuestInQuestPanel(questId, newStatus);
    }
}
