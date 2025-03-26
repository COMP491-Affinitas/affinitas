using UnityEngine;

namespace MainGame
{
    public class UseItem : MonoBehaviour
    {
        public void UseSelectedItem()
        {
            // TODO: Check if item can be used here,
            // and if it can, handle LLM prompt accordingly

            // For now

            Destroy(this.gameObject);
        }
    }
}