using System.Collections;
using UnityEngine;

public class LightEvent : MonoBehaviour
{
    Light lightController;
    [SerializeField] GameObject lightObject;
    [SerializeField] Color lightColor;
    [SerializeField] float lightRadius;
    Material lightMaterial;

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
        if (Input.GetKeyDown(KeyCode.E))
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
                lightMaterial.SetColor("_EmissionColor", lightColor);
            }
        }
    }
    void LightSettings()
    {
        lightController = GetComponent<Light>();
        lightMaterial = lightObject.GetComponent<Renderer>().material;
        lightController.innerSpotAngle = lightRadius;
    }
}