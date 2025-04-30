using UnityEngine;
using System.Collections.Generic;

public class Npc
{
    public int npcId;
    //public string npc_id; // this comes from database
    public string npcName;
    public int affinitasValue;
    public List<Quest> questList = new(); // First quest is main quest, others subquests
    public List<string> dialogueSummary = new(); // One dialogue summary for each day
}

public class Quest
{
    public string name;
    public string description;
    public bool started;
    public string status;
}

public class MainGameManager : MonoBehaviour
{
    // Singleton
    public static MainGameManager Instance { get; private set; }

    public List<Npc> npcList = new(6);
    //public Dictionary<string, Npc> npcDict = new();
    public int dailyActionPoints;
    public int dayNo;

    public Dictionary<string, bool> dialoguesDict = new();
    public Dictionary<string, bool> questDict = new();

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

    //Call when End Day button is pressed
    public void EndDay()
    {
        if (dayNo > 9)
            return;
            // End game panel

        // Check dailyActionPoints and end day accordingly
        //if (dailyActionPoints)
        dayNo += 1;
        dailyActionPoints = 15;
    }

    public void InitializeNpcsUis()
    {
        foreach (Npc npc in npcList)
        {
            MainGame.MainGameUiManager.Instance.InitializeNpcUIs(npc);
        }
    }






}
