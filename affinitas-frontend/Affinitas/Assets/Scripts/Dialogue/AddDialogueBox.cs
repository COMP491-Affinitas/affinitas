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

        Transform contentTransform;
        ScrollRectHelper scrollRectHelper;

        private void Start()
        {
            scrollRectHelper = GetComponent<ScrollRectHelper>();
            contentTransform = transform.GetChild(0).transform.GetChild(0).transform;
        }

        public void AddPlayerDialogueBox(string playerInp, Action onComplete, Action onCompleteTwo)
        {
            GameObject newPlayerDialogueBox = Instantiate(playerDialogueBoxPrefab);
            // Using parent:false in SetParent fixes sizing issues for 4K resolution. 
            newPlayerDialogueBox.transform.SetParent(contentTransform, false);

            TextMeshProUGUI dialogueTextMesh = newPlayerDialogueBox.transform.GetComponentInChildren<TextMeshProUGUI>();
            dialogueTextMesh.text = "";

            StartCoroutine(AddTextLetterByLetter(dialogueTextMesh, playerInp, onComplete, onCompleteTwo));
        }

        public void AddNpcDialogueBox(string npcDialogue, Action onComplete, Action onCompleteTwo)
        {
            GameObject newNpcDialogueBox = Instantiate(npcDialogueBoxPrefab);
            newNpcDialogueBox.transform.SetParent(contentTransform, false);

            TextMeshProUGUI dialogueTextMesh = newNpcDialogueBox.transform.GetComponentInChildren<TextMeshProUGUI>();
            dialogueTextMesh.text = "";

            StartCoroutine(AddTextLetterByLetter(dialogueTextMesh, npcDialogue, onComplete, onCompleteTwo));
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
                scrollRectHelper.ScrollToBottom();
                yield return new WaitForSeconds(0.05f);
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

    }
}