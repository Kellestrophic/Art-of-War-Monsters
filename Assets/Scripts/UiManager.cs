using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;



#if UNITY_EDITOR
#endif

public class UiManager : MonoBehaviour
{
    public GameObject damageTextPrefab;
    public GameObject healthTextPrefab;
    public Canvas gameCanvas;

    private void Awake()
    {
        gameCanvas = FindFirstObjectByType<Canvas>(); // or FindAnyObjectByType<Canvas>()  
    }

    private void OnEnable()
    {
        CharacterEvents.characterDamaged += (CharacterTookDamage);
        CharacterEvents.characterHealed += (CharacterHealed);
    }

    private void OnDisable()
    {
        CharacterEvents.characterDamaged -= (CharacterTookDamage);
        CharacterEvents.characterHealed -= (CharacterHealed);
    }
    public void CharacterTookDamage(GameObject character, int damageRecieved)
    {
        Vector3 spawnPosition = Camera.main.WorldToScreenPoint(character.transform.position);

        TMP_Text tmpText = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity, gameCanvas.transform)
        .GetComponent<TMP_Text>();

        tmpText.text = damageRecieved.ToString();
    }



    public void CharacterHealed(GameObject character, int healthRestored)
    {
        Vector3 spawnPosition = Camera.main.WorldToScreenPoint(character.transform.position);

        TMP_Text tmpText = Instantiate(healthTextPrefab, spawnPosition, Quaternion.identity, gameCanvas.transform)
        .GetComponent<TMP_Text>();

        tmpText.text = healthRestored.ToString();
    }


public void OnExitGame(InputAction.CallbackContext context)
{
    if (context.started)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        SceneManager.LoadScene("QuitScene"); // Optional: replace with your quit scene name
#else
        Application.Quit(); // For PC/Standalone builds
#endif
    }
}
}
