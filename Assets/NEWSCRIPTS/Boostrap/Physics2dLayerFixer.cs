using UnityEngine;

/// Ensures the "Projectile" layer collides with all needed target layers at runtime.
public class Physics2DLayerFixer : MonoBehaviour
{
    [SerializeField] private string projectileLayer = "Projectile";
    [SerializeField] private string[] ensureCollidesWith = 
        { "Default", "Ground", "Enemy", "Enemies", "Hostile", "Player" };

    private void Awake()
    {
        int proj = LayerMask.NameToLayer(projectileLayer);
        if (proj < 0)
        {
            Debug.LogWarning("[Physics2DLayerFixer] Projectile layer not found: " + projectileLayer);
            return;
        }

            // Start with whatever the project currently has
        int mask = Physics2D.GetLayerCollisionMask(proj);

        foreach (var n in ensureCollidesWith)
        {
            int l = LayerMask.NameToLayer(n);
            if (l >= 0) mask |= (1 << l);
        }

        Physics2D.SetLayerCollisionMask(proj, mask);
    }
}
