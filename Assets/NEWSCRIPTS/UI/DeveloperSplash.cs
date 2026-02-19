// Assets/NEWSCRIPTS/Core/DeveloperSplash.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class DeveloperSplash : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Image fadeImage;      // full-screen black Image
    [SerializeField] CanvasGroup logoGroup; // parent CanvasGroup for logo items

    [Header("Timing")]
    [SerializeField] float fadeInTime = 1f;
    [SerializeField] float holdTime   = 2f;
    [SerializeField] float fadeOutTime = 1f;
    [SerializeField] string nextScene = "MainMenu";

    void Start()
    {
        StartCoroutine(RunSplash());
    }

    IEnumerator RunSplash()
    {
        if (fadeImage) fadeImage.color = new Color(0,0,0,1);
        if (logoGroup) logoGroup.alpha = 0f;

        // Fade in logo (screen goes from black to clear, logo fades in)
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeInTime);

            if (fadeImage) fadeImage.color = new Color(0,0,0,1f - lerp);
            if (logoGroup) logoGroup.alpha = lerp;

            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out to black
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeOutTime);

            if (fadeImage) fadeImage.color = new Color(0,0,0,lerp);
            if (logoGroup) logoGroup.alpha = 1f - lerp;

            yield return null;
        }

        // Load main menu
        SceneManager.LoadScene(nextScene);
    }
}
