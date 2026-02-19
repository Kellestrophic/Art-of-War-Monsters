using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AoW/Cosmetics/Global Title Library")]
public class GlobalTitleLibrary : ScriptableObject
{
    [System.Serializable]
    public class TitleEntry
    {
        public string key;          // e.g. "ScaredBaby"
        public string displayName;  // e.g. "Scared Baby"
    }

    [SerializeField] private List<TitleEntry> titles = new();

    private Dictionary<string, string> _map;
    private void OnEnable()
    {
        if (_map == null)
        {
            _map = new Dictionary<string, string>();
            foreach (var t in titles)
            {
                if (t != null && !string.IsNullOrEmpty(t.key))
                    _map[t.key] = string.IsNullOrEmpty(t.displayName) ? t.key : t.displayName;
            }
        }
    }

    public string GetDisplayName(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (_map == null) OnEnable();
        return _map != null && _map.TryGetValue(key, out var name) ? name : key;
    }

    // Optional helpers
    public bool HasKey(string key) => _map != null && _map.ContainsKey(key);
    public IEnumerable<string> AllKeys()
    {
        if (_map == null) OnEnable();
        return _map.Keys;
    }
}
