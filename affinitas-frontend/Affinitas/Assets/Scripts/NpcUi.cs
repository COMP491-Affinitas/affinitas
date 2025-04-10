using UnityEngine;
using TMPro;

public class NpcUi : MonoBehaviour
{
    Npc npc;

    [SerializeField]
    GameObject dialogueBox;
    [SerializeField]
    TextMeshProUGUI affinitasTextMesh;
    //[SerializeField]
    //GameObject journalTab;

    public void InitializeNpc(Npc npcData)
    {
        npc = npcData;
        UpdateUI();
    }

    void UpdateUI()
    {
        affinitasTextMesh.text = npc.npcName + "\nAffinitas: " + npc.affinitasValue.ToString();
    }
}
