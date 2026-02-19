using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// ProfileUploader — SECURE SERVER VERSION
/// ✅ No Firestore REST calls from Unity
/// ✅ All writes go through your Render server
/// ✅ JWT required on every call
/// 
/// Server base:
///   https://mss-payout.onrender.com
/// 
/// Expected server endpoints (you already started these patterns):
///   POST /profile/stats/save
///   POST /profile/cosmetics/save
///   POST /profile/cosmetic/set
///   POST /profile/patch/int
///   POST /profile/patch/kill
///   POST /profile/savefull
/// 
/// If any endpoint isn't deployed yet, the game will compile,
/// but calls will fail until that endpoint exists.
/// </summary>
public static class ProfileUploader
{
    // ✅ Your secure server (Render)
    private static readonly string serverBase = "https://mss-payout.onrender.com";

    // ============================================================
    // PUBLIC API (kept compatible with your current calls)
    // ============================================================

    public static async Task<bool> PatchSingleEnemyKill(string wallet, string key, int value)
    {
        return await PostAuthed("/profile/patch/kill", new
        {
            wallet,
            kind = "enemy",
            key,
            value
        });
    }

    public static async Task<bool> PatchSingleBossKill(string wallet, string key, int value)
    {
        return await PostAuthed("/profile/patch/kill", new
        {
            wallet,
            kind = "boss",
            key,
            value
        });
    }

    public static Task<bool> UpdateTotalXP(string wallet, int totalXP)
        => PatchSimpleInt(wallet, "totalXP", totalXP);

    public static async Task<bool> UpdateEnemyKills(string wallet, Dictionary<string, int> kills)
    {
        kills ??= new Dictionary<string, int>();
        return await PostAuthed("/profile/stats/save", new
        {
            wallet,
            enemyKills = kills
        });
    }

    public static async Task<bool> UpdateBossKills(string wallet, Dictionary<string, int> kills)
    {
        kills ??= new Dictionary<string, int>();
        return await PostAuthed("/profile/stats/save", new
        {
            wallet,
            bossKills = kills
        });
    }

    public static async Task<bool> UpdateMatchStats(string wallet, int aiWins, int mpWins, int mpLosses)
    {
        return await PostAuthed("/profile/stats/save", new
        {
            wallet,
            aiWins,
            multiplayerWins = mpWins,
            multiplayerLosses = mpLosses
        });
    }

    /// <summary>
    /// ✅ Batch stats save (recommended)
    /// </summary>
    public static async Task<bool> SaveStatsBatch(string wallet, NewProfileData p)
    {
        if (p == null || string.IsNullOrWhiteSpace(wallet))
            return false;

        p.enemyKills ??= new Dictionary<string, int>();
        p.bossKills ??= new Dictionary<string, int>();

        return await PostAuthed("/profile/stats/save", new
        {
            wallet,
            totalXP = p.totalXP,
            level = p.level,
            aiWins = p.aiWins,
            multiplayerWins = p.multiplayerWins,
            multiplayerLosses = p.multiplayerLosses,
            enemyKills = p.enemyKills,
            bossKills = p.bossKills,
            longestSurvivalTime = p.longestSurvivalTime
        });
    }

    // ==============================
    // COSMETICS
    // ==============================

    public static Task<bool> UpdateActiveIcon(string wallet, string icon)
        => SetCosmetic(wallet, "activeIcon", icon);

    public static Task<bool> UpdateActiveFrame(string wallet, string frame)
        => SetCosmetic(wallet, "activeFrame", frame);

    public static Task<bool> UpdateActiveTitle(string wallet, string title)
        => SetCosmetic(wallet, "activeTitle", title);

    public static Task<bool> SaveUnlockedCosmetics(string wallet, List<string> cosmetics)
    {
        cosmetics ??= new List<string>();
        return PostAuthed("/profile/cosmetics/save", new
        {
            wallet,
            unlockedCosmetics = cosmetics
        });
    }

    // ==============================
    // MSS + Survival + Multiplayer aliases (kept for compatibility)
    // ==============================

    public static Task<bool> UpdatemssBanked(string w, int v)
        => PatchSimpleInt(w, "mssBanked", v);

    public static Task<bool> UpdatetotalMssEarned(string w, int v)
        => PatchSimpleInt(w, "totalMssEarned", v);

    public static Task<bool> UpdatelongestSurvivalTime(string w, float seconds)
        => PatchSimpleFloat(w, "longestSurvivalTime", seconds);

    public static Task<bool> UpdateMultiplayerWins(string w, int v)
        => PatchSimpleInt(w, "multiplayerWins", v);

    public static Task<bool> UpdateMultiplayerLosses(string w, int v)
        => PatchSimpleInt(w, "multiplayerLosses", v);

    // ==============================
    // FULL PROFILE SAVE (create/migration only)
    // ==============================

    public static async Task<bool> SaveFullProfile(NewProfileData p)
    {
        if (p == null || string.IsNullOrWhiteSpace(p.wallet))
            return false;

        // send whole object; server should sanitize/validate fields it accepts
        return await PostAuthed("/profile/savefull", new
        {
            wallet = p.wallet,
            profile = p
        });
    }

    // ============================================================
    // INTERNAL HELPERS
    // ============================================================

    private static Task<bool> SetCosmetic(string wallet, string field, string value)
    {
        return PostAuthed("/profile/cosmetic/set", new
        {
            wallet,
            field,
            value
        });
    }

    private static Task<bool> PatchSimpleInt(string wallet, string field, int value)
    {
        return PostAuthed("/profile/patch/int", new
        {
            wallet,
            field,
            value
        });
    }

    private static Task<bool> PatchSimpleFloat(string wallet, string field, float value)
    {
        return PostAuthed("/profile/patch/float", new
        {
            wallet,
            field,
            value
        });
    }

    private static async Task<bool> PostAuthed(string path, object payloadObj)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!AuthTokenStore.HasToken)
        {
            Debug.LogWarning("[ProfileUploader] No JWT yet (AuthTokenStore empty).");
            return false;
        }

        string url = serverBase.TrimEnd('/') + path;
        string json = JsonConvert.SerializeObject(payloadObj);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + AuthTokenStore.Jwt);

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ProfileUploader] POST FAIL {path} HTTP {req.responseCode}\n{req.downloadHandler.text}");
                return false;
            }

            // Expect: { ok: true } style
            try
            {
                var resp = JsonConvert.DeserializeObject<OkResp>(req.downloadHandler.text);
                if (resp != null && resp.ok) return true;
            }
            catch { /* ignore parse errors */ }

            // If server returns something else but HTTP is 200, still treat as success:
            return true;
        }
    }

    [Serializable]
    private class OkResp
    {
        public bool ok;
        public string error;
    }
}
