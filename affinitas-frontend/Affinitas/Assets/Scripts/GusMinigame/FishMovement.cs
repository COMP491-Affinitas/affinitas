using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GusMinigame
{
    public class FishMovement : MonoBehaviour
    {
        Vector2 speed = new(.4f, 0);
        int rightBorderX = 800;
        RectTransform fishRectTransform;
        Vector3 scale;

        private void Start()
        {
            fishRectTransform = GetComponent<RectTransform>();
        }

        //void Update()
        //{
        //    if (FishingGameManager.Instance.currGameState == FishingGameStates.GameStarted)
        //    {
        //        fishRectTransform.anchoredPosition += speed;

        //        if (fishRectTransform.anchoredPosition.x <= -rightBorderX || fishRectTransform.anchoredPosition.x >= rightBorderX)
        //            speed *= -1;
        //    }
        //}

        IEnumerator MoveFishRoutine()
        {
            while (GusMinigameManager.Instance.fishingGameStarted)
            {
                fishRectTransform.anchoredPosition += speed;

                if (fishRectTransform.anchoredPosition.x <= -rightBorderX || fishRectTransform.anchoredPosition.x >= rightBorderX)
                {
                    speed *= -1;
                    scale = fishRectTransform.localScale;
                    scale.x *= -1;
                    fishRectTransform.localScale = scale;
                }
                    

                yield return null;
            }
        }

        public void StartMoving()
        {
            StartCoroutine(MoveFishRoutine());
        }

    }

}
