using UnityEngine;

[DefaultExecutionOrder(10000)]
public class AIVersusResultBridge : MonoBehaviour
{
    [Header("Drag Damagable on AI and Player (or leave blank to auto-find)")]
    [SerializeField] private Damagable aiDamagable;
    [SerializeField] private Damagable playerDamagable;

    private bool sentWin, sentLoss;

    private void Awake()
    {
        TryAutoWire();

        // Safety: if AI accidentally has EnemyKillReporter, disable it so it won't write enemyKills.AI_Bot
        if (aiDamagable)
        {
            var wrongReporter = aiDamagable.GetComponent<EnemyKillReporter>();
            if (wrongReporter) wrongReporter.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (aiDamagable)     aiDamagable.onDeath.AddListener(OnAIDied);
        if (playerDamagable) playerDamagable.onDeath.AddListener(OnPlayerDied);
    }

    private void OnDisable()
    {
        if (aiDamagable)     aiDamagable.onDeath.RemoveListener(OnAIDied);
        if (playerDamagable) playerDamagable.onDeath.RemoveListener(OnPlayerDied);
    }

   private void OnAIDied()
{
    if (sentWin) return;
    sentWin = true;

    var store = RuntimeStatsStore.Instance;
    if (store == null)
    {
        Debug.LogError("[AIVersusResultBridge] No RuntimeStatsStore in scene.");
        return;
    }

    Debug.Log("[AIVersusResultBridge] AI died → player wins AI match");

    store.RecordMatchResult(
        aiWin: true,   // player beat AI
        mpWin: false,
        mpLoss: false
    );
}


    private void OnPlayerDied()
{
    if (sentLoss) return;
    sentLoss = true;

    var store = RuntimeStatsStore.Instance;
    if (store == null)
    {
        Debug.LogError("[AIVersusResultBridge] No RuntimeStatsStore in scene.");
        return;
    }

    Debug.Log("[AIVersusResultBridge] Player died → player loses vs AI");

    store.RecordMatchResult(
        aiWin: false,
        mpWin: false,
        mpLoss: false
    );
}


    private void TryAutoWire()
    {
        // Player via tag first
        if (!playerDamagable)
        {
            var player = GameObject.FindWithTag("Player");
            if (player) playerDamagable = player.GetComponent<Damagable>();
        }

        // AI via "AI" tag, optional AIOpponentMarker, or fallback to "any Damagable that's not the player"
        if (!aiDamagable)
        {
            GameObject ai = GameObject.FindWithTag("AI");

            if (!ai)
            {
                var marker = FindFirstObjectByType<AIOpponentMarker>();
                if (marker) ai = marker.gameObject;
            }

            if (!ai)
            {
                Damagable[] all;
    #if UNITY_2023_1_OR_NEWER
                all = FindObjectsByType<Damagable>(FindObjectsSortMode.None);
    #else
                all = FindObjectsOfType<Damagable>();
    #endif
                foreach (var d in all)
                {
                    if (playerDamagable && d == playerDamagable) continue;
                    ai = d.gameObject;
                    break;
                }
            }

            if (ai) aiDamagable = ai.GetComponent<Damagable>();
        }
    }
}

public class AIOpponentMarker : MonoBehaviour {}
