using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmetics/Icon Registry", fileName = "IconRegistry")]
public class IconRegistry : ScriptableObject
{
    public List<IconDef> entries = new();

    private Dictionary<string, Sprite> _map;

    void OnEnable()
    {
        BuildMap();
    }

    void BuildMap()
    {
        _map = new Dictionary<string, Sprite>();

        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.key) || e.sprite == null)
                continue;

            if (!_map.ContainsKey(e.key))
                _map.Add(e.key, e.sprite);
        }
    }

    public Sprite GetIconSprite(string key)
    {
        if (_map == null)
            BuildMap();

        return _map.TryGetValue(key, out var sprite) ? sprite : null;
    }
}
