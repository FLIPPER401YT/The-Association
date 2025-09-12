using System.Collections;
using UnityEngine;

public class LightEvent : MonoBehaviour
{
    Light lightController;
    [SerializeField] GameObject lightObject;
    [SerializeField] Color lightColor;

    void Start()
    {
        LightSettings();
    }

    void Update()
    {
        Flashlight();
    }
    void Flashlight()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (lightController.isActiveAndEnabled)
            {
                lightController.enabled = false;
                lightObject.SetActive(false);
            }
            else
            {
                lightController.enabled = true;
                lightObject.SetActive(true);
            }
        }
    }
    void LightSettings()
    {
        lightController = GetComponent<Light>();
    }
}