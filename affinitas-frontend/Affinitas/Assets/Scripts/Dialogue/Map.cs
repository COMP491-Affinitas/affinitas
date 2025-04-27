using UnityEngine;

namespace MainGame
{
    public class Map : MonoBehaviour
    {
        // This code needs to be on the MapPanel

        GameObject mapPanel;
        [SerializeField]
        GameObject npcDialoguePanels;

        int npcCount;

        private void Start()
        {
            mapPanel = gameObject;

            npcCount = npcDialoguePanels.transform.childCount;

            CloseCharacterDialogue();
        }

        void CloseCharacterDialogue()
        {
            for (int i = 0; i < npcCount; i++)
            {
                npcDialoguePanels.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        // Call from Go to Map button
        public void OpenMap()
        {
            mapPanel.SetActive(true);
        }

        // Call at house button pressed
        public void CloseMap()
        {
            CloseCharacterDialogue();
            mapPanel.SetActive(false);
        }



    }
}