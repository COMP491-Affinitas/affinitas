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
        [SerializeField] TextMeshProUGUI[] journalTextMeshes;
        [SerializeField] Button journalButton;

        [SerializeField] GameObject tutorialPanel;

        [SerializeField] GameObject warningPanel;
        [SerializeField] TextMeshProUGUI warningPanelTextMesh;

        [SerializeField] GameObject saveGamePanel;
        [SerializeField] GameObject saveGameText;
        [SerializeField] TMP_InputField saveGameInputField;

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
            CloseQuestDetailslPanel();
            CloseTutoriallPanel();
            CloseJournalPanel();
            CloseWarningPanel();
            CloseSaveGamePanel();
            OpenJournalTab(-1);
            OpenMapPanel();
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
            InitializeDialoguePanels();
            LoadChatHistory();
            UpdateDaysLeftPanel();
            EmptyQuestPanel();
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
            questDetailsPanel.SetActive(true);
            ToggleActivePopupPanel(PopupPanelType.QuestDetailsPanel, true);
        }

        // Call from close (x) button on the Quest Details panel
        public void CloseQuestDetailslPanel()
        {
            //questDetailsPanel.SetActive(false);
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
            tutorialPanel.SetActive(true);
            ToggleActivePopupPanel(PopupPanelType.TutorialPanel, true);
        }

        // Call from close (x) button on the Tutorial panel
        public void CloseTutoriallPanel()
        {
            //tutorialPanel.SetActive(false);
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
            journalTextMeshes[0].text = texts[0];
            journalTextMeshes[1].text = texts[1];
        }

        // Call from Open Journal button
        public void OpenJournalPanel()
        {
            journalPanel.SetActive(true);
            AddTextToJournal();
            ToggleActivePopupPanel(PopupPanelType.JournalPanel, true);
        }

        // Call from close (x) button on the Journal panel
        public void CloseJournalPanel()
        {
            //journalPanel.SetActive(false);
            ToggleActivePopupPanel(PopupPanelType.JournalPanel, false);
        }

        public void OpenWarningPanel(string warningText)
        {
            warningPanel.SetActive(true);
            ToggleActivePopupPanel(PopupPanelType.WarningPanel, true);
            warningPanelTextMesh.text = warningText;
        }

        // Call from close (x) button on the Warning panel
        public void CloseWarningPanel()
        {
            //warningPanel.SetActive(false);
            ToggleActivePopupPanel(PopupPanelType.WarningPanel, false);
        }

        public void OpenSaveGamePanel()
        {
            saveGamePanel.SetActive(true);
            ToggleActivePopupPanel(PopupPanelType.SaveGamePanel, true);
            saveGameText.SetActive(false);
        }

        public string GetSaveNameFromPanel()
        {
            string saveName = saveGameInputField.text;
            saveGameInputField.text = "";
            saveGameText.GetComponent<TextMeshProUGUI>().text = "Game saved as \"" + saveName + "\"";
            saveGameText.SetActive(true);
            return saveName;
        }

        // Call from close (x) button on the Warning panel
        public void CloseSaveGamePanel()
        {
            //saveGamePanel.SetActive(false);
            ToggleActivePopupPanel(PopupPanelType.SaveGamePanel, false);
        }

        //Call when End Day button is pressed
        // TODO: if days left is zero, also update End Day button to say End Game!!!
        public void UpdateDaysLeftPanel()
        {
            string panelText = "Day No: " + MainGameManager.Instance.dayNo.ToString() + "\n\nAction Points Left: " + MainGameManager.Instance.dailyActionPoints.ToString();
            daysLeftPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = panelText;
        }

        // Call when Tab buttons are clicked
        public void OpenJournalTab(int index)
        {
            for (int i = 0; i < journalTabPanels.Length; i++)
            {
                //npcDialoguePanels[i].SetActive(i == (index-1));
                bool isActive = i == index - 1;
                journalTabPanels[i].alpha = isActive ? 1f : 0f;
                journalTabPanels[i].interactable = isActive;
                journalTabPanels[i].blocksRaycasts = isActive;
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
                //npcDialoguePanels[i].SetActive(i == (index-1));
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
            //SceneManager.LoadScene(minigameSceneIndex);
            SceneManager.LoadScene(minigameSceneIndex, LoadSceneMode.Additive);
        }

        // Call from End Day button
        public void EndDayButton()
        {
            MainGameManager.Instance.EndDay();
        }


        // npcId indexing starts from 1
        public void InitializeNpcUIs(Npc npcData)
        {
            int i = npcData.npcId - 1;
            questDetailsTabButtonTextMeshes[i].text = npcData.npcName;
            dialoguePanelNameTextMeshes[i].text = npcData.npcName;
            affinitasTextMeshes[i].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
        }

        public void UpdateNpcAffinitasUi(Npc npcData)
        {
            affinitasTextMeshes[npcData.npcId - 1].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
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
        }

        public void UpdateQuestInQuestPanel(string questId, string status)
        {
            Debug.Log("why not work?");
            if (status.Equals(MainGameManager.Instance.questDict[QuestStatus.Completed]))
            {
                TextMeshProUGUI questTextMesh = instantiatedQuests[questId];
                string oldQuestText = questTextMesh.text;
                questTextMesh.text = "<s>" + oldQuestText + "</s>";
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
            gusFishItem.transform.SetParent(unusedItemsContent);
            foreach (UseItem moraPiece in moraPieceItems)
            {
                moraPiece.transform.SetParent(unusedItemsContent);
            }
        }

        public void InitializeDialoguePanels()
        {
            foreach (SendText sendText in npcDialogueSendTexts)
            {
                // TODO: main game manager dan current Npc quests al ve npc id lerine göre Get quest butonunu inaktif yap
                sendText.InitializeDialoguePanels();
            }
        }

        public void LoadChatHistory()
        {
            foreach (SendText sendText in npcDialogueSendTexts)
            {
                // TODO: main game manager dan current Npc quests al ve npc id lerine göre Get quest butonunu inaktif yap
                sendText.LoadChatHistory();
            }
        }

        // When subquest is completed, return itemName of given item to notify server
        public string NpcGivesItemToPlayer(int npcId)
        {
            if (npcId == 1)
            {
                foreach (UseItem moraPiece in moraPieceItems)
                {
                    if (!moraPiece.inInventory)
                    {
                        moraPiece.transform.SetParent(inventoryContent);
                        moraPiece.inInventory = true;
                        return moraPiece.itemName;
                    }
                }
            }
            // For Gus this comes from minigame
            else if (npcId == 2)
            {
                if (!gusFishItem.inInventory)
                {
                    gusFishItem.transform.SetParent(inventoryContent);
                    gusFishItem.inInventory = true;
                    return gusFishItem.itemName;
                }
            }
            return null;
        }

        // Call from Give Item to Npc button
        public string PlayerGivesItemToNpc(int npcId)
        {
            if (npcId == 1)
            {
                foreach (UseItem moraPiece in moraPieceItems)
                {
                    if (moraPiece.inInventory)
                    {
                        moraPiece.transform.SetParent(unusedItemsContent);
                        moraPiece.inInventory = false;
                        return moraPiece.itemName;
                    }
                }
            }
            // For Gus this comes from minigame
            else if (npcId == 2)
            {
                if (gusFishItem.inInventory)
                {
                    gusFishItem.transform.SetParent(unusedItemsContent);
                    gusFishItem.inInventory = false;
                    return gusFishItem.itemName;
                }
            }
            return null;
        }

        public void ToggleJournalButtonActive(bool newActive)
        {
            journalButton.interactable = newActive;
        }
    }
}