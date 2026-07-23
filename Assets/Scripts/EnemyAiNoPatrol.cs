using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAiNoPatrol : MonoBehaviour
{
    public enum EnemyState { Idle, Chasing, Attacking, Hit }
    public EnemyState currentState = EnemyState.Idle;

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

    [Header("Animation")]
    public Animator animator;
    
    [Header("Audio")]
    public AudioSource enemyAudio;
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
    public bool forceChase = false; // Naya variable: Agar ye true hai, to door se hi chase karega bina arena banaye

    void Start()
    {
        maxHealth = 1000; // Forcefully set to 1000
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

        // Animator dhundna
        animator = GetComponent<Animator>(); 
        if (animator == null) 
        {
            animator = GetComponentInChildren<Animator>(); 
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;

            if (!animator.enabled) animator.enabled = true; // Auto-enable

            if (animator.runtimeAnimatorController == null)
            {
#if UNITY_EDITOR
                RuntimeAnimatorController controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Animator/Enemy.controller");
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
#endif
            }

            if (animator.avatar == null)
            {
                Debug.LogError("<color=red><b>CRITICAL ERROR: Is Enemy ke Animator me 'AVATAR' missing hai!</b></color>");
            }
        }

        currentHealth = maxHealth;

        // Player agar assign nahi kiya inspector me to tag se dhundhega
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
        else if (distanceToPlayer <= triggerRadius || forceChase) // forceChase hai to door se hi chase karega
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
            currentState = EnemyState.Idle; // Idle state me khada rahega
            hasRoared = false;
        }

        // 100% Reliable Animation logic
        if (animator != null)
        {
            float targetSpeed = 0f;
            bool isAlert = false;
            
            if (currentState == EnemyState.Idle)
            {
                targetSpeed = 0f; 
                isAlert = false; 
                agent.speed = 0f; 
                animator.speed = 1.0f; 
                lastAttackTime = 0f; 
            }
            else if (currentState == EnemyState.Chasing)
            {
                targetSpeed = 1.0f; 
                isAlert = true; 
                agent.speed = 8.5f; 
                animator.speed = 2.0f; 
            }
            else if (currentState == EnemyState.Attacking)
            {
                targetSpeed = 0f; 
                isAlert = true;
                agent.speed = 0f; 
                animator.speed = 1.5f; 
            }

            animator.SetFloat("Speed", targetSpeed);
            animator.SetBool("IsAlert", isAlert);
        }

        // Action perform based on state
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
                footstepTimer = 0.4f; // Running speed
            }
        }
    }

    void Idle()
    {
        agent.isStopped = true; // Bas apni jagah khada rahega
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
                animator.CrossFade("Attack1", 0.05f); 
                PlaySound(attackSound);
            }
            
            StartCoroutine(DealDamageToPlayer()); 
            lastAttackTime = Time.time;
        }
    }

    private System.Collections.IEnumerator DealDamageToPlayer()
    {
        yield return new WaitForSeconds(0.4f); 
        
        if (isDead || player == null) yield break;

        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= attackRange + 1.5f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(100f);
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
            animator.speed = 1.0f;
            animator.CrossFade("Hit", 0.05f); 
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
            animator.speed = 1.0f;
            animator.CrossFade("Death", 0.2f);
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
        
        // Arena khatam ho raha hai to lamp wapas chalu kar do
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

        // Player ka lamp freeze kardo taaki fight ke beech me oil khatam na ho
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetLampFreeze(true);
        }

        Vector3 centerPosition = (transform.position + player.position) / 2f;
        centerPosition.y = transform.position.y;

        proceduralArena = new GameObject("Epic_Arena_Ring_" + gameObject.name);
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

    // --- AUDIO HELPER ---
    public void PlaySound(AudioClip clip)
    {
        if (enemyAudio != null && clip != null)
        {
            enemyAudio.PlayOneShot(clip);
        }
    }
}
