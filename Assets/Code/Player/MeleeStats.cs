using UnityEngine;

[CreateAssetMenu(fileName = "MeleeStats", menuName = "Scriptable Objects/MeleeStats")]
public class MeleeStats : ScriptableObject
{
    public int damage;
    public float swingRate;
    public float swingDistance;
    public GameObject model;
    public AnimationClip equipWeapon;
    public AnimationClip unequipWeapon;
    public AudioClip attackSound;
    public AudioClip heavyAttackSound;
    public AudioClip equipSound;
    public Vector3 scale;
}
