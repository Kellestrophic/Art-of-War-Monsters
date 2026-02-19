using UnityEngine;

public class LevelFlowManager : MonoBehaviour
{
    public static LevelFlowManager Instance { get; private set; }

    [Header("Flow State")]
    public LevelFlowState currentState = LevelFlowState.NormalLevel;

    [Header("Boss Flows")]
    public BossFlowDefinition[] bossFlows;

    [Header("Defaults")]
    public string mainMenuSceneName = "Main_menu";

    private BossFlowDefinition activeFlow;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetActiveLevel(string levelId)
    {
        foreach (var flow in bossFlows)
        {
            if (flow.levelId == levelId)
            {
                activeFlow = flow;
                currentState = LevelFlowState.NormalLevel;
                Debug.Log($"[Flow] Active level set → {levelId}");
                return;
            }
        }

        Debug.LogError($"[Flow] No BossFlowDefinition found for levelId: {levelId}");
    }

    public string GetNextSceneAfterRewards()
    {
        if (activeFlow == null)
        {
            Debug.LogError("[Flow] No active flow set!");
            return mainMenuSceneName;
        }

        switch (currentState)
        {
            case LevelFlowState.NormalLevel:
                currentState = LevelFlowState.PreBoss;
                return activeFlow.preBossCinematic;

            case LevelFlowState.BossFight:
                currentState = LevelFlowState.PostBoss;
                return activeFlow.postBossCinematic;

            case LevelFlowState.PostBoss:
                currentState = LevelFlowState.NormalLevel;
                return mainMenuSceneName;

            default:
                return mainMenuSceneName;
        }
    }

    // ============================================================
    // 🔑 THIS IS THE FIX
    // ============================================================
    public void NotifyBossFightStarted()
    {
        currentState = LevelFlowState.BossFight;

        var run = FindFirstObjectByType<StatsTrackerRunAdapter>();
        if (run != null)
        {
            run.CaptureStart();
            Debug.Log("[StatsRunAdapter] CaptureStart → BossFight");
        }
        else
        {
            Debug.LogWarning("[StatsRunAdapter] No adapter found at BossFight start");
        }
    }
}
