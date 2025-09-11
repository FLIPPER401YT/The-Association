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
    [SerializeField] StatusEffects statusEffects;

    int healthMax;

    void Start()
    {
        healthMax = health;
        updatePlayerHealthBarUI();
    }

    void FixedUpdate()
    {
        if (statusEffects.IsStunned)
        {
            movement.Movement();
            dash.Dash();
        }
    }

    void Update()
    {
        updatePlayerHealthBarUI();
        
        if (statusEffects.IsStunned)
        {
            jump.Jump();
            crouch.Crouch();
            shoot.Shoot();
        }

        GameManager.instance.playerHealthText.text = health.ToString("F0");
        GameManager.instance.playerHealthMaxText.text = healthMax.ToString("F0");
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
