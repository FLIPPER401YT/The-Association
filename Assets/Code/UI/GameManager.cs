using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuSettings;
    [SerializeField] GameObject menuControlSettings;
    [SerializeField] GameObject menuAudioSettings;
    [SerializeField] MenuState menuCurr;

    public bool isPaused;

    [SerializeField] public GameObject interactableTextObject;
    [SerializeField] public TMP_Text interactableText;

    public GameObject player;
    public PlayerController playerScript;
    public Image playerHealthBar;
    public GameObject playerDamageEffect;
    public GameObject playerSpawnPos;

    public GameObject contractBoardCam;
    public GameObject contractBoardUI;

    float timeScaleOriginal;

    public enum MenuState
    {
        None,
        Pause,
        Settings
    }

    public MenuState originalMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        timeScaleOriginal = Time.timeScale;

        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            if(menuActive == null)
            {
                statePaused();
                menuActive = menuPause;
                menuActive.SetActive(true);
            }
            else if(menuActive == menuPause)
            {
                stateUnpaused();
            }
        }
    }

    public void statePaused()
    {
        isPaused = !isPaused;
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        menuCurr = MenuState.Pause;
        originalMenu = MenuState.Pause;


    }

    public void stateUnpaused()
    {
        isPaused = !isPaused;
        Time.timeScale = timeScaleOriginal;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        menuActive.SetActive(false);
        menuActive = null;
        menuCurr = 0;
        originalMenu = 0;
    }

    public void settingsOpen()
    {
        menuActive.SetActive(false);
        menuActive = null;
        menuActive = menuSettings;
        menuActive.SetActive(true);
    }

    public void controlSettingsOpen()
    {
        menuActive.SetActive(false);
        menuActive = null;
        menuActive = menuControlSettings;
        menuActive.SetActive(true);
        menuCurr = MenuState.Settings;
    }

    public void audioSettingsOpen()
    {
        menuActive.SetActive(false);
        menuActive = null;
        menuActive = menuAudioSettings;
        menuActive.SetActive(true);
        menuCurr = MenuState.Settings;
    }
    public void settingsClosed()
    {
        switch (menuCurr)
        {
            case MenuState.Pause:
                {
                    menuActive.SetActive(false);
                    menuActive = null;
                    menuActive = menuPause;
                    menuActive.SetActive(true);
                    break;
                }
            case MenuState.Settings:
                {
                    menuCurr = originalMenu;
                    menuActive.SetActive(false);
                    menuActive = null;
                    menuActive = menuSettings;
                    menuActive.SetActive(true);
                    break;
                }
            default:
                break;
        }
    }

    public void enableInteractableText(string text)
    {
        interactableText.text = text + "(E)";
        interactableText.gameObject.SetActive(true);
    }

    public void disableInteractableText()
    {
        interactableText.gameObject.SetActive(false);
    }

    public void contractBoardMouseVisibility()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void contractBoardMouseInvisibiliy()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
