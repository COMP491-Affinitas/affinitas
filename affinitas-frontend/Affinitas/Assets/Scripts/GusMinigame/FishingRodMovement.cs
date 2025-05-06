using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace GusMinigame
{
    public class FishingRodMovement : MonoBehaviour
    {
        // This code needs to be on the Fishing Rod
        //Note: Set pivot position of line image to top center.

        [SerializeField] Button sendLineButton; 
        [SerializeField] GameObject fishingLine;
        [SerializeField] GameObject fishingHook;

        RectTransform boatRectTransform;
        RectTransform lineRectTransform;
        RectTransform parentRectTransform;
        float boatMovementSpeed = 600f;
        float lineSpeed = 800f;
        int minHookY = 300;
        int maxHookY = 955;
        Vector2 initialSizeDelta;
        Vector2 lineSpeedVector;
        bool isCoroutineRunning;

        void Start()
        {
            boatRectTransform = GetComponent<RectTransform>();
            lineRectTransform = fishingLine.GetComponent<RectTransform>();
            parentRectTransform = transform.parent.GetComponent<RectTransform>();
            initialSizeDelta = lineRectTransform.sizeDelta;
            //sendLineButton.interactable = true;
        }

        private void Update()
        {
            if (GusMinigameManager.Instance.fishingGameStarted)
            {
                if (Input.GetKeyDown(KeyCode.Space) && !isCoroutineRunning)
                    SendFishingLine();

                Vector2 currentPos = boatRectTransform.anchoredPosition;

                if (Input.GetKey(KeyCode.LeftArrow))
                    currentPos.x -= boatMovementSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.RightArrow))
                    currentPos.x += boatMovementSpeed * Time.deltaTime;

                // Clamp position inside parent bounds
                float halfBoatWidth = boatRectTransform.rect.width / 2f;
                float parentHalfWidth = parentRectTransform.rect.width / 2f;

                currentPos.x = Mathf.Clamp(currentPos.x, -parentHalfWidth + halfBoatWidth, parentHalfWidth - halfBoatWidth);

                boatRectTransform.anchoredPosition = currentPos;
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
                    break;
                }

                lineRectTransform.sizeDelta += lineSpeedVector * Time.deltaTime;
            }
            sendLineButton.interactable = true;
            isCoroutineRunning = false;
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
            if (GusMinigameManager.Instance.fishingGameStarted && !isCoroutineRunning)
            {
                ResetFishingLine();

                //if (!isCoroutineRunning)
                    StartCoroutine(FishingLineRoutine());
            }

        }

    }
}