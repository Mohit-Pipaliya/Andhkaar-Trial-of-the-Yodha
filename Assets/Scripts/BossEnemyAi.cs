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

    // Procedural Arena
    private GameObject proceduralArena;
    public bool isDead = false;

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
    }

    void Update()
    {
        if (isDead || player == null || currentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!isPlayerInArena && distanceToPlayer <= triggerRadius)
        {
            isPlayerInArena = true;
            SpawnProceduralArena(); 
        }

        // State change logic
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
        }
        else if (distanceToPlayer <= triggerRadius)
        {
            currentState = EnemyState.Chasing;
        }
        else
        {
            currentState = EnemyState.Idle; 
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

            // Lerp speed to make it smooth
            float currentAnimSpeed = animator.GetFloat("Speed");
            animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * 5f));
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
    }

    void Idle()
    {
        agent.isStopped = true;
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
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
            }
            
            StartCoroutine(DealDamageToPlayer()); 
            lastAttackTime = Time.time;
        }
    }

    private System.Collections.IEnumerator DealDamageToPlayer()
    {
        yield return new WaitForSeconds(0.6f); // Boss ka attack thoda slow ho sakta hai
        
        if (isDead || player == null) yield break;

        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= attackRange + 1.5f)
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
            StartCoroutine(HitRecovery());
        }
        else
        {
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
            if (pc != null) pc.SetLampFreeze(false);
        }
        
        Destroy(gameObject, 8f); // Boss body thodi der tak rukegi
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
}
