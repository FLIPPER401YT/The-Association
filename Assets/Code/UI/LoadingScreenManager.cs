using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class LoadingScreenManager : MonoBehaviour
{
    private float loadDelay = 2f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator LoadAsynScene(string sceneName)
    {
        yield return new WaitForSeconds(loadDelay);
        AsyncOperation asynLoad = SceneManager.LoadSceneAsync(sceneName);
        asynLoad.allowSceneActivation = false;
        while (!asynLoad.isDone)
        {
            yield return null;
        }

    }
}
