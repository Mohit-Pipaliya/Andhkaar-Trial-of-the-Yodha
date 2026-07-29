using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class DemonAi : MonoBehaviour
{
    public enum State { Patrolling, Chasing, Attacking, Hit }
    public State currentState = State.Patrolling;

    [Header("Patrol Points")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentPatrolTarget;

    [Header("Player & Ranges")]
    public Transform player;
    public float triggerRadius = 15f; 
    public float attackRange = 2f;    
    public float runSpeed = 8.5f;

    [Header("Combat Stats")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    public int maxHealth = 1000;
    private int currentHealth;
    public Slider healthBarSlider;

    [Header("Animation & Movement Sync")]
    public Animator animator;
    public float clipBaseSpeed = 4f; 
    public bool syncAnimationSpeed = true;

    private NavMeshAgent agent;
    private GameObject arenaWall;
    private bool isPlayerTrapped = false;
    public bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        // Player dhoondna
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Animator dhoondna
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;

        // Health slider update
        if (healthBarSlider == null) healthBarSlider = GetComponentInChildren<Slider>();
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        // Patrol start Point A se
        currentPatrolTarget = pointA;
        if (agent != null && currentPatrolTarget != null)
        {
            agent.SetDestination(currentPatrolTarget.position);
        }
    }

    void Update()
    {
        if (isDead || player == null || currentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Agar player paas aaye, toh Arena bana do aur usko trap kar lo
        if (!isPlayerTrapped && distanceToPlayer <= triggerRadius)
        {
            isPlayerTrapped = true;
            CreateArenaWall();
        }

        // 2. State decide karna
        if (currentState != State.Hit)
        {
            if (isPlayerTrapped) // Jab trap ho gaya, tabhi chase/attack karega
            {
                if (distanceToPlayer <= attackRange)
                {
                    currentState = State.Attacking;
                }
                else
                {
                    currentState = State.Chasing;
                }
            }
            else
            {
                currentState = State.Patrolling;
            }
        }

        // 3. Animation and Speed Control
        if (animator != null)
        {
            float targetSpeedParam = 0f;

            if (currentState == State.Patrolling)
            {
                agent.speed = runSpeed; // Run speed during patrol
                targetSpeedParam = (agent.speed > 0f) ? (agent.velocity.magnitude / agent.speed) : 0f;
                animator.SetBool("IsAlert", true); // Set to true so it uses the combat run animation which is fully set up
            }
            else if (currentState == State.Chasing)
            {
                agent.speed = runSpeed; // Run speed
                targetSpeedParam = (agent.speed > 0f) ? (agent.velocity.magnitude / agent.speed) : 0f;
                animator.SetBool("IsAlert", true);
            }
            else if (currentState == State.Attacking)
            {
                agent.speed = 0f; // Attack me ruk jayega
                targetSpeedParam = 0f;
                animator.SetBool("IsAlert", true);
            }

            // Damping ke sath Smooth Transition
            animator.SetFloat("Speed", targetSpeedParam, 0.1f, Time.deltaTime);

            // Natural stride speed match karne ke liye Animator speed scale
            if (currentState == State.Patrolling || currentState == State.Chasing)
            {
                if (syncAnimationSpeed && agent.velocity.magnitude > 0.1f)
                {
                    animator.speed = agent.velocity.magnitude / clipBaseSpeed;
                }
                else
                {
                    animator.speed = 1f; // Idle me normal speed
                }
            }
            else
            {
                animator.speed = 1f; // Attack/Hit wagaira me normal speed
            }
        }

        // 4. Action Perform Karna
        if (currentState == State.Patrolling) Patrol();
        else if (currentState == State.Chasing) Chase();
        else if (currentState == State.Attacking) Attack();
    }

    void Patrol()
    {
        if (pointA == null || pointB == null) return;
        agent.isStopped = false;

        if (agent.isOnNavMesh && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
                agent.SetDestination(currentPatrolTarget.position);
            }
        }
    }

    void Chase()
    {
        agent.isStopped = false;
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }
    }

    void Attack()
    {
        agent.isStopped = true;

        // Player ki taraf dekhna
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);

        // Attack Karna
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (animator != null) animator.CrossFade("Attack1", 0.1f);
            StartCoroutine(DealDamage());
            lastAttackTime = Time.time;
        }
    }

    private IEnumerator DealDamage()
    {
        yield return new WaitForSeconds(0.4f); // Animation ka hit time
        if (isDead || player == null) yield break;

        if (Vector3.Distance(transform.position, player.position) <= attackRange + 1.5f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.TakeDamage(100f); // Player ko 100 damage
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (healthBarSlider != null) healthBarSlider.value = currentHealth;

        if (currentHealth > 0)
        {
            // Agar hit hua hai to hit animation play karo
            if (animator != null) 
            {
                animator.SetTrigger("Hit");
                animator.CrossFade("Hit", 0.05f);
            }
            
            // Start Hit routine to pause movement briefly
            StopCoroutine("HitRoutine");
            StartCoroutine(HitRoutine());
        }
        else
        {
            Die();
        }
    }

    private IEnumerator HitRoutine()
    {
        currentState = State.Hit;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        
        yield return new WaitForSeconds(0.6f); // Hit animation time
        
        if (!isDead)
        {
            currentState = State.Chasing;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (animator != null) 
        {
            animator.SetTrigger("Death");
            animator.SetTrigger("Die");
            animator.CrossFade("Death", 0.2f);
        }

        // Arena wall tod do taaki player aazad ho jaye
        if (arenaWall != null) Destroy(arenaWall);

        // Fight khatam - player ka lamp state wapas lao
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) 
            {
                pc.SetLampFreeze(false);
                pc.SetCombatState(false);
            }
        }

        Destroy(gameObject, 5f); // 5 second baad body gayab
    }

    void CreateArenaWall()
    {
        if (arenaWall != null) return;

        // Player ka lamp freeze aur combat state on kardo
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) 
            {
                pc.SetLampFreeze(true);
                pc.SetCombatState(true);
            }
        }

        // Player aur Enemy ke theek beech (center) me arena banega
        Vector3 center = (transform.position + player.position) / 2f;
        center.y = transform.position.y;

        arenaWall = new GameObject("Demon_Arena_Wall");
        arenaWall.transform.position = center;

        float radius = triggerRadius;

        // Laal rang ka circle (LineRenderer) zameen par dikhane ke liye
        LineRenderer line = arenaWall.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.startWidth = 0.4f;
        line.endWidth = 0.4f;
        line.positionCount = 51;
        line.loop = true;
        
        Material redMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        redMat.color = new Color(1f, 0f, 0f, 0.7f); 
        line.material = redMat;

        for (int i = 0; i < 51; i++)
        {
            float angle = i * (360f / 50f);
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            line.SetPosition(i, new Vector3(x, 0.2f, z));
        }

        // Asli invisible diwarein (Box Colliders) banana taaki player bahar na ja paye
        int segments = 24; 
        for (int i = 0; i < segments; i++)
        {
            float angle = i * (360f / segments);
            GameObject wall = new GameObject("Wall_Segment");
            wall.transform.SetParent(arenaWall.transform);
            
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            
            wall.transform.localPosition = new Vector3(x, 10f, z); 
            wall.transform.LookAt(arenaWall.transform); 
            
            BoxCollider box = wall.AddComponent<BoxCollider>();
            float width = (radius * 2f * Mathf.PI) / segments;
            box.size = new Vector3(width + 1.5f, 25f, 1f); 
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
