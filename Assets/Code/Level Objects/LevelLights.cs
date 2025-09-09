using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelLightEvents : MonoBehaviour
{
    [SerializeField] Light lightObject;
    [SerializeField] Color color1, color2;
    [SerializeField] bool isIntensityChanging, isColorChanging, isRandChanging;
    [SerializeField] float lightIntensitySpeed, lightColorSpeed;
    float gameTimer, randInterval, randTimer;

    void Start()
    {
        lightObject = GetComponent<Light>();
        gameTimer = Time.time;
    }

    void Update()
    {
        randInterval += Time.deltaTime;
        if (isRandChanging && randTimer >= randInterval)
        {
            lightIntensitySpeed = Random.Range(0f, 1f);
            lightColorSpeed = Random.Range(1f, 5f);
        }
        if (isIntensityChanging)
        {
            lightObject.intensity = (Mathf.Sin((Time.time - gameTimer) * lightIntensitySpeed) + 1f) * 0.5f;
        }
        if (isColorChanging)
        {
            float timer = (Mathf.Sin((Time.time - gameTimer) * lightColorSpeed) + 1f) * 0.5f;
            lightObject.color = Color.Lerp(color1, color2, timer);
        }
    }
}
