using TMPro;
using UnityEngine;

namespace MainGame
{
    public class MainGameUiManager : MonoBehaviour
    {
        // Singleton
        public static MainGameUiManager Instance { get; private set; }

        [SerializeField] GameObject mapPanel;
        [SerializeField] GameObject daysLeftPanel;

        [SerializeField] TextMeshProUGUI[] affinitasTextMeshes;

        [SerializeField] GameObject[] npcDialoguePanels;
        [SerializeField] TMP_InputField[] dialogueInputFields;

        [SerializeField] GameObject journalPanel;
        [SerializeField] GameObject[] journalTabPanels;
        [SerializeField] TextMeshProUGUI[] journalTabButtonTextMeshes;
        [SerializeField] TextMeshProUGUI[] journalTextMeshes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            InitilizeMainPanels();

            // Send Text also when user presses Enter
            foreach (TMP_InputField dialogueInputField in dialogueInputFields)
            {
                //TODO: 
                //dialogueInputField.onSubmit.AddListener((str) => CreateMessageForSendPlayerInput(str));
            }
        }

        public void InitilizeMainPanels()
        {
            CloseJournalPanel();
            OpenJournalTab(1);
            UpdateDaysLeftPanel();
            OpenCharacterDialogue(-1);
            OpenMapPanel();
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

        //Call when End Day button is pressed
        // TODO: if days left is zero, also update End Day button to say End Game!!!
        public void UpdateDaysLeftPanel()
        {
            daysLeftPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = "Day No: " + MainGameManager.Instance.dayNo.ToString();
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
                npcDialoguePanels[i].SetActive(i == (index-1));
            }
        }

        // Call from Go to Map button
        public void OpenMapPanel()
        {
            mapPanel.SetActive(true);
        }

        // npcId indexing starts from 1
        public void InitializeNpcUIs(Npc npcData)
        {
            int i = npcData.npcId - 1;
            //npcList.Insert(i, npcData);
            journalTabButtonTextMeshes[i].text = npcData.npcName;
            affinitasTextMeshes[i].text = npcData.npcName + "\nAffinitas: " + npcData.affinitasValue.ToString();
        }

        void UpdateAffinitasUIs()
        {
            // Just iterate over all npcs or use a function that takes npcId
            foreach (Npc npc in MainGameManager.Instance.npcList)
            {
                int i = npc.npcId - 1;
                affinitasTextMeshes[i].text = npc.npcName + "\nAffinitas: " + npc.affinitasValue.ToString();
            }
        }
    }
}