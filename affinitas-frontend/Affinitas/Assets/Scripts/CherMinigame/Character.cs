using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace CherMinigame
{
    public class Character : MonoBehaviour
    {
        public char character;
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Image image;
        public RectTransform rectTransform;
        public int index;

        [SerializeField] Color normalColor = new(1f, 1f, 1f, 1f);
        [SerializeField] Color selectedColor = new(1f, 0f, 0f, 1f);

        bool isSelected = false;

        public Character Init(char c)
        {
            character = c;
            text.text = c.ToString();
            gameObject.SetActive(true);
            return this;
        }

        public void Select()
        {
            isSelected = !isSelected;

            image.color = isSelected ? selectedColor : normalColor;

            if (isSelected)
            {
                CherMinigameManager.Instance.Select(this);
            }
            else
            {
                CherMinigameManager.Instance.UnSelect();
            }
        }
    }
}
