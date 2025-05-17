using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] TMP_InputField[] dialogueInputFields;
        [SerializeField] TextMeshProUGUI[] dialoguePanelNameTextMeshes;

        [SerializeField] GameObject journalPanel;
        [SerializeField] GameObject[] journalTabPanels;
        [SerializeField] TextMeshProUGUI[] journalTabButtonTextMeshes;
        [SerializeField] TextMeshProUGUI[] journalTextMeshes;

        [SerializeField] GameObject warningPanel;
        [SerializeField] TextMeshProUGUI warningPanelTextMesh;

        [SerializeField] GameObject saveGamePanel;
        [SerializeField] TMP_InputField saveGameInputField;

        [SerializeField] GameObject questPanelContent;
        [SerializeField] GameObject questPrefab;
        Dictionary<string, TextMeshProUGUI> instantiatedQuests = new();

        [SerializeField] GameObject gusFishItem;
        [SerializeField] GameObject[] moraPieceItems;

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
            InitilizeMainPanels();
        }

        public void InitilizeMainPanels()
        {
            CloseJournalPanel();
            CloseWarningPanel();
            CloseSaveGamePanel();
            OpenJournalTab(1);
            UpdateDaysLeftPanel();
            OpenMapPanel();
            for (int i = 0; i < journalTextMeshes.Length; i++)
            {
                journalTextMeshes[i].text = "";
            }
        }

        // Call from Open Journal button
        public void OpenJournalPanel()
        {
            journalPanel.SetActive(true);
        }

        // Call from close (x) button on the Journal panel
        public void CloseJournalPanel()
        {
            journalPanel.SetActive(false);
        }

        public void OpenWarningPanel(string warningText)
        {
            warningPanel.SetActive(true);
            warningPanelTextMesh.text = warningText;
        }

        // Call from close (x) button on the Warning panel
        public void CloseWarningPanel()
        {
            warningPanel.SetActive(false);
        }

        public void OpenSaveGamePanel()
        {
            saveGamePanel.SetActive(true);
        }

        public string GetSaveNameFromPanel()
        {
            return saveGameInputField.text;
        }

        // Call from close (x) button on the Warning panel
        public void CloseSaveGamePanel()
        {
            saveGamePanel.SetActive(false);
        }

        //Call when End Day button is pressed
        // TODO: if days left is zero, also update End Day button to say End Game!!!
        public void UpdateDaysLeftPanel()
        {
            string panelText = "Day No: " + MainGameManager.Instance.dayNo.ToString() + "\n\nAction Points Left: " + MainGameManager.Instance.dailyActionPoints.ToString();
            daysLeftPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = panelText;
        }

        // Call when Tab buttons are clicked
        // indexing starts at 1 for NPC1
        public void OpenJournalTab(int index)
        {
            for (int i = 0; i < journalTabPanels.Length; i++)
            {
                journalTabPanels[i].SetActive(i == (index-1));
            }
        }

        // Call when House button is pressed for corresponding NPC
        // indexing starts at 1 for NPC1
        public void OpenCharacterDialogue(int index)
        {
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
                OpenWarningPanel("You do not have enough action points to play this minigame. End the day!");
                return;
            }
            MainGameManager.Instance.ReduceActionPointsForMinigame(minigameSceneIndex-1);
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
            journalTabButtonTextMeshes[i].text = npcData.npcName;
            dialoguePanelNameTextMeshes[i].text = npcData.npcName;
            affinitasTextMeshes[i].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
        }

        public void UpdateNpcAffinitasUi(Npc npcData)
        {
            affinitasTextMeshes[npcData.npcId - 1].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
        }

        public void AddQuestToQuestPanel(string questId, string questText)
        {
            GameObject newQuest = Instantiate(questPrefab);
            newQuest.transform.SetParent(questPanelContent.transform, false);

            TextMeshProUGUI newQuestTextMesh = newQuest.GetComponent<TextMeshProUGUI>();
            newQuestTextMesh.text = questText;

            instantiatedQuests[questId] = newQuestTextMesh;
        }

        public void AddQuestToJournal(int npcId, string questText)
        {
            journalTextMeshes[npcId - 1].text += questText;
        }

        public void UpdateQuestInQuestPanel(string questId, QuestStatus status)
        {
            TextMeshProUGUI questTextMesh = instantiatedQuests[questId];
            string oldQuestText = questTextMesh.text;
            string newQuestText;
            if (status == QuestStatus.Completed)
            {
                newQuestText = "<s>" + oldQuestText + "</s>";
                questTextMesh.text = newQuestText;
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

        public void AddMoraPieceToInventory()
        {
            foreach (GameObject moraPiece in moraPieceItems)
            {
                if (moraPiece.activeSelf == false)
                {
                    moraPiece.SetActive(true);
                    return;
                }
            }
        }

        public void AddGusFishToInventory()
        {
            gusFishItem.SetActive(true);
        }

        public void RemoveAllItemsFromInventory()
        {
            gusFishItem.SetActive(false);
            foreach (GameObject moraPiece in moraPieceItems)
            {
                moraPiece.SetActive(false);
            }
        }

        //TODO: write code to give only a number of items from mora pieces for load game

        public void GiveItemToMora()
        {
            foreach (GameObject moraPiece in moraPieceItems)
            {
                moraPiece.SetActive(false);
            }
        }

        public void GiveItemToGus()
        {
            gusFishItem.SetActive(false);
        }

    }
}