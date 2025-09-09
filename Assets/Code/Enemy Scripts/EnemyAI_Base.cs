using UnityEngine;
using System.Collections;
using UnityEditor.Build.Content;

[RequireComponent(typeof(Collider))]
public class EnemyAI_Base : MonoBehaviour //,IDamage
{ 
    [Header("Visuals & Stats")]
    [SerializeField] protected Renderer model;
    [SerializeField] public int HP;

    [Header("Perception")]
    [SerializeField] protected int faceTargetSpeed;
    [SerializeField] protected int FOV;

    [Header("Movement")]
    [SerializeField] protected EnemyMovmentBaseRB mover;

    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        [Range(0, 1)] public float chance = 1;
        public int minCount;
        public int maxCount;
    }

    [Header("Drops")]
    [SerializeField] bool enableDrops = true;
    [SerializeField] DropItem[] drops;

    //public EnemyStats enemyStats;
    protected Transform player;
    protected Color colorOrig;

    protected bool playerInTrigger;
    protected Vector3 playerDir;
    protected float angleToPlayer;
    protected bool aggro;

    void Start()
    {
        player = GameManager.instance.player.transform;
        colorOrig = model.material.color;

        //GameManager.instance.updateGameGoal(1);

        if (!mover) mover = GetComponent<EnemyMovementBaseRB>();
        if (!mover)
        {
            Debug.LogError($"{name}: Missing EnemyMovementBaseRB component for Rigidbody movement.");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected bool CanSeePlayer()
    {
        if(!player)return false;

        Vector3 toPlayer = player.position - transform.position;
        float angle = Vector3.Angle(toPlayer, transform.forward);
        if (angle > FOV) return false;

        if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hit))
            return hit.collider.CompareTag("Player");

        return false;
    }

    public void FaceTarget()
    {
        if (playerDir.sqrMagnitude < 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(playerDir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * faceTargetSpeed);
    }

    public void TakeDamage(int amount)
    {
        if (HP > 0)
        {
            HP -= amount;

            aggro = true;
            if (mover && player) mover.SetTarget(player);

            StartCoroutine(flashRed());
        }

        if (HP <= 0)
        {
            //GameManger.instance.updateGameGoal(-1);

            if (enableDrops) DropLoot();

            Destroy(gameObject);
        }

    }

    IEnumerator flashRed()
    {
        if(!model) yield break;

        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;

    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("player"))
        {
            playerInTrigger = true;
            aggro = true;
        }
    }


}
