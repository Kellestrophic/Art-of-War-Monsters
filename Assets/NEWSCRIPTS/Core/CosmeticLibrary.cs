// Assets/NEWSCRIPTS/Cosmetics/CosmeticLibrary.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization; // ← lets Unity remap old serialized field names

public enum CosmeticType
{
    Title,
    Icon,
    Frame,
    // add more if needed
}

[CreateAssetMenu(menuName = "AoW/Cosmetics/Cosmetic Library")]
public class CosmeticLibrary : ScriptableObject
{
    [System.Serializable]
    public class CosmeticItem
    {
        [Header("Premium")]
public bool isPremium = false;
public float priceUSD = 0.99f;
[Header("Availability")]
public bool unlockEnabled = true;      // can be earned via gameplay
public bool purchaseEnabled = true;    // can be bought (premium)

        // New API (+ back-compat remaps)
        [FormerlySerializedAs("itemType")]
        [FormerlySerializedAs("typeName")]
        public CosmeticType type;       // e.g., Title, Icon, Frame

        [FormerlySerializedAs("key")]
        [FormerlySerializedAs("idString")]
        public string id;               // stable identifier ("0", "dracula_icon", etc.)

        [FormerlySerializedAs("name")]
        [FormerlySerializedAs("display")]
        public string displayName;      // user-facing label

        [FormerlySerializedAs("icon")]
        [FormerlySerializedAs("spriteRef")]
        public Sprite sprite;           // optional (for icons/frames)

        // ─── Back-compat shims ──────────────────────────────
        public string key => id;  // old code expected .key

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(displayName)) return displayName;
            return string.IsNullOrEmpty(id) ? "Unnamed" : id;
        }
    }

    [SerializeField] public List<CosmeticItem> items = new();


    private Dictionary<string, CosmeticItem> _byKey;

    // ─── Modern API (safe for WebGL/IL2CPP) ─────────────────
    public List<CosmeticItem> GetItemsByType(CosmeticType t)
    {
        var result = new List<CosmeticItem>();
        if (items == null || items.Count == 0) return result;

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it == null) continue;
            if (it.type == t) result.Add(it);
        }
        return result;
    }

    // String overload (old code sometimes passed "Icon" etc.)
    public List<CosmeticItem> GetItemsByType(string typeName)
    {
        var result = new List<CosmeticItem>();
        if (string.IsNullOrEmpty(typeName)) return result;

        CosmeticType parsed;
        if (!TryParseType(typeName, out parsed)) return result;
        return GetItemsByType(parsed);
    }

    private static bool TryParseType(string s, out CosmeticType t)
    {
        var cmp = s.ToLowerInvariant();
        if (cmp == "title") { t = CosmeticType.Title; return true; }
        if (cmp == "icon")  { t = CosmeticType.Icon;  return true; }
        if (cmp == "frame") { t = CosmeticType.Frame; return true; }
        t = default;
        return false;
    }

    public CosmeticItem GetItem(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        EnsureLookup();
        CosmeticItem it;
        if (_byKey != null && _byKey.TryGetValue(key, out it)) return it;

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var c = items[i];
                if (c != null && c.id == key) return c;
            }
        }
        return null;
    }

    public bool TryGetItem(string key, out CosmeticItem item)
    {
        item = GetItem(key);
        return item != null;
    }

    // ─── Back-compat shims ──────────────────────────────
    public void Initialize()
    {
        EnsureLookup();
    }

    public string GetDisplayName(string key)
    {
        var it = GetItem(key);
        if (it != null) return it.GetDisplayName();
        return string.IsNullOrEmpty(key) ? "Unnamed" : key;
    }

    // (Optional but useful) if older UI fetched sprite directly from library
    public Sprite GetSprite(string key)
{
    var it = GetItem(key);
    return (it != null) ? it.sprite : null;
}


    // ─── Internal ──────────────────────────────────────
    private void EnsureLookup()
    {
        if (_byKey == null) _byKey = new Dictionary<string, CosmeticItem>();
        _byKey.Clear();

        if (items == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it == null) continue;
            // last one wins for duplicate ids
            _byKey[it.id] = it;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (items != null)
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (items[i] == null) items.RemoveAt(i);
        }
        EnsureLookup();
    }
#endif
}
