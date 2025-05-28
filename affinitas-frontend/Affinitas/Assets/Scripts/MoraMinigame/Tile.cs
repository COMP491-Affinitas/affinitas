using UnityEngine;
using UnityEngine.UI;

namespace MoraMinigame
{
    public class Tile : MonoBehaviour
    {
        public int correctIndex;    // changes at shuffle with sprite swaps
        public int gridIndex;       // always the same

        public Image image;
        public Button button;

        void Awake()
        {
            image = GetComponent<Image>();
            button = GetComponent<Button>();
        }

        public void InitializeTile()
        {
            correctIndex = gridIndex;
        }

        // Call from button
        public void OnTileClicked()
        {
            MoraMinigameManager.Instance.OnTileClicked(gridIndex);
        }
    }
}
