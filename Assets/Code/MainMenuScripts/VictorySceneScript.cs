using UnityEngine;
using UnityEngine.SceneManagement;

public class VictorySceneScript : MonoBehaviour
{
    public GameObject quitButton;
    void Start()
    {
#if UNITY_WEBGL
        if (quitButton != null)
            quitButton.SetActive(false);
#else
        if (quitButton != null)
            quitButton.SetActive(true);
#endif
    }
    public void StartGame()
    {
        SceneManager.LoadScene("HubArea");
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
