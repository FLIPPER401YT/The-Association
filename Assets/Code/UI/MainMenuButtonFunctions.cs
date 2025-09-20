using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtonFunctions : MonoBehaviour
{
    public void StartGame()
    {
        ButtonFunctions.quitingToMain = false;
        //SceneManager.LoadScene("HubArea");
        //LoadingScreenManager.instance.swapToScene("HubArea");
    }
    public void settings()
    {
        MainMenuScript.instance.settingsOpen();
    }

    public void credits()
    {
        MainMenuScript.instance.creditsOpen();
    }

    public void assetsCredits()
    {
        MainMenuScript.instance.assetsCreditsOpen();
    }
    public void controlSettings()
    {
        MainMenuScript.instance.controlSettingsOpen();
    }
    public void audioSettings()
    {
        MainMenuScript.instance.audioSettingsOpen();
    }
    public void back()
    {
        MainMenuScript.instance.settingsClosed();
    }

    public void quitGame()
    {
#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;

#else
        Application.Quit();
#endif
    }
}
