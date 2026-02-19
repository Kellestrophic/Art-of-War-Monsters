using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Multiplayer/Map Pool", fileName = "MapPool")]
public class MapPoolSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string sceneName;          // must be in Build Settings
        [Range(0.01f, 10f)] public float weight = 1f; // higher = more likely
    }

    public List<Entry> maps = new List<Entry>();

    public string GetRandomScene()
    {
        if (maps == null || maps.Count == 0) return null;
        if (maps.Count == 1) return maps[0].sceneName;

        float total = 0f;
        foreach (var e in maps) total += Mathf.Max(0.0001f, e.weight);

        float r = Random.value * total;
        foreach (var e in maps)
        {
            float w = Mathf.Max(0.0001f, e.weight);
            r -= w;
            if (r <= 0f) return e.sceneName;
        }
        return maps[0].sceneName; // fallback
    }
}
