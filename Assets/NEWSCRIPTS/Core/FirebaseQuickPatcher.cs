using UnityEngine;
using System.Collections;

public static class FirebaseQuickPatcher
{
    public static IEnumerator AddInt(string wallet, string fieldName, int value)
    {
        Debug.LogError("❌ FirebaseQuickPatcher is deprecated. Use secure server endpoint instead.");
        yield break;
    }
}
