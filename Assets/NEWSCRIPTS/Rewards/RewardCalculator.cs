using UnityEngine;
using System.Collections.Generic;

public class RewardCalculator : MonoBehaviour
{
    [System.Serializable]
    public class EnemyReward { public string id; [Min(0)] public int mssPerKill; }
    [System.Serializable]
    public class BossReward  { public string id; [Min(0)] public int mssPerKill; }

    [Header("Enemy Rewards (per kill)")]
    public List<EnemyReward> enemyRewardTable = new()
    {
        new EnemyReward{ id="Cultist",                mssPerKill=2 },
        new EnemyReward{ id="ExplodingCultist",      mssPerKill=3 },
        new EnemyReward{ id="SkeletonWarrior",       mssPerKill=3 },
        new EnemyReward{ id="SkeletonAxeWarrior",    mssPerKill=4 },
        new EnemyReward{ id="SkeletonSpearWarrior",  mssPerKill=4 },
        new EnemyReward{ id="SkeletonArcher",          mssPerKill=5 },
        new EnemyReward{ id="SkeletonNun",           mssPerKill=6 },
        new EnemyReward{ id="Zombie",                mssPerKill=2 },
        new EnemyReward{ id="NaziZombie",            mssPerKill=4 },
        new EnemyReward{ id="Demon",                 mssPerKill=7 },
        new EnemyReward{ id="FlyingDemon",           mssPerKill=6 },
        new EnemyReward{ id="ShadowThrall",          mssPerKill=8 },
        new EnemyReward{ id="BloodThrall",           mssPerKill=8 },
        new EnemyReward{ id="FlyingNun",             mssPerKill=7 },
    };

    [Header("Boss Rewards (per kill)")]
    public List<BossReward> bossRewardTable = new()
    {
        new BossReward{ id="MrSmiley",                    mssPerKill=10000 },
        new BossReward{ id="TheFallen",                  mssPerKill=25 },
        new BossReward{ id="DrFaustus_Mephistopheles",   mssPerKill=30 },
        new BossReward{ id="MortemNobis",                mssPerKill=35 },
        new BossReward{ id="TheSorceress",               mssPerKill=40 },
        new BossReward{ id="TheDeadKing",                mssPerKill=50 },
    };

    private Dictionary<string,int> _enemyMap;
    private Dictionary<string,int> _bossMap;

    [Header("Modifiers")]
    [Tooltip("MSS granted per 1% of life remaining (0..100). 0.8 → up to +80 MSS at 100% HP.")]
    public float mssPerLifePercent = 0.8f;

    [Tooltip("Flat MSS per player level at victory.")]
    public int mssPerPlayerLevel = 5;

    void Awake()
    {
        // Fix typo if you pasted: change 'mssPerKillSome' → 'mssPerKill' above.
        _enemyMap = new();
        foreach (var e in enemyRewardTable)
            if (!string.IsNullOrEmpty(e.id)) _enemyMap[e.id] = e.mssPerKill;

        _bossMap = new();
        foreach (var b in bossRewardTable)
            if (!string.IsNullOrEmpty(b.id)) _bossMap[b.id] = b.mssPerKill;
    }

    public int CalculateReward(
        Dictionary<string, int> enemiesKilled,
        Dictionary<string, int> bossesKilled,
        float lifeRemainingPercent,
        int playerLevel)
    {
        int total = 0;

        if (enemiesKilled != null)
            foreach (var kvp in enemiesKilled)
                if (_enemyMap.TryGetValue(kvp.Key, out var perKill))
                    total += kvp.Value * perKill;

        if (bossesKilled != null)
            foreach (var kvp in bossesKilled)
                if (_bossMap.TryGetValue(kvp.Key, out var perKill))
                    total += kvp.Value * perKill;

        var clampedLife = Mathf.Clamp(lifeRemainingPercent, 0f, 100f);
        total += Mathf.RoundToInt(clampedLife * mssPerLifePercent);
        total += Mathf.Max(0, playerLevel) * Mathf.Max(0, mssPerPlayerLevel);

        Debug.Log($"[MSS Reward] total={total}");
        return Mathf.Max(0, total);
    }
    public struct RewardBreakdown
{
    public int enemies;
    public int bosses;
    public int lifeBonus;
    public int levelBonus;
    public int total;
}

public RewardBreakdown CalculateRewardWithBreakdown(
    Dictionary<string,int> enemiesKilled,
    Dictionary<string,int> bossesKilled,
    float lifeRemainingPercent,
    int playerLevel)
{
    var bd = new RewardBreakdown();

    // reuse your existing maps/logic; mirror your CalculateReward internals:
    if (enemiesKilled != null)
        foreach (var kvp in enemiesKilled)
            if (_enemyMap.TryGetValue(kvp.Key, out var perKill))
                bd.enemies += kvp.Value * perKill;

    if (bossesKilled != null)
        foreach (var kvp in bossesKilled)
            if (_bossMap.TryGetValue(kvp.Key, out var perKill))
                bd.bosses += kvp.Value * perKill;

    var clampedLife = Mathf.Clamp(lifeRemainingPercent, 0f, 100f);
    bd.lifeBonus = Mathf.RoundToInt(clampedLife * mssPerLifePercent);
    bd.levelBonus = Mathf.Max(0, playerLevel) * Mathf.Max(0, mssPerPlayerLevel);

    bd.total = Mathf.Max(0, bd.enemies + bd.bosses + bd.lifeBonus + bd.levelBonus);

    Debug.Log($"[MSS Reward] Enemies={bd.enemies} Bosses={bd.bosses} Life={bd.lifeBonus} Level={bd.levelBonus} → Total={bd.total}");
    return bd;
}
}
