using UnityEngine;
using TMPro;

public class HealthText : MonoBehaviour
{
    public Vector3 moveSpeed = new Vector3(0, 75, 0);
    public float timeToFade = 1f;

    private RectTransform textTransform;
    private TextMeshProUGUI textMeshPro;
    private float timeElapsed;
    private Color startColor;

    void Awake()
    {
        textTransform = GetComponent<RectTransform>();
        textMeshPro = GetComponent<TextMeshProUGUI>();
        if (textMeshPro != null)
        {
            startColor = textMeshPro.color;
        }
    }

    void Update()
    {
        if (textTransform != null)
            textTransform.position += moveSpeed * Time.deltaTime;

        timeElapsed += Time.deltaTime;

        if (timeElapsed < timeToFade)
        {
            if (textMeshPro != null)
            {
                float t = timeElapsed / timeToFade;
                textMeshPro.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            }
        }
        else
        {
            Destroy(gameObject); // ✅ only kills the text prefab
        }
    }
}