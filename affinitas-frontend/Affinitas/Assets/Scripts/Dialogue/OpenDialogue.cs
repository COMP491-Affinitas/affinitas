using UnityEngine;

namespace MainGame
{
    public class OpenDialogue : MonoBehaviour
    {
        [SerializeField]
        GameObject npcDialoguePanel;

        // Call at House button pressed
        public void OpenCharacterDialogue()
        {
            npcDialoguePanel.SetActive(true);
            // TODO: Also display previous conversations.
        }
    }
}