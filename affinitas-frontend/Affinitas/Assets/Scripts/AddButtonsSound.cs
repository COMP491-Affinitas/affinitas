using UnityEngine;
using UnityEngine.UI;

public class AddButtonsSound : MonoBehaviour
{
    public AudioClip clickSound;
    public AudioSource audioSource;

    void Awake()
    {
        // Hook into all buttons in the scene
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(() => PlayClickSound());
        }
    }

    void PlayClickSound()
    {
        audioSource.PlayOneShot(clickSound);
    }
}
