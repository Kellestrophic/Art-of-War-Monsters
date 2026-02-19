using SimpleJSON;
using System.Collections.Generic;

public static class FirestoreJsonHelpers
{
    public static Dictionary<string, int> ParseIntMap(string json, string field)
    {
        var dict = new Dictionary<string, int>();
        var root = JSON.Parse(json);

        var fields = root?["fields"]?[field]?["mapValue"]?["fields"];
        if (fields == null)
            return dict;

        foreach (var kv in fields)
            dict[kv.Key] = kv.Value["integerValue"].AsInt;

        return dict;
    }
}
