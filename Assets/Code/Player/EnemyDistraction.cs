using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class EnemyDistraction : MonoBehaviour
{
    [SerializeField] private UnityEvent onCollide = new UnityEvent();
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private bool destroyedCollision = false;

    private void OnCollisionEnter(Collision collision)
    {
        if(collisionLayer == (collisionLayer | (1 << collision.gameObject.layer)))
        {
            onCollide?.Invoke();
            if (destroyedCollision) Destroy(this);
        }
    }
    //public void MakeASound(float range)
    //{
    //    var sound = new Sound(transform.position, range);
    //    sound.soundType = Sound.SoundType.Rock;
    //    Sounds.MakeSound(sound);
    //}
}
