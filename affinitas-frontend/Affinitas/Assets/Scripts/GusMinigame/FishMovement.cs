using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GusMinigame
{
    public class FishMovement : MonoBehaviour
    {
        Vector2 speed;
        readonly int rightBorderX = 800;
        RectTransform fishRectTransform;
        Vector3 scale;
        Image fishImage;
        float fishFadeDuration = 1f;

        private void Start()
        {
            fishRectTransform = GetComponent<RectTransform>();
            fishImage = GetComponent<Image>();
        }

        private void Update()
        {
            if (GusMinigameManager.Instance.fishingGameStarted)
            {
                fishRectTransform.anchoredPosition += speed * Time.deltaTime;

                if (fishRectTransform.anchoredPosition.x <= -rightBorderX || fishRectTransform.anchoredPosition.x >= rightBorderX)
                {
                    speed *= -1;
                    scale = fishRectTransform.localScale;
                    scale.x *= -1;
                    fishRectTransform.localScale = scale;
                }
            }
        }

        public IEnumerator FadeInFish()
        {
            fishImage.color = new Color(1f, 1f, 1f, 0f);
            float elapsed = 0f;
            while (elapsed < fishFadeDuration)
            {
                fishImage.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0, 1f, elapsed / fishFadeDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }
            fishImage.color = new Color(1f, 1f, 1f, 1f);
        }

        public void StartMoving()
        {
            if (fishRectTransform.localScale.x > 0)
                speed = new(Random.Range(100f, 300f), 0);
            else
                speed = new(Random.Range(-300f, -100f), 0);
        }

        public void StopMoving()
        {
            speed = Vector2.zero;
        }

    }

}
