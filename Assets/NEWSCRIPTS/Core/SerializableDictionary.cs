using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [Serializable]
    public struct KeyValuePair
    {
        public TKey Key;
        public TValue Value;
    }

    [UnityEngine.SerializeField]
    private List<KeyValuePair> keyValuePairs = new();

    public void OnBeforeSerialize()
    {
        keyValuePairs.Clear();
        foreach (var pair in this)
            keyValuePairs.Add(new KeyValuePair { Key = pair.Key, Value = pair.Value });
    }

    public void OnAfterDeserialize()
    {
        Clear();
        foreach (var pair in keyValuePairs)
            this[pair.Key] = pair.Value;
    }
}
