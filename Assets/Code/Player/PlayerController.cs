using System.Data;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamage
{
    [SerializeField] float health;
    [SerializeField] PlayerJump jump;
    [SerializeField] PlayerCrouch crouch;
    [SerializeField] PlayerMovement movement;
    [SerializeField] PlayerDash dash;
    [SerializeField] PlayerShoot shoot;

    void FixedUpdate()
    {
        movement.Movement();
        dash.Dash();
    }

    void Update()
    {
        jump.Jump();
        crouch.Crouch();
        shoot.Shoot();
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Player Dies
            Destroy(gameObject);
        }
    }
}
