using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace MainGame
{
    public class MainGameUiManager : MonoBehaviour
    {
        // Singleton
        public static MainGameUiManager Instance { get; private set; }

        [SerializeField] GameObject mapPanel;
        [SerializeField] GameObject[] mapButtons; // all map buttons except for Bart Ender's house
        [SerializeField] GameObject daysLeftPanel;

        [SerializeField] TextMeshProUGUI[] affinitasTextMeshes;

        [SerializeField] CanvasGroup[] npcDialoguePanels;
        [SerializeField] SendText[] npcDialogueSendTexts;
        [SerializeField] TMP_InputField[] dialogueInputFields;
        [SerializeField] TextMeshProUGUI[] dialoguePanelNameTextMeshes;

        [SerializeField] GameObject questDetailsPanel;
        [SerializeField] CanvasGroup[] questDetailsTabPanels;
        [SerializeField] TextMeshProUGUI[] questDetailsTabButtonTextMeshes;
        [SerializeField] TextMeshProUGUI[] questDetailsTextMeshes;

        [SerializeField] GameObject journalPanel;
        [SerializeField] CanvasGroup[] journalTabPanels;
        [SerializeField] CanvasGroup[] journalCharacterTabPanels;
        [SerializeField] TextMeshProUGUI[] journalCharacterTabButtonTextMeshes;
        [SerializeField] TextMeshProUGUI[] journalTextMeshes;
        [SerializeField] Button journalButton;

        [SerializeField] Button endDayButton;
        [SerializeField] Button mapButton;
        [SerializeField] Button goToMenuButton;

        [SerializeField] GameObject tutorialPanel;

        [SerializeField] GameObject warningPanel;
        [SerializeField] TextMeshProUGUI warningPanelTextMesh;
        [SerializeField] GameObject goToMenuAnywayButton;

        [SerializeField] GameObject saveGamePanel;
        [SerializeField] GameObject saveGameText;
        [SerializeField] TMP_InputField saveGameInputField;
        [SerializeField] Button saveGameButton;

        [SerializeField] GameObject questPanelContent;
        [SerializeField] GameObject questPrefab;
        Dictionary<string, TextMeshProUGUI> instantiatedQuests = new();

        [SerializeField] Transform inventoryContent;
        [SerializeField] Transform unusedItemsContent;
        public UseItem gusFishItem;
        public UseItem[] moraPieceItems;

        enum PopupPanelType
        {
            JournalPanel,
            TutorialPanel,
            QuestDetailsPanel,
            WarningPanel,
            SaveGamePanel
        }

        Dictionary<PopupPanelType, CanvasGroup> popupPanels;
        Dictionary<string, UseItem> useItemDict;
        bool doneActionAfterSaveGame = false;

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            popupPanels = new(){
                { PopupPanelType.JournalPanel, journalPanel.GetComponent<CanvasGroup>() },
                { PopupPanelType.TutorialPanel, tutorialPanel.GetComponent<CanvasGroup>() },
                { PopupPanelType.QuestDetailsPanel, questDetailsPanel.GetComponent<CanvasGroup>() },
                { PopupPanelType.WarningPanel, warningPanel.GetComponent<CanvasGroup>() },
                { PopupPanelType.SaveGamePanel, saveGamePanel.GetComponent<CanvasGroup>() }
            };
            InitializeMainPanels();
        }

        // Every time a game is started new or saved
        public void InitializeMainPanels()
        {
            questDetailsPanel.SetActive(true);
            journalPanel.SetActive(true);
            tutorialPanel.SetActive(true);
            warningPanel.SetActive(true);
            saveGamePanel.SetActive(true);
            CloseQuestDetailslPanel();
            CloseTutoriallPanel();
            CloseJournalPanel();
            CloseWarningPanel();
            CloseSaveGamePanel();
            OpenJournalTab(-1);
            OpenQuestDetailTab(-1);
            OpenJournalCharacterTab(-1);
            OpenMapPanel();
            InitializeItemDict();
            RemoveAllItemsFromInventory();
            EmptyQuestPanel();
        }

        public void InitializeMainPanelsForNewGame()
        {
            EmptyJournal();
            EmptyQuestDetails();
            OpenQuestDetailTab(-1);
            UpdateDaysLeftPanel();
            InitializeDialoguePanels();
            EmptyQuestPanel();
        }

        public void InitializeMainPanelsForSavedGame()
        {
            EmptyQuestPanel();
            InitializeDialoguePanels();
            LoadChatHistory();
            UpdateDaysLeftPanel();
            InitializeInventoryForSavedGame();
        }

        void InitializeItemDict()
        {
            useItemDict = new();
            useItemDict[gusFishItem.itemName] = gusFishItem;
            foreach (UseItem item in moraPieceItems)
            {
                useItemDict[item.itemName] = item;
            }
        }

        // Call from Send text, Get quest, Give item, End day buttons
        public void ActionAfterGameSaved()
        {
            doneActionAfterSaveGame = true;
        }

        // Call from Go to Menu button in main panel
        public void GoToMenuIfSaved()
        {
            if (doneActionAfterSaveGame)
            {
                OpenWarningPanel("You should consider saving before you leave the game!");
                ToggleWarningPanelButtonActive(true);
            }
            else
            {
                UIManager.Instance.GoToMenu();
                GameManager.Instance.EndCurrentGame();
                doneActionAfterSaveGame = false;
            }
                
        }

        void ToggleWarningPanelButtonActive(bool newActive)
        {
            goToMenuAnywayButton.SetActive(newActive);
        }

        public void EmptyQuestDetails()
        {
            for (int i = 0; i < questDetailsTextMeshes.Length; i++)
            {
                questDetailsTextMeshes[i].text = "";
            }
        }

        void ToggleActivePopupPanel(PopupPanelType popupPanelType, bool isActive)
        {
            popupPanels[popupPanelType].alpha = isActive ? 1f : 0f;
            popupPanels[popupPanelType].interactable = isActive;
            popupPanels[popupPanelType].blocksRaycasts = isActive;
        }

        // Call from Quest Details button
        public void OpenQuestDetailsPanel()
        {
            ToggleActivePopupPanel(PopupPanelType.QuestDetailsPanel, true);
        }

        // Call from close (x) button on the Quest Details panel
        public void CloseQuestDetailslPanel()
        {
            ToggleActivePopupPanel(PopupPanelType.QuestDetailsPanel, false);
        }

        // Call when Tab buttons are clicked in Quest Details
        // indexing starts at 1 for NPC1
        public void OpenQuestDetailTab(int index)
        {
            for (int i = 0; i < questDetailsTabPanels.Length; i++)
            {
                //npcDialoguePanels[i].SetActive(i == (index-1));
                bool isActive = i == index - 1;
                questDetailsTabPanels[i].alpha = isActive ? 1f : 0f;
                questDetailsTabPanels[i].interactable = isActive;
                questDetailsTabPanels[i].blocksRaycasts = isActive;
            }
        }

        // Call from Open Tutorial button
        public void OpenTutorialPanel()
        {
            ToggleActivePopupPanel(PopupPanelType.TutorialPanel, true);
        }

        // Call from close (x) button on the Tutorial panel
        public void CloseTutoriallPanel()
        {
            ToggleActivePopupPanel(PopupPanelType.TutorialPanel, false);
        }

        public void EmptyJournal()
        {
            for (int i = 0; i < journalTextMeshes.Length; i++)
            {
                journalTextMeshes[i].text = "";
            }
        }

        public void AddTextToJournal()
        {
            List<string> texts = MainGameManager.Instance.CreateJournalText();
            for (int i = 0; i < journalTextMeshes.Length; i++)
            {
                journalTextMeshes[i].text = texts[i];
                RefreshPanelLayout(journalTextMeshes[i]);
            }
        }

        IEnumerator RefreshPanelLayout(TextMeshProUGUI textMesh)
        {
            yield return new WaitForEndOfFrame();
            textMesh.text += " ";
            yield return new WaitForEndOfFrame();
            textMesh.text += " ";
        }

        // Call from Open Journal button
        public void OpenJournalPanel()
        {
            AddTextToJournal();
            ToggleActivePopupPanel(PopupPanelType.JournalPanel, true);
        }

        // Call from close (x) button on the Journal panel
        public void CloseJournalPanel()
        {
            ToggleActivePopupPanel(PopupPanelType.JournalPanel, false);
        }

        public void OpenWarningPanel(string warningText)
        {
            ToggleActivePopupPanel(PopupPanelType.WarningPanel, true);
            ToggleWarningPanelButtonActive(false);
            warningPanelTextMesh.text = warningText;
        }

        // Call from close (x) button on the Warning panel
        public void CloseWarningPanel()
        {
            ToggleActivePopupPanel(PopupPanelType.WarningPanel, false);
        }

        public void OpenSaveGamePanel()
        {
            ToggleActivePopupPanel(PopupPanelType.SaveGamePanel, true);
            saveGameText.SetActive(false);
        }

        public string GetSaveNameFromPanel()
        {
            string saveName = saveGameInputField.text;
            saveGameInputField.text = "";
            saveGameText.GetComponent<TextMeshProUGUI>().text = "Game saved as \"" + saveName + "\"";
            saveGameText.SetActive(true);
            doneActionAfterSaveGame = false;
            return saveName;
        }

        // Call from close (x) button on the Warning panel
        public void CloseSaveGamePanel()
        {
            ToggleActivePopupPanel(PopupPanelType.SaveGamePanel, false);
        }

        //Call when End Day button is pressed
        public void UpdateDaysLeftPanel()
        {
            string panelText = "Day No: " + MainGameManager.Instance.dayNo.ToString() + "\n\nAction Points Left: " + MainGameManager.Instance.dailyActionPoints.ToString();
            daysLeftPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = panelText;
            UpdateEndGameButtonText();
        }

        // If on 10th day then  update End Day button text to say End Game!!!
        void UpdateEndGameButtonText()
        {
            if (MainGameManager.Instance.dayNo < 10)
                endDayButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "End Day";
            else
                endDayButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "End Game";
        }

        // Call when Tab buttons are clicked
        public void OpenJournalTab(int index)
        {
            for (int i = 0; i < journalTabPanels.Length; i++)
            {
                bool isActive = i == index - 1;
                journalTabPanels[i].alpha = isActive ? 1f : 0f;
                journalTabPanels[i].interactable = isActive;
                journalTabPanels[i].blocksRaycasts = isActive;
            }
        }

        // Call when Tab buttons are clicked
        public void OpenJournalCharacterTab(int index)
        {
            for (int i = 0; i < journalCharacterTabPanels.Length; i++)
            {
                bool isActive = i == index - 1;
                journalCharacterTabPanels[i].alpha = isActive ? 1f : 0f;
                journalCharacterTabPanels[i].interactable = isActive;
                journalCharacterTabPanels[i].blocksRaycasts = isActive;
            }
        }

        // Call when House button is pressed for corresponding NPC
        // indexing starts at 1 for NPC1
        public void OpenCharacterDialogue(int index)
        {
            if (MainGameManager.Instance.EnoughActionPointsForDialogue() == false)
            {
                OpenWarningPanel("You do not have enough action points to see this NPC. End the day!");
                return;
            }
            MainGameManager.Instance.ReduceActionPointsForDialogue();
            UpdateDaysLeftPanel();
            mapPanel.SetActive(false);
            for (int i = 0; i < npcDialoguePanels.Length; i++)
            {
                bool isActive = i == index-1;
                npcDialoguePanels[i].alpha = isActive ? 1f : 0f;
                npcDialoguePanels[i].interactable = isActive;
                npcDialoguePanels[i].blocksRaycasts = isActive;
            }
        }

        // Call from Go to Map button
        public void OpenMapPanel()
        {
            mapPanel.SetActive(true);
        }

        // Call from minigame buttons with correct indexing (starts from 1 since 0 is main game index)
        public void OpenMinigameScene(int minigameSceneIndex)
        {
            if (MainGameManager.Instance.EnoughActionPointsForMinigame() == false)
            {
                OpenWarningPanel("You do not have enough action points to play this minigame.");
                return;
            }
            MainGameManager.Instance.ReduceActionPointsForMinigame();
            UpdateDaysLeftPanel();
            SceneManager.LoadScene(minigameSceneIndex, LoadSceneMode.Additive);
        }

        // Call from End Day button
        public void EndDay()
        {
            MainGameManager.Instance.EndDay();
            InformDayEndForAll();
        }

        // npcId indexing starts from 1
        public void InitializeNpcUIs(Npc npcData)
        {
            int i = npcData.npcId - 1;
            questDetailsTabButtonTextMeshes[i].text = npcData.npcName;
            journalCharacterTabButtonTextMeshes[i].text = npcData.npcName;
            dialoguePanelNameTextMeshes[i].text = npcData.npcName;
            affinitasTextMeshes[i].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
        }

        public void UpdateNpcAffinitasUi(Npc npcData)
        {
            affinitasTextMeshes[npcData.npcId - 1].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
        }

        public void EmphasizeAffinitas(Npc npcData)
        {
            StartCoroutine(EmphasizeAffinitasCoroutine(npcData));
        }

        IEnumerator EmphasizeAffinitasCoroutine(Npc npcData)
        {
            for (int i = 0; i < 5; i++)
            {
                affinitasTextMeshes[npcData.npcId - 1].text = $@"<size=24>{npcData.npcName}\nAffinitas: <b><color=red>{npcData.affinitasValue}</color></b></size>";
                yield return new WaitForSeconds(0.3f);

                affinitasTextMeshes[npcData.npcId - 1].text = $@"<size=24>{npcData.npcName}\nAffinitas: <b>{npcData.affinitasValue}</b></size>";
                yield return new WaitForSeconds(0.3f);
            }
            affinitasTextMeshes[npcData.npcId - 1].text = $@"<size=24>{npcData.npcName}\nAffinitas: {npcData.affinitasValue}</size>";
        }


        public void EmptyQuestPanel()
        {
            for (int i = questPanelContent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(questPanelContent.transform.GetChild(i).gameObject);
            }
        }

        public void AddQuestToQuestPanel(string questId, string questText, string status)
        {
            GameObject newQuest = Instantiate(questPrefab);
            newQuest.transform.SetParent(questPanelContent.transform, false);

            TextMeshProUGUI newQuestTextMesh = newQuest.GetComponent<TextMeshProUGUI>();
            newQuestTextMesh.text = questText;

            instantiatedQuests[questId] = newQuestTextMesh;

            UpdateQuestInQuestPanel(questId, status);
        }

        public void AddQuestToQuestDetails(int npcId, string questText)
        {
            questDetailsTextMeshes[npcId - 1].text += questText;
            RefreshPanelLayout(questDetailsTextMeshes[npcId - 1]);
        }

        public void UpdateQuestInQuestPanel(string questId, string status)
        {
            if (status.Equals(MainGameManager.Instance.questStatusDict[QuestStatus.Completed]))
            {
                if (instantiatedQuests.TryGetValue(questId, out TextMeshProUGUI questTextMesh) && questTextMesh != null)
                {
                    string oldQuestText = questTextMesh.text;
                    questTextMesh.text = "<s>" + oldQuestText + "</s>";
                }
            }
        }

        // Make everyone but Bart Ender's houses invisible at the beginning of the game
        public void ToggleMapButtonsVisibility(bool visibility)
        {
            foreach (GameObject mapButton in mapButtons)
            {
                mapButton.SetActive(visibility);
            }
        }

        public void RemoveAllItemsFromInventory()
        {
            gusFishItem.inInventory = false;
            gusFishItem.transform.SetParent(unusedItemsContent);
            foreach (UseItem moraPiece in moraPieceItems)
            {
                moraPiece.inInventory = false;
                moraPiece.transform.SetParent(unusedItemsContent);
            }
        }

        public void InitializeDialoguePanels()
        {
            foreach (SendText sendText in npcDialogueSendTexts)
            {
                sendText.InitializeDialoguePanel();
            }
        }

        public void LoadChatHistory()
        {
            foreach (SendText sendText in npcDialogueSendTexts)
            {
                sendText.LoadChatHistory();
            }
        }

        public void InformDayEndForAll()
        {
            foreach(SendText sendText in npcDialogueSendTexts)
            {
                sendText.InformDayEnd();
            }
        }

        // When subquest is completed, return itemName of given item to notify server
        public bool AddItemToInventory(string itemName)
        {
            Debug.Log("try adding item: " + itemName);
            if (useItemDict.TryGetValue(itemName, out UseItem item) && item != null)
            {
                Debug.Log("found item: " + item.itemName);
                if (!item.inInventory)
                {
                    item.transform.SetParent(inventoryContent);
                    item.inInventory = true;
                    return true;
                }
            }
            return false;
        }

        // Call from Give Item to Npc button
        public Item GiveItemFromInventory(int npcId)
        {
            foreach (Item item in MainGameManager.Instance.itemDict.Values)
            {
                if (MainGameManager.Instance.questDict.TryGetValue(item.linkedQuestId, out Quest linkedQuest) && linkedQuest != null)
                {
                    if (linkedQuest.linkedNpcId == npcId)
                    {
                        if (useItemDict.TryGetValue(item.itemName, out UseItem useItem) && useItem != null)
                        {
                            if (useItem.inInventory)
                            {
                                useItem.transform.SetParent(unusedItemsContent);
                                useItem.inInventory = false;
                                return item;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public void ToggleJournalButtonActive(bool newActive)
        {
            journalButton.interactable = newActive;
        }

        public void InitializeInventoryForSavedGame()
        {
            foreach (Item item in MainGameManager.Instance.itemDict.Values)
            {
                if (item.active)
                {
                    if (useItemDict.TryGetValue(item.itemName, out UseItem useItem) && useItem != null)
                    {
                        useItem.transform.SetParent(inventoryContent);
                        useItem.inInventory = true;
                    }
                }
            }
        }

        public void ToggleMapEndDaySaveButtonsActive(bool newActive)
        {
            endDayButton.interactable = newActive;
            saveGameButton.interactable = newActive;
            mapButton.interactable = newActive;
            goToMenuButton.interactable = newActive;
        }
    }
}