using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject settingsPanel;

    [Header("Optional UI Focus")]
    [SerializeField] private GameObject firstPauseButton;
    [SerializeField] private GameObject firstSettingsWidget;

    private bool isPaused;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused) ShowPause();
            else if (settingsPanel != null && settingsPanel.activeSelf) CloseSettings();
            else HidePause();
        }
    }

    public void OnPause() => (isPaused ? (System.Action)HidePause : ShowPause)();

    public void ShowPause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuUI) pauseMenuUI.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
        SetFocus(firstPauseButton);
    }

    public void HidePause()
    {
        isPaused = false;
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        if (!pauseMenuUI) return;
        if (settingsPanel) settingsPanel.SetActive(true);
        SetFocus(firstSettingsWidget);
    }

    public void CloseSettings()
    {
        Debug.Log("[PauseMenuManager] CloseSettings()");
        if (!settingsPanel) { Debug.LogError("settingsPanel is NULL"); return; }
        settingsPanel.SetActive(false);
    }

    public void ExitToMainMenu()
    {
        // ⭐ FIX: Make sure the game is unpaused before switching scenes
        Time.timeScale = 1f;

        SceneManager.LoadScene("Main_Menu");
    }

    private void SetFocus(GameObject go)
    {
        if (!go) return;
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(go);
    }
}
