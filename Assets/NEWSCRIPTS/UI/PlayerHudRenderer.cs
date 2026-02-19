// Assets/NEWSCRIPTS/UI/PlayerHudRenderer.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHudRefs))]
public class PlayerHudRenderer : MonoBehaviour
{
    [Header("Bind these in Inspector")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("HP UI")]
    [SerializeField] private Image hpFill;          // Image type: Filled (fill amount)
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Strike UI")]
    [SerializeField] private Image strikeFill;      // Image type: Filled
    [SerializeField] private TextMeshProUGUI strikeText;

    [Header("Cosmetics")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image frameImage;

    private PlayerHudRefs _refs;

    private void Awake()
    {
        _refs = GetComponent<PlayerHudRefs>();
    }

    private void LateUpdate()
    {
        if (_refs == null) return;

        // Identity / profile
        if (nameText)  nameText.text  = _refs.PlayerName;
        if (titleText) titleText.text = _refs.PlayerTitle;
        if (levelText) levelText.text = $"Lv {_refs.Level}";

        // HP
        float hp = Mathf.Max(0f, _refs.CurrentHP);
        float hpMax = Mathf.Max(1f, _refs.MaxHP);
        if (hpFill)  hpFill.fillAmount = hp / hpMax;
        if (hpText)  hpText.text = $"{Mathf.RoundToInt(hp)}/{Mathf.RoundToInt(hpMax)}";

        // Strike
        float s = Mathf.Max(0f, _refs.CurrentStrike);
        float sMax = Mathf.Max(1f, _refs.MaxStrike);
        if (strikeFill)  strikeFill.fillAmount = s / sMax;
        if (strikeText)  strikeText.text = $"{Mathf.RoundToInt(s)}/{Mathf.RoundToInt(sMax)}";

        // Cosmetics
        if (iconImage)  iconImage.sprite  = _refs.PlayerIcon;
        if (frameImage) frameImage.sprite = _refs.DefaultFrame;
    }
}
