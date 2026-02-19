using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class AutoLoginBootstrap : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Main_Menu";
    [SerializeField] private string createProfileSceneName = "Create_Profile_Screen";

    private bool _isHandling = false;

    private async void Start()
    {
        // ❗ IMPORTANT: Only run if we are in the Connect Wallet scene
        if (SceneManager.GetActiveScene().name != "Connect_Wallet_Screen")
        {
            Debug.Log("⏳ AutoLoginBootstrap ignored: Not in Connect_Wallet_Screen.");
            return;
        }

        // Wait 1 frame so KeepManagersAlive & ActiveProfileStore exist
        await Task.Yield();
        await Task.Delay(100);

        await TryAutoLogin();

        // Clean up old pending data
        RuntimeProfileHolder.ClearPending();
    }

    private async Task TryAutoLogin()
    {
        if (_isHandling) return;
        _isHandling = true;

        Debug.Log("🔄 AutoLogin starting (Connect Wallet Screen)...");

        // 1. Get stored wallet
        string wallet = PlayerPrefs.GetString("walletAddress", "");

        if (string.IsNullOrWhiteSpace(wallet))
        {
            Debug.Log("🛑 No wallet saved — waiting for manual login.");
            _isHandling = false;
            return;
        }

        Debug.Log($"🔑 Wallet from PlayerPrefs: {wallet}");
        Debug.Log("📡 Attempting auto-load profile from Firebase...");

        // 2. Load or create profile
        NewProfileData loadedProfile = await ProfileDataLoader.LoadOrCreateProfile(wallet);

        if (loadedProfile == null)
        {
            Debug.Log("🆕 No profile found, going to Create Profile screen...");
            SceneManager.LoadScene(createProfileSceneName);
            _isHandling = false;
            return;
        }

        Debug.Log("✅ AutoLogin: Profile loaded successfully.");

        // 3. Apply profile to runtime
        RuntimeProfileHolder.SetProfile(loadedProfile);

        // Apply to ActiveProfileStore
        if (ActiveProfileStore.Instance != null)
            ActiveProfileStore.Instance.SetProfile(loadedProfile);

        // Upload baseline stats if missing
    

        Debug.Log("➡ AutoLogin: Loading Main Menu...");
        SceneManager.LoadScene(mainMenuSceneName);

        _isHandling = false;
    }
}
