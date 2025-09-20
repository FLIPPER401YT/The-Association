using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class LoadingScreenManager : MonoBehaviour
{ 
    public static LoadingScreenManager instance;
    public GameObject loadingScreenObject;

    void Awake()
    {
        //if (instance != null && instance != this)
        //{
        //    Destroy(this.gameObject);
        //}
        //else
        //{
        //    instance = this;
        //    DontDestroyOnLoad(this.gameObject);
        //}
        instance = this;
    }

    //public void swapToScene(string name)
    //{
    //    loadingScreenObject.SetActive(true);
    //    StartCoroutine(SwapToSceneAsync(name));
    //}

    //IEnumerator SwapToSceneAsync(string name)
    //{
    //    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name);

    //    while(!asyncLoad.isDone)
    //    {
    //        yield return null;
    //    }
    //    yield return new WaitForSeconds(0.2f);
    //    loadingScreenObject.SetActive(false);
    //}

}