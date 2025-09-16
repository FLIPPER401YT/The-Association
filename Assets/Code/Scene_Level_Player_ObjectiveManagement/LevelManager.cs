using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance {  get; private set; }
    private List<GameObject> bosses = new List<GameObject>();
    public delegate void AllBossesDestroyed();
    public event AllBossesDestroyed BossesDestroyed;
    public bool isVictoryScene = false;
    public PlayerController player;
    #region Persistance
    [Serializable]
    public class SaveData
    {
        public int health, healthMax, bloodSamples;
        public Dictionary<string, bool> bossesDefeated = new Dictionary<string, bool>();

        public SaveData()
        {
            health = 100;
            healthMax = 100;
            bloodSamples = 0;
            bossesDefeated = new Dictionary<string, bool>();
        }
    }
    public SaveData currentSave = new SaveData();
    private string saveFile;
    #endregion
    #region Unity
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        saveFile = Path.Combine(Application.persistentDataPath, "save.json");
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadGame();
    }
    private void Start()
    {
        player = GameManager.instance?.playerScript;
        if(player != null)
        {
            player.health = currentSave.health;
            player.bloodSamples = currentSave.bloodSamples;
            player.updatePlayerHealthBarUI();
            player.UpdateSampleCount(player.bloodSamples);
        }
        BossesDestroyed += Victory;
        if(isVictoryScene)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    #endregion
    #region Scene Handler
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals("VictoryScene"))
        {
            StartCoroutine(DelayedCursorUnlock());
            isVictoryScene = true;
        }
        else
        {
            GameManager.instance?.mouseInvisibility();
            isVictoryScene = false;
        }
        player = GameManager.instance?.playerScript;
        if (player != null) {
            player.health = currentSave.health;
            player.bloodSamples= currentSave.bloodSamples;
            player.updatePlayerHealthBarUI();
            player.UpdateSampleCount(player.bloodSamples);
        }
    }
    private IEnumerator DelayedCursorUnlock()
    {
        yield return new WaitForEndOfFrame();
        GameManager.instance?.mouseVisibility();
    }
    #endregion
    #region Save and Load
    public void SaveGame()
    {
        if (player != null)
        {
            currentSave.health = player.health;
            currentSave.bloodSamples = player.bloodSamples;
        }
        string jsonFile = JsonUtility.ToJson(currentSave, prettyPrint: true);
        File.WriteAllText(saveFile, jsonFile);
    }
    public void LoadGame()
    {
        if (File.Exists(saveFile))
        {
            try
            {
                string jsonFile = File.ReadAllText(saveFile);
                currentSave = JsonUtility.FromJson<SaveData>(jsonFile);
                if (currentSave.bossesDefeated == null)
                {
                    currentSave.bossesDefeated = new Dictionary<string, bool>();
                }
            }
            catch (Exception except)
            {
                currentSave = new SaveData();
            }
        }
        else currentSave = new SaveData();
    }
    #endregion
    #region Scene Management
    public void LoadScene(string sceneName)
    {
        SaveGame();
        SceneManager.sceneLoaded += OnScene;
        SceneManager.LoadScene(sceneName);
    }
    private void OnScene(Scene scene, LoadSceneMode mode)
    {
        LoadGame();
        SceneManager.sceneLoaded -= OnScene;
    }
    #endregion
    #region Victory Condition
    public void RegisterTrackable(GameObject boss)
    {
        if(!bosses.Contains(boss)) bosses.Add(boss);
    }
    public void UnregisterTrackable(GameObject boss)
    {
        if(bosses.Contains(boss))
        {
            bosses.Remove(boss);
            if (bosses.Count == 0 && !ButtonFunctions.quitingToMain) BossesDestroyed?.Invoke();
        }
    }
    #endregion
    #region Boss Tracking
    public void MarkBossDefeated(string bossName)
    {
        if (!currentSave.bossesDefeated.ContainsKey(bossName))
        {
            currentSave.bossesDefeated[bossName] = true;
        }
        SaveGame();
        CheckVictoryCondition();
    }
    private void CheckVictoryCondition()
    {
        string[] bossRoster = new string[] { "Bigfoot", "Mothman", "Wendigo" };
        foreach(string boss in bossRoster)
        {
            if (!currentSave.bossesDefeated.ContainsKey(boss) || !currentSave.bossesDefeated[boss]) return;
        }
        Victory();
    }
    public void Victory()
    {
        isVictoryScene = true;
        if (player != null) Destroy(GameManager.instance.player);
        SceneManager.LoadScene("VictoryScene");
        GameManager.instance?.mouseVisibility();
    }
    #endregion
}