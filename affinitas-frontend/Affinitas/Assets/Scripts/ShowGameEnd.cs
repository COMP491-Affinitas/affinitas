using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class ShowGameEnd : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI endingTextMesh;
    [SerializeField]
    ScrollRectHelper scrollRectHelper;

    // TODO: Get endingText from server and call function
    // Call when days No is 10
    public void ShowEndingText(string endingText)
    {
        endingTextMesh.text = "";
        StartCoroutine(AddTextLetterByLetter(endingText, null));

        scrollRectHelper.ScrollToBottom();
    }

    IEnumerator AddTextLetterByLetter(string str, Action onComplete)
    {
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < str.Length; i++)
        {
            endingTextMesh.text += str[i];
            scrollRectHelper.ScrollToBottom();
            yield return new WaitForSeconds(0.05f);
        }

        onComplete?.Invoke();
    }
}
