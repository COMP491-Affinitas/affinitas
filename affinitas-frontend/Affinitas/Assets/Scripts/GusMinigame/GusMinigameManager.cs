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
        [SerializeField] TextMeshProUGUI timerTextMesh;

        public bool fishingGameStarted;
        public int gusMinigameScore;
        public float timeLimit = 100f;
        float remainingTime;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            fishingGameStarted = false;
            InitializeFishingGame();
        }

        private void Update()
        {
            if (fishingGameStarted)
            {
                if (remainingTime <= 0f)
                {
                    Debug.Log("no time left, score is: " + gusMinigameScore.ToString());
                }
                remainingTime -= Time.deltaTime;
                timerTextMesh.text = "Time Left: " + remainingTime.ToString("0");
            }
        }

        void InitializeFishingGame()
        {
            gusMinigameScore = 0;
            remainingTime = timeLimit;
            scoreTextMesh.text = "Score: 0";
            timerTextMesh.text = "Time Left: " + remainingTime.ToString("0");
            FishingGameStartButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Start Game";
            FishingGameStartButton.GetComponent<Button>().interactable = true;
        }

        public void StartFishingGame()
        {
            if (!fishingGameStarted)
            {
                fishingGameStarted = true;
                FishingGameStartButton.GetComponent<Button>().interactable = false;

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