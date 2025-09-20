using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class LoadingScreenManager : MonoBehaviour
{
    public static string nextScene;
    public GameObject loadingUI;
    private AsyncOperation asyncLoad;

    private void Start()
    {
        loadingUI.SetActive(true);
        StartCoroutine(LoadingAsync());
    }
    private IEnumerator LoadingAsync()
    {
        asyncLoad = SceneManager.LoadSceneAsync(nextScene);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}