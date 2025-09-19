using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuScript : MonoBehaviour
{
    public static MainMenuScript instance;

    [SerializeField] GameObject activeMenu;
    [SerializeField] GameObject titleScreen;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject creditsMenu;
    [SerializeField] GameObject assetsCreditsMenu;
    [SerializeField] GameObject menuControlSettings;
    [SerializeField] GameObject menuAudioSettings;
    [SerializeField] MenuState menuCurr;

    public enum MenuState
    {
        None,
        Title,
        Settings,
        Credits
    }
    public MenuState originalMenu;

    [SerializeField] private GameObject titleFirst;
    [SerializeField] private GameObject settingsFirst;
    [SerializeField] private GameObject creditsFirst;
    [SerializeField] private GameObject assetsCreditsFirst;
    [SerializeField] private GameObject audioSettingsFirst;
    [SerializeField] private GameObject controlsSettingsFirst;

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
        EventSystem.current.SetSelectedGameObject(titleFirst);
    }

    public void settingsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = settingsMenu;
        activeMenu.SetActive(true);
        EventSystem.current.SetSelectedGameObject(settingsFirst);
    }

    public void creditsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = creditsMenu;
        activeMenu.SetActive(true);
        EventSystem.current.SetSelectedGameObject(creditsFirst);
    }
    public void assetsCreditsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = assetsCreditsMenu;
        activeMenu.SetActive(true);
        menuCurr = MenuState.Credits;
        EventSystem.current.SetSelectedGameObject(creditsFirst);
    }

    public void controlSettingsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = menuControlSettings;
        activeMenu.SetActive(true);
        menuCurr = MenuState.Settings;
        EventSystem.current.SetSelectedGameObject(controlsSettingsFirst);
    }

    public void audioSettingsOpen()
    {
        activeMenu.SetActive(false);
        activeMenu = null;
        activeMenu = menuAudioSettings;
        activeMenu.SetActive(true);
        menuCurr = MenuState.Settings;
        EventSystem.current.SetSelectedGameObject(audioSettingsFirst);
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
                    EventSystem.current.SetSelectedGameObject(titleFirst);
                    break;
                }
            case MenuState.Settings:
                {
                    menuCurr = originalMenu;
                    activeMenu.SetActive(false);
                    activeMenu = null;
                    activeMenu = settingsMenu;
                    activeMenu.SetActive(true);
                    EventSystem.current.SetSelectedGameObject(settingsFirst);
                    break;
                }
            case MenuState.Credits:
                {
                    menuCurr = originalMenu;
                    activeMenu.SetActive(false);
                    activeMenu = null;
                    activeMenu = creditsMenu;
                    activeMenu.SetActive(true);
                    EventSystem.current.SetSelectedGameObject(creditsFirst);
                    break;
                }
            default:
                break;
        }
    }

}
