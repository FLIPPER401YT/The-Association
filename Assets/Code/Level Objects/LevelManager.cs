using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance {  get; private set; }
    public PlayerManager playerData = new PlayerManager();
    private List<GameObject> objects = new List<GameObject>();
    public delegate void OnAllObjectsDestroyed();
    public event OnAllObjectsDestroyed ObjectsDestroyed;
    public PlayerController player;
    public bool isVictoryScene = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnLoaded;
    }
    private void Start()
    {
        player = GameManager.instance.playerScript;
        ObjectsDestroyed += Victory;
        if (LevelManager.Instance != null && LevelManager.Instance.isVictoryScene)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnLoaded;
    }
    private IEnumerator DelayedUnlock()
    {
        yield return new WaitForEndOfFrame();
        GameManager.instance?.mouseVisibility();
    }
    private void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "VictoryScene")
        {
            StartCoroutine(DelayedUnlock());
            isVictoryScene = true;
        }
        else
        {
            GameManager.instance?.mouseInvisibility();
            isVictoryScene= false;
        }
    }
    #region Persistence
    public void SaveGame()
    {
        PlayerPrefs.SetInt("Health", playerData.hp);
        PlayerPrefs.SetInt("HealthMax", playerData.hpMax);
        PlayerPrefs.SetInt("BloodSamples", playerData.bloodSample);
        PlayerPrefs.Save();
    }
    public void LoadGame() {
        playerData.hp = PlayerPrefs.GetInt("Health", player.health);
        playerData.hpMax = PlayerPrefs.GetInt("HealthMax", player.healthMax);
        playerData.bloodSample = PlayerPrefs.GetInt("BloodSamples", playerData.bloodSample);
    }
    public void LoadScene(string sceneName)
    {
        SaveGame();
        SceneManager.sceneLoaded += OnScene;
        SceneManager.LoadScene(sceneName);
    }
    public void OnScene(Scene scene, LoadSceneMode mode)
    {
        LoadGame();
        SceneManager.sceneLoaded -= OnScene;
    }
    #endregion

    #region ObjectTracking
    public void RegisterTrackable(GameObject Object)
    {
        if (!objects.Contains(Object)) objects.Add(Object);
    }
    public void UnregisterTrackable(GameObject Object)
    {
        if(objects.Contains(Object))
        {
            objects.Remove(Object);
            if (objects.Count == 0 && !ButtonFunctions.quitingToMain) ObjectsDestroyed?.Invoke();
        }
    }
    #endregion

    #region WinCondition
    public void Victory()
    {
        isVictoryScene = true;
        Destroy(GameManager.instance.player);
        SceneManager.LoadScene("VictoryScene");
        GameManager.instance?.mouseVisibility();
    }
    #endregion
}
