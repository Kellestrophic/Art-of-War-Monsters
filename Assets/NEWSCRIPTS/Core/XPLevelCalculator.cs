using UnityEngine;

public static class XPLevelCalculator
{
    private const int BaseXP = 100;
    private const float Growth = 1.40f;
    private const int MaxLevel = 100;  // hard cap prevents 999 jumps

    // Total XP required to REACH the START of a given level
    private static int TotalXPForLevel(int level)
    {
        if (level <= 1) return 0;

        float req = BaseXP;
        float total = 0f;

        for (int i = 2; i <= level; i++)
        {
            total += req;
            req *= Growth;

            // safety measure
            if (total > 10_000_000) break;
        }

        return Mathf.RoundToInt(total);
    }


    public static int GetLevelFromTotalXP(int totalXP)
    {
        int level = 1;

        while (level < MaxLevel)
        {
            int nextLevelXP = TotalXPForLevel(level + 1);
            if (totalXP < nextLevelXP)
                return level;

            level++;
        }

        return MaxLevel;
    }


    public static void GetProgressInLevel(
        int totalXP,
        out int level,
        out int levelStart,
        out int nextLevelStart,
        out int xpInto,
        out int xpNeed,
        out float pct)
    {
        level = GetLevelFromTotalXP(totalXP);

        levelStart = TotalXPForLevel(level);
        nextLevelStart = TotalXPForLevel(level + 1);

        xpInto = Mathf.Clamp(totalXP - levelStart, 0, int.MaxValue);
        xpNeed = Mathf.Max(1, nextLevelStart - levelStart);

        pct = Mathf.Clamp01((float)xpInto / xpNeed);
    }
}
