using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    public static MainMenuScript instance;

    [SerializeField] GameObject activeMenu;
    [SerializeField] GameObject titleScreen;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject menuControlSettings;
    [SerializeField] GameObject menuAudioSettings;
    [SerializeField] MenuState menuCurr;

    public enum MenuState
    {
        None,
        Title,
        Settings
    }
    public MenuState originalMenu;

    void Awake()
    {
        instance = this;
        titleScreenOpen();
    }

    public void titleScreenOpen()
    {
        activeMenu = titleScreen;
        activeMenu.SetActive(true);
        menuCurr = MenuState.Title;
        originalMenu = MenuState.Title;
    }

    public void settingsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = settingsMenu;
        activeMenu.SetActive(true);
    }

    public void controlSettingsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = menuControlSettings;
        activeMenu.SetActive(true);
        menuCurr = MenuState.Settings;
    }

    public void audioSettingsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = menuAudioSettings;
        activeMenu.SetActive(true);
        menuCurr = MenuState.Settings;
    }

    public void settingsClosed()
    {
        switch (menuCurr)
        {
            case MenuState.Title:
                {
                    activeMenu.SetActive(false);
                    activeMenu = null;
                    activeMenu = titleScreen;
                    activeMenu.SetActive(true);
                    break;
                }
            case MenuState.Settings:
                {
                    menuCurr = originalMenu;
                    activeMenu.SetActive(false);
                    activeMenu = null;
                    activeMenu = settingsMenu;
                    activeMenu.SetActive(true);
                    break;
                }
            default:
                break;
        }
    }

}
