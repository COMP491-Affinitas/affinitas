using TMPro;
using UnityEngine;

namespace MainGame
{
    public class MainGameUiManager : MonoBehaviour
    {
        [SerializeField]
        GameObject warningPanel;

        [SerializeField]
        GameObject journalPanel;

        [SerializeField]
        GameObject daysLeftPanel;

        private void Start()
        {
            warningPanel.SetActive(false);
            journalPanel.SetActive(false);
        }

        public void OpenWarningPanel(string warningText)
        {
            if (warningText == "")
                warningText = "Warning.";

            // Make sure WarningText is first child of Panel
            TextMeshProUGUI warningTextMesh = warningPanel.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            warningTextMesh.text = warningText;
            warningPanel.SetActive(true);
        }

        public void CloseWarningPanel()
        {
            warningPanel.SetActive(false);
        }

        public void OpenJournalPanel()
        {
            journalPanel.SetActive(true);
        }

        public void CloseJournalPanel()
        {
            journalPanel.SetActive(false);
        }

        //Call when End Day button is pressed
        // TODO: if days left is zero, also update End Day button to say End Game!!!
        public void UpdateDaysLeftPanel()
        {
            daysLeftPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = GameManager.Instance.daysLeft.ToString() + " Days Left";
        }
    }
}