using UnityEngine;
using TMPro;

public class NpcUi : MonoBehaviour
{
    Npc npc;

    [SerializeField]
    GameObject dialogueBox;
    [SerializeField]
    TextMeshProUGUI affinitasTextMesh;
    [SerializeField]
    TextMeshProUGUI journalTabTextMesh;
    [SerializeField]
    TMP_InputField dialogueInputField;

    private void Start()
    {
        // Send Text also when user presses Enter
        dialogueInputField.onSubmit.AddListener((str) => CreateMessageForSendPlayerInput(str));
    }

    public void InitializeNpc(Npc npcData)
    {
        npc = npcData;
        journalTabTextMesh.text = npc.npcName;
        //npc.OnAffinitasChanged += UpdateAffinitasUI;
    }

    void UpdateAffinitasUI(int newAffinitas)
    {
        affinitasTextMesh.text = npc.npcName + "\nAffinitas: " + newAffinitas.ToString();
    }

    //Call this from Send Text button
    public void CreateMessageForSendPlayerInput(string playerInput)
    {
        if (ServerConnection.Instance.canSendMessage == false || playerInput == "")
            return;

        ClientResponse message = new("user", playerInput);
        //GameManager.Instance.SendAndReceiveFromServer(message, "/npcs/" + npc.npcId + "/chat");

        //For action points
        //GameManager.Instance.dialoguesDict[npc.npcName] = true;
    }

    // Call this from Get Quest button
    //public void CreateMessageForGetQuest()
    //{
    //    ClientResponse message = new ClientResponse("system", npc.npcId, (int)RequestType.requestNpcQuest, "quest");

    //    GameManager.Instance.SendAndReceiveFromServer(message, ServerDirectory.npc);

    //    //For action points
    //    GameManager.Instance.questDict[npc.npcName] = true;
    //}


}
