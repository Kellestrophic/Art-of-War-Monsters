using System.Reflection;
using UnityEngine;

// Adapters so HUD code can always call GetIconSprite / GetFrameSprite
public static class LibraryExtensions
{
    public static Sprite GetIconSprite(this CosmeticLibrary lib, string key)
        => ResolveSprite(lib, key, isIcon:true);

    public static Sprite GetFrameSprite(this FrameLibrary lib, string key)
        => ResolveSprite(lib, key, isIcon:false);

    // ---- helpers -------------------------------------------------------------
    static Sprite ResolveSprite(object lib, string key, bool isIcon)
    {
        if (lib == null || string.IsNullOrEmpty(key)) return null;
        var t = lib.GetType();

        // 1) Try common method names first (if your library already has them)
        string[] methods = isIcon
            ? new[] { "GetIconSprite", "GetSprite", "GetIcon", "FindIcon", "TryGetIconSprite", "TryGetSprite" }
            : new[] { "GetFrameSprite", "GetSprite", "GetFrame", "FindFrame", "TryGetFrameSprite", "TryGetSprite" };

        foreach (var name in methods)
        {
            var mi = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                                 new[] { typeof(string) }, null);
            if (mi != null)
            {
                var r = mi.Invoke(lib, new object[] { key });
                if (r is Sprite s1) return s1;
            }
        }

        // 2) Try common field names (Dictionary/array-of-entries with id+sprite)
        string[] fields = isIcon
            ? new[] { "icons", "iconSprites", "IconSprites", "Icons", "iconMap", "iconDict" }
            : new[] { "frames", "frameSprites", "FrameSprites", "Frames", "frameMap", "frameDict" };

        foreach (var fname in fields)
        {
            var fi = t.GetField(fname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) continue;

            var val = fi.GetValue(lib);

            // Dictionary<string, Sprite>
            if (val is System.Collections.IDictionary dict)
            {
                if (dict.Contains(key)) return dict[key] as Sprite;
            }

            // List/Array of entries with fields: id/key/name + sprite/image
            if (val is System.Collections.IEnumerable enumerable)
            {
                foreach (var entry in enumerable)
                {
                    if (entry == null) continue;
                    var et = entry.GetType();
                    var idF  = et.GetField("id")    ?? et.GetField("key")   ?? et.GetField("name") ?? et.GetField("title");
                    var spF  = et.GetField("sprite")?? et.GetField("image") ?? et.GetField("Icon") ?? et.GetField("Sprite");
                    if (idF != null && spF != null)
                    {
                        var id = idF.GetValue(entry) as string;
                        if (id == key) return spF.GetValue(entry) as Sprite;
                    }
                }
            }
        }

        // 3) Last resort: Resources lookup (keep if you use Resources/Icons or Resources/Frames)
        var resPath = (isIcon ? "Icons/" : "Frames/") + key;
        var res = Resources.Load<Sprite>(resPath);
        return res;
    }
}
