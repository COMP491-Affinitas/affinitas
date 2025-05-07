using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

        [SerializeField]
        Button getQuestButton;
        bool getQuestDone;

        void Start()
        {
            // Send Text also when user presses Enter
            dialogueInputField.onSubmit.AddListener((str) => SendInputtedText());
            dialogueInputField.onSubmit.AddListener((str) => SendTextGetNpcResponse(str));
            addDialogueBox = gameObject.GetComponent<AddDialogueBox>();
            scrollRectHelper = gameObject.GetComponent<ScrollRectHelper>();
        }

        // Call this from Send Text button
        public void SendInputtedText()
        {
            if (MainGameManager.Instance.EnoughActionPointsForDialogue() == false)
            {
                MainGameUiManager.Instance.OpenWarningPanel("You do not have enough action points to talk. End the day!");
                return;
            }
            if (ServerConnection.Instance.canSendMessage == false)
                return;

            MainGameManager.Instance.ReduceActionPointsForDialogue(MainGameManager.Instance.npcList[npcId - 1].npcName);
            MainGameUiManager.Instance.UpdateDaysLeftPanel();

            getQuestButton.interactable = false;

            playerInput = dialogueInputField.text;
            if (playerInput == "")
                return;

            addDialogueBox.AddPlayerDialogueBox(playerInput, null, null);
            scrollRectHelper.ScrollToBottom();

            dialogueInputField.text = "";
            dialogueInputField.ActivateInputField();
        }

        public async void SendTextGetNpcResponse(string playerInput)
        {
            if (MainGameManager.Instance.EnoughActionPointsForDialogue() == false)
                return;

            if (ServerConnection.Instance.canSendMessage == false)
                return;

            ServerConnection.Instance.canSendMessage = false;

            string dbNpcId = MainGameManager.Instance.npcList[npcId-1].dbNpcId;

            string npcResponse = await GameManager.Instance.CreateMessageForSendPlayerInput(playerInput, dbNpcId);

            if (!string.IsNullOrEmpty(npcResponse))
            {
                addDialogueBox.AddNpcDialogueBox(npcResponse, ServerConnection.Instance.OnServerMessageReceived, MakeGetQuestClickable);
                scrollRectHelper.ScrollToBottom();
            }
        }

        //Call when Get Quest is pressed
        public void GetQuestText()
        {
            if (MainGameManager.Instance.EnoughActionPointsForGetQuest() == false)
            {
                MainGameUiManager.Instance.OpenWarningPanel("You do not have enough action points to get a new quest. End the day!");
                return;
            }
            MainGameManager.Instance.ReduceActionPointsForGetQuest(MainGameManager.Instance.npcList[npcId - 1].npcName);
            MainGameUiManager.Instance.UpdateDaysLeftPanel();

            //TODO: SERVER SYSTEM CALL
            addDialogueBox.AddNpcDialogueBox("Hey your quest is to go adrgasd", null, null);
            getQuestDone = true;
            getQuestButton.interactable = false;
        }

        public void MakeGetQuestClickable()
        {
            if (!getQuestDone)
                getQuestButton.interactable = true;
        }

    }
}
