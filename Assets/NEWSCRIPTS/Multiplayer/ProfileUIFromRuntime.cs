using UnityEngine;

public class ProfileUIFromRuntime : MonoBehaviour
{
    private void OnEnable()
    {
        TryRefresh();
    }

    public void TryRefresh()
    {
        if (ProfileUIRenderer.Instance == null)
        {
            Debug.LogWarning("[ProfileUIFromRuntime] No ProfileUIRenderer in scene.");
            return;
        }

        ProfileUIRenderer.Instance.RefreshUI();
    }
}
