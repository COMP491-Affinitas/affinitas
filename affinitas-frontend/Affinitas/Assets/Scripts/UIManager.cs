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
    }

    public void GoToMenu()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        endingPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void OpenEndingPanel()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        menuPanel.SetActive(false);
        endingPanel.SetActive(true);
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
        // TODO: Stop Python/LLM Connection before quitting!
        // Also handle this at user quitting via the close button.
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


}
