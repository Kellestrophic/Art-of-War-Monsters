using System.Collections.Generic;
using System.Text;

public static class FirestoreHelpers
{
    public static string String(string value)
    {
        if (value == null) value = "";
        return "{\"stringValue\":\"" + Escape(value) + "\"}";
    }

    public static string Int(int value)
    {
        return "{\"integerValue\":\"" + value + "\"}";
    }

    public static string Double(float value)
    {
        return "{\"doubleValue\":" + value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";
    }

    public static string StringArray(List<string> list)
    {
        if (list == null || list.Count == 0)
            return "{\"arrayValue\":{}}";

        var sb = new StringBuilder();
        sb.Append("{\"arrayValue\":{\"values\":[");

        for (int i = 0; i < list.Count; i++)
        {
            sb.Append("{\"stringValue\":\"").Append(Escape(list[i])).Append("\"}");
            if (i < list.Count - 1) sb.Append(",");
        }

        sb.Append("]}}");
        return sb.ToString();
    }

    public static string IntMap(Dictionary<string, int> map)
    {
        if (map == null || map.Count == 0)
            return "{\"mapValue\":{\"fields\":{}}}";

        var sb = new StringBuilder();
        sb.Append("{\"mapValue\":{\"fields\":{");

        bool first = true;
        foreach (var kv in map)
        {
            if (!first) sb.Append(",");
            first = false;

            sb.Append("\"").Append(Escape(kv.Key)).Append("\":");
            sb.Append("{\"integerValue\":\"").Append(kv.Value).Append("\"}");
        }

        sb.Append("}}}");
        return sb.ToString();
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
