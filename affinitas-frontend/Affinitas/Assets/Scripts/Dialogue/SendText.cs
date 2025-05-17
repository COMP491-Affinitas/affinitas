using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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
        public async void GetQuestText()
        {
            if (MainGameManager.Instance.EnoughActionPointsForGetQuest() == false)
            {
                MainGameUiManager.Instance.OpenWarningPanel("You do not have enough action points to get a new quest. End the day!");
                return;
            }

            MainGameManager.Instance.ReduceActionPointsForGetQuest(MainGameManager.Instance.npcList[npcId - 1].npcName);
            MainGameUiManager.Instance.UpdateDaysLeftPanel();

            string dbNpcId = MainGameManager.Instance.npcList[npcId - 1].dbNpcId;

            List<string> questDescriptions = await GameManager.Instance.CreateMessageForGetQuest(dbNpcId, npcId);

            if (questDescriptions != null)
            {
                getQuestButton.interactable = false;
                getQuestDone = true;

                StartCoroutine(ShowQuestsOneByOne(questDescriptions));
            } 
        }

        IEnumerator ShowQuestsOneByOne(List<string> questDescriptions)
        {
            for (int i = 0; i < questDescriptions.Count; i++)
            {
                if (i == questDescriptions.Count - 1)
                    yield return addDialogueBox.AddNpcDialogueBoxForQuests(questDescriptions[i], null, null);
                else
                    yield return addDialogueBox.AddNpcDialogueBoxForQuests(questDescriptions[i], ServerConnection.Instance.OnServerMessageReceived, null);
                scrollRectHelper.ScrollToBottom();
            }
        }

        public void MakeGetQuestClickable()
        {
            if (!getQuestDone)
                getQuestButton.interactable = true;
        }

    }
}
