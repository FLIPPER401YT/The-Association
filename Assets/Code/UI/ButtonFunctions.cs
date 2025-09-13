using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    public void resume()
    {
        
        GameManager.instance.stateUnpaused();
        GameManager.instance.mouseInvisibility();
        
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
        GameManager.instance.playerUI.SetActive(true);
        GameManager.instance.mouseInvisibility();
        GameManager.instance.contractBoardListUI.SetActive(false);
        GameManager.instance.player.SetActive(true);
        GameManager.instance.contractBoardCam.SetActive(false);
        GameManager.instance.interactableTextObject.SetActive(true);
        GameManager.instance.contractBoardCurr = GameManager.ContractBoardState.None;
    }

    public void returnToGameFromShop()
    {
        GameManager.instance.playerUI.SetActive(true);
        GameManager.instance.mouseInvisibility();
        GameManager.instance.shopUI.SetActive(false);
        GameManager.instance.player.SetActive(true);
        GameManager.instance.shopCam.SetActive(false);
        GameManager.instance.interactableTextObject.SetActive(true);
    }

    public void swapToBigfootPage()
    {
        GameManager.instance.bigfootPageOpen();
    }

    public void returnToListOfCryptids()
    {
        GameManager.instance.contractBoardReturn();
    }

    public void respawn()
    {
        LevelManager.Instance.LoadGame();
        GameManager.instance.playerScript.enabled = true;
        GameManager.instance.playerScript.anim.enabled = false;
        GameManager.instance.player.transform.localRotation = Quaternion.Euler(0, 0, 0);
        GameManager.instance.playerScript.Heal(100);
        GameManager.instance.stateUnpaused();
        GameManager.instance.settingsClosed();
    }
}
