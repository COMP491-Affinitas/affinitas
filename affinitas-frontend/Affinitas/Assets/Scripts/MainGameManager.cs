using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Npc
{
    public int npcId;
    public string npcName;
    public int affinitasValue;

    // First quest is main quest, others subquests
    public List<string> questList = new();
    public List<int> questStatus = new();
    // One dialogue summary for each day
    public List<string> dialogueSummary = new();
}

public class MainGameManager : MonoBehaviour
{
    List<Npc> npcList = new(6);

    [SerializeField]
    TextMeshProUGUI[] affinitasTextMeshes;

    [SerializeField]
    GameObject[] dialogueContents;
    [SerializeField]
    TMP_InputField[] dialogueInputFields;

    [SerializeField]
    TextMeshProUGUI[] journalTabButtonTextMeshes;
    [SerializeField]
    TextMeshProUGUI[] journalTextMeshes;

    private void Start()
    {
        // Send Text also when user presses Enter
        foreach (TMP_InputField dialogueInputField in dialogueInputFields)
        {
            //TODO: 
            //dialogueInputField.onSubmit.AddListener((str) => CreateMessageForSendPlayerInput(str));
        }
    }
        
    // npcId indexing starts from 1
    public void InitializeNpc(Npc npcData)
    {
        int i = npcData.npcId -1;
        npcList.Insert(i, npcData);
        journalTabButtonTextMeshes[i].text = npcData.npcName;
        affinitasTextMeshes[i].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
        //npc.OnAffinitasChanged += UpdateAffinitasUI;
    }

    void UpdateAffinitasUIs()
    {
        // Just iterate over all npcs or use a function that takes npcId
        foreach (Npc npc in npcList)
        {
            int i = npc.npcId -1;
            affinitasTextMeshes[i].text = npc.npcName + "\nAffinitas: " + npc.affinitasValue.ToString();
        }
    }
}
