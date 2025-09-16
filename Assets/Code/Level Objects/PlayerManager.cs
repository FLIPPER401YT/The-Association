using UnityEngine;

[System.Serializable]
public class PlayerManager
{
    public int hp, hpMax, bloodSample;
    public PlayerController controller;
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
