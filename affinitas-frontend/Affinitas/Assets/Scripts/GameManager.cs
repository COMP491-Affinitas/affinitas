using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Npc[] npcList;
    [SerializeField]
    NpcUi[] npcUiList;

    string[] npcNames = {"Mora Lysa", "Gus Tider", "Bart Ender"};

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
        npcList = new Npc[3];
        Npc currNpc;

        for (int i = 0; i < npcList.Length; i++)
        {
            // Random affinitas values for now
            currNpc = new Npc(i, npcNames[i], i * 10);
            string[] questList = { "Say hello to the world." };
            currNpc.AddQuestList(questList);

            npcUiList[i].InitializeNpc(currNpc);

            npcList[i] = currNpc;
        }

    }
}
