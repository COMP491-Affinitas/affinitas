using UnityEngine;
using TMPro;

namespace MainGame
{
    public class SendText : MonoBehaviour
    {
        // This code needs to be on DialogueScrollView
        [SerializeField]
        int npcId;
        [SerializeField]
        TMP_InputField dialogueInputField;
        string playerInput;
        AddDialogueBox addDialogueBox;
        ScrollRectHelper scrollRectHelper;

        void Start()
        {
            // Send Text also when user presses Enter
            dialogueInputField.onSubmit.AddListener((str) => SendInputtedText());
            dialogueInputField.onSubmit.AddListener( (str) => SendTextGetNpcResponse(str));
            addDialogueBox = gameObject.GetComponent<AddDialogueBox>();
            scrollRectHelper = gameObject.GetComponent<ScrollRectHelper>();
        }

        // Call this from Send Text button
        public void SendInputtedText()
        {
            if (ServerConnection.Instance.canSendMessage == false)
                return;

            playerInput = dialogueInputField.text;
            if (playerInput == "")
                return;

            addDialogueBox.AddPlayerDialogueBox(playerInput, null);
            scrollRectHelper.ScrollToBottom();

            dialogueInputField.text = "";
            dialogueInputField.ActivateInputField();
        }

        public async void SendTextGetNpcResponse(string playerInput)
        {
            if (ServerConnection.Instance.canSendMessage == false)
                return;

            ServerConnection.Instance.canSendMessage = false;

            string dbNpcId = MainGameManager.Instance.npcList[npcId-1].dbNpcId;

            string npcResponse = await GameManager.Instance.CreateMessageForSendPlayerInput(playerInput, dbNpcId);

            if (!string.IsNullOrEmpty(npcResponse))
            {
                addDialogueBox.AddNpcDialogueBox(npcResponse, ServerConnection.Instance.OnServerMessageReceived);
                scrollRectHelper.ScrollToBottom();
            }
        }

    }
}
