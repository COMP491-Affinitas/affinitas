using System.Collections;
using UnityEngine;

namespace GusMinigame
{
    public class FishMovement : MonoBehaviour
    {
        Vector2 speed = new(.4f, 0);
        readonly int rightBorderX = 800;
        RectTransform fishRectTransform;
        Vector3 scale;

        private void Start()
        {
            fishRectTransform = GetComponent<RectTransform>();

            speed = new(Random.Range(.4f, .8f), 0);
        }

        private void Update()
        {
            if (GusMinigameManager.Instance.fishingGameStarted)
            {
                fishRectTransform.anchoredPosition += speed;

                if (fishRectTransform.anchoredPosition.x <= -rightBorderX || fishRectTransform.anchoredPosition.x >= rightBorderX)
                {
                    speed *= -1;
                    scale = fishRectTransform.localScale;
                    scale.x *= -1;
                    fishRectTransform.localScale = scale;
                }
            }
        }

        public void StartMoving()
        {
            speed = new(Random.Range(.4f, .8f), 0);
        }

        public void StopMoving()
        {
            speed = Vector2.zero;
        }

    }

}
