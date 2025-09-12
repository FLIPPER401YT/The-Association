using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LevelManager.Instance.RegisterTrackable(gameObject);
    }

    // Update is called once per frame
    void OnDestroy()
    {
        if(LevelManager.Instance != null) LevelManager.Instance.UnregisterTrackable(gameObject);
    }
}
