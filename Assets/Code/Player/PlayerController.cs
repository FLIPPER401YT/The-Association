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
    [SerializeField] Animator anim;

    public PlayerShoot shoot;
    public AudioSource audioSource;

    int healthMax;
    bool canMove = true;

    void Start()
    {
        healthMax = health;
        if (LevelManager.Instance != null)
        {
            var data = LevelManager.Instance.playerData;
            health = data.hp;
            healthMax = data.hpMax;
        }
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
        canMove = !statusEffects.IsStunned;
        updatePlayerHealthBarUI();

        if (Input.GetButtonDown("KnockbackDebug")) statusEffects.ApplyKnockback(transform.position + new Vector3(0, 0, 2), 1);

        if (canMove)
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
        if (LevelManager.Instance != null) LevelManager.Instance.playerData.hp = health;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (LevelManager.Instance != null) LevelManager.Instance.playerData.hp = health;
        StartCoroutine(damageScreenEffect());

        if (health <= 0)
        {
            anim.enabled = true;
            anim.SetTrigger("Death");
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
    
    public void Lose()
    {
        GameManager.instance.Lose();
    }
}
