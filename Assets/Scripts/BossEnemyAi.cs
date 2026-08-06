using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossEnemyAi : MonoBehaviour
{
    public enum EnemyState { Idle, Chasing, Attacking, Hit }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Targeting & Ranges")]
    public Transform player;
    public float triggerRadius = 25f; // Boss ka trigger area bada hona chahiye
    public float attackRange = 3.5f;  // Boss ka attack range thoda bada ho

    [Header("Combat Stats")]
    public float attackCooldown = 2.0f; // Boss ka cooldown thoda jyada
    private float lastAttackTime;
    public int maxHealth = 3000; // Boss ki health jyada hogi
    private int currentHealth;
    public Slider healthBarSlider;

    private NavMeshAgent agent;
    private bool isPlayerInArena = false; 

    [Header("Animation")]
    public Animator animator;
    
    [Header("Audio")]
    public AudioSource bossAudio;
    public AudioClip roarSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip footstepSound;
    private float footstepTimer;
    private bool hasRoared = false;

    // Procedural Arena
    private GameObject proceduralArena;
    public bool isDead = false;

    // AAA Perf: Pre-computed squared radii — eliminates sqrt every frame
    private float sqrTriggerRadius;
    private float sqrAttackRange;

    // AAA Perf: NavMesh destination throttle — only update when player moves >1.5m
    private Vector3 lastSetDestination = Vector3.positiveInfinity;
    private const float SqrDestinationThreshold = 2.25f; // 1.5m * 1.5m

    void OnEnable()
    {
        if (!isDead)
            EnemyRegistry.Register(transform);
    }

    void OnDisable()
    {
        EnemyRegistry.Unregister(transform);
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        
        if (healthBarSlider == null)
        {
            healthBarSlider = GetComponentInChildren<Slider>();
        }

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        animator = GetComponent<Animator>(); 
        if (animator == null) 
        {
            animator = GetComponentInChildren<Animator>(); 
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (!animator.enabled) animator.enabled = true;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // AAA Perf: Pre-compute squared radii once — avoid sqrt every frame
        sqrTriggerRadius = triggerRadius * triggerRadius;
        sqrAttackRange   = attackRange   * attackRange;
    }

    void Update()
    {
        if (isDead || player == null || currentHealth <= 0) return;

        // AAA Perf: sqrMagnitude instead of Distance (no sqrt = ~3x faster)
        float sqrDist = (transform.position - player.position).sqrMagnitude;

        if (!isPlayerInArena && sqrDist <= sqrTriggerRadius)
        {
            isPlayerInArena = true;
            PlaySound(roarSound);
            hasRoared = true;
            SpawnProceduralArena(); 
        }

        // State change logic
        if (sqrDist <= sqrAttackRange)
        {
            currentState = EnemyState.Attacking;
        }
        else if (sqrDist <= sqrTriggerRadius)
        {
            currentState = EnemyState.Chasing;
            if (!hasRoared)
            {
                PlaySound(roarSound);
                hasRoared = true;
            }
        }
        else
        {
            currentState = EnemyState.Idle; 
            hasRoared = false;
        }

        // Animation logic based on BossEnemy.controller parameters
        if (animator != null)
        {
            float targetSpeed = 0f;
            bool isAlert = false;
            
            if (currentState == EnemyState.Idle)
            {
                targetSpeed = 0f; 
                isAlert = false; 
                agent.speed = 0f; 
                lastAttackTime = 0f; 
            }
            else if (currentState == EnemyState.Chasing)
            {
                targetSpeed = 1.0f; // Boss run speed
                isAlert = true; 
                agent.speed = 7.5f; 
            }
            else if (currentState == EnemyState.Attacking)
            {
                targetSpeed = 0f; 
                isAlert = true;
                agent.speed = 0f; 
            }

            // AAA Perf: Animator built-in dampTime — eliminates manual GetFloat call every frame
            animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("IsAlert", isAlert);
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Chasing:
                ChasePlayer();
                break;
            case EnemyState.Attacking:
                AttackPlayer();
                break;
        }

        // Footsteps logic
        if (agent.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlaySound(footstepSound);
                footstepTimer = 0.5f; // Boss footstep is slower and heavier
            }
        }
    }

    void Idle()
    {
        agent.isStopped = true;
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        // AAA Perf: Only update NavMesh path when player moves >1.5m — saves pathfinding cost
        float sqrDelta = (player.position - lastSetDestination).sqrMagnitude;
        if (sqrDelta > SqrDestinationThreshold)
        {
            agent.SetDestination(player.position);
            lastSetDestination = player.position;
        }
    }

    void AttackPlayer()
    {
        agent.isStopped = true; 

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; 
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (animator != null)
            {
                // BossEnemy controller has Attack1, Attack2, JumpAttack triggers
                int randomAttack = Random.Range(0, 3);
                
                if (randomAttack == 0) animator.SetTrigger("Attack1");
                else if (randomAttack == 1) animator.SetTrigger("Attack2");
                else animator.SetTrigger("JumpAttack");

                PlaySound(attackSound);
            }
            
            StartCoroutine(DealDamageToPlayer()); 
            lastAttackTime = Time.time;
        }
    }

    private System.Collections.IEnumerator DealDamageToPlayer()
    {
        yield return new WaitForSeconds(0.6f); // Boss ka attack thoda slow ho sakta hai
        
        if (isDead || player == null) yield break;

        float sqrDist = (transform.position - player.position).sqrMagnitude;
        
        if (sqrDist <= (attackRange + 1.5f) * (attackRange + 1.5f))
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(100f); // Boss bhi sirf 100 damage dega ab
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
        
        if (currentHealth > 0)
        {
            PlaySound(hitSound);
            StartCoroutine(HitRecovery());
        }
        else
        {
            PlaySound(deathSound);
            Die();
        }
    }

    private System.Collections.IEnumerator HitRecovery()
    {
        currentState = EnemyState.Hit; 
        agent.isStopped = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Hit"); // Using Hit trigger found in controller
        }

        yield return new WaitForSeconds(0.8f); 
        
        if (!isDead)
        {
            agent.isStopped = false;
            currentState = EnemyState.Chasing; 
        }
    }

    void Die()
    {
        isDead = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Death"); // Using Death trigger found in controller
        }
        
        if (proceduralArena != null)
        {
            Destroy(proceduralArena);
        }
        
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        agent.enabled = false; 
        
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) 
            {
                pc.SetLampFreeze(false);
                pc.SetCombatState(false);
            }
        }
        
        StartCoroutine(ShowFinishUIAfterDeath());
        Destroy(gameObject, 8f); // Boss body thodi der tak rukegi
    }

    private System.Collections.IEnumerator ShowFinishUIAfterDeath()
    {
        yield return new WaitForSeconds(4.5f); // Wait for death animation to finish
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.GameFinished();
        }
    }

    void SpawnProceduralArena()
    {
        if (proceduralArena != null) return;

        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) 
            {
                pc.SetLampFreeze(true);
                pc.SetCombatState(true);
            }
        }

        Vector3 centerPosition = (transform.position + player.position) / 2f;
        centerPosition.y = transform.position.y;

        proceduralArena = new GameObject("Epic_Arena_Ring_Boss_" + gameObject.name);
        proceduralArena.transform.position = centerPosition;

        float arenaRadius = triggerRadius;

        LineRenderer line = proceduralArena.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.startWidth = 0.6f; // Boss ki deewar thodi moti
        line.endWidth = 0.6f;
        line.positionCount = 51;
        line.loop = true;
        
        Material purpleMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        purpleMat.color = new Color(0.5f, 0f, 1f, 0.7f); // Boss ke liye Purple/Dark color arena
        line.material = purpleMat;

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
            GameObject wall = new GameObject("BossWallSegment");
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // --- AUDIO HELPER ---
    public void PlaySound(AudioClip clip)
    {
        if (bossAudio != null && clip != null)
        {
            bossAudio.PlayOneShot(clip);
        }
    }
}
