using UnityEngine;

public class PlayerController : MonoBehaviour, IDamage
{
    [SerializeField] float health;
    [SerializeField] PlayerJump jump;
    [SerializeField] PlayerCrouch crouch;
    [SerializeField] PlayerMovement movement;
    [SerializeField] PlayerDash dash;

    void FixedUpdate()
    {
        movement.Movement();
        dash.Dash();
    }

    void Update()
    {
        jump.Jump();
        crouch.Crouch();
    }

    public void TakeDamage(float damage)
    {
        
    }
}
