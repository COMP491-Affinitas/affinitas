using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace MainGame
{
    public class MainGameUiManager : MonoBehaviour
    {
        [SerializeField]
        GameObject warningPanel;

        [SerializeField]
        GameObject journalPanel;

        public UnityEvent clickItemEvent;

        private void Start()
        {
            warningPanel.SetActive(false);
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
    }
}