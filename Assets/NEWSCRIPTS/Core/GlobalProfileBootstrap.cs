using UnityEngine;
using System.Threading.Tasks;

public class GlobalProfileBootstrap : MonoBehaviour
{
    private static bool _booted;

    [Header("Defaults if cloud load fails")]
    [SerializeField] private string defaultName  = "Player";
    [SerializeField] private string defaultIcon  = "default_icon";
    [SerializeField] private string defaultFrame = "bronze_frame";
    [SerializeField] private string defaultTitle = "Scared Baby";
    [SerializeField] private int    defaultLevel = 1;

    [Header("Prefs")]
    [SerializeField] private string walletPrefKey = "walletAddress";

    private async void Awake()
    {
        if (_booted) { Destroy(gameObject); return; }
        _booted = true;
        DontDestroyOnLoad(gameObject);

        // If WalletLoginManager already set a profile, do nothing
        if (ActiveProfileStore.Instance?.CurrentProfile != null) return;

        string wallet = PlayerPrefs.GetString(walletPrefKey, "").Trim();

        // 🛑 FIX: Do NOT create a fallback profile if a wallet exists.
        //        Let WalletLoginManager handle it.
        if (!string.IsNullOrEmpty(wallet))
        {
            Debug.Log("[GlobalProfileBootstrap] Wallet exists. Waiting for WalletLoginManager to load profile.");
            return;
        }

        // 🟩 No wallet → create local fallback so UI doesn’t break
        var local = new NewProfileData
        {
            wallet      = "local_default",
            playerName  = defaultName,
            activeIcon  = defaultIcon,
            activeFrame = defaultFrame,
            activeTitle = defaultTitle,
            level       = defaultLevel,
            mssBanked      = 0,
            unlockedCosmetics = new System.Collections.Generic.List<string> { defaultIcon },
            enemyKills   = new System.Collections.Generic.Dictionary<string, int>(),
            bossKills    = new System.Collections.Generic.Dictionary<string, int>(),
            multiplayerWins   = 0,
            multiplayerLosses = 0,
            longestSurvivalTime = 0f,
        };

        ActiveProfileStore.Instance.SetProfile(local);
        Debug.Log("[GlobalProfileBootstrap] Created LOCAL fallback profile (no wallet/cloud).");
    }
}
