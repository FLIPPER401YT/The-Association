using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [SerializeField] private string bossName;
    [SerializeField] private GameObject bossPrefab;
    private void Start()
    {
        LevelManager.Instance?.RegisterTrackable(bossPrefab);
    }
    private void OnDestroy()
    {
        Debug.Log("Boss defeated: " +  bossName);
        if(LevelManager.Instance != null)
        {
            LevelManager.Instance.UnregisterTrackable(bossPrefab);
            if (!string.IsNullOrEmpty(bossName)) LevelManager.Instance.MarkBossDefeated(bossName);
        }
    }
}
