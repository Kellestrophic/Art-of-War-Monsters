// Assets/NEWSCRIPTS/Libraries/EnemyBossLibrary.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AoW/Libraries/Enemy + Boss Library")]
public class EnemyBossLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;        // e.g. "Cultist" or "MrHappy"
        public string displayName; // Pretty name for UI
    }

    [Header("Regular Enemies")]
    public List<Entry> enemies = new();

    [Header("Bosses")]
    public List<Entry> bosses = new();

    // =================================================================
    //  SAFE LOOKUPS
    // =================================================================
    private Dictionary<string, string> _enemyDisplay;
    private Dictionary<string, string> _bossDisplay;

    private void OnEnable()
    {
        BuildLookups();
    }
public string NormalizeToKey(string raw)
{
    if (string.IsNullOrEmpty(raw))
        return raw;

    // Exact key match
    if (_enemyDisplay.ContainsKey(raw) || _bossDisplay.ContainsKey(raw))
        return raw;

    // Try display name match
    foreach (var e in enemies)
        if (string.Equals(e.displayName, raw, StringComparison.OrdinalIgnoreCase))
            return e.key;

    foreach (var b in bosses)
        if (string.Equals(b.displayName, raw, StringComparison.OrdinalIgnoreCase))
            return b.key;

    return raw; // fallback (won’t crash)
}

    private void BuildLookups()
    {
        _enemyDisplay = new Dictionary<string, string>();
        _bossDisplay  = new Dictionary<string, string>();

        foreach (var e in enemies)
            if (e != null && !string.IsNullOrEmpty(e.key))
                _enemyDisplay[e.key] = string.IsNullOrEmpty(e.displayName) ? e.key : e.displayName;

        foreach (var b in bosses)
            if (b != null && !string.IsNullOrEmpty(b.key))
                _bossDisplay[b.key] = string.IsNullOrEmpty(b.displayName) ? b.key : b.displayName;
    }

    // SAFE GETTERS
    public bool IsEnemy(string key) => _enemyDisplay.ContainsKey(key);
    public bool IsBoss(string key)  => _bossDisplay.ContainsKey(key);

    public string GetEnemyPretty(string key)
        => _enemyDisplay.TryGetValue(key, out var v) ? v : key;

    public string GetBossPretty(string key)
        => _bossDisplay.TryGetValue(key, out var v) ? v : key;

    public IEnumerable<string> AllEnemyKeys()
    {
        foreach (var e in enemies)
            if (e != null && !string.IsNullOrEmpty(e.key))
                yield return e.key;
    }

    public IEnumerable<string> AllBossKeys()
    {
        foreach (var b in bosses)
            if (b != null && !string.IsNullOrEmpty(b.key))
                yield return b.key;
    }
}
