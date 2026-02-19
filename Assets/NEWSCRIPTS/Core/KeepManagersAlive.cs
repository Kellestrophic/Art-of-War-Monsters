using UnityEngine;

public class KeepManagersAlive : MonoBehaviour
{
    private static bool created = false;

    private void Awake()
    {
        if (created)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        created = true;
    }


}


