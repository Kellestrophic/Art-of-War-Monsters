using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public TMP_Text healthBarText;
    private Damagable playerDamagable;

    void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("No player found in the scene. Make sure the player GameObject has the 'Player' tag.");
        }

        playerDamagable = player.GetComponent<Damagable>();
    }

    void Start()
    {
        // Set up slider bounds ONCE
        healthSlider.minValue = 0;
        healthSlider.maxValue = playerDamagable.MaxHealth;
        healthSlider.value = playerDamagable.Health;

        UpdateHealthText(playerDamagable.Health, playerDamagable.MaxHealth);
    }

    void OnEnable()
    {
        playerDamagable.healthChanged.AddListener(OnPlayerHealthChanged);
    }

    void OnDisable()
    {
        playerDamagable.healthChanged.RemoveListener(OnPlayerHealthChanged);
    }

    private void OnPlayerHealthChanged(int newHealth, int maxHealth)
    {
        healthSlider.value = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateHealthText(newHealth, maxHealth);
    }

    private void UpdateHealthText(int current, int max)
    {
        healthBarText.text = $"HP {current} / {max}";
    }
}
