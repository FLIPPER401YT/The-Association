using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Menu UI")]
    [SerializeField] public GameObject menuActive;
    [SerializeField] public GameObject menuPause;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuSettings;
    [SerializeField] GameObject menuControlSettings;
    [SerializeField] GameObject menuAudioSettings;
    [SerializeField] MenuState menuCurr;

    public bool isPaused;
    public enum MenuState
    {
        None,
        Pause,
        Lose,
        Settings
    }
    public MenuState originalMenu;

    [Header("Player UI")]
    [SerializeField] GameObject playerPrefab;

    public GameObject player;
    public PlayerController playerScript;
    public CameraController cameraController;
    public Image playerHealthBar;
    public GameObject playerDamageEffect;
    public GameObject playerSpawnPos;
    public TMP_Text playerHealthMaxText;
    public TMP_Text playerHealthText;
    public GameObject playerUI;

    public GameObject ammoUIObject;
    public TMP_Text currentAmmo;
    public TMP_Text totalAmmo;

    [Header("Contract Board UI")]
    public GameObject contractBoardCam;
    public GameObject contractBoardActiveMenu;
    public GameObject contractBoardListUI;
    public GameObject contractBoardBigfootUI;
    public ContractBoardState contractBoardCurr;

    public enum ContractBoardState
    {
        None,
        List,
        Bigfoot
    }
    public ContractBoardState origContractBoardState;

    [Header("Shop UI")]
    public GameObject shopCam;
    public GameObject shopUI;

    [Header("Miscellaneous stuff")]
    float timeScaleOriginal;

    [SerializeField] public GameObject interactableTextObject;
    [SerializeField] public TMP_Text interactableText;

    [SerializeField] public GameObject spawnPoint;
    [SerializeField] public TMP_Text samples;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        ButtonFunctions.quitingToMain = false;
        timeScaleOriginal = Time.timeScale;

        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = Instantiate(playerPrefab, null);
            playerScript = player.GetComponent<PlayerController>();
        }

        cameraController = Camera.main.GetComponent<CameraController>();
        spawnPoint = GameObject.FindWithTag("Respawn");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (menuActive == null)
            {
                statePaused();
                menuActive = menuPause;
                menuActive.SetActive(true);
            }
            else if (menuActive == menuPause)
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

    public void updateToLoseScreen()
    {
        statePaused();
        menuActive = menuLose;
        menuActive.SetActive(true);
        menuCurr = MenuState.Lose;
        originalMenu = MenuState.Lose;

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
            case MenuState.Lose:
                {
                    menuActive.SetActive(false);
                    menuActive = null;
                    menuActive = menuLose;
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

    public void mouseVisibility()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void mouseInvisibility()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void bigfootPageOpen()
    {
        contractBoardActiveMenu.SetActive(false);
        contractBoardActiveMenu = null;
        contractBoardActiveMenu = contractBoardBigfootUI;
        contractBoardActiveMenu.SetActive(true);
        GameManager.instance.contractBoardCurr = GameManager.ContractBoardState.List;
    }

    // More will be added later but for now this is all we got
    public void contractBoardReturn()
    {
        switch (contractBoardCurr)
        {
            case ContractBoardState.List:
                {
                    contractBoardActiveMenu.SetActive(false);
                    contractBoardActiveMenu = null;
                    contractBoardActiveMenu = contractBoardListUI;
                    contractBoardActiveMenu.SetActive(true);
                    break;
                }
            default:
                break;
        }
    }

    public void Lose()
    {
        Debug.Log("Runs Lose");
        cameraController.ResetRotation();
        updateToLoseScreen();
        cameraController.canLook = false;
        if (playerScript) playerScript.enabled = false;
    }
    public void SampleCount(int count)
    {
        if (samples != null) samples.text = $"{count}";
    }
}
