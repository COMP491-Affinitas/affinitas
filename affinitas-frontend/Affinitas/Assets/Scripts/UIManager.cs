using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Singleton
    public static UIManager Instance { get; private set; }

    [SerializeField]
    GameObject menuPanel;
    [SerializeField]
    GameObject settingsPanel;
    [SerializeField]
    GameObject mainPanel;
    [SerializeField]
    GameObject endingPanel;

    [SerializeField]
    Toggle fullscreenToggle;

    [SerializeField]
    TextMeshProUGUI endingTextMesh;
    [SerializeField]
    ScrollRectHelper endingPanelScrollRectHelper;

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

    void InitiliazePanels()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(false);
        endingPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void StartGame()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        endingPanel.SetActive(false);
        mainPanel.SetActive(true);
        MainGame.MainGameUiManager.Instance.UpdateDaysLeftPanel();
    }

    public void GoToMenu()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        endingPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    // Open panel and put ending text from server
    public void OpenEndingPanel()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        menuPanel.SetActive(false);
        endingPanel.SetActive(true);
        endingTextMesh.text = "";
    }

    public void PutEndingTextToPanel(string endingText)
    {
        StartCoroutine(AddTextLetterByLetter(endingTextMesh, endingPanelScrollRectHelper, endingText));
    }

    // Make sure that SettingsPanel is above all other panels in hierarchy (at the bottom of list)
    public void PauseGame()
    {
        OpenSettingsPanel();
        Time.timeScale = 0;
    }

    // Make sure that SettingsPanel is above all other panels in hierarchy (at the bottom of list)
    public void ContinueGame()
    {
        settingsPanel.SetActive(false);
        Time.timeScale = 1;    
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

    void OpenSettingsPanel()
    {
        settingsPanel.SetActive(true);
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
