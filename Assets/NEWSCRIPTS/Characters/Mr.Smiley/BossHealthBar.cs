using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    [Header("Boss Reference")]
    [SerializeField] private Damagable boss;

    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text hpText;

    [Header("Display Settings")]
    [SerializeField] private string bossDisplayName = "MR. SMILEY";
    [SerializeField] private float lerpSpeed = 8f;
    [SerializeField] private bool hideUntilDamaged = true;
    [SerializeField] private bool hideOnDeath = true;

    private bool activated = false;
    private Coroutine lerpRoutine;
    private int lastHP;
    private int lastMaxHP;

    private void Awake()
    {

        if (boss == null)
        {
            Debug.LogError("[BossHealthBar] No Damagable assigned");
            enabled = false;
            return;
        }

        if (!fillImage || !bossNameText || !hpText)
        {
            Debug.LogError("[BossHealthBar] Missing UI references");
            enabled = false;
            return;
        }

        boss.healthChanged.AddListener(OnHealthChanged);

        if (hideOnDeath)
            boss.onDeath.AddListener(OnBossDeath);

        // Initialize UI
        bossNameText.text = bossDisplayName;
        lastHP = boss.Health;
        lastMaxHP = boss.MaxHealth;

        fillImage.fillAmount = (float)lastHP / lastMaxHP;
        UpdateHPText(lastHP, lastMaxHP);
activated = true;
gameObject.SetActive(true);

    }

    private void OnDestroy()
    {
        if (boss != null)
        {
            boss.healthChanged.RemoveListener(OnHealthChanged);
            boss.onDeath.RemoveListener(OnBossDeath);
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        lastHP = current;
        lastMaxHP = max;

        float targetFill = Mathf.Clamp01((float)current / max);

        if (!activated)
        {
            activated = true;
            gameObject.SetActive(true);
        }

        UpdateHPText(current, max);

        if (lerpRoutine != null)
            StopCoroutine(lerpRoutine);

        lerpRoutine = StartCoroutine(LerpFill(targetFill));
    }

    private IEnumerator LerpFill(float target)
    {
        while (Mathf.Abs(fillImage.fillAmount - target) > 0.001f)
        {
            fillImage.fillAmount = Mathf.Lerp(
                fillImage.fillAmount,
                target,
                Time.deltaTime * lerpSpeed
            );
            yield return null;
        }

        fillImage.fillAmount = target;
    }

    private void UpdateHPText(int current, int max)
    {
        hpText.text = $"{current} / {max}";
    }

private void OnBossDeath()
{
    if (!hideOnDeath) return;

    // Hide visuals ONLY — do not disable GameObject
    CanvasGroup cg = GetComponent<CanvasGroup>();
    if (cg != null)
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}

}
