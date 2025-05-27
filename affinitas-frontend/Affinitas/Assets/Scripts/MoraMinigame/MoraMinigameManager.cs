using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//// Call from Go to map button
//public void GoToMap()
//{
//    SceneManager.UnloadSceneAsync(3);
//}

namespace MoraMinigame
{
    public class MoraMinigameManager : MonoBehaviour
    {
        // Make sure to enable Read/write in advanced settings in the source images!

        // TODO: Add GoToMap function to button
        // TODO: Erase Event System

        private int emptyIndex = 8;
        private int currentImageIndex;
        private bool gameEnded = false;
        private bool firstClick = true;

        [SerializeField] private Transform tileParent;
        [SerializeField] private List<Texture2D> sourceImages;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject giveUpPanel;
        [SerializeField] private GameObject gameBoard;
        [SerializeField] private Image fullPaintingDisplay;

        private List<Image> squares = new();
        private List<List<Sprite>> tileSets = new();
        private List<Sprite> correctOrder;
        private List<Sprite> currentTileState;

        private void Start()
        {
            winPanel.SetActive(false);
            giveUpPanel.SetActive(false);
            gameBoard.SetActive(true);

            foreach (Transform child in tileParent)
            {
                Image img = child.GetComponent<Image>();
                Button btn = child.GetComponent<Button>();
                if (img != null && btn != null)
                {
                    int index = squares.Count;
                    squares.Add(img);
                    btn.onClick.AddListener(() => OnTileClicked(index));
                }
            }

            foreach (var tex in sourceImages)
            {
                tileSets.Add(SplitImageIntoNine(tex));
            }

            currentImageIndex = Random.Range(0, tileSets.Count);
            AssignOriginal();
            Invoke(nameof(AssignShuffled), 1.5f);
        }

        private void OnTileClicked(int clickedIndex)
        {
            if (firstClick)
            {
                giveUpPanel.SetActive(true);
                firstClick = false;
            }

            if (gameEnded) return;

            if (IsAdjacent(clickedIndex, emptyIndex))
            {
                MoveTile(clickedIndex);
                if (CheckWin())
                {
                    gameEnded = true;
                    gameBoard.SetActive(false);
                    winPanel.SetActive(true);
                }
            }
        }

        private void AssignOriginal()
        {
            correctOrder = new List<Sprite>(tileSets[currentImageIndex]);

            for (int i = 0; i < squares.Count; i++)
            {
                squares[i].sprite = correctOrder[i];
                squares[i].preserveAspect = true;
            }

            if (sourceImages.Count > currentImageIndex && fullPaintingDisplay != null)
            {
                Sprite fullSprite = Sprite.Create(
                    sourceImages[currentImageIndex],
                    new Rect(0, 0, sourceImages[currentImageIndex].width, sourceImages[currentImageIndex].height),
                    new Vector2(0.5f, 0.5f));
                fullPaintingDisplay.sprite = fullSprite;
                fullPaintingDisplay.preserveAspect = true;
            }
        }

        private void AssignShuffled()
        {
            List<Sprite> shuffledTiles = new(correctOrder);

            // Keep shuffling until it's not the same as correctOrder
            do
            {
                Shuffle(shuffledTiles);
            } while (IsSameOrder(shuffledTiles, correctOrder));

            shuffledTiles[8] = null;

            for (int i = 0; i < squares.Count; i++)
            {
                squares[i].sprite = shuffledTiles[i];
                squares[i].preserveAspect = true;
            }

            currentTileState = new List<Sprite>(shuffledTiles);
            emptyIndex = 8;
        }

        private bool IsSameOrder(List<Sprite> a, List<Sprite> b)
        {
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] == null || b[i] == null) continue;
                if (a[i].name != b[i].name)
                    return false;
            }
            return true;
        }

        private void MoveTile(int clickedIndex)
        {
            squares[emptyIndex].sprite = squares[clickedIndex].sprite;
            squares[clickedIndex].sprite = null;

            currentTileState[emptyIndex] = currentTileState[clickedIndex];
            currentTileState[clickedIndex] = null;

            emptyIndex = clickedIndex;
        }

        private bool IsAdjacent(int a, int b)
        {
            int rowA = a / 3, colA = a % 3;
            int rowB = b / 3, colB = b % 3;
            return Mathf.Abs(rowA - rowB) + Mathf.Abs(colA - colB) == 1;
        }

        private void Shuffle(List<Sprite> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rand = Random.Range(i, list.Count);
                (list[i], list[rand]) = (list[rand], list[i]);
            }
        }

        private bool CheckWin()
        {
            // Empty tile must be at index 8
            if (emptyIndex != 8)
                return false;

            for (int i = 0; i < currentTileState.Count; i++)
            {
                if (i == emptyIndex)
                {
                    if (currentTileState[i] != null)
                        return false;
                }
                else
                {
                    if (currentTileState[i] == null || correctOrder[i] == null || currentTileState[i].name != correctOrder[i].name)
                        return false;
                }
            }
            return true;
        }


        public void RestartGame()
        {
            winPanel.SetActive(false);
            giveUpPanel.SetActive(false);
            gameEnded = false;
            gameBoard.SetActive(true);
            firstClick = true;

            int newIndex = Random.Range(0, tileSets.Count);
            while (newIndex == currentImageIndex && tileSets.Count > 1)
            {
                newIndex = Random.Range(0, tileSets.Count);
            }
            currentImageIndex = newIndex;

            AssignOriginal();
            Invoke(nameof(AssignShuffled), 1.5f);
        }

        public void GiveUp()
        {
            gameEnded = true;
            giveUpPanel.SetActive(true);
            gameBoard.SetActive(false);
        }

        public List<Sprite> SplitImageIntoNine(Texture2D texture)
        {
            List<Sprite> tiles = new();
            int tileWidth = texture.width / 3;
            int tileHeight = texture.height / 3;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Rect rect = new Rect(col * tileWidth, (2 - row) * tileHeight, tileWidth, tileHeight);
                    Vector2 pivot = new Vector2(0.5f, 0.5f);
                    Sprite tile = Sprite.Create(texture, rect, pivot);
                    tiles.Add(tile);
                }
            }

            return tiles;
        }
    }
}