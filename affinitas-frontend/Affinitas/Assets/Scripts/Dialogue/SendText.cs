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
            addDialogueBox = gameObject.GetComponent<AddDialogueBox>();
            scrollRectHelper = gameObject.GetComponent<ScrollRectHelper>();
        }

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
