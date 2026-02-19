using UnityEngine;

public class BinderVisualEvents : MonoBehaviour
{
    private BinderSkeletonMage binder;

    private void Awake()
    {
        binder = GetComponentInParent<BinderSkeletonMage>();
    }

    // 🔥 Animation Event calls THIS
    public void FireOrb()
    {
        if (binder != null)
            binder.FireOrb();
    }
}
