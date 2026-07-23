using UnityEngine;

/// <summary>
/// A stationary enemy that guards its spawn position. Enters a chase state
/// when the player gets close, and attacks when in range. Returns to spawn
/// position if the player runs beyond the leash range.
/// </summary>
public class StationaryEnemy : EnemyBase
{
    public enum StationaryState
    {
        Idle,
        Chase,
        Attack,
        Return
    }

    [Header("Stationary Settings")]
    [Tooltip("Distance at which the enemy notices the player and becomes alert.")]
    [SerializeField] private float detectionRange = 10f;
    [Tooltip("Maximum distance the enemy will chase before giving up and returning.")]
    [SerializeField] private float leashRange = 20f;

    private StationaryState currentState = StationaryState.Idle;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    protected override void Awake()
    {
        base.Awake();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        
        // We now need the NavMeshAgent to chase
        if (agent != null)
        {
            agent.enabled = true;
        }
    }

    protected override void UpdateAI()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case StationaryState.Idle:
                UpdateIdleState(distanceToPlayer);
                break;
            case StationaryState.Chase:
                UpdateChaseState(distanceToPlayer);
                break;
            case StationaryState.Attack:
                UpdateAttackState(distanceToPlayer);
                break;
            case StationaryState.Return:
                UpdateReturnState(distanceToPlayer);
                break;
        }
    }

    private void UpdateIdleState(float distanceToPlayer)
    {
        IsAlert = false;
        
        if (distanceToPlayer <= detectionRange)
        {
            currentState = StationaryState.Chase;
            return;
        }

        // Return to original facing direction when idle at spawn
        transform.rotation = Quaternion.Slerp(transform.rotation, spawnRotation, Time.deltaTime * rotationSpeed);
    }

    private void UpdateChaseState(float distanceToPlayer)
    {
        IsAlert = true;

        if (distanceToPlayer > leashRange)
        {
            currentState = StationaryState.Return;
            if (agent != null && agent.isOnNavMesh) agent.SetDestination(spawnPosition);
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            currentState = StationaryState.Attack;
            if (agent != null && agent.isOnNavMesh && agent.hasPath) agent.ResetPath();
            return;
        }

        if (agent != null && agent.isOnNavMesh) agent.SetDestination(playerTransform.position);
        SmoothRotateTowards(playerTransform.position);
    }

    private void UpdateAttackState(float distanceToPlayer)
    {
        IsAlert = true;

        // If we just swung our sword, we are locked in the animation and cannot chase yet!
        if (attackLockTimer > 0)
        {
            SmoothRotateTowards(playerTransform.position);
            return;
        }

        if (distanceToPlayer > attackRange)
        {
            currentState = StationaryState.Chase;
            return;
        }

        SmoothRotateTowards(playerTransform.position);

        if (attackTimer <= 0)
        {
            PerformAttack();
            // Idle for a random duration between attacks based on inspector settings
            attackTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
            attackLockTimer = 1.2f; // Lock movement for 1.2 seconds while animation plays
            
            // Ensure they stop moving when attacking and idling
            if (agent != null && agent.isOnNavMesh && agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }

    private void UpdateReturnState(float distanceToPlayer)
    {
        IsAlert = false;

        // Can still detect player on the way back
        if (distanceToPlayer <= detectionRange)
        {
            currentState = StationaryState.Chase;
            return;
        }

        if (agent != null && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentState = StationaryState.Idle;
            // Snap position exactly just in case
            if (agent.isOnNavMesh) agent.ResetPath();
        }
    }

    private void PerformAttack()
    {
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
