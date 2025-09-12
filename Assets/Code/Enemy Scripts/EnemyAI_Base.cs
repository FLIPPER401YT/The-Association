using UnityEngine;
using System.Collections;


[RequireComponent(typeof(Collider))]
public class EnemyAI_Base : MonoBehaviour, IDamage
{ 
    [Header("Visuals & Stats")]
    [SerializeField] protected Renderer model;
    [SerializeField] public int HP;

    [Header("Perception")]
    [SerializeField] protected int faceTargetSpeed;
    [SerializeField] protected int FOV;
    [SerializeField] protected float viewDistance;
    [SerializeField] protected float eyeHeight;
    [SerializeField] protected LayerMask losMask;
    [SerializeField] protected float aggroMemorySeconds;

    [Header("Movement")]
    [SerializeField] protected EnemyMovementBaseRB mover;

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
    protected float aggroTimer;

    protected virtual void Start()
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
    protected virtual void Update()
    {
        if (!player) return;

        bool sees = CanSeePlayer();

        // refresh memory if we see the player or they are in our close-range trigger
        if (sees || playerInTrigger)
            aggroTimer = aggroMemorySeconds;

        // tick memory
        if (aggroTimer > 0f)
            aggroTimer -= Time.deltaTime;

        // aggro if: in trigger OR currently see player OR memory still active
        aggro = playerInTrigger || sees || (aggroTimer > 0f);

        if (mover) mover.SetTarget(aggro ? player : null);

        if (aggro)
        {
            playerDir = player.position - transform.position;
            FaceTarget();
        }

    }

    protected bool CanSeePlayer()
    {
        if (!player) return false;

        // distance gate
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position - eyePos;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;

        // FOV gate (flattened so vertical doesn�t matter)
        Vector3 flatTo = new Vector3(toPlayer.x, 0f, toPlayer.z);
        Vector3 flatFwd = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (flatTo.sqrMagnitude < 0.0001f) return true;
        float angle = Vector3.Angle(flatFwd.normalized, flatTo.normalized);
        if (angle > FOV) return false;

        // LOS raycast
        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, viewDistance, losMask, QueryTriggerInteraction.Ignore))
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

            aggroTimer = aggroMemorySeconds;

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
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            Debug.Log($"{name}: Player ENTER trigger");
            aggroTimer = aggroMemorySeconds;
            aggro = true;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            Debug.Log($"{name}: Player EXIT trigger");
        }
    }

    private void DropLoot()
    {
        if (drops == null) return;
        
        foreach(DropItem item in drops)
        {
            if(item == null || item.prefab == null) continue;
            if(Random.value > item.chance) continue;

            int count = Random.Range(item.minCount, item.maxCount + 1);
            for(int i = 0; i < count; i++)
            {
                Vector3 pos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
                Instantiate(item.prefab, pos, Quaternion.identity);
            }
        }
            
       
    }


}
