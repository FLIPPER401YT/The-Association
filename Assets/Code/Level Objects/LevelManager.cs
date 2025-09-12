using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance {  get; private set; }
    public PlayerManager playerData = new PlayerManager();
    private List<GameObject> objects = new List<GameObject>();
    public delegate void OnAllObjectsDestroyed();
    public event OnAllObjectsDestroyed ObjectsDestroyed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #region Persistence
    public void SaveGame()
    {
        PlayerPrefs.SetInt("Health", playerData.hp);
        PlayerPrefs.Save();
    }
    public void LoadGame() {
        playerData.hp = PlayerPrefs.GetInt("Health", 100);
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
            if (objects.Count == 0) ObjectsDestroyed?.Invoke();
        }
    }
    #endregion

    #region WinCondition
    public void Victory()
    {
        SceneManager.LoadScene("Victory Scene");
    }
    #endregion
}
