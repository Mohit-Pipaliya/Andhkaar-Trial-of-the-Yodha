using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAi : MonoBehaviour
{
    public enum EnemyState { Patrolling, Chasing, Attacking, Hit }
    public EnemyState currentState = EnemyState.Patrolling;

    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentPatrolTarget;

    [Header("Targeting & Ranges")]
    public Transform player;
    public float triggerRadius = 15f; // Jab player iske andar aayega, enemy chase karega
    public float attackRange = 2f;    // Attack karne ki range

    [Header("Combat Stats")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    public int maxHealth = 1000;
    private int currentHealth;
    public Slider healthBarSlider;

    private NavMeshAgent agent;
    private bool isPlayerInArena = false; // Check karne ke liye ki player arena me trap ho chuka hai ya nahi

    [Header("Audio")]
    public AudioSource enemyAudio;
    public AudioClip roarSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip footstepSound;
    private float footstepTimer;
    private bool hasRoared = false;

    [Header("Animation")]
    public Animator animator;

    // Procedural Arena
    private GameObject proceduralArena;
    public bool isDead = false;

    void Start()
    {
        maxHealth = 1000; // Forcefully set to 1000 taaki Inspector ki purani value (100) overwrite ho jaye
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        
        // Agar slider assign nahi kiya inspector me to khud dhundh lega
        if (healthBarSlider == null)
        {
            healthBarSlider = GetComponentInChildren<Slider>();
        }

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        // 1. Animator dhundna
        animator = GetComponent<Animator>(); // Pehle khud ke upar check karein
        if (animator == null) 
        {
            animator = GetComponentInChildren<Animator>(); // Agar khud me nahi hai to child me check karein
        }

        // Apply Root Motion ko false kar diya taaki Agent aur Animation me conflict na ho
        if (animator != null)
        {
            animator.applyRootMotion = false;

            if (!animator.enabled) animator.enabled = true; // Auto-enable

            // Agar Controller assign nahi hai to script khud assign kar legi
            if (animator.runtimeAnimatorController == null)
            {
#if UNITY_EDITOR
                RuntimeAnimatorController controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Animator/Enemy.controller");
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("<color=green><b>EnemyAi ne automatically Animator Controller assign kar diya hai!</b></color>");
                }
#endif
            }

            if (animator.avatar == null)
            {
                Debug.LogError("<color=red><b>CRITICAL ERROR: Is Enemy ke Animator me 'AVATAR' missing hai! Iski wajah se ye bina animation ke ghisat (slide) ke chalega. Kripya Inspector me Animator component me Avatar daalein!</b></color>");
            }
        }
        else
        {
            Debug.LogError("Is enemy me Animator component hi nahi hai! Please ek Animator add karein: " + gameObject.name);
        }

        currentHealth = maxHealth;

        // Player agar assign nahi kiya inspector me to tag se dhundhega
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Patrol start karna point A se
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

        // Agar player trigger area me aa gaya pehli baar, tab use trap karna hai
        if (!isPlayerInArena && distanceToPlayer <= triggerRadius)
        {
            isPlayerInArena = true;
            PlaySound(roarSound);
            hasRoared = true;
            SpawnProceduralArena(); // Automatically ek gol (ring) deewar banayega
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
            currentState = EnemyState.Patrolling;
            hasRoared = false; // Reset roar
        }

        // Footsteps logic
        if (agent.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlaySound(footstepSound);
                footstepTimer = 0.4f; // Chase running speed
            }
        }

        // 100% Reliable Animation logic (State-based)
        if (animator != null)
        {
            float targetSpeed = 0f;
            bool isAlert = false;
            
            if (currentState == EnemyState.Patrolling)
            {
                targetSpeed = 0.25f; // Aapke Blend Tree ke mutabiq Walk = 0.25
                isAlert = true; 
                agent.speed = 3.5f; // Physical Walk Speed badhaya
                animator.speed = 1.8f; // Walk animation aur tezi se chalegi
                lastAttackTime = 0f; // Patrol karte time cooldown reset kar do taaki player dikhte hi maare
            }
            else if (currentState == EnemyState.Chasing)
            {
                targetSpeed = 1.0f; // Pehle 0.5 tha, ab 1.0 kar diya taaki full speed Run animation chale
                isAlert = true; 
                agent.speed = 8.5f; // Physical Run Speed badhaya
                animator.speed = 2.0f; // Run animation bohot tezi se chalegi
            }
            else if (currentState == EnemyState.Attacking)
            {
                targetSpeed = 0f; 
                isAlert = true;
                agent.speed = 0f; 
                animator.speed = 1.5f; // Attack animation fast chalegi taaki turant hit kare
            }

            animator.SetFloat("Speed", targetSpeed);
            animator.SetBool("IsAlert", isAlert);
        }

        // Action perform based on state
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

    void Patrol()
    {
        if (pointA == null || pointB == null) return;

        agent.isStopped = false; // Move hone do

        // Check agar destination (Point A ya B) ke paas pahuch gaya hai
        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            // Target change karo (Agar A tha to B kar do, B tha to A kar do)
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
        agent.isStopped = true; // Attack ke time enemy ruk jayega

        // Player ki taraf dekhne ke liye (Rotation)
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; 
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);

        // Cooldown check karke attack karna
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (animator != null)
            {
                // CrossFade time bohot kam kar diya (0.05) taaki ekdum se attack kare
                animator.CrossFade("Attack1", 0.05f); 
                PlaySound(attackSound);
            }
            
            Debug.Log("Enemy Attacking Player!");
            StartCoroutine(DealDamageToPlayer()); // Damage ke liye
            lastAttackTime = Time.time;
        }
    }

    private System.Collections.IEnumerator DealDamageToPlayer()
    {
        yield return new WaitForSeconds(0.4f); // Animation me jab haath (sword) girti hai tab tak ka wait
        
        if (isDead || player == null) yield break;

        float distance = Vector3.Distance(transform.position, player.position);
        
        // Agar player thoda aas-paas hi hai to damage lag jayega
        if (distance <= attackRange + 1.5f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(100f);
                Debug.Log("<color=red>Enemy ne player par attack kiya aur 100 health kam kar di!</color>");
            }
        }
    }

    // Is function ko call karein jab player enemy ko damage de
    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        
        // UI Update karein
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
        
        if (currentHealth <= 0)
        {
            PlaySound(deathSound);
            Die();
        }
        else
        {
            PlaySound(hitSound);
            StartCoroutine(HitRecovery());
        }
    }

    private System.Collections.IEnumerator HitRecovery()
    {
        currentState = EnemyState.Hit; // State change kar diya taaki Update me normal logic na chale
        agent.isStopped = true;
        
        if (animator != null)
        {
            animator.speed = 1.0f;
            // CrossFade "Hit" taaki hit ka reaction properly aaye
            animator.CrossFade("Hit", 0.05f); 
        }

        yield return new WaitForSeconds(0.6f); // 0.6 second ka flinch / stun
        
        if (!isDead)
        {
            agent.isStopped = false;
            currentState = EnemyState.Chasing; // Wapas chase karne lag jayega
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Enemy Died!");
        
        // Death Animation
        if (animator != null)
        {
            animator.speed = 1.0f;
            animator.CrossFade("Death", 0.2f);
        }
        
        // Enemy mar gaya to Arena (Ring) tod do taaki player bahar ja sake
        if (proceduralArena != null)
        {
            Destroy(proceduralArena);
        }
        
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        agent.enabled = false; // NavMeshAgent ko disable kar do taaki gravity/collision issues na aaye
        
        // Arena khatam ho raha hai to lamp wapas chalu kar do
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetLampFreeze(false);
        }
        
        // GetComponent<Collider>().enabled = false; // Isko hataya taaki enemy floor ke niche na gire
        // this.enabled = false; // Script disable karne se Coroutines bhi stop ho jate hain, to isko bhi hata diya
        
        Destroy(gameObject, 5f); // 5 second baad body gayab
    }

    // Apne hisaab se Arena (Wall) banane ka kamaal ka function!
    void SpawnProceduralArena()
    {
        if (proceduralArena != null) return;

        // Player ka lamp freeze kardo taaki fight ke beech me oil khatam na ho
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetLampFreeze(true);
        }

        // Player aur Enemy ke beech ka center nikal kar arena banayenge
        Vector3 centerPosition = (transform.position + player.position) / 2f;
        centerPosition.y = transform.position.y; // Zameen par rakho

        proceduralArena = new GameObject("Epic_Arena_Ring_" + gameObject.name);
        proceduralArena.transform.position = centerPosition;

        // Ring thodi badi honi chahiye (trigger radius ke barabar)
        float arenaRadius = triggerRadius;

        // 1. Zameen par ek laal rang ka visual circle (LineRenderer) banayenge
        LineRenderer line = proceduralArena.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.startWidth = 0.4f;
        line.endWidth = 0.4f;
        line.positionCount = 51;
        line.loop = true;
        
        Material redMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        redMat.color = new Color(1f, 0f, 0f, 0.7f); // Red Ring
        line.material = redMat;

        float angle = 0f;
        for (int i = 0; i < 51; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * arenaRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * arenaRadius;
            line.SetPosition(i, new Vector3(x, 0.2f, z)); // Zameen se thoda upar
            angle += (360f / 50f);
        }

        // 2. Invisible physical Deewarein banayenge taaki player bahar na ja sake
        int segments = 24; // 24 boxes mil kar gol deewar banayenge
        angle = 0f;
        for (int i = 0; i < segments; i++)
        {
            GameObject wall = new GameObject("InvisibleWallSegment");
            wall.transform.SetParent(proceduralArena.transform);
            
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * arenaRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * arenaRadius;
            
            wall.transform.localPosition = new Vector3(x, 10f, z); // Height 10f
            wall.transform.LookAt(proceduralArena.transform); // Center ki taraf dekhe
            
            BoxCollider box = wall.AddComponent<BoxCollider>();
            float width = (arenaRadius * 2f * Mathf.PI) / segments;
            box.size = new Vector3(width + 1f, 25f, 1f); // Moti aur lambi deewar
            
            angle += (360f / segments);
        }
        
        Debug.Log("Player trapped in Arena!");
    }

    // Unity Editor me ranges dekhne ke liye (Gizmos)
    private void OnDrawGizmosSelected()
    {
        // Trigger Range (Yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // --- AUDIO HELPER ---
    public void PlaySound(AudioClip clip)
    {
        if (enemyAudio != null && clip != null)
        {
            enemyAudio.PlayOneShot(clip);
        }
    }
}
