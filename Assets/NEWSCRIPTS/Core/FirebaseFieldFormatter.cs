using System.Collections.Generic;

public static class FirebaseFieldFormatter
{
    public static Dictionary<string, object> ToFirestoreMap(Dictionary<string, int> input)
    {
        var map = new Dictionary<string, object>();
        foreach (var kvp in input)
        {
            map[kvp.Key] = new Dictionary<string, object> { { "integerValue", kvp.Value } };
        }

        return new Dictionary<string, object>
        {
            { "mapValue", new Dictionary<string, object> { { "fields", map } } }
        };
    }
}
