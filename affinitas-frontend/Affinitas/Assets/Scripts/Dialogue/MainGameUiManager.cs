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
        [SerializeField] GameObject daysLeftPanel;

        [SerializeField] TextMeshProUGUI[] affinitasTextMeshes;

        [SerializeField] GameObject[] npcDialoguePanels;
        [SerializeField] TMP_InputField[] dialogueInputFields;
        [SerializeField] TextMeshProUGUI[] dialoguePanelNameTextMeshes;

        [SerializeField] GameObject journalPanel;
        [SerializeField] GameObject[] journalTabPanels;
        [SerializeField] TextMeshProUGUI[] journalTabButtonTextMeshes;
        [SerializeField] TextMeshProUGUI[] journalTextMeshes;


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
            //GameManager.Instance.SubscribeToNpcDataLoaded(SetupDialogueListeners);
        }

        
        //private void SetupDialogueListeners()
        //{
        //    MainGameManager manager = MainGameManager.Instance;

        //    for (int i = 0; i < dialogueInputFields.Length; i++)
        //    {
        //        int capturedIndex = i;
        //        Npc npc = manager.npcList[i];
        //        AddDialogueBox dialogueUI = npcDialoguePanels[i].GetComponent<AddDialogueBox>();

        //        dialogueInputFields[i].onSubmit.AddListener((str) => SendMessageGetNpcResponse(str, npc));

        //        dialogueInputFields[i].onSubmit.AddListener(async (str) =>
        //        {
        //            //dialogueUI.AddPlayerDialogueBox(str, () => { });
        //            string npcResponse = await GameManager.Instance.CreateMessageForSendPlayerInput(str, npc.dbNpcId);

        //            Debug.Log("npc says: " + npcResponse);

        //            if (!string.IsNullOrEmpty(npcResponse))
        //            {
        //                Debug.Log("npc adds: boxxx");
        //                dialogueUI.AddNpcDialogueBox(npcResponse, null);//ServerConnection.Instance.OnServerMessageReceived);
        //            }
        //        });
        //    }
        //}

        //public async void SendMessageGetNpcResponse(string playerInput, Npc npc)
        //{
        //    string npcResponse = await GameManager.Instance.CreateMessageForSendPlayerInput(playerInput, npc.dbNpcId);

        //    Debug.Log("npc says: " + npcResponse);

        //    if (!string.IsNullOrEmpty(npcResponse))
        //    {
        //        Debug.Log("npc adds: boxxx");
        //        dialogueUI.AddNpcDialogueBox(npcResponse, null);//ServerConnection.Instance.OnServerMessageReceived);
        //    }
        //}


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

        // Call from minigame buttons with correct indexing
        public void OpenMinigameScene(int minigameSceneIndex)
        {
            //SceneManager.LoadScene(minigameSceneIndex);
            SceneManager.LoadScene(minigameSceneIndex, LoadSceneMode.Additive);
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

    }
}