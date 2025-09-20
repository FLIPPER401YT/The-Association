using System.Collections;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour, IDamage
{
    public static PlayerController instance;

    [SerializeField] public int health;
    [SerializeField] int lastBitOfLifeDamageAmount;
    [SerializeField] PlayerJump jump;
    [SerializeField] public PlayerCrouch crouch;
    [SerializeField] PlayerMovement movement;
    [SerializeField] public PlayerDash dash;
    [SerializeField] StatusEffects statusEffects;
    [SerializeField] public Animator anim;
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioClip takeDamageSound;
    [SerializeField] AnimationClip deathClip;

    public int healthMax;
    public int bloodSamples;
    public bool lastBitOfLifeDamageTaken = false;
    public PlayerShoot shoot;
    public AudioSource audioSource;

    bool canMove = true;

    private Rigidbody rigidBody;
    [SerializeField] public Transform spawnPoint;
    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != null && instance != this) Destroy(gameObject);
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        rigidBody = GetComponent<Rigidbody>();
        if (GameManager.instance != null && GameManager.instance.spawnPoint != null) spawnPoint = GameManager.instance.spawnPoint.transform;

        healthMax = health;
        if (LevelManager.Instance != null)
        {
            var data = LevelManager.Instance.currentSave;
            health = data.health;
            healthMax = data.healthMax;
        }
        SpawnPlayer();
        updatePlayerHealthBarUI();
        UpdateSampleCount(bloodSamples);
    }

    void FixedUpdate()
    {
        if (!statusEffects.IsStunned && !statusEffects.isKnockingBack)
        {
            movement.Movement();
        }
    }

    void Update()
    {
        canMove = !statusEffects.IsStunned;
        updatePlayerHealthBarUI();

        if (Input.GetButtonDown("KnockbackDebug")) statusEffects.ApplyKnockback(transform.position + new Vector3(0, 0, 2), 5f);

        if (canMove)
        {
            dash.Dash();
            jump.Jump();
            crouch.Crouch();
            if (!shoot.isReloading && !shoot.changingWeapons)
            {
                shoot.Shoot();
                shoot.Reload();
            }
        }

        GameManager.instance.playerHealthText.text = health.ToString("F0");
        GameManager.instance.playerHealthMaxText.text = healthMax.ToString("F0");


    }

    public void Heal(int amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, healthMax);
        lastBitOfLifeDamageTaken = false;
        SavePlayerStats();
    }

    public void TakeDamage(int damage)
    {
        if (enabled)
        {
            if (!lastBitOfLifeDamageTaken && damage >= lastBitOfLifeDamageAmount && health - damage <= 0)
            {
                health = 1;
                lastBitOfLifeDamageTaken = true;
            }
            else health -= damage;
            SavePlayerStats();
            StartCoroutine(damageScreenEffect());
            updatePlayerHealthBarUI();

            if (health > 0)
            {
                audioSource.PlayOneShot(takeDamageSound);
            }
            else if (health <= 0)
            {
                anim.enabled = true;
                audioSource.PlayOneShot(deathSound);
                anim.SetTrigger("Death");
                StartCoroutine(Lose());
                enabled = false;
                lastBitOfLifeDamageTaken = false;
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

    IEnumerator Lose()
    {
        yield return new WaitForSeconds(deathClip.length);

        GameManager.instance.Lose();
    }

    public void PickupBloodSample(int amount)
    {
        bloodSamples += amount;
        UpdateSampleCount(bloodSamples);
        SavePlayerStats();
    }
    public void SpawnPlayer()
    {
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        enabled = true;
        lastBitOfLifeDamageTaken = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastBitOfLifeDamageTaken = false;
        if (GameManager.instance != null && GameManager.instance.spawnPoint != null) spawnPoint = GameManager.instance.spawnPoint.transform;
        if (LevelManager.Instance != null)
        {
            var data = LevelManager.Instance.currentSave;
            health = data.health;
            bloodSamples = data.bloodSamples;
            updatePlayerHealthBarUI();
            UpdateSampleCount(bloodSamples);
        }
        Time.timeScale = 1.0f;
        if (scene.name.Equals("MainMenu")) gameObject.SetActive(false);
        else gameObject.SetActive(true);
        GameManager.instance.player = gameObject;
        GameManager.instance.playerScript = this;
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        Debug.Log("Player destroyed: " + gameObject.name);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public void UpdateSampleCount(int count)
    {
        bloodSamples = count;
        GameManager.instance?.SampleCount(bloodSamples);
    }
    void SavePlayerStats()
    {
        if(LevelManager.Instance != null)
        {
            LevelManager.Instance.currentSave.health = health;
            LevelManager.Instance.currentSave.bloodSamples = bloodSamples;
            LevelManager.Instance.SaveGame();
        }
    }
}
