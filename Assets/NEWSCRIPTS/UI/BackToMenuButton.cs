using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMainMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "Main_Menu";
    public void OnBack() => SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
}
