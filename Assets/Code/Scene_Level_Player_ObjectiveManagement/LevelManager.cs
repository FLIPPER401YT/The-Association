using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
    public Transform spawnPoint;

    #region Persistance
    [Serializable]
    public class SaveData
    {
        public int health, healthMax, bloodSamples;
        public List<string> defeatedBosses = new List<string>();
        public Dictionary<string, bool> GetBossesDefeatedDict()
        {
            var dictionary = new Dictionary<string, bool>();
            foreach (var name in defeatedBosses)
            {
                dictionary[name] = true;
            }
            return dictionary;
        }
        public void DefeatedBossesDictionary(Dictionary<string, bool> dict)
        {
            defeatedBosses.Clear();
            foreach (var keyPair in dict)
            {
                if (keyPair.Value) defeatedBosses.Add(keyPair.Key);
            }
        }
    }
    public SaveData currentSave = new SaveData();
    private const string saveData = "SaveData";
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
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadGame();
    }
    private void Start()
    {
        player = GameManager.instance?.playerScript;
        if(player != null)
        {
            player.health = currentSave.health;
            player.healthMax = currentSave.healthMax;
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
    public void Update()
    {
        
    }
    #endregion
    #region Scene Handler
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        spawnPoint = SpawnPoint(scene.name);
        if(player != null && spawnPoint != null)
        {
            player.spawnPoint = spawnPoint;
            player.SpawnPlayer();
        }
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
            player.healthMax = currentSave.healthMax;
            player.bloodSamples= currentSave.bloodSamples;
            player.updatePlayerHealthBarUI();
            player.UpdateSampleCount(player.bloodSamples);
        }
    }
    private Transform SpawnPoint(string sceneName)
    {
        GameObject spawnObject = GameObject.FindGameObjectWithTag("Respawn");
        if (spawnObject != null) return spawnObject.transform;
        return null;
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
            currentSave.healthMax = player.healthMax;
            currentSave.bloodSamples = player.bloodSamples;
        }
        PlayerPrefs.SetInt("Health", currentSave.health);
        PlayerPrefs.SetInt("BloodSamples", currentSave.bloodSamples);
        PlayerPrefs.SetInt("DefeatedBossesCount", currentSave.defeatedBosses.Count);
        for (int index = 0; index < currentSave.defeatedBosses.Count; index++)
        {
            PlayerPrefs.SetString("DefeatedBoss_" + index, currentSave.defeatedBosses[index]);
        }
        PlayerPrefs.Save();
    }
    public void LoadGame()
    {
        currentSave = new SaveData();
        if (PlayerPrefs.HasKey("Health")) currentSave.health = PlayerPrefs.GetInt("Health");
        if (PlayerPrefs.HasKey("HealthMax")) currentSave.healthMax = PlayerPrefs.GetInt("HealthMax");
        if (PlayerPrefs.HasKey("BloodSamples")) currentSave.bloodSamples = PlayerPrefs.GetInt("BloodSamples");
        currentSave.defeatedBosses.Clear();
        int count = PlayerPrefs.GetInt("DefeatedBossesCount", 0);
        for (int index = 0; index < count; index++)
        {
            string bossName = PlayerPrefs.GetString("DefeatedBoss_" + index, "");
            if (!string.IsNullOrEmpty(bossName))
                currentSave.defeatedBosses.Add(bossName);
        }
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoadedForSave;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedForSave;
    }
    private void OnSceneLoadedForSave(Scene scene, LoadSceneMode mode)
    {
        SaveGame();
    }
    #endregion
    #region Scene Management
    public void LoadScene(string sceneName)
    {
        LoadingScreenManager.nextScene = sceneName;
        SceneManager.LoadScene("LoadingScreen");
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
        if(bosses.Contains(boss)) bosses.Remove(boss);
    }
    #endregion
    #region Boss Tracking
    public void MarkBossDefeated(string bossName)
    {
        Debug.Log("Boss defeated: " +  bossName);
        if (!currentSave.defeatedBosses.Contains(bossName))
        {
            currentSave.defeatedBosses.Add(bossName);
        }
        
        if(bossName == "Bigfoot" || bossName == "Mothman")
        {
            LoadScene("HubArea");
            return;
        }
        CheckVictoryCondition();
    }
    private void CheckVictoryCondition()
    {
        string[] bossRoster = new string[] { "Bigfoot", "Mothman", "Wendigo" };
        foreach(string boss in bossRoster)
        {
            if (!currentSave.defeatedBosses.Contains(boss)) return;
        }
        Victory();
    }
    public void Victory()
    {
        isVictoryScene = true;
        SaveGame();
        if (player != null) Destroy(GameManager.instance.player);
        SceneManager.LoadScene("VictoryScene");
        GameManager.instance?.mouseVisibility();
    }
    #endregion
}