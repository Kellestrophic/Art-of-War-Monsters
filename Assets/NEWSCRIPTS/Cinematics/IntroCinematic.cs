using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;


public class CinematicSequence : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        public Sprite image;

        [TextArea(2, 5)]
        public string text;

        // Optional: per-slide hold time override (0 = use global holdTime)
        public float holdOverride = 0f;
    }

    [Header("UI")]
    [SerializeField] private Image cinematicImage;
    [SerializeField] private TMP_Text cinematicText;

    [Header("Slides")]
    [SerializeField] private Slide[] slides;

    [Header("Timing")]
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private float holdTime = 3f;

    [Header("Controls")]

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName;
private InputAction attackAction;

    private int index = 0;
    private bool isFading = false;
    private bool requestNext = false;

    private void Start()
    {
        // Start fully invisible
        SetAlpha(0f);

        if (slides == null || slides.Length == 0)
        {
            Debug.LogError("[CinematicSequence] No slides assigned.");
            return;
        }

        StartCoroutine(Run());
    }

private void OnEnable()
{
    var playerInput = FindFirstObjectByType<PlayerInput>();
    if (playerInput == null)
    {
        Debug.LogError("[CinematicSequence] No PlayerInput found in scene.");
        return;
    }

    attackAction = playerInput.actions["Attack"];
    attackAction.performed += OnAdvance;
}
public void SkipCinematic()
{
    StopAllCoroutines();
    SceneManager.LoadScene(nextSceneName);
}

private void OnDisable()
{
    if (attackAction != null)
        attackAction.performed -= OnAdvance;
}

private void OnAdvance(InputAction.CallbackContext ctx)
{
    requestNext = true;
}


    private IEnumerator Run()
    {
        while (index < slides.Length)
        {
            Slide s = slides[index];

            cinematicImage.sprite = s.image;
            cinematicText.text = s.text;

            requestNext = false;

            // Fade in
            yield return Fade(0f, 1f);

            // Hold (or skip)
            float hold = (s.holdOverride > 0f) ? s.holdOverride : holdTime;
            float t = 0f;
            while (t < hold && !requestNext)
            {
                t += Time.deltaTime;
                yield return null;
            }

            requestNext = false;

            // Fade out
            yield return Fade(1f, 0f);

            index++;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        isFading = true;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeTime);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(to);
        isFading = false;
    }

    private void SetAlpha(float a)
    {
        if (cinematicImage != null)
        {
            Color c = cinematicImage.color;
            c.a = a;
            cinematicImage.color = c;
        }

        if (cinematicText != null)
        {
            Color c = cinematicText.color;
            c.a = a;
            cinematicText.color = c;
        }
    }
}
