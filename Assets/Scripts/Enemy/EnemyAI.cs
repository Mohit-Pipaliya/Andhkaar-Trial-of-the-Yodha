using UnityEngine;
using System.Collections;

public enum EnemyState { Idle, Alert, Chasing, Attacking, Dead }

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target & Arena")]
    public Transform player; 
    public GameObject arenaWalls; 

    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 1.5f;
    private Transform currentPatrolTarget;

    [Header("Stats")]
    public float triggerRange = 30f; 
    public float cutsceneRange = 25f; 
    public float attackRange = 1.5f; 
    public float chaseSpeed = 2.5f; // Walk speed instead of run speed
    public float rotationSpeed = 10f;
    public float gravity = 25f;
    public int maxHealth = 100;

    [Header("Combat Settings")]
    public float attackCooldown = 0.5f;

    private int currentHealth;
    private EnemyState currentState = EnemyState.Idle;
    
    private CharacterController controller;
    private Animator animator;
    private PlayerController playerControllerScript;
    
    private float verticalVelocity = 0f;
    private bool isAttackCoolingDown = false;
    private bool isWaitingAtWaypoint = false;
    private GameObject activeLightningArena;
    
    // AAA Camera Shake Variables
    private CameraFollow camFollow;
    private float footstepTimer = 0f;
    private float footstepInterval = 0.5f; // Matches walk animation roughly
    private EnemyHealthBar healthBar;
    private Renderer[] renderers;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        
        if (pointA != null)
        {
            currentPatrolTarget = pointA;
        }
        
        // Add the glowing soul effect automatically
        gameObject.AddComponent<SoulEffect>();
        
        // Add AAA Voice Effect
        gameObject.AddComponent<AAAVoiceEffect>();
        
        // Add Health Bar (Blue Glowing for Enemy 1)
        healthBar = gameObject.AddComponent<EnemyHealthBar>();
        healthBar.Initialize(new Color(0f, 0.5f, 2.5f, 1f)); // HDR Blue
        
        if (arenaWalls != null)
            arenaWalls.SetActive(false); 

        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (player != null)
        {
            playerControllerScript = player.GetComponent<PlayerController>();
        }

        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (controller != null && controller.enabled)
        {
            ApplyGravity();
        }

        if (currentState == EnemyState.Dead)
            return; 

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(distanceToPlayer);
                break;
            case EnemyState.Alert:
                HandleAlert();
                break;
            case EnemyState.Chasing:
                HandleChasing(distanceToPlayer);
                break;
            case EnemyState.Attacking:
                HandleAttacking(distanceToPlayer);
                break;
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        
        controller.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
    }

    void HandleIdle(float distance)
    {
        // Jaise hi player Trigger Range me aayega, tab alert sequence shuru hoga
        if (distance <= triggerRange)
        {
            StartCoroutine(AlertSequence());
            return;
        }

        // Patrol logic
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("EnemyAI: Point A or Point B is not assigned in the Inspector! Enemy will not patrol.");
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            return;
        }

        if (currentPatrolTarget != null)
        {
            if (isWaitingAtWaypoint)
            {
                // Transition to idle smoothly while waiting
                animator.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
                return;
            }

            // Ensure Walk animation plays (0.5 is Walk in Blend Tree, 1.0 is Run)
            animator.SetFloat("Speed", 0.5f, 0.1f, Time.deltaTime);

            // Calculate distance ignoring Y axis (height) so enemy doesn't get stuck if waypoint is floating
            Vector3 pos2D = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 target2D = new Vector3(currentPatrolTarget.position.x, 0, currentPatrolTarget.position.z);
            float distToTarget = Vector3.Distance(pos2D, target2D);
            
            // Jab point ke paas pahunch jaye
            if (distToTarget < 0.5f) // Thoda bada threshold diya hai taaki stuck na ho
            {
                StartCoroutine(WaitAtWaypoint());
                return;
            }

            Vector3 direction = (currentPatrolTarget.position - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                // Rotate smoothly
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }

            controller.Move(direction * patrolSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        isWaitingAtWaypoint = true;
        
        // Wait for a natural pause before turning
        float pauseTime = Random.Range(0.5f, 1.2f);
        yield return new WaitForSeconds(pauseTime);
        
        // Switch target
        currentPatrolTarget = currentPatrolTarget == pointA ? pointB : pointA;
        isWaitingAtWaypoint = false;
    }

    private IEnumerator AlertSequence()
    {
        currentState = EnemyState.Alert;
        
        // Wait for 1 second while enemy turns to face player (handled in HandleAlert)
        yield return new WaitForSeconds(1.0f);

        CreateLightningArena();
        
        // Wait 0.5s to show the arena forming before rushing
        yield return new WaitForSeconds(0.5f);
        
        currentState = EnemyState.Chasing;
    }

    void HandleAlert()
    {
        animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        FacePlayer();
    }

    void CreateLightningArena()
    {
        if (activeLightningArena == null)
        {
            activeLightningArena = new GameObject("LightningArena");
            Vector3 center = (transform.position + player.position) / 2f;
            center.y = transform.position.y;
            activeLightningArena.transform.position = center;
            
            LightningArena arenaScript = activeLightningArena.AddComponent<LightningArena>();
            float dist = Vector3.Distance(transform.position, player.position);
            arenaScript.Initialize(Mathf.Max(dist, 12f));
        }
    }

    void HandleChasing(float distance)
    {
        if (distance <= attackRange)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        FacePlayer();

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        controller.Move(direction * chaseSpeed * Time.deltaTime);
        
        // 1.0f se 'Run' animation chalega jo zyada fast lagta hai
        animator.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }

    void HandleAttacking(float distance)
    {
        animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime); 
        FacePlayer();

        if (distance > attackRange && !isAttackCoolingDown)
        {
            currentState = EnemyState.Chasing;
        }
        else if (!isAttackCoolingDown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; 
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttackCoolingDown = true;

        int attackChoice = Random.Range(0, 2); 
        
        if (attackChoice == 0)
        {
            int slashType = Random.Range(1, 3);
            animator.SetInteger("AttackType", slashType);
            animator.SetTrigger("SlashAttack");
        }
        else
        {
            int attackType = Random.Range(1, 4);
            animator.SetInteger("AttackType", attackType);
            animator.SetTrigger("SwordAttack");
        }

        yield return new WaitForSeconds(attackCooldown);

        isAttackCoolingDown = false;
    }

    public void TakeDamage(int damage)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
        StartCoroutine(HitPause());
        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("TakeDamage");
        }
    }

    IEnumerator HitPause()
    {
        animator.speed = 0.1f;
        yield return new WaitForSeconds(0.1f);
        animator.speed = 1f;
    }

    private IEnumerator DamageFlashRoutine()
    {
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        propBlock.SetColor("_Color", Color.red);
        propBlock.SetColor("_BaseColor", Color.red);

        foreach (Renderer r in renderers)
        {
            if (r != null) r.SetPropertyBlock(propBlock);
        }

        yield return new WaitForSecondsRealtime(0.1f);

        foreach (Renderer r in renderers)
        {
            if (r != null) r.SetPropertyBlock(null);
        }
    }

    void Die()
    {
        currentState = EnemyState.Dead;
        animator.SetTrigger("Die"); 
        
        if (healthBar != null)
        {
            healthBar.HideBar();
        }

        if (arenaWalls != null)
            arenaWalls.SetActive(false);
            
        if (activeLightningArena != null)
        {
            Destroy(activeLightningArena);
        }
            
        StartCoroutine(DisableControllerAfterDeath());
    }

    IEnumerator DisableControllerAfterDeath()
    {
        yield return new WaitForSeconds(3f);
        if (controller != null)
            controller.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pointA.position, 0.5f);
            Gizmos.DrawWireSphere(pointB.position, 0.5f);
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}
