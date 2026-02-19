using UnityEngine;

public class BossFightBootstrap : MonoBehaviour
{
 private void Start()
{
    LevelFlowManager.Instance.NotifyBossFightStarted();

    var run = FindFirstObjectByType<StatsTrackerRunAdapter>();
    run?.CaptureStart();

    Debug.Log("[Flow] BossFight state entered");
}

    
}
