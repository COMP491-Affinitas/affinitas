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
    TMP_InputField dialogueInputField;
    //[SerializeField]
    //GameObject journalTab;

    private void Start()
    {
        // Send Text also when user presses Enter
        dialogueInputField.onSubmit.AddListener((str) => CreateMessageForSendPlayerInput(str));
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

    // Call this from Send Text button
    public void CreateMessageForSendPlayerInput(string playerInput)
    {
        if (playerInput == "")
            return;

        string directoryName = ServerConnection.Instance.serverDirectoriesDict[(int)ServerDirectory.npc] + npc.npcId.ToString() + "/chat";

        // TODO: CHANGE ALL THIS
        ClientResponse message = new ClientResponse("user", npc.npcId, (int)RequestType.sendPlayerInput, playerInput);

        GameManager.Instance.SendAndReceiveFromServer(message, ServerDirectory.npc);

        //For action points
        GameManager.Instance.dialoguesDict[npc.npcName] = true;
    }

    // Call this from Get Quest button
    public void CreateMessageForGetQuest()
    {
        ClientResponse message = new ClientResponse("system", npc.npcId, (int)RequestType.requestNpcQuest, "quest");

        GameManager.Instance.SendAndReceiveFromServer(message, ServerDirectory.npc);

        //For action points
        GameManager.Instance.questDict[npc.npcName] = true;
    }


}
