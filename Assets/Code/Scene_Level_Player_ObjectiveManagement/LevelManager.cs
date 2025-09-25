using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
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
        public int health = 100;
        public int healthMax = 100;
        public int bloodSamples = 0;
        public List<int> clip = new List<int>();
        public List<int> ammo = new List<int>();
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

        public SaveData()
        {

        }

        public SaveData(SaveData data)
        {
            health = data.health;
            healthMax = data.health;
            clip = new List<int>(data.clip);
            ammo = new List<int>(data.ammo);
            defeatedBosses = new List<string>(data.defeatedBosses);
        }
    }
    public SaveData currentSave = new SaveData();
    public SaveData sceneStartSave = new SaveData();
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
        ResetSave();
        player = GameManager.instance?.playerScript;
        if (player != null)
        {
            foreach (GunStats stat in player.shoot.gunList) player.shoot.FillAmmo(stat);
            player.health = player.healthMax;
            player.bloodSamples = 0;
            player.SavePlayerStats();
            player.updatePlayerHealthBarUI();
            player.UpdateSampleCount(player.bloodSamples);
        }
        SetStartSaveToCurrent();
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
        if (player != null && spawnPoint != null)
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
        if (!player) player = GameManager.instance?.playerScript;
        if (player != null)
        {
            if (!player.resetting)
            {
                SetPlayerStats(currentSave);
                SetStartSaveToCurrent();
            }
            else
            {
                SetPlayerStats(sceneStartSave);
                currentSave = new SaveData(sceneStartSave);
                player.resetting = false;
            }
        }
    }

    void SetPlayerStats(SaveData save)
    {
        player.health = save.health;
        player.healthMax = save.healthMax;
        player.bloodSamples = save.bloodSamples;
        player.updatePlayerHealthBarUI();
        player.UpdateSampleCount(player.bloodSamples);
        for (int gunIndex = 0; gunIndex < player.shoot.gunList.Count; gunIndex++)
        {
            if (gunIndex < save.ammo.Count)
            {
                player.shoot.gunList[gunIndex].ammo = save.ammo[gunIndex];
                player.shoot.gunList[gunIndex].clip = save.clip[gunIndex];
            }
        }
    }

    void SetStartSaveToCurrent()
    {
        // sceneStartSave.health = currentSave.health;
        // sceneStartSave.healthMax = currentSave.healthMax;
        // sceneStartSave.bloodSamples = currentSave.bloodSamples;
        // sceneStartSave.clip = new List<int>(currentSave.clip);
        // sceneStartSave.ammo = new List<int>(currentSave.ammo);
        sceneStartSave = new SaveData(currentSave);
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
        PlayerPrefs.SetInt("GunsCount", currentSave.ammo.Count);
        for (int index = 0; index < currentSave.ammo.Count; index++)
        {
            PlayerPrefs.SetInt("GunAmmo_" + index, currentSave.ammo[index]);
            PlayerPrefs.SetInt("GunClip_" + index, currentSave.clip[index]);
        }
        PlayerPrefs.Save();
    }
    public void LoadGame()
    {
        currentSave = new SaveData();
        if (PlayerPrefs.HasKey("Health")) currentSave.health = PlayerPrefs.GetInt("Health");
        if (PlayerPrefs.HasKey("HealthMax")) currentSave.healthMax = PlayerPrefs.GetInt("HealthMax");
        if (PlayerPrefs.HasKey("BloodSamples")) currentSave.bloodSamples = PlayerPrefs.GetInt("BloodSamples");
        int count = PlayerPrefs.GetInt("DefeatedBossesCount", 0);
        for (int index = 0; index < count; index++)
        {
            string bossName = PlayerPrefs.GetString("DefeatedBoss_" + index, "");
            if (!string.IsNullOrEmpty(bossName)) currentSave.defeatedBosses.Add(bossName);
        }
        currentSave.ammo.Clear();
        currentSave.clip.Clear();
        for (int index = 0; index < PlayerPrefs.GetInt("GunsCount"); index++)
        {
            currentSave.ammo.Add(PlayerPrefs.GetInt("GunAmmo_" + index));
            currentSave.clip.Add(PlayerPrefs.GetInt("GunClip_" + index));
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
    public void ResetSave()
    {
        currentSave = new SaveData();
        sceneStartSave = new SaveData();
        SaveGame();
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
        BossesDestroyed?.Invoke();
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