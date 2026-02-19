// Assets/NEWSCRIPTS/Rewards/LevelCompleteOrchestrator_Stats.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelCompleteOrchestrator_Stats : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StatsTrackerRunAdapter runAdapter;
    [SerializeField] private RewardCalculator calculator;
    [SerializeField] private RewardUI rewardUI;
    [SerializeField] private FirebaseMSSService firebaseMSS;

    [Header("Finish Stats")]
    [Range(0, 100)] public float lifeRemainingPercent = 50f;
    public int playerLevel = 1;

    [Header("UI")]
    public string mainMenuSceneName = "MainMenu";
    public string rewardTitle = "Level Complete!";
    [TextArea] public string rewardBody = "You earned MSS this run.";

    // 🔹 Auto-wire MSS if the field is empty
    private void Awake()
    {
        if (!firebaseMSS)
            firebaseMSS = FirebaseMSSService.Instance ?? FindFirstObjectByType<FirebaseMSSService>(FindObjectsInactive.Include);
    }

    // Called by EndLevelPortal
    public void CompleteLevel()
    {
        if (!calculator || !rewardUI || !runAdapter)
        {
            Debug.LogError("[Orchestrator] Missing refs. Need RunAdapter, RewardCalculator, RewardUI.");
            return;
        }

        // 1) Gather deltas
        Dictionary<string, int> enemies = runAdapter.GetEnemyRunDeltas();
        Dictionary<string, int> bosses = runAdapter.GetBossRunDeltas();

        string DumpMap(Dictionary<string, int> m)
        {
            if (m == null || m.Count == 0) return "(none)";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var kv in m)
                if (kv.Value > 0) sb.Append($"{kv.Key}:{kv.Value}, ");
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        Debug.Log($"[MSS] Deltas — Enemies: {DumpMap(enemies)} | Bosses: {DumpMap(bosses)}");

        // 2) Life % from player
        float lifePct = GetPlayerLifePercentOrFallback(lifeRemainingPercent);

        // 3) Level from profile if present
        int lvl = playerLevel;
        var prof = ActiveProfileStore.Instance?.CurrentProfile as NewProfileData;
        if (prof != null && prof.level > 0) lvl = prof.level;

        // 4) Calculate reward
        var bd = calculator.CalculateRewardWithBreakdown(enemies, bosses, lifePct, lvl);

        // 5) Show end-of-level UI
        string body = $"{rewardBody}\n\n" +
                      $"Enemies: {bd.enemies}\n" +
                      $"Bosses: {bd.bosses}\n" +
                      $"Life: {bd.lifeBonus}\n" +
                      $"Level: {bd.levelBonus}\n";

        var data = new RewardUI.RewardData
        {
            title = rewardTitle,
            body = body,
            mssBanked = bd.total
        };

        string nextScene = LevelFlowManager.Instance != null
    ? LevelFlowManager.Instance.GetNextSceneAfterRewards()
    : mainMenuSceneName;

rewardUI.Open(data, nextScene);

        ActiveProfileStore.Instance?.ForceBroadcast();
        // 6) Bank it (if MSS service exists)
      if (firebaseMSS)
    StartCoroutine(BankRunEarningsAndRefresh(bd.total));
        else
            Debug.LogWarning("[MSSBank] No FirebaseMSSService; showing rewards only, not banking.");
    }

    private float GetPlayerLifePercentOrFallback(float fallback)
    {
        var all = FindObjectsOfType<Damagable>(true);
        Damagable playerDam = null;

        foreach (var d in all)
        {
            if (d != null && d.isActiveAndEnabled && d.CompareTag("Player") && !d.isEnemy)
            {
                playerDam = d;
                break;
            }
        }

        if (playerDam == null)
        {
            foreach (var d in all)
            {
                if (d != null && d.isActiveAndEnabled && !d.isEnemy)
                {
                    playerDam = d;
                    break;
                }
            }
        }

        if (playerDam == null)
        {
            Debug.LogWarning("[MSS] Player Damagable not found; using fallback life% " + fallback);
            return Mathf.Clamp(fallback, 0f, 100f);
        }

        int cur = Mathf.Max(0, playerDam.Health);
        int max = Mathf.Max(1, playerDam.MaxHealth);
        float pct = (cur / (float)max) * 100f;

        Debug.Log($"[MSS] Player HP {cur}/{max} → {pct:0.#}%");
        return Mathf.Clamp(pct, 0f, 100f);
    }

private IEnumerator BankRunEarningsAndRefresh(int mss)
{
    if (mss <= 0) yield break;
    if (!firebaseMSS) yield break;

    string wallet = null;

    var rm = FindFirstObjectByType<RewardManager>();
    if (rm != null && !string.IsNullOrWhiteSpace(rm.connectedWallet))
        wallet = rm.connectedWallet;

    if (string.IsNullOrWhiteSpace(wallet))
    {
        var aps = FindFirstObjectByType<ActiveProfileStore>();
        if (aps != null && aps.CurrentProfile is NewProfileData p && !string.IsNullOrEmpty(p.wallet))
            wallet = p.wallet;
    }

    if (string.IsNullOrWhiteSpace(wallet))
    {
        Debug.LogWarning("[MSSBank] No wallet found; cannot update banked MSS.");
        yield break;
    }

    // 🔹 Actually update Firestore
    yield return firebaseMSS.AddToBank(wallet, mss);

    // 🔹 Now refresh UI with the NEW MSS amount
    ActiveProfileStore.Instance?.ForceBroadcast();
}

}
