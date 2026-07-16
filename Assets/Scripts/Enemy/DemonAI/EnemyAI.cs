using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Hurt, Dead }
    
    [Header("Current State")]
    public State currentState;
    
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 2f;
    
    [Header("Ranges")]
    public float detectionRange = 15f;
    public float attackRange = 2.5f;
    
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    
    [Header("References")]
    public Transform playerTransform;
    
    // Components
    private NavMeshAgent agent;
    private EnemyAnimator enemyAnimator;
    private EnemyAttack enemyAttack;
    private EnemyHealth enemyHealth;
    
    // Internal Variables
    private int currentPatrolIndex;
    private float waitTimer;
    private bool isWaiting;

    #region Unity Callbacks
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyAnimator = GetComponent<EnemyAnimator>();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false; // We handle rotation manually for smoother Slerp
    }

    private void Start()
    {
        ChangeState(State.Patrol);
    }

    private void Update()
    {
        if (currentState == State.Dead) return;

        switch (currentState)
        {
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                CheckAttackRange();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Hurt:
                // Waiting for recovery
                break;
        }
    }
    #endregion

    #region State Management
    public void ChangeState(State newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case State.Patrol:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                isWaiting = false;
                GoToNextPatrolPoint();
                break;
                
            case State.Chase:
                agent.isStopped = false;
                agent.speed = runSpeed;
                break;
                
            case State.Attack:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                enemyAttack.StartAttack();
                break;
                
            case State.Hurt:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                enemyAnimator.TriggerDamage();
                // We use an animation event or invoke to recover from hurt
                Invoke(nameof(RecoverFromHurt), 0.8f); 
                break;
                
            case State.Dead:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                break;
        }
    }
    #endregion

    #region Patrol Logic
    private void UpdatePatrol()
    {
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = 0f;
                enemyAnimator.UpdateSpeed(0f); // Idle
            }
            else
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= patrolWaitTime)
                {
                    isWaiting = false;
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    GoToNextPatrolPoint();
                }
            }
        }
        else
        {
            enemyAnimator.UpdateSpeed(0.5f); // Walk
            if (agent.desiredVelocity.sqrMagnitude > 0.1f)
            {
                SmoothFaceTarget(transform.position + agent.desiredVelocity);
            }
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length > 0 && patrolPoints[currentPatrolIndex] != null)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }
    #endregion

    #region Chase Logic
    private void UpdateChase()
    {
        if (playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
            enemyAnimator.UpdateSpeed(1f); // Run
            SmoothFaceTarget(playerTransform.position);
        }
    }

    private void CheckAttackRange()
    {
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= attackRange)
            {
                ChangeState(State.Attack);
            }
        }
    }
    #endregion

    #region Attack Logic
    private void UpdateAttack()
    {
        if (playerTransform != null)
        {
            if (!enemyAttack.IsAttacking)
            {
                SmoothFaceTarget(playerTransform.position);
                
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance > attackRange)
                {
                    ChangeState(State.Chase);
                }
                else if (enemyAttack.CanAttack())
                {
                    enemyAttack.StartAttack();
                }
                else
                {
                    enemyAnimator.UpdateSpeed(0f); // Idle while on cooldown
                }
            }
        }
    }
    
    public void OnAttackFinished()
    {
        if (currentState == State.Attack)
        {
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > attackRange)
            {
                ChangeState(State.Chase);
            }
        }
    }
    #endregion

    #region Hurt Logic
    public void OnDamageTaken()
    {
        if (currentState != State.Dead)
        {
            if (enemyAttack.IsAttacking)
            {
                enemyAttack.CancelAttack();
            }
            CancelInvoke(nameof(RecoverFromHurt));
            ChangeState(State.Hurt);
        }
    }

    private void RecoverFromHurt()
    {
        if (currentState == State.Hurt)
        {
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance <= detectionRange)
                {
                    ChangeState(State.Chase);
                    return;
                }
            }
            ChangeState(State.Patrol);
        }
    }
    #endregion

    #region Utility & Detection
    public void SmoothFaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Ignore vertical difference
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player Detection Area using Trigger Collider
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            if (currentState == State.Patrol)
            {
                ChangeState(State.Chase);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentState == State.Chase || currentState == State.Attack)
            {
                ChangeState(State.Patrol);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Detection Range (Visual only - Actual detection uses Trigger Collider)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Patrol Path
        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }
    #endregion
}
