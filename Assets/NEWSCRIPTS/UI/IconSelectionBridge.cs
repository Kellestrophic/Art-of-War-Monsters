using UnityEngine;

public class IconSelectionBridge : MonoBehaviour
{
    [SerializeField] private IconGridBuilder grid;

    private void OnEnable()
    {
        if (grid != null)
            grid.RefreshGrid();
    }
}
