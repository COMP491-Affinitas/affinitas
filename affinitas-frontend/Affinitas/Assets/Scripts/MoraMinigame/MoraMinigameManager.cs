using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoraMinigame
{
    public class MoraMinigameManager : MonoBehaviour
    {
        // Make sure to enable Read/write in advanced settings in the source images!
        // Tiles as 2D array [[0,1,2], [3,4,5], [6,7,8]] so start axis should be Horizontal to match in Grid layout group

        // Singleton
        public static MoraMinigameManager Instance { get; private set; }

        int shuffleCount = 20;
        public int emptyIndex = 8;
        
        public bool gameEnded = false;
        public bool firstClick = true;

        int currentImageIndex;
        List<int> prevPlayedImageIndices = new();

        [SerializeField] GameObject endPanel;
        [SerializeField] GameObject giveUpButton;
        [SerializeField] GameObject gameBoard;
        [SerializeField] Image completeImagePanel;

        [SerializeField] List<Texture2D> sourceImages;
        // List of 9 square images created by dividing the original images
        List<List<Sprite>> tiledImages = new();

        // List of game tiles (all children of the game board)
        List<Tile> tiles = new();

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            InitializePanelsAndVariables();
            InitializeGame();
            StartGame();
        }

        // Call from Go to map button
        public void GoToMap()
        {
            SceneManager.UnloadSceneAsync(3);
        }

        void InitializePanelsAndVariables()
        {
            endPanel.SetActive(false);
            giveUpButton.SetActive(false);
            gameBoard.SetActive(true);
            gameEnded = false;
            firstClick = true;
        }

        void InitializeGame()
        {
            // Configure game buttons and images
            Tile tile;
            for (int i = 0; i < gameBoard.transform.childCount; i++)
            {
                tile = gameBoard.transform.GetChild(i).gameObject.GetComponent<Tile>();
                tile.InitializeTile();
                tiles.Add(tile);
            }

            ToggleButtonsInteractable(false);

            // Split images and add each texture to tile
            foreach (Texture2D texture in sourceImages)
            {
                tiledImages.Add(SplitImageIntoNine(texture));
            }
        }

        void StartGame()
        {
            // Choose random image to start with
            // if image already played, choose different image
            while (prevPlayedImageIndices.Contains(currentImageIndex))
                currentImageIndex = Random.Range(0, tiledImages.Count);

            prevPlayedImageIndices.Add(currentImageIndex);
            // if all images already played then empty prev images list
            if (prevPlayedImageIndices.Count >= sourceImages.Count)
                prevPlayedImageIndices = new();

            AssignOriginal();
            StartCoroutine(AssignShuffled());
        }

        public void OnTileClicked(int clickedIndex)
        {
            if (firstClick)
            {
                giveUpButton.SetActive(true);
                firstClick = false;
            }

            if (gameEnded)
                return;

            if (IsAdjacent(clickedIndex, emptyIndex))
            {
                MoveTile(clickedIndex);
                if (CheckWin())
                {
                    gameEnded = true;
                    giveUpButton.SetActive(false);
                    endPanel.SetActive(true);
                    ToggleButtonsInteractable(false);
                }
            }
        }

        void AssignOriginal()
        {
            // Put divided-to-9 images to game buttons in correct order
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].image.sprite = tiledImages[currentImageIndex][i];
                tiles[i].image.preserveAspect = true;
            }

            // Also add a small version of the completed image on a panel
            if (sourceImages.Count > currentImageIndex && completeImagePanel != null)
            {
                Sprite fullSprite = Sprite.Create(
                    sourceImages[currentImageIndex],
                    new Rect(0, 0, sourceImages[currentImageIndex].width, sourceImages[currentImageIndex].height),
                    new Vector2(0.5f, 0.5f));
                completeImagePanel.sprite = fullSprite;
                completeImagePanel.preserveAspect = true;
            }
        }

        IEnumerator AssignShuffled()
        {
            yield return new WaitForSeconds(1.5f);

            // Shuffle
            for (int i = 0; i < shuffleCount; i++)
            {
                Shuffle();
            }

            // Make last tile empty 
            emptyIndex = 8;
            tiles[emptyIndex].image.sprite = null;

            ToggleButtonsInteractable(true);
        }

        void MoveTile(int clickedIndex)
        {
            // Swap game button sprites and indices with clicked square and empty square
            tiles[emptyIndex].image.sprite = tiles[clickedIndex].image.sprite;
            tiles[clickedIndex].image.sprite = null;

            int tmp = tiles[emptyIndex].correctIndex;
            tiles[emptyIndex].correctIndex = tiles[clickedIndex].correctIndex;
            tiles[clickedIndex].correctIndex = tmp;

            emptyIndex = clickedIndex;
        }

        public bool IsAdjacent(int a, int b)
        {
            int rowA = a / 3;
            int colA = a % 3;
            int rowB = b / 3;
            int colB = b % 3;

            if (Mathf.Abs(rowA - rowB) + Mathf.Abs(colA - colB) == 1)
                return true;
            return false;
        }

        void Shuffle()
        {
            // Swap game button sprites and indices to shuffle image
            int shuffleCount = tiles.Count;
            int rand, tmpInt;
            Sprite tmpSprite;
            for (int i = 0; i < shuffleCount; i++)
            {
                rand = Random.Range(i, tiles.Count);

                tmpSprite = tiles[i].image.sprite;
                tiles[i].image.sprite = tiles[rand].image.sprite;
                tiles[rand].image.sprite = tmpSprite;

                tmpInt = tiles[i].correctIndex;
                tiles[i].correctIndex = tiles[rand].correctIndex;
                tiles[rand].correctIndex = tmpInt;
            }
        }

        bool CheckWin()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].gridIndex != tiles[i].correctIndex)
                    return false;
            }
            return true;
        }

        // Call from Give up button
        public void RestartGame()
        {
            InitializePanelsAndVariables();
            ToggleButtonsInteractable(false);
            StartGame();
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
                    Rect rect = new(col * tileWidth, (2 - row) * tileHeight, tileWidth, tileHeight);
                    Vector2 pivot = new(0.5f, 0.5f);
                    Sprite tile = Sprite.Create(texture, rect, pivot);
                    tiles.Add(tile);
                }
            }
            return tiles;
        }

        public void OnFirstClick()
        {
            if (firstClick)
            {
                giveUpButton.SetActive(true);
                firstClick = false;
            }
        }

        void ToggleButtonsInteractable(bool newActive)
        {
            foreach (Tile tile in tiles)
            {
                tile.button.interactable = newActive;
            }
        }
    }
}