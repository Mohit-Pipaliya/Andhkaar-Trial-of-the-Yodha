using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonAI : MonoBehaviour
{
    public enum EnemyState { Patrolling, Chasing, Attacking, Hit }
    public EnemyState currentState = EnemyState.Patrolling;

    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentPatrolTarget;

    [Header("Targeting & Ranges")]
    public Transform player;
    public float triggerRadius = 15f; 
    public float attackRange = 2f;    

    [Header("Combat Stats")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    public int maxHealth = 1000;
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

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        currentPatrolTarget = pointA;
        if (currentPatrolTarget != null)
        {
            agent.SetDestination(currentPatrolTarget.position);
        }
    }

    void Update()
    {
        if (isDead || player == null || currentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Jab player trigger area me aaye tab arena wall banaye (ek hi baar)
        if (!isPlayerInArena && distanceToPlayer <= triggerRadius)
        {
            isPlayerInArena = true;
            SpawnProceduralArena();
        }

        // State check
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
            currentState = EnemyState.Patrolling;
        }

        UpdateAnimations();

        switch (currentState)
        {
            case EnemyState.Patrolling:
                Patrol();
                break;
            case EnemyState.Chasing:
                ChasePlayer();
                break;
            case EnemyState.Attacking:
                AttackPlayer();
                break;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null || currentState == EnemyState.Hit) return;

        float targetSpeed = 0f;
        
        if (currentState == EnemyState.Patrolling)
        {
            targetSpeed = 0.5f; // Walk
            agent.speed = 2f; 
        }
        else if (currentState == EnemyState.Chasing)
        {
            targetSpeed = 1.0f; // Run
            agent.speed = 6f; 
        }
        else if (currentState == EnemyState.Attacking)
        {
            targetSpeed = 0f; // Attack me speed 0
            agent.speed = 0f; 
        }

        // Animator me "Speed" naam ka float parameter hona zaroori hai
        animator.SetFloat("Speed", targetSpeed);
    }

    void Patrol()
    {
        if (pointA == null || pointB == null) return;

        agent.isStopped = false; 

        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
            agent.SetDestination(currentPatrolTarget.position);
        }
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
                // Random attack (1 or 2)
                int randomAttack = Random.Range(1, 3);
                animator.CrossFade("Attack" + randomAttack, 0.1f); 
            }
            
            StartCoroutine(DealDamageToPlayer()); 
            lastAttackTime = Time.time;
        }
    }

    private IEnumerator DealDamageToPlayer()
    {
        yield return new WaitForSeconds(0.4f); 
        
        if (isDead || player == null) yield break;

        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= attackRange + 1.5f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(100f); // Skeleton player ko 100 damage dega
                Debug.Log("<color=orange>Skeleton ne Player ko 100 damage diya!</color>");
            }
        }
    }

    // Jab player skeleton ko mare
    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        // Player agar mare toh exactly 100 HP kam karna hai, toh hardcode bhi kar sakte hain:
        // currentHealth -= 100;
        // Par dynamic rakhna better hai:
        currentHealth -= damageAmount;
        
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitRecovery());
        }
    }

    private IEnumerator HitRecovery()
    {
        currentState = EnemyState.Hit; 
        agent.isStopped = true;
        
        if (animator != null)
        {
            animator.CrossFade("Hit", 0.05f); // Hit animation play hogi
        }

        yield return new WaitForSeconds(0.6f); 
        
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
            animator.CrossFade("Death", 0.2f); // Death animation
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
        
        // Agar zameen ke niche gir raha ho death ke baad to isko uncomment kar dena
        // GetComponent<Collider>().enabled = false; 
        
        Destroy(gameObject, 5f); 
    }

    // Trigger area me aane par arena wall banane ka function
    void SpawnProceduralArena()
    {
        if (proceduralArena != null) return;

        Vector3 centerPosition = (transform.position + player.position) / 2f;
        centerPosition.y = transform.position.y; 

        proceduralArena = new GameObject("Skeleton_Arena_Ring_" + gameObject.name);
        proceduralArena.transform.position = centerPosition;

        float arenaRadius = triggerRadius;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
