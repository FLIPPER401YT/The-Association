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
    [SerializeField] StatusEffects statusEffects;

    public PlayerShoot shoot;

    int healthMax;

    void Start()
    {
        healthMax = health;
        updatePlayerHealthBarUI();
    }

    void FixedUpdate()
    {
        if (!statusEffects.IsStunned)
        {
            movement.Movement();
            dash.Dash();
        }
    }

    void Update()
    {
        updatePlayerHealthBarUI();

        if (!statusEffects.IsStunned)
        {
            jump.Jump();
            crouch.Crouch();
            shoot.Shoot();
            shoot.Reload();
        }

        GameManager.instance.playerHealthText.text = health.ToString("F0");
        GameManager.instance.playerHealthMaxText.text = healthMax.ToString("F0");
    }

    public void Heal(int amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, healthMax);
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
