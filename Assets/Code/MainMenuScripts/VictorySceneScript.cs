using UnityEngine;
using UnityEngine.SceneManagement;

public class VictorySceneScript : MonoBehaviour
{
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
