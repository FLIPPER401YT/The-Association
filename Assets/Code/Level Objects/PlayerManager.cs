using UnityEngine;

[System.Serializable]
public class PlayerManager
{
    public int hp, hpMax;
    public PlayerController controller;
    public PlayerManager()
    {
        hp = 100;
        hpMax = 100;
    }
    public PlayerManager(int hp, int hpMax)
    {
        this.hp = hp;
        this.hpMax = hpMax;
    }
}
