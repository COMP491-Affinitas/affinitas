using UnityEngine;

namespace GusMinigame
{
    public class Hook : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            GusMinigameManager.Instance.AddToScore();
            GusMinigameManager.Instance.AddMoreFish();
            Destroy(collision.gameObject);
        }
    }
}