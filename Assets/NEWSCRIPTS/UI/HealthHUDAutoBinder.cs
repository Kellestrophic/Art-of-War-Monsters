using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_NETCODE
using Unity.Netcode;
#endif

public class HealthHUDAutoBinder : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueText;

    [Header("Binding")]
    [Tooltip("Poll until the local player's Damagable exists, then bind.")]
    [SerializeField] private bool autoPoll = true;
    [SerializeField] private float pollHz = 5f;

    private Damagable bound;
    private float nextPoll;

    private void OnEnable()
    {
        TryBindNow();
        nextPoll = 0f;
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        if (!autoPoll || bound) return;
        if (Time.unscaledTime >= nextPoll)
        {
            nextPoll = Time.unscaledTime + (1f / Mathf.Max(1f, pollHz));
            TryBindNow();
        }
    }

    private void TryBindNow()
    {
        var candidate = FindLocalPlayerDamagable();
        if (candidate && candidate != bound)
        {
            Unbind();
            bound = candidate;

            // initialize UI immediately
            SafeSet(slider, bound.Health, bound.MaxHealth);
            SafeSet(valueText, bound.Health, bound.MaxHealth);

            // subscribe
            bound.healthChanged.AddListener(OnHealthChanged);
            bound.onDeath.AddListener(OnDeath);
        }
    }

    private void Unbind()
    {
        if (bound != null)
        {
            bound.healthChanged.RemoveListener(OnHealthChanged);
            bound.onDeath.RemoveListener(OnDeath);
            bound = null;
        }
    }

    private static Damagable FindLocalPlayerDamagable()
    {
        // 1) Netcode path (preferred when running)
        #if UNITY_NETCODE
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            var localPO = nm.LocalClient?.PlayerObject;
            if (localPO != null)
            {
                var d = localPO.GetComponentInChildren<Damagable>();
                if (d) return d;
            }

            // Fallback: any owned/local player object
            var allNOs = Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var no in allNOs)
            {
                if (!no) continue;
                if (no.IsOwner || no.IsLocalPlayer)
                {
                    var d = no.GetComponentInChildren<Damagable>();
                    if (d) return d;
                }
            }
        }
        #endif

        // 2) Tag fallback
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go)
        {
            var d = go.GetComponentInChildren<Damagable>();
            if (d) return d;
        }

        // 3) Last resort: first non-enemy Damagable in scene
        var candidates = Object.FindObjectsByType<Damagable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var d in candidates)
            if (d && !d.isEnemy) return d;

        return null;
    }

    private void OnHealthChanged(int hp, int max)
    {
        SafeSet(slider, hp, max);
        SafeSet(valueText, hp, max);
    }

    private void OnDeath()
    {
        // Force 0 on death to avoid any desync
        SafeSet(slider, 0, bound ? bound.MaxHealth : 0);
        SafeSet(valueText, 0, bound ? bound.MaxHealth : 0);
    }

    private static void SafeSet(Slider s, int hp, int max)
    {
        if (!s) return;
        s.maxValue = Mathf.Max(1, max);
        s.value = Mathf.Clamp(hp, 0, s.maxValue);
    }

    private static void SafeSet(TMP_Text t, int hp, int max)
    {
        if (!t) return;
        t.text = $"{Mathf.Clamp(hp, 0, max)}/{Mathf.Max(1, max)}";
    }
}
