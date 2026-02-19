using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class CosmeticUnlockManager
{
    private static CosmeticLibrary _library;
    private static CosmeticUnlockRules _rules;

    private static CosmeticLibrary Library
    {
        get
        {
            if (_library == null)
                _library = Resources.Load<CosmeticLibrary>("CosmeticLibrary");
            return _library;
        }
    }

    private static CosmeticUnlockRules Rules
    {
        get
        {
            if (_rules == null)
                _rules = Resources.Load<CosmeticUnlockRules>("CosmeticUnlockRules");
            return _rules;
        }
    }

    // ======================================================================
    // MAIN ENTRY — Called by StatsTracker.RecordKill, LevelUp, XP rewarded,
    // or MatchResults.
    // ======================================================================
    public static async Task EvaluateAllAsync(NewProfileData profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("[CosmeticUnlockManager] Profile NULL");
            return;
        }

        if (Rules == null)
        {
            Debug.LogWarning("[CosmeticUnlockManager] No rules found in Resources/CosmeticUnlockRules");
            return;
        }

        bool changed = false;

        foreach (var rule in Rules.rules)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.cosmeticKey))
                continue;

            // Already unlocked?
            if (profile.unlockedCosmetics.Contains(rule.cosmeticKey))
                continue;

            if (RulePasses(rule, profile))
            {
                Debug.Log($"[CosmeticUnlockManager] 🎉 Unlock: {rule.cosmeticKey}");
                profile.unlockedCosmetics.Add(rule.cosmeticKey);
                changed = true;
            }
        }

        // Only save if *any* cosmetic was gained
        if (changed)
        {
            await ProfileUploader.SaveUnlockedCosmetics(profile.wallet, profile.unlockedCosmetics);
            Debug.Log("[CosmeticUnlockManager] Saved unlocked cosmetics ✔");
        }
    }



    // ======================================================================
    // RULE CHECKER
    // ======================================================================
    private static bool RulePasses(CosmeticUnlockRules.Rule rule, NewProfileData p)
    {
        switch (rule.condition)
        {
            // ---------------------------------------------------------
            // Basic stats (like aiWins, multiplayerWins, coins)
            // ---------------------------------------------------------
            case CosmeticUnlockRules.ConditionType.StatAtLeast:
                return CheckBasicStat(rule.statName, p) >= rule.threshold;


            // ---------------------------------------------------------
            // Dictionary stats (enemyKills["Cultist"] >= 10)
            // ---------------------------------------------------------
            case CosmeticUnlockRules.ConditionType.DictStatAtLeast:
                return CheckDictStat(rule.statName, rule.dictKey, p) >= rule.threshold;


            // ---------------------------------------------------------
            // Level requirements (your frames are based on this)
            // ---------------------------------------------------------
            case CosmeticUnlockRules.ConditionType.LevelAtLeast:
                return p.level >= rule.threshold;
        }

        return false;
    }



    // ======================================================================
    // STAT LOOKUPS
    // ======================================================================
    private static int CheckBasicStat(string statName, NewProfileData p)
    {
        switch (statName)
        {
            case "aiWins":             return p.aiWins;
            case "multiplayerWins":    return p.multiplayerWins;
            case "multiplayerLosses":  return p.multiplayerLosses;
            case "mssBanked":              return p.mssBanked;
            case "totalXP":            return p.totalXP;
        }

        Debug.LogWarning($"[CosmeticUnlockManager] Unknown basic stat: {statName}");
        return 0;
    }
public static string GetNextUnlockDescription(
    string cosmeticKey,
    NewProfileData profile
)
{
    if (Rules == null || Rules.rules == null || profile == null)
        return "Locked";

    CosmeticUnlockRules.Rule bestRule = null;

    foreach (var rule in Rules.rules)
    {
        if (rule == null || rule.cosmeticKey != cosmeticKey)
            continue;

        int current = GetCurrentProgress(rule, profile);

        if (current < rule.threshold)
        {
            bestRule = rule;
            break; // FIRST unmet rule is the next goal
        }
    }

    if (bestRule == null)
        return "Unlocked";

    return FormatRule(bestRule, profile);
}

public static string GetUnlockDescription(string cosmeticKey)
{
    if (Rules == null) return "Unlock condition unknown";

    var profile = ActiveProfileStore.Instance?.CurrentProfile;
    if (profile == null) return "Profile not loaded";

    foreach (var rule in Rules.rules)
    {
        if (rule.cosmeticKey != cosmeticKey)
            continue;

        int current = GetCurrentProgress(rule, profile);
        int target = rule.threshold;

        // Pretty name for dict stats (Skeleton Archer instead of SkeletonArcher)
        string pretty = rule.dictKey;
        if (!string.IsNullOrEmpty(pretty))
            pretty = SplitCamelCase(pretty);

        switch (rule.condition)
        {
            case CosmeticUnlockRules.ConditionType.DictStatAtLeast:
                return $"Unlock: {current} / {target} {pretty} kills";

            case CosmeticUnlockRules.ConditionType.StatAtLeast:
                return $"Unlock: {current} / {target} {rule.statName}";

            case CosmeticUnlockRules.ConditionType.LevelAtLeast:
                return $"Unlock at Level {target}";
        }
    }

    return "Unlocked by playing";
}
private static string SplitCamelCase(string input)
{
    if (string.IsNullOrEmpty(input)) return input;
    return System.Text.RegularExpressions.Regex
        .Replace(input, "(\\B[A-Z])", " $1");
}

private static int CheckDictStat(string statName, string key, NewProfileData p)
{
    if (string.IsNullOrEmpty(key))
        return 0;

    // ✅ ALWAYS prefer live stats (already bootstrapped)
    if (StatsTracker.Instance != null)
    {
        if (statName == "enemyKills")
            return StatsTracker.Instance.GetEnemyCount(key);

        if (statName == "bossKills")
            return StatsTracker.Instance.GetBossCount(key);
    }

    // 🔒 Fallback only (editor / edge cases)
    if (p == null) return 0;

    if (statName == "enemyKills" && p.enemyKills != null && p.enemyKills.TryGetValue(key, out int ev))
        return ev;

    if (statName == "bossKills" && p.bossKills != null && p.bossKills.TryGetValue(key, out int bv))
        return bv;

    return 0;
}




    private static int GetCurrentProgress(
    CosmeticUnlockRules.Rule rule,
    NewProfileData p
)
{
    switch (rule.condition)
    {
        case CosmeticUnlockRules.ConditionType.StatAtLeast:
    return CheckBasicStat(rule.statName, p);

case CosmeticUnlockRules.ConditionType.DictStatAtLeast:
    return CheckDictStat(rule.statName, rule.dictKey, p);


        case CosmeticUnlockRules.ConditionType.LevelAtLeast:
            return p.level;
    }
    return 0;
}

private static string FormatRule(
    CosmeticUnlockRules.Rule rule,
    NewProfileData p
)
{
    int current = GetCurrentProgress(rule, p);

    switch (rule.condition)
    {
        case CosmeticUnlockRules.ConditionType.DictStatAtLeast:
            return $"Unlock: {current} / {rule.threshold} {rule.dictKey} kills";

        case CosmeticUnlockRules.ConditionType.StatAtLeast:
            return $"Unlock: {current} / {rule.threshold} {rule.statName}";

        case CosmeticUnlockRules.ConditionType.LevelAtLeast:
            return $"Unlock: Reach Level {rule.threshold}";
    }

    return "Locked";
}

}
