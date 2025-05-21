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
    public List<List<string>> chatHistory = new();
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

    public Dictionary<QuestStatus, string> questDict = new()
    {
        { QuestStatus.Pending, "pending" },
        { QuestStatus.InProgress, "active" },
        { QuestStatus.Completed, "complete" }
    };

    public Dictionary<string, bool> hadDialogueDict = new();
    public Dictionary<string, bool> gotQuestDict = new();
    // 0: GusMinigame, 1: CherMinigame, 2: MonaMinigame
    public bool[] minigameList = { false, false, false };

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
        gotQuestDict = new();
        hadDialogueDict = new();
        foreach (Npc npc in npcList)
        {
            MainGame.MainGameUiManager.Instance.InitializeNpcUIs(npc);
            gotQuestDict.Add(npc.npcName, false);
            hadDialogueDict.Add(npc.npcName, false);
        }
        for (int i = 0; i < minigameList.Length; i++)
            minigameList[i] = false;

        ResetVariables();
        CheckBartEnderQuest();
        //MainGame.MainGameUiManager.Instance.UpdateDaysLeftPanel();
    }

    public void CheckBartEnderQuest()
    {
        Debug.Log("WHYYYY");
        if (dayNo > 1)
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);

        // if Bart Ender quest has not begun, then that means first day has not ended
        else if (npcList[2].questList[0].status.Equals(questDict[QuestStatus.Pending]))
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(false);
        else
            MainGame.MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);
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
            ResetVariables();
            //MainGame.MainGameUiManager.Instance.UpdateDaysLeftPanel();
            await GameManager.Instance.NotifyForEndDay();
        }
    }

    // Call this from dialogues, minigames, quests etc. after an action is done
    // Check if action already done, if so do nothing, if not decrease action points
    public void ReduceActionPointsForDialogue(string itemKey)
    {
        if (dayNo == 1)
            return;
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
        if (dayNo == 1)
            return;
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
        if (dayNo == 1)
            return;
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


    // When subquest is completed, return itemName of given item to notify server
    public string NpcGivesItemToPlayer(int npcId)
    {
        if (npcId == 1)
        {
            foreach (MainGame.UseItem moraPiece in MainGame.MainGameUiManager.Instance.moraPieceItems)
            {
                if (!moraPiece.inInventory)
                {
                    moraPiece.AddToInventory();
                    return moraPiece.itemName;
                }
            }
        }
        // For Gus this comes from minigame
        else if (npcId == 2)
        {
            if (!MainGame.MainGameUiManager.Instance.gusFishItem.inInventory)
            {
                MainGame.MainGameUiManager.Instance.gusFishItem.AddToInventory();
                return MainGame.MainGameUiManager.Instance.gusFishItem.itemName;
            }
        }
        return null;
    }

    // Call from Give Item to Npc button
    public string PlayerGivesItemToNpc(int npcId)
    {
        if (npcId == 1)
        {
            foreach (MainGame.UseItem moraPiece in MainGame.MainGameUiManager.Instance.moraPieceItems)
            {
                if (moraPiece.inInventory)
                {
                    moraPiece.RemoveFromInventory();
                    //TODO: NECESSARY CODE!!!!
                    return moraPiece.itemName;
                }
            }
        }
        // For Gus this comes from minigame
        else if (npcId == 2)
        {
            if (MainGame.MainGameUiManager.Instance.gusFishItem.inInventory)
            {
                MainGame.MainGameUiManager.Instance.gusFishItem.RemoveFromInventory();
                //TODO: NECESSARY CODE!!!!
                return MainGame.MainGameUiManager.Instance.gusFishItem.itemName;
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

}
