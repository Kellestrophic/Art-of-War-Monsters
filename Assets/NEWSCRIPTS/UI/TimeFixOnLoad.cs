using UnityEngine;
using UnityEngine.SceneManagement;   // ⭐ REQUIRED for SceneManager

public class TimeFixOnLoad : MonoBehaviour
{
    private void Awake()
    {
        // If the scene loads with time frozen, fix it
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[TimeFixOnLoad] Scene started with timeScale = 0. Restoring to 1.");
            Time.timeScale = 1f;
        }
    }
}
