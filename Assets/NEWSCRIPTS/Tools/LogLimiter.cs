// Assets/NEWSCRIPTS/Debug/LogLimiter.cs
using UnityEngine;

public class LogLimiter : MonoBehaviour
{
    [SerializeField] int maxLogs = 300;
    int _count;

    void OnEnable()  { Application.logMessageReceived += Handler; }
    void OnDisable() { Application.logMessageReceived -= Handler; }

    void Handler(string condition, string stack, LogType type)
    {
        _count++;
        if (_count == maxLogs)
        {
            Debug.LogWarning("[LogLimiter] Too many logs; disabling further logging.");
            Debug.unityLogger.logEnabled = false;
        }
    }
}
