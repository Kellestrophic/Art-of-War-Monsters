using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTitleLibrary", menuName = "Profile/Player Title Library")]
public class PlayerTitleLibrary : ScriptableObject
{
    [System.Serializable]
    public class TitleEntry
    {
        public string key;
        public string displayName;
    }

    public List<TitleEntry> titles = new();

    private Dictionary<string, string> titleDict;

    public void Initialize()
    {
        if (titleDict == null)
        {
            titleDict = new Dictionary<string, string>();
            foreach (var title in titles)
            {
                if (!titleDict.ContainsKey(title.key))
                    titleDict.Add(title.key, title.displayName);
            }
        }
    }

    public string GetDisplayName(string key)
    {
        if (titleDict == null)
            Initialize();

        titleDict.TryGetValue(key, out string name);
        return name ?? key;
    }

    public List<TitleEntry> GetAllTitles()
    {
        return titles;
    }
      [System.Serializable]
    public class Entry
    {
        public string key;
        public string displayName;
    }

    [SerializeField] private List<Entry> entries = new();

    // 🔑 Lookup by key
}
