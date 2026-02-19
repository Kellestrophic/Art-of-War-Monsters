using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

public static class ProfileDataLoader
{
    // ✅ Your secure server (Render)
    private static readonly string serverBase = "https://mss-payout.onrender.com";

    // ============================================================
    // LOAD PROFILE (SECURE SERVER, JWT REQUIRED)
    // ============================================================
    public static async Task<NewProfileData> LoadProfileFromServer(string wallet)
    {
        if (string.IsNullOrWhiteSpace(wallet))
        {
            Debug.LogError("[ProfileDataLoader] wallet is empty.");
            return null;
        }

        // Must have JWT
        if (!AuthTokenStore.HasToken)
        {
            Debug.LogWarning("[ProfileDataLoader] No JWT yet. Call auth first.");
            return null;
        }

        string url = serverBase.TrimEnd('/') + "/profile/load";

        string payload = JsonUtility.ToJson(new ProfileLoadReq { wallet = wallet });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // ✅ JWT header
            req.SetRequestHeader("Authorization", "Bearer " + AuthTokenStore.Jwt);

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ProfileDataLoader] /profile/load HTTP {req.responseCode}\n{req.downloadHandler.text}");
                return null;
            }

            var resp = JsonUtility.FromJson<ProfileLoadResp>(req.downloadHandler.text);

            if (resp == null || !resp.ok)
            {
                Debug.LogError("[ProfileDataLoader] Invalid response: " + req.downloadHandler.text);
                return null;
            }

            // Profile can legitimately be null (new wallet)
            if (resp.profile == null)
                return null;

            // ✅ Convert server profile → NewProfileData
            NewProfileData p = resp.profile;

            // Local safety defaults
            p.wallet = wallet;
            p.unlockedCosmetics ??= new List<string>();
            p.enemyKills ??= new Dictionary<string, int>();
            p.bossKills ??= new Dictionary<string, int>();

            if (p.level <= 0) p.level = 1;
            if (p.mssBanked < 0) p.mssBanked = 0;
            if (p.totalMssEarned < 0) p.totalMssEarned = 0;
            if (p.totalXP < 0) p.totalXP = 0;

            if (string.IsNullOrWhiteSpace(p.activeIcon)) p.activeIcon = "default_icon";
            if (string.IsNullOrWhiteSpace(p.activeFrame)) p.activeFrame = "bronze_frame";
            if (string.IsNullOrWhiteSpace(p.activeTitle)) p.activeTitle = "scaredbaby_title";

            return p;
        }
    }

    // Backwards compatible name (so your other code keeps compiling)
    public static async Task<NewProfileData> LoadProfileFromFirebase(string wallet)
    {
        // 🔥 NO MORE FIRESTORE REST CALLS
        return await LoadProfileFromServer(wallet);
    }

    public static async Task<NewProfileData> LoadOrCreateProfile(string wallet)
    {
        // Your server returns profile:null if not found.
        return await LoadProfileFromServer(wallet);
    }

    // ============================================================
    // DTOs
    // ============================================================
    [Serializable]
    private class ProfileLoadReq
    {
        public string wallet;
    }

    [Serializable]
    private class ProfileLoadResp
    {
        public bool ok;
        public NewProfileData profile;
    }
}
