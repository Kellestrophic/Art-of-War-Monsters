using UnityEngine;

public class LevelInitializer : MonoBehaviour
{
    [SerializeField] private string levelId;

    private void Start()
    {
        if (LevelFlowManager.Instance != null)
            LevelFlowManager.Instance.SetActiveLevel(levelId);
    }
}
