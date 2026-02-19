using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSFX : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip clickClip;

    [Header("Tuning")]
    [Range(0f,1f)] public float volume = 1f;
    [Range(0f,0.5f)] public float pitchJitter = 0.05f;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(PlayClickSafe);
    }

    private void PlayClickSafe()
    {
        // Never throw — other listeners must still run (e.g., CloseSettings)
        try
        {
            if (!clickClip) return;

            // Prefer 2D for UI sounds so they’re consistent
            if (pitchJitter > 0f)
            {
                var inst = SFXBus.Instance; if (inst == null) return;
                float p = 1f + Random.Range(-pitchJitter, pitchJitter);
                inst.Play2D(clickClip, volume, p);
            }
            else
            {
                SFXBus.Instance?.Play2D(clickClip, volume, 1f);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UIButtonSFX] Click sound failed (won't block other listeners): {ex.Message}");
        }
    }
}
