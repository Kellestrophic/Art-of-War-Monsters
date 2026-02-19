// Assets/NEWSCRIPTS/Tools/LogNetworkPrefabs.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

public class LogNetworkPrefabs : MonoBehaviour
{
    [ContextMenu("Log Network Prefabs")]
    void LogIt()
    {
        var nm = NetworkManager.Singleton;
        if (!nm)
        {
            Debug.LogWarning("[LogNetworkPrefabs] No NetworkManager.Singleton in scene.");
            return;
        }

        var prefabsEnumerable = TryGetPrefabEntries(nm);
        if (prefabsEnumerable == null)
        {
            Debug.LogWarning("[LogNetworkPrefabs] Could not find the prefab list on NetworkConfig.");
            return;
        }

        int i = 0;
        foreach (var entry in prefabsEnumerable)
        {
            // The entry type is Unity.Netcode.NetworkPrefab (version dependent).
            var t = entry.GetType();

            var prefabGO                = (GameObject)GetPropOrField(t, entry, "Prefab");
            var sourcePrefabToOverride  = (GameObject)GetPropOrField(t, entry, "SourcePrefabToOverride");
            var overrideModeObj         = GetPropOrField(t, entry, "Override"); // enum in newer NGO

            string name = prefabGO ? prefabGO.name :
                          sourcePrefabToOverride ? sourcePrefabToOverride.name : "(null)";

            string mode = overrideModeObj != null ? overrideModeObj.ToString() :
                          (prefabGO ? "Direct" : "Unknown");

            Debug.Log($"[{++i:00}] {name}  (mode={mode})");
        }

        if (i == 0)
            Debug.Log("[LogNetworkPrefabs] List is empty.");
    }

    // --- Helpers ----------------------------------------------------------------

    // Works with multiple NGO versions by probing likely property names via reflection.
    static IEnumerable TryGetPrefabEntries(NetworkManager nm)
    {
        var cfg = nm.NetworkConfig;
        if (cfg == null) return null;

        // First try new style: NetworkConfig.NetworkPrefabs.(Prefabs/List/NetworkPrefabs)
        var cfgType = cfg.GetType();
        var wrapper = GetPropOrField(cfgType, cfg, "NetworkPrefabs")
                  ?? GetPropOrField(cfgType, cfg, "Prefabs"); // older style

        if (wrapper == null) return null;

        var wType = wrapper.GetType();

        // The wrapper commonly exposes a list named "Prefabs". Fallbacks just in case.
        var inner = GetPropOrField(wType, wrapper, "Prefabs")
                ?? GetPropOrField(wType, wrapper, "NetworkPrefabs")
                ?? GetPropOrField(wType, wrapper, "List");

        return inner as IEnumerable;
    }

    static object GetPropOrField(System.Type t, object obj, string name)
    {
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) return p.GetValue(obj, null);

        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) return f.GetValue(obj);

        return null;
    }
}
