using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HUDHpBinder : MonoBehaviour
{
    public enum TargetSide { Local, Opponent }

    [Header("Who does this bar represent?")]
    public TargetSide target = TargetSide.Local;

    [Header("UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText; // optional

    private DamageableNet boundHp;
    private bool subscribed;

    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // Wait until NetworkManager exists and clients are spawned
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        // Find the correct player's DamageableNet
        while (boundHp == null)
        {
            var all = FindObjectsByType<DamageableNet>(FindObjectsSortMode.None);
            ulong localId = NetworkManager.Singleton.LocalClientId;
            foreach (var hp in all)
            {
                var no = hp.GetComponent<NetworkObject>();
                if (!no || !no.IsSpawned) continue;

                bool isLocal = no.IsOwner; // owner = local player instance
                if ((target == TargetSide.Local && isLocal) ||
                    (target == TargetSide.Opponent && !isLocal))
                {
                    boundHp = hp;
                    break;
                }
            }
            if (boundHp == null) yield return null;
        }

        // Init UI range/value
        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = boundHp.MaxHP;
            hpSlider.value = boundHp.Hp.Value;
        }
        if (hpText != null)
            hpText.text = $"{boundHp.Hp.Value}/{boundHp.MaxHP}";

        // Subscribe to changes (non-owners will receive updates too)
        boundHp.Hp.OnValueChanged += OnHpChanged;
        subscribed = true;
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        if (hpSlider != null) hpSlider.value = newValue;
        if (hpText != null) hpText.text = $"{newValue}/{boundHp.MaxHP}";
    }

    private void OnDisable()
    {
        if (subscribed && boundHp != null)
        {
            boundHp.Hp.OnValueChanged -= OnHpChanged;
            subscribed = false;
        }
    }
}
