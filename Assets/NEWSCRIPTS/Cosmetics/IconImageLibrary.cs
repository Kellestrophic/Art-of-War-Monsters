using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconImageLibrary", menuName = "AoW/Cosmetics/Icon Image Library")]
public class IconImageLibrary : ScriptableObject
{
    [System.Serializable]
    public class IconEntry
    {
        public string key;
        public Sprite sprite;
    }

    // Keep this PUBLIC for older scripts that iterate iconLibrary.icons
    public List<IconEntry> icons = new();

    [SerializeField] private Sprite fallbackIcon;

    private Dictionary<string, Sprite> iconDict;

    private void OnEnable() { BuildDict(); }

    private void BuildDict()
    {
        if (iconDict == null) iconDict = new Dictionary<string, Sprite>();
        else iconDict.Clear();

        foreach (var e in icons)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) continue;
            if (!iconDict.ContainsKey(e.key)) iconDict.Add(e.key, e.sprite);
        }
    }

    // ---------- Backward-compat wrappers ----------
    // Your other scripts call Initialize(); keep it as a no-op that (re)builds the dict
    public void Initialize() => BuildDict();

    // Your other scripts call GetIcon(); keep it and route to GetSprite()
    public Sprite GetIcon(string key) => GetSprite(key);
    // ------------------------------------------------

    // Preferred API going forward
    public Sprite GetSprite(string key)
    {
        if (iconDict == null || iconDict.Count != icons.Count) BuildDict();
        if (string.IsNullOrEmpty(key)) return fallbackIcon;
        return iconDict.TryGetValue(key, out var sprite) ? sprite : fallbackIcon;
    }

    public IEnumerable<string> AllKeys()
    {
        foreach (var e in icons)
            if (e != null && !string.IsNullOrEmpty(e.key))
                yield return e.key;
    }

    public Sprite GetFallback() => fallbackIcon;
}
