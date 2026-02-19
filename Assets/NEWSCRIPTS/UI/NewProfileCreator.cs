using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NewProfileCreator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button createProfileButton;

    [Header("Scene Settings")]
    [SerializeField] private string targetScene = "Main_Menu";

    private string wallet;
    private bool isSaving = false;

    private void Start()
    {
        wallet = PlayerPrefs.GetString("walletAddress", "");

        if (string.IsNullOrEmpty(wallet))
        {
            Debug.LogError("[NewProfileCreator] No wallet found.");
            return;
        }

        if (ActiveProfileStore.Instance != null &&
            ActiveProfileStore.Instance.CurrentProfile != null)
        {
            Debug.Log("[NewProfileCreator] Profile already exists → redirecting.");
            SceneManager.LoadScene(targetScene);
            return;
        }

        if (createProfileButton)
            createProfileButton.onClick.AddListener(SubmitNewProfile);
    }

    public void SubmitNewProfile()
    {
        if (isSaving)
        {
            Debug.LogWarning("[NewProfileCreator] Already saving...");
            return;
        }

        isSaving = true;
        if (createProfileButton)
            createProfileButton.interactable = false;

        _ = HandleSubmitAsync();
    }

    private async System.Threading.Tasks.Task HandleSubmitAsync()
    {
        try
        {
            string enteredName = playerNameInput ? playerNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(wallet) || string.IsNullOrEmpty(enteredName))
            {
                Debug.LogWarning("[NewProfileCreator] Missing wallet or name.");
                return;
            }

            Debug.Log($"[NewProfileCreator] Creating profile for {wallet}, name={enteredName}");

            var unlockedCosmetics = new List<string>
            {
                "default_icon",
                "bronze_frame",
                "scaredbaby_title",
                "Level1_Happiest"
            };

            NewProfileData newProfile = new NewProfileData
            {
                wallet = wallet,
                playerName = enteredName,
                activeIcon = "default_icon",
                activeFrame = "bronze_frame",
                activeTitle = "scaredbaby_title",
                unlockedCosmetics = unlockedCosmetics,
                level = 1,
                mssBanked = 0,
                totalXP = 0,
                enemyKills = new Dictionary<string, int>(),
                bossKills = new Dictionary<string, int>(),
                multiplayerWins = 0,
                multiplayerLosses = 0,
                longestSurvivalTime = 0f,
            };

            Debug.Log("[NewProfileCreator] Calling SaveFullProfile...");
            bool success = await ProfileUploader.SaveFullProfile(newProfile);

            if (!success)
            {
                Debug.LogError("[NewProfileCreator] ❌ SaveFullProfile failed.");
                return;
            }

            Debug.Log("[NewProfileCreator] ✅ Profile created. Loading Main Menu.");

            RuntimeProfileHolder.SetProfile(newProfile);
            ActiveProfileStore.Instance?.SetProfile(newProfile);

            SceneManager.LoadScene(targetScene);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NewProfileCreator] EXCEPTION:\n" + ex);
        }
        finally
        {
            isSaving = false;
            if (createProfileButton)
                createProfileButton.interactable = true;
        }
    }
}
