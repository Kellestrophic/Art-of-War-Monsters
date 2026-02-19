using UnityEngine;
using System.Collections;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    public static void Run(IEnumerator routine)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("CoroutineRunner");
            _instance = go.AddComponent<CoroutineRunner>();
            Object.DontDestroyOnLoad(go);
        }

        _instance.StartCoroutine(routine);
    }
}
