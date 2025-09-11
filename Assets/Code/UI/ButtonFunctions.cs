using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    public void resume()
    {
        GameManager.instance.stateUnpaused();
    }
    public void settings()
    {
        GameManager.instance.settingsOpen();
    }

    public void controlSettings()
    {
        GameManager.instance.controlSettingsOpen();
    }
    public void audioSettings()
    {
        GameManager.instance.audioSettingsOpen();
    }
    public void back()
    {
        GameManager.instance.settingsClosed();
    }


    public void restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        GameManager.instance.stateUnpaused();
    }

    public void quitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void returnToGameFromBoard()
    {
        GameManager.instance.contractBoardMouseInvisibiliy();
        GameManager.instance.contractBoardUI.SetActive(false);
        GameManager.instance.player.SetActive(true);
        GameManager.instance.contractBoardCam.SetActive(false);
        GameManager.instance.interactableTextObject.SetActive(true);
    }
}
