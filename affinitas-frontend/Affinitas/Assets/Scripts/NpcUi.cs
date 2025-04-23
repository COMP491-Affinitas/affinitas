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

    private void Start()
    {
        
    }

    public void InitializeNpc(Npc npcData)
    {
        npc = npcData;
        npc.OnAffinitasChanged += UpdateAffinitasUI;
    }

    void UpdateAffinitasUI(int newAffinitas)
    {
        affinitasTextMesh.text = npc.npcName + "\nAffinitas: " + newAffinitas.ToString();
    }
}
