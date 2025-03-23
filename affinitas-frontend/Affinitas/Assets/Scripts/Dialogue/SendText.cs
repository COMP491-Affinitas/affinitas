using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System;

namespace MainGame
{
    public class SendText : MonoBehaviour
    {
        [SerializeField]
        GameObject playerSpeechBubblePrefab;
        [SerializeField]
        TMP_InputField dialogueInputField;

        TextMeshProUGUI bubbleTextMesh;
        Transform contentTransform;
        ScrollRectHelper scrollRectHelper;

        string playerInput;

        void Start()
        {
            scrollRectHelper = GetComponent<ScrollRectHelper>();
            contentTransform = transform.GetChild(0).transform.GetChild(0).transform;

            // Send Text also when user presses Enter
            dialogueInputField.onSubmit.AddListener((str) => SendInputtedText());
        }

        public void SendInputtedText()
        {
            playerInput = dialogueInputField.text;
            if (playerInput == "")
                return;
            AddTextBubble(playerInput);

            scrollRectHelper.ScrollToBottom();

            dialogueInputField.text = "";
            dialogueInputField.ActivateInputField();
        }

        void AddTextBubble(string playerInp)
        {
            GameObject newPlayerSpeechBubble = Instantiate(playerSpeechBubblePrefab);
            // Using parent:false in SetParent fixes sizing issues for 4K resolution. 
            newPlayerSpeechBubble.transform.SetParent(contentTransform, false);

            // Make sure TextMesh is first child of bubble gameobject
            bubbleTextMesh = newPlayerSpeechBubble.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            bubbleTextMesh.text = "";

            StartCoroutine(AddTextLetterByLetter(bubbleTextMesh, playerInp));
        }

        IEnumerator AddTextLetterByLetter(TextMeshProUGUI textMesh, string str)
        {
            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < str.Length; i++)
            {
                textMesh.text += str[i];
                scrollRectHelper.ScrollToBottom();
                yield return new WaitForSeconds(0.05f);
            }
        }

        // Put this instead of AddTextLetterByLetter in AddTextBubble code to use it
        IEnumerator AddTextWordByWord(TextMeshProUGUI textMesh, string str)
        {
            yield return new WaitForSeconds(0.2f);

            string[] words = str.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                textMesh.text += words[i] + " ";
                scrollRectHelper.ScrollToBottom();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
