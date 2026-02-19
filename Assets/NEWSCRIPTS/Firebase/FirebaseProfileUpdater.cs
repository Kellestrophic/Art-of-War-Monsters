using System.Threading.Tasks;
using UnityEngine;

public static class FirebaseProfileUpdater
{
    /// <summary>
    /// Set active icon locally + save to /profiles/{wallet}/activeIcon
    /// </summary>
    public static async Task UpdatePlayerIcon(string newIconKey)
{
    var store   = ActiveProfileStore.Instance;
    var profile = store?.CurrentProfile;

    string wallet = profile?.wallet ?? PlayerPrefs.GetString("walletAddress", "");
    if (string.IsNullOrEmpty(wallet) || string.IsNullOrEmpty(newIconKey)) return;

    // Update runtime now so UI changes immediately
    if (profile != null)
    {
        profile.activeIcon = newIconKey;
        store.SetProfile(profile);
    }

    // ✅ NEW: persist a wallet-scoped local override
    PlayerPrefs.SetString($"profile.{wallet}.activeIcon", newIconKey);
    PlayerPrefs.Save();

    // Save to Firestore
    await ProfileUploader.UpdateActiveIcon(wallet, newIconKey);

    // ✅ NEW: re-broadcast after PATCH completes (guards against a late remote overwrite)
    if (profile != null)
        store.SetProfile(profile);

    Debug.Log($"✅ Icon updated to: {newIconKey}");
}

    // === Update Title In Firebase ===
    /// <summary>
    /// Set active title locally + save to /profiles/{wallet}/activeTitle
    /// </summary>
    public static void UpdateTitleInFirebase(string newTitle)
    {
        var store   = ActiveProfileStore.Instance;
        var profile = store?.CurrentProfile;

        string wallet = profile?.wallet ?? PlayerPrefs.GetString("walletAddress", "");
        if (string.IsNullOrEmpty(wallet))
        {
            Debug.LogError("❌ Wallet address missing. Cannot update title.");
            return;
        }

        // Update runtime so UI reflects immediately
        if (profile != null)
        {
            profile.activeTitle = newTitle;
            store.SetProfile(profile);
        }

        // Fire-and-forget save to /profiles
        _ = ProfileUploader.UpdateActiveTitle(wallet, newTitle);
        Debug.Log("✅ Title updated in Firebase: " + newTitle);
    }
    // ============================================================
// STATS + XP SAVE
// ============================================================
}

