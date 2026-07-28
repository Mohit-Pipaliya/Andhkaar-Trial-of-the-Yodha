using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAi : MonoBehaviour
{
    public enum EnemyState { Patrolling, Chasing, Telegraphing, Attacking, Hit }
    public EnemyState currentState = EnemyState.Patrolling;

    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentPatrolTarget;

    [Header("Vision & Hearing (AAA Features)")]
    public Transform player;
    [Tooltip("Kitni door tak dekh sakta hai")]
    public float viewRadius = 20f;
    [Tooltip("Aankhon ka angle (FOV). 110 degree matlab aage ki taraf thoda wide.")]
    [Range(0, 360)] public float viewAngle = 110f;
    [Tooltip("Agar player iske paas aawaz karta hai (ya bohot paas hai), to bina dekhe pata chal jayega")]
    public float hearingRadius = 5f;
    [Tooltip("Deewarein jo vision block karti hain. Default aur kuch select karein.")]
    public LayerMask obstacleMask;

    [Header("Combat Stats")]
    public float attackRange = 2.5f;
    public float attackCooldown = 2.0f;
    [Tooltip("Attack karne se pehle kitna time wait karega (Telegraphing)")]
    public float attackAnticipationTime = 0.5f;
    private float lastAttackTime;
    
    public int maxHealth = 1000;
    private int currentHealth;
    public Slider healthBarSlider;

    private NavMeshAgent agent;
    private bool isPlayerInArena = false;
    private bool playerSpotted = false;

    private float footstepTimer;

    [Header("Animation")]
    public Animator animator;
    
    // Smooth Blend targets
    private float currentAnimSpeed = 0f;

    // Procedural Arena
    private GameObject proceduralArena;
    public bool isDead = false;

    void Start()
    {
        maxHealth = 1000; 
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        
        if (healthBarSlider == null)
            healthBarSlider = GetComponentInChildren<Slider>();

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        SetupAnimator();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        currentPatrolTarget = pointA;
        SafeSetDestination(currentPatrolTarget != null ? currentPatrolTarget.position : transform.position);
    }

    private void SetupAnimator()
    {
        animator = GetComponent<Animator>(); 
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (!animator.enabled) animator.enabled = true;
        }
        else
        {
            Debug.LogError("Is enemy me Animator component nahi hai!");
        }
    }

    void Update()
    {
        if (isDead || player == null || currentHealth <= 0) return;

        CheckLineOfSight(); // Naya Vision system

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (playerSpotted && !isPlayerInArena)
        {
            isPlayerInArena = true;
            SpawnProceduralArena(); 
        }

        // Logic runs only if not in middle of attack animation or hit reaction
        if (currentState != EnemyState.Hit && currentState != EnemyState.Attacking && currentState != EnemyState.Telegraphing)
        {
            if (playerSpotted && distanceToPlayer <= attackRange)
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    StartCoroutine(TelegraphAndAttack());
                }
                else
                {
                    // Cooldown me paas hai to bas ghoorega (Face player)
                    FaceTarget(player.position);
                    currentState = EnemyState.Chasing; // Keeps him alert
                }
            }
            else if (playerSpotted)
            {
                currentState = EnemyState.Chasing;
            }
            else
            {
                currentState = EnemyState.Patrolling;
            }
        }

        UpdateMovementAndAnimation();
    }

    // --- VISION SYSTEM (AAA Style) ---
    void CheckLineOfSight()
    {
        if (playerSpotted) return; // Ek baar dekh liya to chhorega nahi (arena mode)

        float distance = Vector3.Distance(transform.position, player.position);

        // 1. Hearing (Bahut paas hai to sun lega)
        if (distance <= hearingRadius)
        {
            playerSpotted = true;
            return;
        }

        // 2. Vision (Aage ki taraf dekhega)
        if (distance <= viewRadius)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                // Raycast se check karo ki beech me koi deewar to nahi
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distance, obstacleMask))
                {
                    playerSpotted = true;
                }
            }
        }
    }

    void UpdateMovementAndAnimation()
    {
        float targetAnimSpeed = 0f;
        bool isAlert = false;
        float targetAgentSpeed = 0f;

        switch (currentState)
        {
            case EnemyState.Patrolling:
                targetAnimSpeed = 0.25f; 
                targetAgentSpeed = 3.5f;
                Patrol();
                break;
            case EnemyState.Chasing:
                targetAnimSpeed = 1.0f;
                targetAgentSpeed = 8.5f;
                isAlert = true;
                ChasePlayer();
                break;
            case EnemyState.Telegraphing:
            case EnemyState.Attacking:
            case EnemyState.Hit:
                targetAnimSpeed = 0f;
                targetAgentSpeed = 0f;
                isAlert = true;
                SafeStopAgent(true);
                break;
        }

        // Smooth Interpolation (Weight/Heaviness feel)
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = Mathf.Lerp(agent.speed, targetAgentSpeed, Time.deltaTime * 5f);
        }

        if (animator != null)
        {
            currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 5f);
            animator.SetFloat("Speed", currentAnimSpeed);
            animator.SetBool("IsAlert", isAlert);
        }
    }

    void Patrol()
    {
        if (pointA == null || pointB == null) return;

        SafeStopAgent(false);

        if (agent != null && agent.isOnNavMesh && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
            SafeSetDestination(currentPatrolTarget.position);
        }
    }

    void ChasePlayer()
    {
        // Attack range me nahi hai, to move karo
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange * 0.8f) // thoda buffer
        {
            SafeStopAgent(false);
            SafeSetDestination(player.position);
        }
        else
        {
            SafeStopAgent(true);
            FaceTarget(player.position); // Ghoom ke player ki taraf dekhega
        }
    }

    // --- TELEGRAPHING & ATTACK (AAA Combat) ---
    private IEnumerator TelegraphAndAttack()
    {
        currentState = EnemyState.Telegraphing;
        SafeStopAgent(true);

        // Telegraph Phase (Winding up)
        float t = 0;
        while (t < attackAnticipationTime)
        {
            if (isDead) yield break;
            FaceTarget(player.position); // Player ko ghoorta rahega attack karne se theek pehle
            t += Time.deltaTime;
            yield return null;
        }

        // Commit to attack (Rotation lock ho jayegi)
        currentState = EnemyState.Attacking;

        if (animator != null)
        {
            animator.speed = 1.2f; 
            animator.CrossFade("Attack1", 0.15f); // Smooth crossfade instead of snap
        }
        
        // Wait for sword to drop
        yield return new WaitForSeconds(0.4f);
        
        if (!isDead && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            // Sirf aage wale hit honge, peeche bhag gaya to damage nahi lagega
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

            if (distance <= attackRange + 1f && angleToPlayer < 90f) // Fair hitbox
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(100f);
            }
        }

        // Wait for attack animation to finish
        yield return new WaitForSeconds(0.8f);

        lastAttackTime = Time.time;

        if (!isDead && currentState != EnemyState.Hit)
        {
            currentState = EnemyState.Chasing; // Attack ke baad wapas chase/decide karega
        }
    }

    void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    // --- SAFE NAVMESH METHODS (Fixes Errors) ---
    void SafeStopAgent(bool stop)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = stop;
        }
    }

    void SafeSetDestination(Vector3 dest)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(dest);
        }
    }

    // --- HIT & DEATH ---
    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        playerSpotted = true; // Koi dur se mare to bhi dekh lega
        
        if (healthBarSlider != null) healthBarSlider.value = currentHealth;
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Attack cancel if hit
            StopAllCoroutines();
            StartCoroutine(HitRecovery());
        }
    }

    private IEnumerator HitRecovery()
    {
        currentState = EnemyState.Hit;
        SafeStopAgent(true);
        
        if (animator != null)
        {
            animator.speed = 1.0f;
            animator.CrossFade("Hit", 0.1f); 
        }

        yield return new WaitForSeconds(0.6f);
        
        if (!isDead)
        {
            currentState = EnemyState.Chasing;
        }
    }

    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        
        if (animator != null)
        {
            animator.speed = 1.0f;
            animator.CrossFade("Death", 0.2f);
        }
        
        if (proceduralArena != null) Destroy(proceduralArena);
        
        SafeStopAgent(true);
        if (agent != null) agent.enabled = false; 
        
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetLampFreeze(false);
        }
        
        Destroy(gameObject, 5f); 
    }

    void SpawnProceduralArena()
    {
        if (proceduralArena != null) return;
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetLampFreeze(true);
        }

        Vector3 centerPosition = (transform.position + player.position) / 2f;
        centerPosition.y = transform.position.y; 
        proceduralArena = new GameObject("Epic_Arena_Ring_" + gameObject.name);
        proceduralArena.transform.position = centerPosition;

        float arenaRadius = viewRadius > 15f ? 15f : viewRadius; // Thoda balance
        
        LineRenderer line = proceduralArena.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.startWidth = 0.4f;
        line.endWidth = 0.4f;
        line.positionCount = 51;
        line.loop = true;
        
        Material redMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        redMat.color = new Color(1f, 0f, 0f, 0.7f);
        line.material = redMat;

        float angle = 0f;
        for (int i = 0; i < 51; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * arenaRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * arenaRadius;
            line.SetPosition(i, new Vector3(x, 0.2f, z)); 
            angle += (360f / 50f);
        }

        int segments = 24; 
        angle = 0f;
        for (int i = 0; i < segments; i++)
        {
            GameObject wall = new GameObject("InvisibleWallSegment");
            wall.transform.SetParent(proceduralArena.transform);
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * arenaRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * arenaRadius;
            wall.transform.localPosition = new Vector3(x, 10f, z); 
            wall.transform.LookAt(proceduralArena.transform); 
            BoxCollider box = wall.AddComponent<BoxCollider>();
            float width = (arenaRadius * 2f * Mathf.PI) / segments;
            box.size = new Vector3(width + 1f, 25f, 1f); 
            angle += (360f / segments);
        }
    }

    // AAA Visual Debugging
    private void OnDrawGizmosSelected()
    {
        // View Radius
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        
        // Hearing Radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        // Vision Cone (Green lines)
        Gizmos.color = Color.green;
        Vector3 forward = transform.forward;
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * forward;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

        // Attack Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
