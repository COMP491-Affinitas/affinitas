using TMPro;
using UnityEngine;

namespace MainGame
{
    public class MainGameUiManager : MonoBehaviour
    {
        [SerializeField]
        GameObject journalPanel;

        [SerializeField]
        GameObject daysLeftPanel;

        private void Start()
        {
            journalPanel.SetActive(false);
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
    }
}