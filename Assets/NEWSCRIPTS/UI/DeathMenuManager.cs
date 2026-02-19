using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private string mainMenuScene = "Main_Menu";

    private bool isDead = false;
    private bool saved = false;

    void Awake()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    public void ShowDeathMenu()
    {
        if (isDead) return;
        isDead = true;

        Time.timeScale = 0f;

        if (deathPanel != null)
            deathPanel.SetActive(true);
    }

    // Button: Restart level (NO SAVE)
    public void OnRestart()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    // Button: Main Menu (SAVE)
    public async void OnMainMenu()
    {
        if (!saved)
        {
            saved = true;

            var profile = ActiveProfileStore.Instance?.CurrentProfile;
            if (profile != null)
            {
                await ProfileUploader.SaveStatsBatch(profile.wallet, profile);
                Debug.Log("[DeathMenu] ✅ Stats saved on death exit");
            }
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
