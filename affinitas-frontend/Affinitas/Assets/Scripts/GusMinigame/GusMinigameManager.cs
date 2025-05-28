using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace GusMinigame
{
    public class GusMinigameManager : MonoBehaviour
    {
        public static GusMinigameManager Instance { get; private set; }
        public List<FishMovement> fishList = new();
        [SerializeField] GameObject fishPrefab;
        [SerializeField] RectTransform fishesRectTransform;

        [SerializeField] Button fishingGameStartButton;
        [SerializeField] TextMeshProUGUI scoreTextMesh;
        [SerializeField] TextMeshProUGUI timerTextMesh;
        [SerializeField] GameObject endPanel;

        public bool fishingGameStarted;
        public int gusMinigameScore;
        public float timeLimit = 100f;
        float remainingTime;

        int targetScore = 50;
        bool gusQuestCompleted = false;
        int givenFish = 0;
        
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InitializeFishingGame();
        }

        // Call from Go to map button
        public void GoToMap()
        {
            // Save score and go to map
            MainGameManager.Instance.ReturnFromGusMinigame(gusMinigameScore, givenFish);
            SceneManager.UnloadSceneAsync(1);
        }

        void InitializeFishingGame()
        {
            fishingGameStarted = false;
            gusMinigameScore = 0;
            remainingTime = timeLimit;
            scoreTextMesh.text = "Score: 0";
            timerTextMesh.text = "Time Left: " + remainingTime.ToString("0");
            fishingGameStartButton.interactable = true;
            endPanel.SetActive(false);
        }

        // Call from Start Game
        public void StartFishingGame()
        {
            InitializeFishingGame();
            fishingGameStarted = true;
            fishingGameStartButton.interactable = false;

            foreach (FishMovement fish in fishList)
            {
                if (fish != null)
                    fish.StartMoving();
            }
            StartCoroutine(FishingGameTimer());
        }

        IEnumerator FishingGameTimer()
        {
            float timeLeft = remainingTime;

            while (timeLeft > 0f)
            {
                timerTextMesh.text = "Time Left: " + timeLeft.ToString("0");
                yield return null; // wait one frame
                timeLeft -= Time.deltaTime;
            }

            timerTextMesh.text = "No Time Left!";
            EndFishingGame();
        }

        void EndFishingGame()
        {
            fishingGameStarted = false;

            foreach (FishMovement fish in fishList)
            {
                if (fish != null)
                    fish.StopMoving();
            }
            if (!gusQuestCompleted)
                CheckGusQuest();
            if (gusQuestCompleted && givenFish < 1)
            {
                endPanel.GetComponentInChildren<TextMeshProUGUI>().text = "Your Time is Up! Final Score: " + gusMinigameScore.ToString() + "\nObtained fish!";
                givenFish += 1;
            }
            else
                endPanel.GetComponentInChildren<TextMeshProUGUI>().text = "Your Time is Up! Final Score: " + gusMinigameScore.ToString();
            endPanel.SetActive(true);
            fishingGameStartButton.interactable = true;
        }

        public void AddToScore()
        {
            if (fishingGameStarted)
            {
                gusMinigameScore += 1;
                scoreTextMesh.text = "Score: " + gusMinigameScore.ToString();
            }
        }

        public void AddMoreFish()
        {
            if (!fishingGameStarted)
                return;
  
            int randomNum = Random.Range(1, 2);

            for (int i = 0; i < randomNum; i++)
            {
                GameObject newFish = Instantiate(fishPrefab, fishesRectTransform);

                RectTransform fishRectTransform = newFish.GetComponent<RectTransform>();
                fishRectTransform.anchoredPosition = GetRandomPos();

                FishMovement newFishMovement = newFish.GetComponent<FishMovement>();
                newFishMovement.StartMoving();

                fishList.Add(newFishMovement);
            }
        }

        public void ReuseFish(GameObject fishGameObject)
        {
            if (!fishingGameStarted)
                return;

            fishGameObject.SetActive(true);

            RectTransform fishRectTransform = fishGameObject.GetComponent<RectTransform>();
            fishRectTransform.anchoredPosition = GetRandomPos();

            FishMovement fishMovement = fishGameObject.GetComponent<FishMovement>();
            StartCoroutine(fishMovement.FadeInFish());
            fishMovement.StartMoving();
        }

        // Get random position within the confines of fishes
        Vector2 GetRandomPos()
        {
            Vector2 size = fishesRectTransform.rect.size;

            float x = Random.Range(-size.x / 2f + 200f, size.x / 2f - 200f);
            float y = Random.Range(-size.y / 2f + 200f, size.y / 2f - 200f);

            return new Vector2(x, y);
        }

        void CheckGusQuest()
        {
            if (MainGameManager.Instance.itemDict.TryGetValue("gus_fish", out Item gusFish) && gusFish != null)
            {
                if (gusFish.active)
                {
                    gusQuestCompleted = true;
                    givenFish = 1;
                    return;
                }
            }
            if (MainGameManager.Instance.npcDict[2].questList[0].status == MainGameManager.Instance.questStatusDict[QuestStatus.InProgress])
            {
                if (gusMinigameScore >= targetScore)
                    gusQuestCompleted = true;
            }
        }

    }
}