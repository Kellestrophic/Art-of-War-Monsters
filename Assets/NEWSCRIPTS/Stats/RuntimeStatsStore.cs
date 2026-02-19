using System.Collections.Generic;
using UnityEngine;
using System.Linq;


/// <summary>
/// SINGLE SOURCE OF TRUTH for XP + kills at runtime.
/// Loads once from profile. Mutates only here. Saves via StatsAutosaver.
/// </summary>
public class RuntimeStatsStore : MonoBehaviour
{
    public static RuntimeStatsStore Instance { get; private set; }

    public bool IsBootstrapped { get; private set; }
    public bool IsDirty { get; private set; }

    private NewProfileData profile;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================================================
    // BOOTSTRAP
    // ============================================================
   public void BootstrapFromProfile(NewProfileData loaded)
{
    profile = loaded;

    // 🔒 Ensure dictionaries exist (do NOT overwrite)
    profile.enemyKills ??= new Dictionary<string, int>();
    profile.bossKills  ??= new Dictionary<string, int>();

    var lib = FindFirstObjectByType<EnemyBossLibrary>();
    if (lib != null)
    {
        foreach (var enemyKey in lib.AllEnemyKeys())
            profile.enemyKills.TryAdd(enemyKey, profile.enemyKills.GetValueOrDefault(enemyKey));

        foreach (var bossKey in lib.AllBossKeys())
            profile.bossKills.TryAdd(bossKey, profile.bossKills.GetValueOrDefault(bossKey));
    }

    IsBootstrapped = true;
    IsDirty = false;

    Debug.Log(
        $"[RuntimeStatsStore] ✅ Bootstrapped. totalXP={profile.totalXP}, " +
        $"totalKills={profile.enemyKills.Values.Sum()}"
    );
}


    // ============================================================
    // READ ACCESS
    // ============================================================
    public NewProfileData GetProfile() => profile;

    public IReadOnlyDictionary<string, int> GetEnemyKills() => profile.enemyKills;
    public IReadOnlyDictionary<string, int> GetBossKills()  => profile.bossKills;

    // ============================================================
    // MUTATIONS (ONLY HERE)
    // ============================================================
    public void AddXP(int amount, string source = "")
    {
        if (!IsBootstrapped || profile == null || amount <= 0) return;

     profile.totalXP += amount;
profile.level = XPLevelCalculator.GetLevelFromTotalXP(profile.totalXP);
IsDirty = true;


        if (!string.IsNullOrEmpty(source))
            Debug.Log($"[RuntimeStatsStore] +{amount} XP from {source}. totalXP={profile.totalXP}");
    }

    public void RecordEnemyKill(string enemyKey)
    {
        if (!IsBootstrapped || profile == null || string.IsNullOrEmpty(enemyKey)) return;

        if (!profile.enemyKills.ContainsKey(enemyKey))
            profile.enemyKills[enemyKey] = 0;

        profile.enemyKills[enemyKey]++;
      IsDirty = true;
StatsAutosaver.Instance?.MarkMutated();


        Debug.Log($"[RuntimeStatsStore] ☠ enemyKills[{enemyKey}]={profile.enemyKills[enemyKey]}");
    }

    public void RecordBossKill(string bossKey)
    {
        if (!IsBootstrapped || profile == null || string.IsNullOrEmpty(bossKey)) return;

        if (!profile.bossKills.ContainsKey(bossKey))
            profile.bossKills[bossKey] = 0;

        profile.bossKills[bossKey]++;
        IsDirty = true;

        Debug.Log($"[RuntimeStatsStore] 👑 bossKills[{bossKey}]={profile.bossKills[bossKey]}");
    }

    public void RecordMatchResult(bool aiWin, bool mpWin, bool mpLoss)
    {
        if (!IsBootstrapped || profile == null) return;

        if (aiWin)  profile.aiWins++;
        if (mpWin)  profile.multiplayerWins++;
        if (mpLoss) profile.multiplayerLosses++;

        IsDirty = true;

        Debug.Log(
            $"[RuntimeStatsStore] 🏁 MatchResult ai={aiWin} mpWin={mpWin} mpLoss={mpLoss}"
        );
    }


    public void ClearDirty()
    {
        IsDirty = false;
    }

    // ============================================================
    // UTIL
    // ============================================================
    private int Sum(Dictionary<string, int> dict)
    {
        int total = 0;
        foreach (var v in dict.Values)
            total += v;
        return total;
    }
}
