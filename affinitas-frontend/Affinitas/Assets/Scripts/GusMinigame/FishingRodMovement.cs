using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace GusMinigame
{
    public class FishingRodMovement : MonoBehaviour
    {
        //Note: Set pivot position of line image to top center.

        [SerializeField] Button sendLineButton; 
        [SerializeField] GameObject fishingLine;
        [SerializeField] GameObject fishingHook;

        RectTransform lineRectTransform;
        [SerializeField] float lineSpeed = .3f;
        [SerializeField] int minHookY = 250;
        [SerializeField] int maxHookY = 955;
        Vector2 initialSizeDelta;
        Vector2 lineSpeedVector;
        bool isCoroutineRunning;

        void Start()
        {
            lineRectTransform = fishingLine.GetComponent<RectTransform>();
            initialSizeDelta = lineRectTransform.sizeDelta;
            sendLineButton.interactable = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SendFishingLine();
            }
        }

        IEnumerator FishingLineRoutine()
        {
            isCoroutineRunning = true;
            sendLineButton.interactable = false;

            while (GusMinigameManager.Instance.fishingGameStarted)
            {
                yield return null;

                if (lineRectTransform.sizeDelta.y > maxHookY)
                {
                    lineSpeedVector *= -1;
                }
                else if (lineRectTransform.sizeDelta.y < minHookY)
                {
                    yield break;
                }

                lineRectTransform.sizeDelta += lineSpeedVector;
            }
            sendLineButton.interactable = true;
            isCoroutineRunning = false;
            Debug.Log("done fishin");
            yield return null;
        }

        // Should also be called from Start Game Button
        // TOFIX:  PRoblem at restart comes from the fact that coroutine continues extending fishing line WHILE(GameStarted)
        public void ResetFishingLine()
        {
            //if (isCoroutineRunning)
            //    StopCoroutine(FishingLineRoutine());
            lineRectTransform.sizeDelta = initialSizeDelta;
            lineSpeedVector = new Vector2(0f, lineSpeed);
            sendLineButton.interactable = true;
        }

        public void SendFishingLine()
        {
            if (GusMinigameManager.Instance.fishingGameStarted || !isCoroutineRunning)
            {
                ResetFishingLine();

                //if (!isCoroutineRunning)
                    StartCoroutine(FishingLineRoutine());
            }

        }

    }
}