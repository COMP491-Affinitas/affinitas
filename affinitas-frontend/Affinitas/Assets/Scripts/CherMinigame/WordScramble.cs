using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WordScramble : MonoBehaviour
{

    public string[] words;

    [Header("UI REFERENCE")]
    public CharObject prefab;
    public Transform container;
    public float space;
    public float lerpSpeed = 5;

    List<CharObject> charObjects = new List<CharObject>();
    CharObject firstSelected;

    public int currentWord;

    public static WordScramble main;

    private void Awake()
    {
        main = this;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowScramble(currentWord);
    }

    // Update is called once per frame
    void Update()
    {
        RepositionObject();

    }

    string RandomizeWord(string word)
    {
        string result = word;


        while (result == word)
        {
            result = "";
            List<char> characters = new List<char>(word.ToCharArray());
            while (characters.Count > 0)
            {
                int indexChar = Random.Range(0, characters.Count - 1);
                result += characters[indexChar];

                characters.RemoveAt(indexChar);

            }

        }

        return result;
    }


    void RepositionObject()
    {
        if (charObjects.Count == 0)
        {
            return;
        }

        float center = (charObjects.Count - 1) / 2;
        for (int i = 0; i < charObjects.Count; i++)
        {
            charObjects[i].rectTransform.anchoredPosition
                = Vector2.Lerp(charObjects[i].rectTransform.anchoredPosition,
                new Vector2((i - center) * space, 0), lerpSpeed * Time.deltaTime);
            charObjects[i].index = i;
        }
    }

    /// <summary>
    /// Show random word to the script
    /// </summary>
    public void ShowScramble()
    {
        ShowScramble(Random.Range(0, words.Length - 1));
    }

    /// <summary>
    /// Show word from collection with desired index
    /// </summary>
    /// <param name="index">index of the element</param>
    public void ShowScramble(int index)
    {
        //charObjects.Clear();
        //foreach(Transform child in container)
        //{
        //    Destroy(child.gameObject);
        //}

        // First, clear and destroy all previous objects
        foreach (var charObj in charObjects)
        {
            if (charObj != null)
            {
                charObj.gameObject.SetActive(false);


            }
        }

        charObjects.Clear();

        if (index > words.Length - 1)
        {
            Debug.LogError("index out of range, please enter range between 0-" + (words.Length - 1).ToString());
            return;
        }

        char[] chars = words[index].ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            CharObject clone = GetOrCreateCharObject(); // 🔄 reuse or create
            clone.transform.SetParent(container, false); // Ensure parenting
            clone.Init(chars[i]); // Init with new char
        }

        currentWord = index;
    }

    public void Swap(int indexA, int indexB)
    {
        CharObject tmpA = charObjects[indexA];

        charObjects[indexA] = charObjects[indexB];
        charObjects[indexB] = tmpA;

        charObjects[indexA].transform.SetAsLastSibling();
        charObjects[indexB].transform.SetAsLastSibling();

        CheckWord();

    }

    public void Select(CharObject charObject)
    {
        if (firstSelected)
        {
            Swap(firstSelected.index, charObject.index);

            //Unselected
            //firstSelected = null;
            firstSelected.Select();
            charObject.Select();

        }
        else
        {
            firstSelected = charObject;
        }
    }


    public void UnSelect()
    {
        firstSelected = null;

    }
    public void CheckWord()
    {
        StartCoroutine(CoCheckWord());
    }

    IEnumerator CoCheckWord()
    {
        yield return new WaitForSeconds(0.5f);
        string word = "";
        foreach (CharObject charObject in charObjects)
        {
            word += charObject.character;
        }

        if (word == words[currentWord])
        {
            currentWord++;
            ShowScramble(currentWord);

        }

    }


    // Get a CharObject from the pool (reuse if possible)
    CharObject GetOrCreateCharObject()
    {
        foreach (var obj in charObjects)
        {
            if (!obj.gameObject.activeSelf) // Inactive = available
            {
                obj.gameObject.SetActive(true);
                return obj;
            }
        }

        // No available, create a new one
        CharObject newObj = Instantiate(prefab, container);
        charObjects.Add(newObj);
        return newObj;
    }


}
