using UnityEngine;

/// <summary>
/// Compatibility bridge so old calls like
///   FindFirstObjectByType<StrikeBar>()?.AddPoints(x)
/// keep working. Internally routes to StrikeBarHUD bars.
/// </summary>
public class StrikeBar : MonoBehaviour
{
    // Old API still works:
    public void AddPoint() => AddPoints(1);

    public void AddPoints(int amount)
    {
        // Default behavior (matches your old flow): award the PLAYER bar.
        // This keeps existing code compiling and functional right away.
        StrikeBarHUD.GetPlayerBar()?.AddPoints(amount);
    }

    // Extra helpers if you ever want to call them:
    public void AddPointsToPlayer(int amount) => StrikeBarHUD.GetPlayerBar()?.AddPoints(amount);
    public void AddPointsToClosestEnemy(Vector3 fromPos, int amount) =>
        StrikeBarHUD.GetClosestEnemyBar(fromPos)?.AddPoints(amount);

    public void ActivatePlayerStrike() => StrikeBarHUD.GetPlayerBar()?.ActivateStrike();
    public void ActivateClosestEnemyStrike(Vector3 fromPos) =>
        StrikeBarHUD.GetClosestEnemyBar(fromPos)?.ActivateStrike();
}
