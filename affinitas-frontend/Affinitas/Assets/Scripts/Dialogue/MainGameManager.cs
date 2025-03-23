using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace MainGame
{
    public class MainGameManager : MonoBehaviour
    {
        [SerializeField]
        GameObject warningPanel;

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
    }
}