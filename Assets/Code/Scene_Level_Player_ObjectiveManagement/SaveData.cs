using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int health, healthMax, bloodSamples;
    public Dictionary<string, bool> bossesDefeated = new Dictionary<string, bool>();
}

public class Objectives : MonoBehaviour
{
    public string bossID;
    private void OnDestroy()
    {
        if(LevelManager.Instance != null)
        {

        }
    }
}