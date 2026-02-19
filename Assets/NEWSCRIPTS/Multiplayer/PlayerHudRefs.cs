// Assets/NEWSCRIPTS/UI/PlayerHudRefs.cs
using UnityEngine;

public class PlayerHudRefs : MonoBehaviour
{
    [Header("Gameplay sources (NOT UI)")]
    [SerializeField] private MonoBehaviour healthSource;   // Health.cs (not UI)
    [SerializeField] private MonoBehaviour strikeSource;   // Strike/Energy.cs (not UI)
    [SerializeField] private MonoBehaviour levelSource;    // optional fallback

    [Header("Networked identity")]
    [SerializeField] private PlayerIdentityNet identity;

    [Header("Lookups (IDs -> Sprites)")]
    [SerializeField] private IconRegistry iconRegistry;    // IconDef registry
    [SerializeField] private IconRegistry frameRegistry;   // OPTIONAL: FrameDef registry

    [Header("By-level fallback for frames (optional)")]
    [SerializeField] private FrameLibrary frameLibrary;    // level → frame (used if frameRegistry has no match)

    [Header("Safe defaults if identity/profile not ready")]
    [SerializeField] private string defaultPlayerName  = "Player";
    [SerializeField] private string defaultPlayerTitle = "Adventurer";
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Sprite defaultFrame;

    // ---- HUD-accessible data ----
    public float CurrentHP     => TryGetFloat(healthSource, "currentHealth","CurrentHealth","HP") ?? 100f;
    public float MaxHP         => TryGetFloat(healthSource, "maxHealth","MaxHealth","MaxHP")     ?? 100f;
    public float CurrentStrike => TryGetFloat(strikeSource, "current","Current","Value")         ?? 0f;
    public float MaxStrike     => TryGetFloat(strikeSource, "max","Max","MaxValue")              ?? 25f;

    public int Level
    {
        get
        {
            if (identity) return identity.LevelValue;
            return (int)(TryGetFloat(levelSource, "level","Level","currentLevel") ?? 1);
        }
    }

    public string PlayerName
        => !string.IsNullOrWhiteSpace(_nameOverride)
            ? _nameOverride
            : (identity && !string.IsNullOrEmpty(identity.DisplayNameString) ? identity.DisplayNameString : defaultPlayerName);

    public string PlayerTitle
        => !string.IsNullOrWhiteSpace(_titleOverride)
            ? _titleOverride
            : (identity ? identity.TitleString : defaultPlayerTitle);

    public Sprite PlayerIcon
{
    get
    {
        if (_iconOverride) return _iconOverride;

        if (identity && iconRegistry)
        {
            var s = iconRegistry.GetIconSprite(identity.IconIdString);
            if (s) return s;
        }

        return defaultIcon;
    }
}


    public Sprite DefaultFrame
{
    get
    {
        if (_frameOverride) return _frameOverride;

        if (identity && frameRegistry)
        {
            var s = frameRegistry.GetIconSprite(identity.FrameIdString);
            if (s) return s;
        }

        if (frameLibrary)
        {
            var f = frameLibrary.GetForLevel(Level);
            if (f) return f;
        }

        return defaultFrame;
    }
}


    // Optional overrides (e.g., temporary cosmetics before identity arrives)
    public void SetCosmetics(Sprite icon, Sprite frame, string name = null, string title = null)
    {
        if (icon)  _iconOverride  = icon;
        if (frame) _frameOverride = frame;
        if (!string.IsNullOrWhiteSpace(name))  _nameOverride  = name;
        if (!string.IsNullOrWhiteSpace(title)) _titleOverride = title;
    }

    // Convenience overload (uses this.frameLibrary)
    public void SetFrameByLevel(int level) => SetFrameByLevel(level, frameLibrary);

    public void SetFrameByLevel(int level, FrameLibrary lib)
    {
        if (!lib) return;
        var f = lib.GetForLevel(level);
        if (f) _frameOverride = f;
    }

    // ---- internals ----
    private Sprite _iconOverride, _frameOverride;
    private string _nameOverride, _titleOverride;

    private float? TryGetFloat(object obj, params string[] names)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null)
            {
                if (f.FieldType == typeof(int))   return (int)f.GetValue(obj);
                if (f.FieldType == typeof(float)) return (float)f.GetValue(obj);
            }
            var p = t.GetProperty(n);
            if (p != null)
            {
                if (p.PropertyType == typeof(int))   return (int)p.GetValue(obj);
                if (p.PropertyType == typeof(float)) return (float)p.GetValue(obj);
            }
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!identity) identity = GetComponent<PlayerIdentityNet>();
    }
#endif
}
