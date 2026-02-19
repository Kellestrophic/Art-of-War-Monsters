using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuButtons : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "Main_Menu";
    [SerializeField] private Button exitButton;

    private bool isQuitting = false;

    private void Awake()
    {
        if (exitButton)
            exitButton.onClick.AddListener(() => _ = ExitToMainMenuAsync());
    }

    public async Task ExitToMainMenuAsync()
{
    if (isQuitting) return;
    isQuitting = true;

    if (exitButton)
        exitButton.interactable = false;

    // ❌ NO STATS SAVING HERE

    Time.timeScale = 1f;
    SceneManager.LoadScene(mainMenuSceneName);
}

}
