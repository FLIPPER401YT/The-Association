using UnityEngine;
using UnityEngine.SceneManagement;

public class ShowcaseDebug : MonoBehaviour
{
    void Update()
    {
        if (Input.GetButtonDown("Showcase"))
        {
            SceneManager.LoadScene("Showcase");
        }
    }
}
