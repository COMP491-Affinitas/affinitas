using TMPro;
using UnityEngine;

namespace MainGame
{
    public class MainGameUiManager : MonoBehaviour
    {
        [SerializeField]
        GameObject journalPanel;
        [SerializeField]
        GameObject[] journalTabPanels;

        [SerializeField]
        GameObject mapPanel;
        [SerializeField]
        GameObject[] npcDialoguePanels;

        GameObject daysLeftPanel;

        private void Start()
        {
            InitilizeMainPanels();
        }

        void InitilizeMainPanels()
        {
            CloseJournalPanel();
            OpenJournalTab(1);
            UpdateDaysLeftPanel();
            OpenMapPanel();
            OpenCharacterDialogue(-1);
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
            daysLeftPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = "Day No: " + GameManager.Instance.dayNo.ToString();
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
    }
}