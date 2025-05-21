using UnityEngine;

namespace MainGame
{
    public class UseItem : MonoBehaviour
    {
        public string itemName;
        public bool inInventory = false;

        public void AddToInventory()
        {
            gameObject.SetActive(true);
            inInventory = true;
        }

        public void RemoveFromInventory()
        {
            inInventory = false;
            gameObject.SetActive(false);
        }
    }
}