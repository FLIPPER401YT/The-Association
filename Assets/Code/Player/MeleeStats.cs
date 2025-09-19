using UnityEngine;

[CreateAssetMenu(fileName = "MeleeStats", menuName = "Scriptable Objects/MeleeStats")]
public class MeleeStats : ScriptableObject
{
    public int damage;
    public float swingRate;
    public float swingDistance;
    public GameObject model;
    public AudioClip attackSound;
    public AudioClip heavyAttackSound;
    public AudioClip equipSound;
}
