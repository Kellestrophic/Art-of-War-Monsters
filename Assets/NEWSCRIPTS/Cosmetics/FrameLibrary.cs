// Assets/NEWSCRIPTS/Libraries/FrameLibrary.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "AoW/Cosmetics/Frame Library")]
public class FrameLibrary : ScriptableObject
{
    [Serializable]
    public class FrameEntry
    {
        public string key;            // e.g. "bronze_frame"
        public Sprite frameSprite;    // sprite
        public int unlockLevel = 0;   // level required
    }

    // NOTE: this tells Unity that this field used to be called "Entries"
    [FormerlySerializedAs("Entries")]
    [SerializeField] private List<FrameEntry> framesList = new();

    [Header("Optional fallback if key not found")]
    [SerializeField] private Sprite fallbackFrame;

    // --- helpers ---
    public Sprite GetByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return fallbackFrame;
        foreach (var f in framesList)
            if (f != null && f.key == key) return f.frameSprite ?? fallbackFrame;
        return fallbackFrame;
    }

    public Sprite GetForLevel(int level)
    {
        // pick the highest frame whose unlockLevel <= level
        Sprite best = fallbackFrame;
        var bestReq = int.MinValue;
        foreach (var f in framesList)
        {
            if (f == null) continue;
            if (level >= f.unlockLevel && f.unlockLevel >= bestReq)
            {
                bestReq = f.unlockLevel;
                best = f.frameSprite ?? best;
            }
        }
        return best;
    }
// --- compatibility alias for old code ---
public Sprite GetFrame(string key)
{
    return GetByKey(key);
}

    public IEnumerable<string> AllKeys()
    {
        foreach (var f in framesList)
            if (f != null && !string.IsNullOrEmpty(f.key))
                yield return f.key;
    }
    public string GetBestKeyForLevel(int level)
{
    string best = null;
    int bestReq = int.MinValue;

    foreach (var f in framesList)
    {
        if (f == null) continue;
        if (level >= f.unlockLevel && f.unlockLevel >= bestReq)
        {
            bestReq = f.unlockLevel;
            best = f.key;
        }
    }

    return best ?? (framesList.Count > 0 ? framesList[0].key : null);
}

}
