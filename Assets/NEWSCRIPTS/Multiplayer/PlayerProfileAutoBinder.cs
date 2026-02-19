using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Optional: define this tiny interface in your profile code for a perfect, no-reflection hookup.
public interface IProfileSource
{
    string DisplayName { get; }
    string Title { get; }
    int    Level { get; }
    Sprite Icon { get; }
}

public class PlayerProfileAutoBinder : NetworkBehaviour
{
    [Header("Optional: drag your profile component here (recommended)")]
    [SerializeField] private MonoBehaviour profileComponent; // any component implementing IProfileSource or with fields/properties

    [Header("Cosmetics")]
    [SerializeField] private FrameLibrary frameLibrary; // choose your level->frame rules

    private PlayerHudRefs _refs;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; // only the local player's HUD should use local profile
        _refs = GetComponent<PlayerHudRefs>();
        if (_refs == null)
        {
            Debug.LogWarning("[PlayerProfileAutoBinder] Missing PlayerHudRefs on player prefab.");
            return;
        }
        StartCoroutine(BindRoutine());
    }

    private IEnumerator BindRoutine()
    {
        // Try direct component first; if null, try auto-find
        if (profileComponent == null)
            profileComponent = TryAutoFindProfileComponent();

        // Poll until we can read valid data
        var wait = new WaitForSeconds(0.2f);
        for (int i = 0; i < 200; i++) // ~40s max; bail out to avoid infinite loops
        {
            if (TryReadProfile(profileComponent, out string name, out string title, out int level, out Sprite icon))
            {
                // Push to PlayerHudRefs
                _refs.SetCosmetics(icon, null, name, title);
                _refs.SetFrameByLevel(level, frameLibrary);
                Debug.Log($"[PlayerProfileAutoBinder] Applied profile: {name} (L{level})");
                yield break;
            }

            // If we still don't have a component, try finding again
            if (profileComponent == null)
                profileComponent = TryAutoFindProfileComponent();

            yield return wait;
        }

        Debug.LogWarning("[PlayerProfileAutoBinder] Profile not found in time; using defaults.");
    }

    // --- Try to read profile via interface or reflection ---
    private bool TryReadProfile(MonoBehaviour comp, out string name, out string title, out int level, out Sprite icon)
    {
        name = null; title = null; level = 1; icon = null;
        if (comp == null) return false;

        // Interface path (best)
        if (comp is IProfileSource ps)
        {
            name  = ps.DisplayName;
            title = ps.Title;
            level = Mathf.Max(1, ps.Level);
            icon  = ps.Icon;
            return true;
        }

        // Reflection path (no code changes in your existing profile script)
        object o = comp;
        var t = o.GetType();

        name  = TryGetString(o, t, "DisplayName", "PlayerName", "Name", "username", "profileName");
        title = TryGetString(o, t, "Title", "Role", "Class", "playerTitle");
        var lvl = TryGetInt(o, t, "level", "Level", "currentLevel", "playerLevel");
        if (lvl.HasValue) level = Mathf.Max(1, lvl.Value);

        icon = TryGetSprite(o, t, "Icon", "Avatar", "Portrait", "ProfileIcon");

        return !string.IsNullOrEmpty(name) || icon != null; // accept partial; frame-by-level will still work
    }

    // --- Auto-find a likely profile component in the scene ---
    private MonoBehaviour TryAutoFindProfileComponent()
    {
        // Fast scan: look for obvious names on active behaviours
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (!mb || !mb.isActiveAndEnabled) continue;
            var n = mb.GetType().Name;
            if (n.Contains("Profile", System.StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Wallet",  System.StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Account", System.StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Avatar",  System.StringComparison.OrdinalIgnoreCase))
            {
                // sanity check: can we read at least one field?
                if (TryReadProfile(mb, out _, out _, out _, out _))
                    return mb;
            }
        }
        return null;
    }

    // ---- reflection helpers ----
    private string TryGetString(object o, System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && f.FieldType == typeof(string)) return (string)f.GetValue(o);
            var p = t.GetProperty(n);
            if (p != null && p.PropertyType == typeof(string)) return (string)p.GetValue(o);
        }
        return null;
    }
    private int? TryGetInt(object o, System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(o);
            var p = t.GetProperty(n);
            if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(o);
        }
        return null;
    }
    private Sprite TryGetSprite(object o, System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType)) return (Sprite)f.GetValue(o);
            var p = t.GetProperty(n);
            if (p != null && typeof(Sprite).IsAssignableFrom(p.PropertyType)) return (Sprite)p.GetValue(o);
        }
        return null;
    }
}
