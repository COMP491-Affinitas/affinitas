using UnityEngine;
using TMPro;

namespace MainGame
{
    public class SendText : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField dialogueInputField;
        string playerInput;
        AddDialogueBox addDialogueBox;
        ScrollRectHelper scrollRectHelper;

        void Start()
        {
            // Send Text also when user presses Enter
            dialogueInputField.onSubmit.AddListener((str) => SendInputtedText());
            dialogueInputField.onSubmit.AddListener((str) => GameManager.Instance.SendAndReceiveFromServer(str, false));
            addDialogueBox = gameObject.GetComponent<AddDialogueBox>();
            scrollRectHelper = gameObject.GetComponent<ScrollRectHelper>();
        }

        // TODO: Make sure player cannot press enter again!!!
        public void SendInputtedText()
        {
            playerInput = dialogueInputField.text;
            if (playerInput == "")
                return;

            addDialogueBox.AddPlayerDialogueBox(playerInput);
            scrollRectHelper.ScrollToBottom();

            dialogueInputField.text = "";
            dialogueInputField.ActivateInputField();

        }

    }
}
