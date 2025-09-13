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
    [SerializeField] AudioClip deathSound;

    public PlayerShoot shoot;
    public AudioSource audioSource;

    int healthMax;
    int bloodSamples;
    bool canMove = true;

    private Rigidbody rigidBody;
    [SerializeField] private Transform spawnPoint;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        rigidBody = GetComponent<Rigidbody>();
        if(GameManager.instance != null && GameManager.instance.spawnPoint != null) spawnPoint = GameManager.instance.spawnPoint.transform;

        healthMax = health;
        if (LevelManager.Instance != null)
        {
            var data = LevelManager.Instance.playerData;
            health = data.hp;
            healthMax = data.hpMax;
        }
        SpawnPlayer();
        updatePlayerHealthBarUI();
    }

    void FixedUpdate()
    {
        if (!statusEffects.IsStunned && !statusEffects.isKnockingBack)
        {
            movement.Movement();
            dash.Dash();
        }
    }

    void Update()
    {
        canMove = !statusEffects.IsStunned;
        updatePlayerHealthBarUI();

        if (Input.GetButtonDown("KnockbackDebug")) statusEffects.ApplyKnockback(transform.position + new Vector3(0, 0, 2), 5f);

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
        if (enabled)
        {
            health -= damage;
            if (LevelManager.Instance != null) LevelManager.Instance.playerData.hp = health;
            StartCoroutine(damageScreenEffect());
            updatePlayerHealthBarUI();

            if (health <= 0)
            {
                anim.enabled = true;
                audioSource.PlayOneShot(deathSound);
                anim.SetTrigger("Death");
                enabled = false;
            }
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

    public void PickupBloodSample(int amount)
    {
        bloodSamples += amount;
    }
    public void SpawnPlayer()
    {
        health = healthMax;
        updatePlayerHealthBarUI();
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        enabled = true;
    }
}
