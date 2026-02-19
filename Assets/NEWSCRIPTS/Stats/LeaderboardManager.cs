using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject entryPrefab;

    [Header("Cosmetics")]
    [SerializeField] private CosmeticLibrary cosmeticLibrary;
    [SerializeField] private FrameLibrary frameLib;

    private async void OnEnable()
    {
        Debug.Log("📊 [Leaderboard] OnEnable → starting RefreshLeaderboard()");

        if (!cosmeticLibrary)
        {
            Debug.LogError("❌ CosmeticLibrary NOT ASSIGNED on LeaderboardManager");
            return;
        }

        if (!frameLib)
        {
            Debug.LogError("❌ FrameLibrary NOT ASSIGNED on LeaderboardManager");
            return;
        }

        RefreshLeaderboard();
    }

    public async void RefreshLeaderboard()
    {
        Debug.Log("📊 [Leaderboard] RefreshLeaderboard() STARTED");

        // Clear UI
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        // Load leaderboard rows
        List<LeaderboardRowData> profiles =
            await FirebaseLeaderboardLoader.LoadLeaderboardRowsAsync();

        if (profiles == null || profiles.Count == 0)
        {
            Debug.LogWarning("[Leaderboard] No profiles found.");
            return;
        }

        Debug.Log($"📊 [Leaderboard] Profiles loaded: {profiles.Count}");

        // Sort by TOTAL MSS (earned, not withdrawn)
        profiles = profiles
            .OrderByDescending(p => p.totalMss)
            .ToList();

        int rank = 1;

        foreach (var p in profiles)
        {
            Debug.Log(
                $"➡ [Leaderboard] Row → Rank:{rank}  Name:{p.playerName}  " +
                $"MSS:{p.totalMss}  Icon:{p.iconKey}  Frame:{p.frameKey}"
            );

            // ---------------- ICON (CORRECT SOURCE) ----------------
            Sprite icon = cosmeticLibrary.GetSprite(p.iconKey);

            if (icon == null)
            {
                Debug.LogWarning($"❗ ICON NOT FOUND: {p.iconKey} → using default_icon");
                icon = cosmeticLibrary.GetSprite("default_icon");
            }

            // ---------------- FRAME ----------------
            Sprite frame = frameLib.GetFrame(p.frameKey);

            if (frame == null)
            {
                Debug.LogWarning($"❗ FRAME NOT FOUND: {p.frameKey} → using bronze_frame");
                frame = frameLib.GetFrame("bronze_frame");
            }

            // ---------------- UI ROW ----------------
            var rowGO = Instantiate(entryPrefab, contentRoot);
            var entry = rowGO.GetComponent<LeaderboardEntry>();

            entry.SetEntry(
                rank,
                string.IsNullOrEmpty(p.playerName) ? "Unknown" : p.playerName,
                p.level,
                p.totalMss,
                icon,
                frame
            );

            Debug.Log($"✔️ Added leaderboard row #{rank}");
            rank++;
        }

        Debug.Log("📊 [Leaderboard] Finished creating leaderboard rows.");
    }
}
