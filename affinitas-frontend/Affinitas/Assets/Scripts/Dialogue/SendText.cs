using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

namespace MainGame
{
    public class SendText : MonoBehaviour
    {
        // This code needs to be on DialogueScrollView
        [SerializeField] int npcId;
        [SerializeField] TMP_InputField dialogueInputField;
        string playerInput;
        AddDialogueBox addDialogueBox;
        ScrollRectHelper scrollRectHelper;

        [SerializeField] Button getQuestButton;
        bool getQuestDone;

        [SerializeField] Button giveItemButton;
        [SerializeField] Button sendTextButton;

        void Awake()
        {
            // Send Text also when user presses Enter
            dialogueInputField.onSubmit.AddListener((str) => SendInputtedText());
            dialogueInputField.onSubmit.AddListener((str) => SendTextGetNpcResponse(str));
            addDialogueBox = gameObject.GetComponent<AddDialogueBox>();
            scrollRectHelper = gameObject.GetComponent<ScrollRectHelper>();
        }

        public void InitializeDialoguePanels()
        {
            dialogueInputField.text = "";
            //TODO:
            //bool getQuestDone = ;
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

            MakeButtonsUnclickable();

            MainGameManager.Instance.ReduceActionPointsForDialogue(MainGameManager.Instance.npcList[npcId - 1].npcName);
            MainGameUiManager.Instance.UpdateDaysLeftPanel();

            playerInput = dialogueInputField.text;
            if (playerInput == "")
                return;

            addDialogueBox.AddPlayerDialogueBox(playerInput, null, null, true);
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
                addDialogueBox.AddNpcDialogueBox(npcResponse, ServerConnection.Instance.OnServerMessageReceived, MakeButtonsClickable, true);
                scrollRectHelper.ScrollToBottom();
            }
        }

        //Call when Get Quest is pressed
        public async void GetQuestText()
        {
            Debug.Log("get quest button pressed");

            if (MainGameManager.Instance.EnoughActionPointsForGetQuest() == false)
            {
                MainGameUiManager.Instance.OpenWarningPanel("You do not have enough action points to get a new quest. End the day!");
                return;
            }

            if (ServerConnection.Instance.canSendMessage == false)
                return;

            ServerConnection.Instance.canSendMessage = false;

            MakeButtonsUnclickable();
            MainGameManager.Instance.ReduceActionPointsForGetQuest(MainGameManager.Instance.npcList[npcId - 1].npcName);
            MainGameUiManager.Instance.UpdateDaysLeftPanel();

            string dbNpcId = MainGameManager.Instance.npcList[npcId - 1].dbNpcId;

            Debug.Log("sending get quest request to server");

            List<string> questDescriptions = await GameManager.Instance.CreateMessageForGetQuest(dbNpcId, npcId);

            Debug.Log("quest: " + questDescriptions[0]);

            if (questDescriptions != null)
            {
                getQuestDone = true;

                StartCoroutine(ShowQuestsOneByOne(questDescriptions));
            }

            // if Bart Ender's get quest button is pressed, then make all map buttons visible
            if (npcId == 3)
            {
                MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);
            }
        }

        IEnumerator ShowQuestsOneByOne(List<string> questDescriptions)
        {
            for (int i = 0; i < questDescriptions.Count; i++)
            {
                if (i == questDescriptions.Count - 1)
                    yield return addDialogueBox.AddNpcDialogueBoxForQuests(questDescriptions[i], ServerConnection.Instance.OnServerMessageReceived, MakeButtonsClickable);
                else
                    yield return addDialogueBox.AddNpcDialogueBoxForQuests(questDescriptions[i], null, null);
                scrollRectHelper.ScrollToBottom();
            }
        }

        public void MakeButtonsUnclickable()
        {
            getQuestButton.interactable = false;
            giveItemButton.interactable = false;
            sendTextButton.interactable = false;
        }

        public void MakeButtonsClickable()
        {
            if (!getQuestDone)
                getQuestButton.interactable = true;
            giveItemButton.interactable = true;
            sendTextButton.interactable = true;
        }


        // Call when Give Item button pressed for an Npc
        public async void GiveItem()
        {
            // If not Gus or Mora, no item can be given
            string itemName = MainGameManager.Instance.PlayerGivesItemToNpc(npcId);
            if (itemName == null)
            {
                MainGameUiManager.Instance.OpenWarningPanel("You have no items to give!");
                return;
            }

            MakeButtonsUnclickable();

            string dbNpcId = MainGameManager.Instance.npcList[npcId - 1].dbNpcId;
            string npcResponse = await GameManager.Instance.NotifyForItemGivenToNpc(dbNpcId, itemName);

            if(!string.IsNullOrEmpty(npcResponse))
            {
                addDialogueBox.AddNpcDialogueBox(npcResponse, ServerConnection.Instance.OnServerMessageReceived, MakeButtonsClickable, true);
                scrollRectHelper.ScrollToBottom();
            }
        }

        public void EmptyChat()
        {
            addDialogueBox.DeleteAllDialogueBoxes();
        }


        public void LoadChatHistory()
        {
            EmptyChat();

            List<List<string>> chatHistory = MainGameManager.Instance.npcList[npcId - 1].chatHistory;

            foreach (List<String> gameChat in chatHistory)
            {
                if (gameChat[0] == "user")
                    addDialogueBox.AddPlayerDialogueBox(gameChat[1], null, null, false);
                else if (gameChat[0] == "ai")
                    addDialogueBox.AddNpcDialogueBox(gameChat[1], null, null, false);
            }
        }
    }
}
