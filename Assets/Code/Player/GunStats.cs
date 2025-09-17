using UnityEngine;

[CreateAssetMenu(fileName = "GunStats", menuName = "Scriptable Objects/GunStats")]
public class GunStats : ScriptableObject
{
    public int damage;
    public float fireRate;
    public float fireDistance;
    public float bloomMod;
    public int bullets;
    public int ammo;
    public int clipSize;
    public int clip;
    public int maxAmmo;
    public bool isAutomatic;
    public GameObject model;
    public AudioClip shootSound;
    public AudioClip shootNoAmmoSound;
    public AudioClip reloadSound;
    public AnimationClip reloadAnimation;
    public AnimationClip shootAnimation;
    public AnimationClip shootNoAmmoAnimation;
}
