using UnityEngine;

namespace GusMinigame
{
    public class Hook : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            GusMinigameManager.Instance.AddToScore();
            collision.gameObject.SetActive(false);
            GusMinigameManager.Instance.ReuseFish(collision.gameObject);
            //GusMinigameManager.Instance.AddMoreFish();
            //Destroy(collision.gameObject);
        }
    }
}