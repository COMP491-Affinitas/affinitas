using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Singleton
    public static UIManager Instance { get; private set; }

    [SerializeField] CanvasGroup[] gamePanels;
    // 0: menuPanel, 1: savesListPanel, 2: settingsPanel, 3: mainPanel, 4: endingPanel, 5: creditsPanel

    [SerializeField] GameObject savesListContent;
    [SerializeField] GameObject savePrefab;

    [SerializeField] Toggle fullscreenToggle;

    [SerializeField] TextMeshProUGUI endingTextMesh;
    [SerializeField] ScrollRectHelper endingPanelScrollRectHelper;

    [SerializeField] Button continueToMenuButton;
    [SerializeField] Button continueToMainGameButton;

    List<string> savedGameIds = new();

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        InitiliazePanels();
    }

    void MakeActive(int index)
    {
        // 0: menuPanel, 1: savesListPanel, 2: settingsPanel, 3: mainPanel, 4: endingPanel
        for (int i = 0; i < gamePanels.Length; i++)
        {
            bool isActive = i == index; // only indexed panel active
            gamePanels[i].alpha = isActive ? 1f : 0f;
            gamePanels[i].interactable = isActive;
            gamePanels[i].blocksRaycasts = isActive;
        }
    }

    void InitiliazePanels()
    {
        MakeActive(0); // only menu panel active
    }

    public void StartGame()
    {
        MakeActive(3); // main panel
        MainGame.MainGameUiManager.Instance.InitializeMainPanels();
        MainGame.MainGameUiManager.Instance.InitializeMainPanelsForNewGame();
    }

    // call from Start New Game button
    public async void LoadNewGame()
    {
        await GameManager.Instance.LoadNewGame();
        StartGame();
        MainGame.MainGameUiManager.Instance.InitializeMainPanelsForSavedGame();
    }

    public void GoToMenu()
    {
        MakeActive(0);
    }

    // Call from Saved Games button in Menu Panel
    public async void OpenSavesListPanel()
    {
        MakeActive(1);

        List<(string, string)> savesTexts = await GameManager.Instance.CreateGameSavesList();

        foreach ((string,string) saveText in savesTexts)
        {
            if (savedGameIds.Contains(saveText.Item1))
                continue;

            GameObject newSave = Instantiate(savePrefab);
            newSave.transform.SetParent(savesListContent.transform, false);
            newSave.GetComponent<SavedGame>().AddSavedGameText(saveText.Item1, saveText.Item2);

            savedGameIds.Add(saveText.Item1);
        }
    }

    // Open panel and put ending text from server
    public void OpenEndingPanel()
    {
        MakeActive(4);
        endingTextMesh.text = "";
    }

    public void PutEndingTextToPanel(string endingText)
    {
        StartCoroutine(AddTextLetterByLetter(endingTextMesh, endingPanelScrollRectHelper, endingText));
    }

    public void PauseGame(bool fromMenu)
    {
        OpenSettingsPanel(fromMenu);
        //Time.timeScale = 0;
    }

    public void ContinueGame(bool toMenu)
    {
        if (toMenu)
            MakeActive(0);
        else
            MakeActive(3);
        //Time.timeScale = 1;    
    }

    public void OpenCreditsPanel()
    {
        MakeActive(5);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void FullscreenToggle()
    {
        if (!Screen.fullScreen)
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
        else
            Screen.fullScreen = !Screen.fullScreen;
    }

    public void OpenSettingsPanel(bool fromMenu)
    {
        continueToMenuButton.gameObject.SetActive(fromMenu);
        continueToMainGameButton.gameObject.SetActive(!fromMenu);
        MakeActive(2);
        FullscreenToggleInitializer();
    }

    void FullscreenToggleInitializer()
    {
        if (Screen.fullScreen)
            fullscreenToggle.isOn = true;
        else
            fullscreenToggle.isOn = false;
    }

    IEnumerator AddTextLetterByLetter(TextMeshProUGUI textMesh, ScrollRectHelper scrollRectHelper, string str)
    {
        textMesh.text = "";
        yield return null;

        for (int i = 0; i < str.Length; i++)
        {
            textMesh.text += str[i];
            scrollRectHelper.ScrollToBottom();
            yield return new WaitForSeconds(0.05f);
        }
    }


}
