using System;

public static class EnemyDeathEvents
{
    // enemyId: e.g. "Cultist", "Skeleton Warrior"
    // isBoss: true for bosses
    public static event Action<string, bool> OnEnemyDied;

    public static void Raise(string enemyId, bool isBoss)
    {
        OnEnemyDied?.Invoke(enemyId, isBoss);
    }
}
