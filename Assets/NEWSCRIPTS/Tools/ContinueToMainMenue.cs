using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ContinueToMainMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool restoreTimeScale = true;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Go);
    }

    public void Go()
    {
        if (restoreTimeScale) Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
