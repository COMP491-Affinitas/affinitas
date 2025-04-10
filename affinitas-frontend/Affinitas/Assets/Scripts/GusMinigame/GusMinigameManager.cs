using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GusMinigame
{
    public class GusMinigameManager : MonoBehaviour
    {
        public static GusMinigameManager Instance { get; private set; }
        [SerializeField] FishMovement[] fishArray;
        [SerializeField] GameObject FishingGameStartButton;
        [SerializeField] TextMeshProUGUI scoreTextMesh;

        public bool fishingGameStarted;
        public int gusMinigameScore;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            FishingGameStartButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Start Game";
            fishingGameStarted = false;
            InitializeFishingGame();
        }

        void InitializeFishingGame()
        {
            gusMinigameScore = 0;
            scoreTextMesh.text = "Score: 0";
            FishingGameStartButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Start Game";
        }

        public void StartFishingGame()
        {
            if (!fishingGameStarted)
            {
                fishingGameStarted = true;

                //FishingGameStartButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Restart Game";

                foreach (FishMovement fish in fishArray)
                {
                    fish.StartMoving();
                }
            }
        }

        public void CheckAllFishCaught()
        {
            if (fishArray.Length == 0)
            {
                fishingGameStarted = false;
                FishingGameStartButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Play Again";
            }
        }

        public void AddToScore()
        {
            gusMinigameScore += 1;
            scoreTextMesh.text = "Score: " + gusMinigameScore.ToString();
            CheckAllFishCaught();
        }

    }
}