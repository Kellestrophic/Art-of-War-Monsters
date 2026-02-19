using UnityEngine;
using UnityEngine.SceneManagement;

public class UIPanelManager : MonoBehaviour
{
    // ================================
    // MAIN PANELS
    // ================================
    [Header("Main Panels")]
    public GameObject mainMenuPanelObject;
    public GameObject singlePlayerPanelObject;

    // ================================
    // SUB PANELS
    // ================================
    [Header("Sub Panels")]
    public GameObject profilePanelObject;
    public GameObject settingsPanelObject;
    public GameObject statsPanelObject;
    public GameObject iconGridPanelObject;
    public GameObject creditsPanelObject; // ⭐ NEW
public void StartSurvivalRun(GameObject selectedCharacterPrefab)
{
    SurvivalRunConfig.SelectedPlayerPrefab = selectedCharacterPrefab;
    LoadSceneByName("Survival_Arena");
}

    // ================================
    // LEADERBOARD
    // ================================
    [Header("Leaderboard")]
    public GameObject leaderboardPanelObject;

    // ================================
    // MAIN PANEL SWITCHING
    // ================================
    public void ShowOnlyMain(GameObject mainPanel)
    {
        if (mainMenuPanelObject != null) mainMenuPanelObject.SetActive(false);
        if (singlePlayerPanelObject != null) singlePlayerPanelObject.SetActive(false);

        if (mainPanel != null) mainPanel.SetActive(true);
    }

    // ================================
    // SUB PANEL LOGIC
    // ================================
    public void ShowSubPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
    }

    public void HidePanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);
    }

    // ================================
    // CREDITS
    // ================================
    public void ShowCredits()
    {
        HideAllSubPanels();

        if (creditsPanelObject != null)
            creditsPanelObject.SetActive(true);
    }

    public void HideCredits()
    {
        if (creditsPanelObject != null)
            creditsPanelObject.SetActive(false);
    }

    // ================================
    // LEADERBOARD
    // ================================
    public void ShowLeaderboard()
    {
        HideAllSubPanels();

        if (leaderboardPanelObject != null)
            leaderboardPanelObject.SetActive(true);
    }

    // ================================
    // UTIL
    // ================================
    private void HideAllSubPanels()
    {
        if (profilePanelObject != null) profilePanelObject.SetActive(false);
        if (settingsPanelObject != null) settingsPanelObject.SetActive(false);
        if (statsPanelObject != null) statsPanelObject.SetActive(false);
        if (iconGridPanelObject != null) iconGridPanelObject.SetActive(false);
        if (creditsPanelObject != null) creditsPanelObject.SetActive(false);
        if (leaderboardPanelObject != null) leaderboardPanelObject.SetActive(false);
    }

    // ================================
    // QUIT
    // ================================
    public void QuitGame()
    {
        Debug.Log("💀 Quit Game Pressed");
        Application.Quit();
    }

    // ================================
    // SCENE LOADING
    // ================================
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;

        Debug.Log("🔁 Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
