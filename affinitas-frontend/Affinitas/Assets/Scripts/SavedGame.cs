using UnityEngine;
using TMPro;

public class SavedGame : MonoBehaviour
{
    string saveId;
    [SerializeField] TextMeshProUGUI savedGameTextMesh;

    public void AddSavedGameText(string saveIdVal, string saveText)
    {
        saveId = saveIdVal;
        savedGameTextMesh.text = saveText;
    }

    // Call from Continue Game button on saved game instance panel
    public async void LoadSavedGame()
    {
        await GameManager.Instance.LoadSavedGame(saveId);
        UIManager.Instance.StartGame();
        MainGame.MainGameUiManager.Instance.InitializeMainPanelsForSavedGame();

        //MainGame.MainGameUiManager.Instance.LoadSavedChatHistories();

        //TODO: Load journal info

        MainGameManager.Instance.LoadSavedQuestsToQuestPanel();
    }

    // Call from delete (X) button on saved game instance panel
    public async void DeleteSavedGame()
    {
        await GameManager.Instance.DeleteGameSave(saveId);  
        Destroy(gameObject);
    }
}
