using UnityEngine;

[System.Serializable]
public class PlayerManager
{
    public int hp, hpMax;
    public PlayerController controller;
    public int bloodSample;
    public PlayerManager()
    {
        hp = 100;
        hpMax = 100;
        bloodSample = 0;
    }
    public PlayerManager(int hp, int hpMax, int sample)
    {
        this.hp = hp;
        this.hpMax = hpMax;
        this.bloodSample = sample;
    }
}
