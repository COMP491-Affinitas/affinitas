using UnityEngine;
using TMPro;
using System.Collections;
using System;

namespace MainGame
{
    public class AddDialogueBox : MonoBehaviour
    {
        // This code needs to be on the DialogueScrollView

        [SerializeField]
        GameObject playerDialogueBoxPrefab;
        [SerializeField]
        GameObject npcDialogueBoxPrefab;
        [SerializeField]
        GameObject infoPanelPrefab;
        [SerializeField]
        Transform contentTransform;
        ScrollRectHelper scrollRectHelper;

        float writingSpeed = 0.035f; //0.05f;

        private void Start()
        {
            scrollRectHelper = GetComponent<ScrollRectHelper>();
        }

        public void AddPlayerDialogueBox(string playerInp, Action onComplete, Action onCompleteTwo, bool writeSlow)
        {
            GameObject newPlayerDialogueBox = Instantiate(playerDialogueBoxPrefab);
            // Using parent:false in SetParent fixes sizing issues for 4K resolution. 
            newPlayerDialogueBox.transform.SetParent(contentTransform, false);

            TextMeshProUGUI dialogueTextMesh = newPlayerDialogueBox.transform.GetComponentInChildren<TextMeshProUGUI>();
            dialogueTextMesh.text = "";

            if (writeSlow)
                StartCoroutine(AddTextLetterByLetter(dialogueTextMesh, playerInp, onComplete, onCompleteTwo));
            else
                dialogueTextMesh.text = playerInp;
        }

        public void AddNpcDialogueBox(string npcDialogue, Action onComplete, Action onCompleteTwo, bool writeSlow)
        {
            GameObject newNpcDialogueBox = Instantiate(npcDialogueBoxPrefab);
            newNpcDialogueBox.transform.SetParent(contentTransform, false);

            TextMeshProUGUI dialogueTextMesh = newNpcDialogueBox.transform.GetComponentInChildren<TextMeshProUGUI>();
            dialogueTextMesh.text = "";

            if (writeSlow)
                StartCoroutine(AddTextLetterByLetter(dialogueTextMesh, npcDialogue, onComplete, onCompleteTwo));
            else
                dialogueTextMesh.text = npcDialogue;
        }

        public void AddInfoPanel(string info, Action onComplete, Action onCompleteTwo, bool writeSlow)
        {
            GameObject newInfoPanel = Instantiate(infoPanelPrefab);
            newInfoPanel.transform.SetParent(contentTransform, false);

            TextMeshProUGUI dialogueTextMesh = newInfoPanel.transform.GetComponentInChildren<TextMeshProUGUI>();
            dialogueTextMesh.text = "";

            if (writeSlow)
                StartCoroutine(AddTextLetterByLetter(dialogueTextMesh, info, onComplete, onCompleteTwo));
            else
                dialogueTextMesh.text = info;
        }

        public IEnumerator AddNpcDialogueBoxForQuests(string npcDialogue, Action onComplete, Action onCompleteTwo)
        {
            GameObject newNpcDialogueBox = Instantiate(npcDialogueBoxPrefab);
            newNpcDialogueBox.transform.SetParent(contentTransform, false);

            TextMeshProUGUI dialogueTextMesh = newNpcDialogueBox.transform.GetComponentInChildren<TextMeshProUGUI>();
            dialogueTextMesh.text = "";

            yield return StartCoroutine(AddTextLetterByLetter(dialogueTextMesh, npcDialogue, onComplete, onCompleteTwo));
        }

        IEnumerator AddTextLetterByLetter(TextMeshProUGUI textMesh, string str, Action onComplete, Action onCompleteTwo)
        {
            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < str.Length; i++)
            {
                textMesh.text += str[i];
                //scrollRectHelper.ScrollToBottom();
                if (i % 5 == 0) scrollRectHelper.ScrollToBottom();
                yield return new WaitForSeconds(writingSpeed);
            }

            onComplete?.Invoke();
            onCompleteTwo?.Invoke();
        }

        // Put this instead of AddTextLetterByLetter in AddTextBubble code to use it
        IEnumerator AddTextWordByWord(TextMeshProUGUI textMesh, string str, Action onComplete, Action onCompleteTwo)
        {
            yield return new WaitForSeconds(0.2f);

            string[] words = str.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                textMesh.text += words[i] + " ";
                scrollRectHelper.ScrollToBottom();
                yield return new WaitForSeconds(0.1f);
            }

            onComplete?.Invoke();
            onCompleteTwo?.Invoke();
        }

        public void DeleteAllDialogueBoxes()
        {
            for (int i = contentTransform.childCount - 1; i >= 0; i--)
            {
                if (contentTransform.GetChild(i) != null)
                    Destroy(contentTransform.GetChild(i).gameObject);
            }
        }

    }
}