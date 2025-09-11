using System.Data;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamage
{
    [SerializeField] int health;
    [SerializeField] PlayerJump jump;
    [SerializeField] PlayerCrouch crouch;
    [SerializeField] PlayerMovement movement;
    [SerializeField] PlayerDash dash;
    [SerializeField] PlayerShoot shoot;

    int healthMax;

    void Start()
    {
        healthMax = health;
        updatePlayerHealthBarUI();
    }

    void FixedUpdate()
    {
        movement.Movement();
        dash.Dash();
    }

    void Update()
    {
        updatePlayerHealthBarUI();
        jump.Jump();
        crouch.Crouch();
        shoot.Shoot();
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Player Dies
            Destroy(gameObject);
        }
    }

    public void updatePlayerHealthBarUI()
    {
        GameManager.instance.playerHealthBar.fillAmount = (float)health / healthMax;
    }
}
