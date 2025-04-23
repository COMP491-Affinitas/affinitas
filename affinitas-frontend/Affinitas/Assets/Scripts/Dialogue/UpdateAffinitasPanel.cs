using UnityEngine;
using TMPro;

public class UpdateAffinitasPanel : MonoBehaviour
{
    // This code needs to be on the Content of the NpcAffinitasScrollView

    Transform content;
    TextMeshProUGUI npcTextMesh;
    string affinitasValue;

    private void Start()
    {
        content = gameObject.transform;
    }

    // For testing
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            UpdateAffinitas("NPC1");
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            UpdateAffinitas("NPC2");
        }
    }

    void UpdateAffinitas(string npcName)
    {
        switch (npcName)
        {
            case "NPC1":

                npcTextMesh = content.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
                npcTextMesh.text = "NPC1\n";
                break;
            case "NPC2":
                npcTextMesh = content.GetChild(1).GetComponentInChildren<TextMeshProUGUI>();
                npcTextMesh.text = "NPC2\n";
                break;
            default:
                break;
        }

        npcTextMesh.text += "Affinitas: ??"; //affinitasValue from JSON
    }
}
