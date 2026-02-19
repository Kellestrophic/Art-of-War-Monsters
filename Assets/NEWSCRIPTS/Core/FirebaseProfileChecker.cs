using System.Threading.Tasks;
using UnityEngine;

public static class FirebaseProfileChecker
{
    // 🔐 SECURE SERVER CHECK
    public static async Task<bool> DoesProfileExist(string walletAddress)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            return false;

        if (!AuthTokenStore.HasToken)
        {
            Debug.LogWarning("[ProfileChecker] No JWT.");
            return false;
        }

        var profile = await ProfileDataLoader.LoadProfileFromServer(walletAddress);

        return profile != null && !string.IsNullOrWhiteSpace(profile.playerName);
    }
}
