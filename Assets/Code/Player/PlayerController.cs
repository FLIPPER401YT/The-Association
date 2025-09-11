using System.Collections;
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

        StartCoroutine(damageScreenEffect());

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

    IEnumerator damageScreenEffect()
    {
        GameManager.instance.playerDamageEffect.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        GameManager.instance.playerDamageEffect.SetActive(false);
    }
}
