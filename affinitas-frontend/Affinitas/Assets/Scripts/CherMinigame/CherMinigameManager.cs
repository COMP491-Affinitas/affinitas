using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace CherMinigame
{
    public class CherMinigameManager : MonoBehaviour
    {
        // Singleton
        public static CherMinigameManager Instance { get; private set; }

        public string[] words = {};

        [SerializeField]
        Character prefab;
        [SerializeField]
        Transform container;
        [SerializeField]
        RectTransform containerRectTransform;
        [SerializeField]
        GameObject minigameEndPanel;

        List<Character> charObjects = new();
        Character firstSelected;

        public int currWordIndex;

        int cherMinigameScore;
        [SerializeField]
        TextMeshProUGUI scoreText;

        float space = 10f;
        float lerpSpeed = 5f;
        float paddingForLeftRightChars = 50f;
        float preferredSpacing = 50f;
        float buttonWidth = 140f;

        Coroutine repositionRoutine;

        void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            InitializeGame();
            ShowScramble(currWordIndex);
        }

        void Update()
        {
            RepositionObject();
        }

        void InitializeGame()
        {
            minigameEndPanel.SetActive(false);
            cherMinigameScore = 0;
            scoreText.text = "Score: 0";
        }

        // Call from Go to map button
        public void GoToMap()
        {
            // Save score and go to map
            MainGameManager.Instance.ReturnFromCherMinigame(cherMinigameScore);
            //SceneManager.LoadScene(0);
            SceneManager.UnloadSceneAsync(2);
        }

        // Randomize letter positions in a given word and return scrambled string
        string RandomizeWord(string word)
        {
            string result = word;
            while (result == word)
            {
                result = "";
                List<char> characters = new(word.ToCharArray());
                while (characters.Count > 0)
                {
                    int indexChar = Random.Range(0, characters.Count - 1);
                    result += characters[indexChar];

                    characters.RemoveAt(indexChar);
                }
            }
            return result;
        }

        public void ShowScramble(int index)
        {
            // First clear and destroy all previous objects
            foreach (var charObj in charObjects)
            {
                if (charObj != null)
                    charObj.gameObject.SetActive(false);
            }
            charObjects.Clear();

            if (index > words.Length - 1)
            {
                EndMinigame();
;               return;
            }

            char[] chars = RandomizeWord(words[index]).ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                Character newCharObject = GetOrCreateCharObject();
                newCharObject.transform.SetParent(container, false);
                newCharObject.Init(chars[i]);
            }
            currWordIndex = index;
            //TriggerReposition();
        }

        // Change charObject button positions
        //void RepositionObject()
        //{
        //    if (charObjects.Count == 0)
        //        return;
        //    float totalWidth = containerRectTransform.rect.width - paddingForLeftRightChars * 2;
        //    float space = charObjects.Count > 1 ? totalWidth / (charObjects.Count - 1) : 0;
        //    float startX = -totalWidth / 2;
        //    for (int i = 0; i < charObjects.Count; i++)
        //    {
        //        Vector2 targetPosition = new(startX + i * space, 0);
        //        charObjects[i].rectTransform.anchoredPosition = Vector2.Lerp(charObjects[i].rectTransform.anchoredPosition,
        //            targetPosition, lerpSpeed * Time.deltaTime);
        //        charObjects[i].index = i;
        //    }
        //}

        void RepositionObject()
        {
            if (charObjects.Count == 0)
                return;

            int count = charObjects.Count;
            float containerWidth = containerRectTransform.rect.width;

            float maxTotalSpacing = preferredSpacing * (count - 1);
            float totalButtonWidth = buttonWidth * count;
            float totalNeededWidth = totalButtonWidth + maxTotalSpacing;

            // Adjust spacing if needed to fit within the container
            float spacing = preferredSpacing;
            if (totalNeededWidth > containerWidth)
            {
                float availableWidth = containerWidth - (buttonWidth * count);
                spacing = Mathf.Max(0f, availableWidth / (count - 1));
            }

            // Calculate starting x pos
            float fullWidth = buttonWidth * count + spacing * (count - 1);
            float startX = -fullWidth / 2 + buttonWidth / 2;

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * (buttonWidth + spacing);
                Vector2 targetPos = new(x, 0);

                RectTransform rt = charObjects[i].rectTransform;
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, lerpSpeed * Time.deltaTime);
                charObjects[i].index = i;
            }
        }

        //void TriggerReposition()
        //{
        //    if (repositionRoutine != null)
        //        StopCoroutine(repositionRoutine);
        //    repositionRoutine = StartCoroutine(RepositionObject());
        //}

        // Swap the position of two charObjects, check word every time
        void SwapTwoCharacters(int indexA, int indexB)
        {
            Character tmpA = charObjects[indexA];

            charObjects[indexA] = charObjects[indexB];
            charObjects[indexB] = tmpA;

            charObjects[indexA].transform.SetAsLastSibling();
            charObjects[indexB].transform.SetAsLastSibling();

            //TriggerReposition();
            CheckWord();
        }

        void CheckWord()
        {
            StartCoroutine(CoCheckWord());
        }

        IEnumerator CoCheckWord()
        {
            yield return new WaitForSeconds(0.5f);
            string word = "";
            foreach (Character charObject in charObjects)
            {
                word += charObject.character;
            }
            // If word is correct, continue game with another word
            if (word == words[currWordIndex])
            {
                cherMinigameScore++;
                scoreText.text = "Score: " + cherMinigameScore.ToString();
                //if (repositionRoutine != null)
                //    StopCoroutine(repositionRoutine);
                currWordIndex++;
                ShowScramble(currWordIndex);
            }
        }

        // Handle two characters' selection for swapping
        public void Select(Character charObject)
        {
            // At second character selected
            if (firstSelected)
            {
                SwapTwoCharacters(firstSelected.index, charObject.index);
                firstSelected.Select();
                charObject.Select();
            }
            // At first character selected
            else
            {
                firstSelected = charObject;
            }
        }

        public void UnSelect()
        {
            firstSelected = null;
        }

        Character GetOrCreateCharObject()
        {
            foreach (var obj in charObjects)
            {
                if (!obj.gameObject.activeSelf) // Inactive = available
                {
                    obj.gameObject.SetActive(true);
                    return obj;
                }
            }
            // Not available create a new one
            Character newObj = Instantiate(prefab, container);
            charObjects.Add(newObj);
            return newObj;
        }

        void EndMinigame()
        {
            string endingStr = "Game Finished!\nYou unscrambled " + cherMinigameScore.ToString() + " words."; 
            minigameEndPanel.SetActive(true);
            minigameEndPanel.GetComponentInChildren<TextMeshProUGUI>().text = endingStr;
        }



    }

}
