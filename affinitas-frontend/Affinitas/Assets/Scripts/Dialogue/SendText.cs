using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Threading.Tasks;

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
        bool getQuestDone = false;

        [SerializeField] Button giveItemButton;
        [SerializeField] Button sendTextButton;

        void Awake()
        {
            // Send Text also when user presses Enter
            dialogueInputField.onSubmit.AddListener((str) => SendTextGetResponse());
            addDialogueBox = gameObject.GetComponent<AddDialogueBox>();
            scrollRectHelper = gameObject.GetComponent<ScrollRectHelper>();
        }

        public void InitializeDialoguePanel()
        {
            getQuestDone = MainGameManager.Instance.CheckGetQuest(npcId);
            MakeButtonsClickable();
            dialogueInputField.text = "";
        }

        // Call this from Send Text button
        public void SendTextGetResponse()
        {
            MainGameManager.Instance.ActivateJournal();

            if (MainGameManager.Instance.EnoughActionPointsForDialogue() == false)
            {
                MainGameUiManager.Instance.OpenWarningPanel("You do not have enough action points to talk. End the day!");
                return;
            }
            // Prevent spamming via pressing enter or buttons
            if (ServerConnection.Instance.canSendMessage == false)
                return;
            ServerConnection.Instance.canSendMessage = false;
            MakeButtonsUnclickable();

            // Do not send text if no text
            playerInput = dialogueInputField.text;
            if (playerInput == "")
                return;

            MainGameUiManager.Instance.ActionAfterGameSaved();

            // Send text to server immediately to cut back on wait time
            string dbNpcId = MainGameManager.Instance.npcDict[npcId].dbNpcId;
            var responseTask = GameManager.Instance.CreateMessageForSendPlayerInput(playerInput, dbNpcId);

            // Write player text to screen and when it is finished, write npc text when it is returned from server
            addDialogueBox.AddPlayerDialogueBox(playerInput, () => HandleNpcResponseAfterTyping(responseTask), null, true);
            scrollRectHelper.ScrollToBottom();

            dialogueInputField.text = "";
            dialogueInputField.ActivateInputField();
        }

        async void HandleNpcResponseAfterTyping(Task<string> responseTask)
        {
            string npcResponse = await responseTask;

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
                MainGameUiManager.Instance.OpenWarningPanel("You do not have enough action points to get a new quest.");
                return;
            }

            if (ServerConnection.Instance.canSendMessage == false)
                return;

            ServerConnection.Instance.canSendMessage = false;

            MainGameUiManager.Instance.ActionAfterGameSaved();

            MakeButtonsUnclickable();
            MainGameManager.Instance.ReduceActionPointsForGetQuest();
            MainGameUiManager.Instance.UpdateDaysLeftPanel();

            string dbNpcId = MainGameManager.Instance.npcDict[npcId].dbNpcId;

            List<string> questDescriptions = await GameManager.Instance.CreateMessageForGetQuest(dbNpcId, npcId);

            if (questDescriptions != null)
            {
                getQuestDone = true;
                addDialogueBox.AddInfoPanel("You asked for a quest!", null, null, true);

                StartCoroutine(ShowQuestsOneByOne(questDescriptions));
            }

            // if Bart Ender's get quest button is pressed, then make all map buttons visible
            if (npcId == 3)
                MainGameUiManager.Instance.ToggleMapButtonsVisibility(true);
        }

        IEnumerator ShowQuestsOneByOne(List<string> questDescriptions)
        {
            for (int i = 0; i < questDescriptions.Count; i++)
            {
                if (i == questDescriptions.Count - 1)
                    yield return addDialogueBox.AddNpcDialogueBoxForQuests(questDescriptions[i], ServerConnection.Instance.OnServerMessageReceived, MakeButtonsClickable);
                else
                    yield return addDialogueBox.AddNpcDialogueBoxForQuests(questDescriptions[i], null, null);
                if (i % 5 == 0) scrollRectHelper.ScrollToBottom();
            }
        }

        public void MakeButtonsUnclickable()
        {
            getQuestButton.interactable = false;
            giveItemButton.interactable = false;
            sendTextButton.interactable = false;
            MainGameUiManager.Instance.ToggleMapEndDaySaveButtonsActive(false);
        }

        public void MakeButtonsClickable()
        {
            getQuestButton.interactable = !getQuestDone;
            giveItemButton.interactable = true;
            sendTextButton.interactable = true;
            MainGameUiManager.Instance.ToggleMapEndDaySaveButtonsActive(true);
        }

        // Call when Give Item button pressed for an Npc
        public async void GiveItem()
        {
            Item item = MainGameUiManager.Instance.GiveItemFromInventory(npcId);
            if (item == null)
            {
                MainGameUiManager.Instance.OpenWarningPanel("You have no items to give!");
                return;
            }

            if (MainGameManager.Instance.questDict.TryGetValue(item.linkedQuestId, out Quest linkedQuest) && linkedQuest != null &&
                MainGameManager.Instance.npcDict.TryGetValue(linkedQuest.linkedNpcId, out Npc linkedNpc) && linkedNpc != null)
            {
                MainGameUiManager.Instance.ActionAfterGameSaved();

                MakeButtonsUnclickable();

                string npcResponse = await GameManager.Instance.NotifyForItemGivenToNpc(linkedNpc.dbNpcId, item.itemName);

                if (!string.IsNullOrEmpty(npcResponse))
                {
                    addDialogueBox.AddInfoPanel("You gave an item!", null, null, true);
                    addDialogueBox.AddNpcDialogueBox(npcResponse, ServerConnection.Instance.OnServerMessageReceived, MakeButtonsClickable, true);
                    scrollRectHelper.ScrollToBottom();
                }

                MainGameManager.Instance.UpdateQuestComplete(linkedNpc, linkedQuest.questId);
                await GameManager.Instance.NotifyForQuestComplete(linkedNpc, linkedQuest.questId);
            }
        }

        public void EmptyChat()
        {
            addDialogueBox.DeleteAllDialogueBoxes();
        }


        public void LoadChatHistory()
        {
            EmptyChat();

            List<List<string>> chatHistory = MainGameManager.Instance.npcDict[npcId].chatHistory;

            foreach (List<string> gameChat in chatHistory)
            {
                if (gameChat[0] == "user")
                    addDialogueBox.AddPlayerDialogueBox(gameChat[1], null, null, false);
                else if (gameChat[0] == "ai")
                    addDialogueBox.AddNpcDialogueBox(gameChat[1], null, null, false);
            }
        }

        public void InformDayEnd()
        {
            addDialogueBox.AddInfoPanel("Day ended. A new day has begun!", null, null, true);
            scrollRectHelper.ScrollToBottom();
        }
    }
}
