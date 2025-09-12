using UnityEngine;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    private bool triggered = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            LevelManager.Instance.LoadScene(sceneToLoad);
        }
    }
}
