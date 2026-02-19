using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Target (leave empty on enemy HUD to auto-grab parent)")]
    [SerializeField] private Damagable target;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText; // optional

    private void Reset()
    {
        healthSlider = GetComponentInChildren<Slider>();
        healthText   = GetComponentInChildren<TMP_Text>();
        target       = GetComponentInParent<Damagable>();
    }

    private void Awake()
    {
        if (target == null) target = GetComponentInParent<Damagable>();
        if (healthSlider != null) healthSlider.minValue = 0;
    }

    private void OnEnable()
    {
        if (target == null) return;
        target.healthChanged.AddListener(OnHealthChanged);
        OnHealthChanged(target.Health, target.MaxHealth);
    }

    private void OnDisable()
    {
        if (target != null)
            target.healthChanged.RemoveListener(OnHealthChanged);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value    = Mathf.Clamp(current, 0, max);
        }
        if (healthText != null) healthText.text = $"HP {current} / {max}";
    }

    public void Bind(Damagable newTarget)
    {
        if (target != null) target.healthChanged.RemoveListener(OnHealthChanged);
        target = newTarget;
        if (isActiveAndEnabled && target != null)
        {
            target.healthChanged.AddListener(OnHealthChanged);
            OnHealthChanged(target.Health, target.MaxHealth);
        }
    }
}
