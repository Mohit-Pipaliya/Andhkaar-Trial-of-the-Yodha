using UnityEngine;

/// <summary>
/// A patrolling enemy that loops between waypoints. Chases the player on sight
/// and returns to patrol if the player escapes its leash range.
/// </summary>
public class PatrolEnemy : EnemyBase
{
    public enum PatrolState
    {
        Patrol,
        Chase,
        Attack,
        Return
    }

    [Header("Patrol Settings")]
    [Tooltip("Waypoints the enemy will cycle between.")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("Distance at which the enemy detects the player and begins chasing.")]
    [SerializeField] private float detectionRange = 10f;
    [Tooltip("Maximum distance the enemy will chase before giving up and returning.")]
    [SerializeField] private float leashRange = 20f;
    [Tooltip("Time to wait at each waypoint.")]
    [SerializeField] private float patrolWaitTime = 2f;
    [Tooltip("Distance at which the enemy can perform a lunge attack.")]
    [SerializeField] private float lungeAttackRange = 4f;

    private PatrolState currentState = PatrolState.Patrol;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    // Advanced AI variables
    private int comboCounter = 0;

    // Keeps track of where the enemy started to return to that spot if necessary
    private Vector3 initialPatrolPoint;

    protected override void Awake()
    {
        base.Awake();
        if (waypoints != null && waypoints.Length > 0)
        {
            initialPatrolPoint = waypoints[0].position;
        }
        else
        {
            initialPatrolPoint = transform.position;
        }
    }

    protected override void UpdateAI()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case PatrolState.Patrol:
                UpdatePatrolState(distanceToPlayer);
                break;
            case PatrolState.Chase:
                UpdateChaseState(distanceToPlayer);
                break;
            case PatrolState.Attack:
                UpdateAttackState(distanceToPlayer);
                break;
            case PatrolState.Return:
                UpdateReturnState(distanceToPlayer);
                break;
        }
    }

    private void UpdatePatrolState(float distanceToPlayer)
    {
        IsAlert = false;
        // Check for player detection
        if (distanceToPlayer <= detectionRange)
        {
            currentState = PatrolState.Chase;
            isWaiting = false;
            return;
        }

        if (waypoints == null || waypoints.Length == 0) return;

        // Handle waypoint waiting and movement
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWaitTime)
            {
                isWaiting = false;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                if (agent.isOnNavMesh) agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                isWaiting = true;
                waitTimer = 0f;
            }
            else if (agent.isOnNavMesh && !agent.hasPath)
            {
                // Ensure we always have a destination
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
    }

    private void UpdateChaseState(float distanceToPlayer)
    {
        IsAlert = true;
        // If player runs too far, give up and return to patrol
        if (distanceToPlayer > leashRange)
        {
            currentState = PatrolState.Return;
            if (agent.isOnNavMesh) agent.SetDestination(initialPatrolPoint);
            return;
        }

        // If close enough, attack
        if (distanceToPlayer <= attackRange || distanceToPlayer <= lungeAttackRange && Random.value < 0.2f)
        {
            currentState = PatrolState.Attack;
            if (agent.isOnNavMesh) agent.ResetPath();
            return;
        }

        // Direct charging behavior
        if (agent.isOnNavMesh) 
        {
            agent.isStopped = false; // Ensure they can move again
            
            // Optional: reset speed if you changed it earlier
            agent.speed = 3.5f; 
            agent.SetDestination(playerTransform.position);
        }

        SmoothRotateTowards(playerTransform.position);
    }

    private void UpdateAttackState(float distanceToPlayer)
    {
        IsAlert = true;

        // Locked in animation (locks position and rotation)
        if (attackLockTimer > 0)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            return;
        }
        
        // Wait for attack cooldown
        if (attackTimer > 0)
        {
            SmoothRotateTowards(playerTransform.position);
            return;
        }

        // If player moves out of attack range, resume chase
        // (but allow lunging if slightly outside normal range)
        if (distanceToPlayer > lungeAttackRange || (distanceToPlayer > attackRange && comboCounter == 0))
        {
            currentState = PatrolState.Chase;
            return;
        }

        SmoothRotateTowards(playerTransform.position);

        PerformAttack();
        
        // Attack finished, start full cooldown
        attackTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
        attackLockTimer = 1.2f;
        
        // Ensure the enemy stops moving while attacking and idling
        if (agent != null && agent.isOnNavMesh)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void UpdateReturnState(float distanceToPlayer)
    {
        IsAlert = false;
        // Can still detect player on the way back
        if (distanceToPlayer <= detectionRange)
        {
            currentState = PatrolState.Chase;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentState = PatrolState.Patrol;
            currentWaypointIndex = 0; // Reset patrol route
            if (waypoints != null && waypoints.Length > 0 && agent.isOnNavMesh)
            {
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
    }

    private void PerformAttack()
    {
        // 50/50 chance to use Attack1 or Attack2
        if (Random.value > 0.5f)
        {
            ExecuteAttack(attack1Hash);
        }
        else
        {
            ExecuteAttack(attack2Hash);
        }
    }
}
