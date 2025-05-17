using UnityEngine;
using TMPro;

public class SavedGame : MonoBehaviour
{
    public string savedGameName;
    [SerializeField] TextMeshProUGUI savedGameTextMesh;

    public void AddSavedGameText(string saveName, string saveText)
    {
        savedGameName = saveName;
        savedGameTextMesh.text = saveText;
    }

    // Call from Continue Game button on saved game instance panel
    public void LoadSavedGame()
    {
        //TODO
    }

    // Call from delete (X) button on saved game instance panel
    public void DeleteSavedGame()
    {
        //TODO
    }
}
