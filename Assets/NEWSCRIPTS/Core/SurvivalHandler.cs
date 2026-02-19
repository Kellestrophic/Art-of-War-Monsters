using UnityEngine;

public class SurvivalEndHandler : MonoBehaviour
{
    public SurvivalDirector director;

    private bool _handled;

    private void Start()
    {
        var player = FindFirstObjectByType<Damagable>();
        if (player != null)
        {
            player.onDeath.AddListener(OnPlayerDeath);
        }
        else
        {
            Debug.LogError("[SurvivalEndHandler] No Damagable player found!");
        }
    }

    private void OnPlayerDeath()
    {
        if (_handled) return;
        _handled = true;

        float survived = director.survivalTime;

        var profileStore = ActiveProfileStore.Instance;
        if (profileStore != null && profileStore.CurrentProfile != null)
        {
            var p = profileStore.CurrentProfile;

            if (survived > p.longestSurvivalTime)
            {
                p.longestSurvivalTime = survived;

                // Persist to Firebase
                ProfileUploader.UpdatelongestSurvivalTime(p.wallet, survived);

                Debug.Log($"[Survival] 🏆 New longest survival time: {survived:F1}s");
            }
            else
            {
                Debug.Log($"[Survival] Survived {survived:F1}s (record: {p.longestSurvivalTime:F1}s)");
            }
        }
        else
        {
            Debug.LogError("[SurvivalEndHandler] ActiveProfileStore missing or uninitialized!");
        }
    }
}
