using System.Collections.Generic;

public static class StatsChecksum
{
    public static long Compute(NewProfileData p)
    {
        long sum = 0;

        // Enemy kills
        if (p.enemyKills != null)
            foreach (var v in p.enemyKills.Values)
                sum += v;

        // Boss kills
        if (p.bossKills != null)
            foreach (var v in p.bossKills.Values)
                sum += v * 10; // weight bosses higher

        sum += p.aiWins * 20;
        sum += p.multiplayerWins * 30;
        sum += p.multiplayerLosses * 5;
        sum += p.totalXP;

        return sum;
    }
}
