using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Add script to Scroll View with ScrollRect element.
public class ScrollRectHelper : MonoBehaviour
{
    ScrollRect scrollRect;

    bool isScrollCoroutineRunning;
    bool autoScroll = true;

    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        scrollRect.verticalNormalizedPosition = 0f;

        scrollRect.onValueChanged.AddListener((Vector2 pos) => OnUserScroll(pos));
    }

    // This is the function is used by a Button or another class.
    public void ScrollToBottom()
    {
        // Forces UI to update so scroll has no problems
        Canvas.ForceUpdateCanvases();

        if (isScrollCoroutineRunning)
            StopCoroutine(ScrollToBottomRoutine());

        if (autoScroll)
            StartCoroutine(ScrollToBottomRoutine());
    }

    IEnumerator ScrollToBottomRoutine()
    {
        isScrollCoroutineRunning = true;

        float startPos = scrollRect.verticalNormalizedPosition;
        float duration = startPos;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, 0, elapsedTime/duration);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = 0f;
        isScrollCoroutineRunning = false;
    }

    // TOFIX: Does not work for the first dialogues!!
    void OnUserScroll(Vector2 scrollPosition)
    {
        float contentHeight = scrollRect.content.rect.height;

        // If user scrolls up
        if (scrollPosition.y > 0.05f)
            autoScroll = false;
        else if (scrollPosition.y <= 0.05f)
            autoScroll = true;
    }


}
