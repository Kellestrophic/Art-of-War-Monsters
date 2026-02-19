#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text;

public static class CosmeticStoreExporter
{
    [MenuItem("Tools/Export STORE_ITEMS (CosmeticLibrary)")]
    public static void Export()
    {
        CosmeticLibrary lib = Resources.Load<CosmeticLibrary>("CosmeticLibrary");
        if (lib == null)
        {
            Debug.LogError("❌ CosmeticLibrary not found in Resources/");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("const STORE_ITEMS = {");

        foreach (var item in lib.items)
        {
            if (item == null) continue;
            if (!item.isPremium) continue;

            sb.AppendLine($"  \"{item.id}\": {{");
            sb.AppendLine($"    priceUSD: {item.priceUSD},");
            sb.AppendLine($"    type: \"{item.type.ToString().ToLower()}\"");
            sb.AppendLine("  },");
        }

        sb.AppendLine("};");

        Debug.Log(
            "===== COPY THIS INTO server.js =====\n\n" +
            sb.ToString()
        );
    }
}
#endif
