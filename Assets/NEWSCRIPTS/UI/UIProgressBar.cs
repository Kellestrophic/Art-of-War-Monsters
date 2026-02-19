using UnityEngine;
using UnityEngine.UI;

public class UIProgressBar : MonoBehaviour
{
    [SerializeField] private Image fillImage; // drag HealthBar_Fill here

    public void SetValue(float value)
    {
        if (!fillImage) return;
        fillImage.fillAmount = Mathf.Clamp01(value);
    }

    public void SetValue(float current, float max)
    {
        SetValue(max > 0f ? current / max : 0f);
    }
}
