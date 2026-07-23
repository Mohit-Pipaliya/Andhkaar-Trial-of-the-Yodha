using UnityEngine;
using System.Collections;

/// <summary>
/// Boss Enemy AI that features two phases and a special jump attack.
/// Inherits from EnemyBase.
/// </summary>
public class BossEnemy : EnemyBase
{
    public enum BossState
    {
        Idle,
        Chase,
        Attack,
        JumpAttack
    }

    [Header("Boss Specific Settings")]
    [Tooltip("Health threshold to enter Enrage phase (e.g., 0.5 for 50%).")]
    [SerializeField] private float enrageHealthPercentage = 0.5f;
    [Tooltip("Movement speed multiplier during Enrage phase.")]
    [SerializeField] private float enrageSpeedMultiplier = 1.5f;
    [Tooltip("Attack cooldown multiplier during Enrage phase (lower is faster).")]
    [SerializeField] private float enrageCooldownMultiplier = 0.5f;
    
    [Header("Reset Settings")]
    [Tooltip("Distance at which the boss will detect the player and start fighting.")]
    [SerializeField] private float aggroRange = 15f;
    [Tooltip("Distance at which the boss will reset its health and enrage state if the player runs away.")]
    [SerializeField] private float fightResetRange = 25f;

    [Header("Jump Attack Settings")]
    [Tooltip("Distance at which the boss will choose to do a jump attack instead of walking.")]
    [SerializeField] private float jumpAttackTriggerRange = 10f;
    [Tooltip("How long the jump animation takes (used to time the movement).")]
    [SerializeField] private float jumpDuration = 1.0f;
    [Tooltip("How high the boss jumps during the jump attack.")]
    [SerializeField] private float jumpHeight = 3.0f;
    [Tooltip("Radius of the AoE damage when the boss lands.")]
    [SerializeField] private float jumpLandingDamageRadius = 3.0f;
    [Tooltip("Sound played when the boss jumps.")]
    [SerializeField] private AudioClip jumpAttackSound;
    
    private BossState currentState = BossState.Idle;
    private bool isEnraged = false;
    private bool isJumping = false;
    private bool _useLeftHandNext = false;
    
    // Advanced AI variables
    private int comboCounter = 0;

    // Mark this enemy as the boss so weapon hits trigger player knockback
    protected override bool IsBoss => true;
    
    // Additional Animator Hash
    private readonly int enrageHash = Animator.StringToHash("Enrage"); // Optional trigger for enrage roar

    [Tooltip("The exact name of the roar animation state in the Animator. The growl sound will play once when this state is active.")]
    [SerializeField] private string roarAnimationStateName = "Roar";
    
    private bool _wasInRoarState = false;

    protected override void UpdateAI()
    {
        if (animator != null && audioSource != null && growlSound != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isInRoarState = stateInfo.IsName(roarAnimationStateName);

            if (isInRoarState && !_wasInRoarState)
            {
                audioSource.PlayOneShot(growlSound);
            }
            _wasInRoarState = isInRoarState;
        }

        if (playerTransform == null || isJumping) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > fightResetRange && currentState != BossState.Idle)
        {
            ResetBoss();
        }

        switch (currentState)
        {
            case BossState.Idle:
                UpdateIdleState(distanceToPlayer);
                break;
            case BossState.Chase:
                UpdateChaseState(distanceToPlayer);
                break;
            case BossState.Attack:
                UpdateAttackState(distanceToPlayer);
                break;
        }
    }

    private void UpdateIdleState(float distanceToPlayer)
    {
        IsAlert = false;
        
        if (distanceToPlayer <= aggroRange)
        {
            currentState = BossState.Chase;
        }
    }

    private void UpdateChaseState(float distanceToPlayer)
    {
        IsAlert = true;

        // If we are close enough to attack normally
        if (distanceToPlayer <= attackRange)
        {
            currentState = BossState.Attack;
            
            // Give the player a tiny break (0.5s) before the boss swings after chasing them down
            if (attackTimer <= 0 && comboCounter == 0)
            {
                attackTimer = 0.5f;
            }
            
            if (agent.isOnNavMesh) agent.ResetPath();
            return;
        }
        
        // If we are far enough to trigger a jump attack, and we can attack
        if (distanceToPlayer > attackRange && distanceToPlayer <= jumpAttackTriggerRange && attackTimer <= 0)
        {
            // 50% chance to jump attack if in range, otherwise just chase normally
            if (Random.value > 0.5f)
            {
                StartJumpAttack();
                return;
            }
            else
            {
                // Reset timer slightly so it doesn't spam the check every frame
                attackTimer = 1.0f; 
            }
        }

        // Direct charging behavior
        if (agent.isOnNavMesh) 
        {
            agent.isStopped = false; // Ensure they can move again
            
            // Reset speed to normal 
            float baseSpeed = 4.5f; // Example base boss speed
            if (isEnraged) baseSpeed *= enrageSpeedMultiplier;
            agent.speed = baseSpeed; 
            
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
        
        // Wait for cooldown
        if (attackTimer > 0)
        {
            SmoothRotateTowards(playerTransform.position);
            return;
        }

        // Out of range -> Chase
        if (distanceToPlayer > attackRange && comboCounter == 0)
        {
            currentState = BossState.Chase;
            return;
        }

        SmoothRotateTowards(playerTransform.position);

        // Alternate between left and right hand
        if (_useLeftHandNext)
        {
            animator.SetBool("MirrorAttack", true);
            ExecuteAttack(attack1Hash, 1); // 1 = Left Hand index
        }
        else
        {
            animator.SetBool("MirrorAttack", false);
            ExecuteAttack(attack1Hash, 0); // 0 = Right Hand index
        }
        
        // Toggle for the next attack (mostly alternate, but occasionally repeat same hand in combos)
        if (Random.value > 0.2f)
        {
            _useLeftHandNext = !_useLeftHandNext;
        }
        // Attack finished, start full cooldown
        float cooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
        if (isEnraged) cooldown *= enrageCooldownMultiplier;
        
        attackTimer = cooldown;
        attackLockTimer = 1.2f;
        
        if (agent != null && agent.isOnNavMesh)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void StartJumpAttack()
    {
        if (isJumping) return;
        isJumping = true;
        
        if (agent.isOnNavMesh) agent.ResetPath();
        
        // Face the player before jumping
        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
        
        // Trigger Jump Attack Animation (reusing Attack2)
        animator.SetTrigger(attack2Hash);

        if (jumpAttackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpAttackSound);
        }
        
        // Lock attacks and start the jump movement coroutine
        attackLockTimer = jumpDuration + 0.5f; 
        
        float cooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
        if (isEnraged) cooldown *= enrageCooldownMultiplier;
        attackTimer = cooldown + jumpDuration;

        StartCoroutine(JumpRoutine(playerTransform.position));
    }

    private IEnumerator JumpRoutine(Vector3 targetPosition)
    {
        if (agent != null) agent.enabled = false; // Disable NavMeshAgent so we can move manually

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        // Enable weapons to deal damage if we land on or hit the player mid-air
        EnableWeapons();

        while (elapsed < jumpDuration)
        {
            if (isDead) yield break;

            elapsed += Time.deltaTime;
            float percent = elapsed / jumpDuration;
            
            // Move horizontally
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, percent);
            
            // Add vertical arc (parabola)
            currentPos.y += Mathf.Sin(percent * Mathf.PI) * jumpHeight;
            
            transform.position = currentPos;
            yield return null;
        }

        // Ensure we end up exactly on the ground
        transform.position = new Vector3(targetPosition.x, startPosition.y, targetPosition.z);
        
        // --- Landing AoE Damage ---
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, jumpLandingDamageRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                PlayerController player = hitCollider.GetComponent<PlayerController>();
                if (player != null)
                {
                    // Deal exactly 100 damage and stun for 1 second, plus a strong knockback slide
                    player.TakeDamageWithKnockback(100f, transform.position);
                    player.Stun(1.0f);
                }
            }
        }
        
        DisableWeapons();
        isJumping = false;
        currentState = BossState.Chase;
        
        if (agent != null && !isDead) agent.enabled = true;
    }

    // Coroutine name constant to allow targeted stopping without killing JumpRoutine
    private Coroutine _knockbackCoroutine;

    public override void TakeDamage(float amount)
    {
        if (isDead) return;

        // Deduct health
        currentHealth -= amount;

        // Play impact sound even though boss doesn't flinch
        if (impactSound != null && audioSource != null) audioSource.PlayOneShot(impactSound);

        // Do NOT play hit animation — boss is too powerful to flinch.
        // We also do NOT reset the attack timer or change state, so the boss 
        // can keep attacking you even while you are hitting it!

        // Boss slides backward away from the player.
        // Only stop the PREVIOUS knockback coroutine — NOT all coroutines
        // (stopping all would kill JumpRoutine and leave the agent disabled).
        if (agent != null && agent.isOnNavMesh && playerTransform != null)
        {
            agent.isStopped = true;

            Vector3 pushDirection = (transform.position - playerTransform.position).normalized;
            pushDirection.y = 0f;

            if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = StartCoroutine(BossKnockbackSlide(pushDirection, distance: 3.0f, duration: 0.3f));

            CancelInvoke(nameof(ResumeMovement));
            Invoke(nameof(ResumeMovement), 0.3f);
        }

        // Damage popup at the vertical center of the boss so it is always in camera view.
        // Using bounds.center.y keeps it near the chest/head area regardless of boss scale.
        float popupHeight = (enemyCollider != null)
            ? enemyCollider.bounds.center.y
            : transform.position.y + 1.0f;
        Vector3 damagePopupPosition = new Vector3(transform.position.x, popupHeight, transform.position.z);
        DamagePopup.Create(damagePopupPosition, Mathf.RoundToInt(amount));

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        CheckEnragePhase();
    }

    /// <summary>
    /// Slides the boss backward using NavMeshAgent.Move with an ease-out curve
    /// so it feels weighty rather than snapping.
    /// </summary>
    private System.Collections.IEnumerator BossKnockbackSlide(Vector3 direction, float distance, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isDead || agent == null || !agent.isOnNavMesh) yield break;

            // Ease-out: fast at first, slows to a stop
            float t = 1f - (elapsed / duration);
            float speed = (distance / duration) * t;

            agent.Move(direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void ResetBoss()
    {
        currentHealth = maxHealth;
        currentState = BossState.Idle;
        IsAlert = false;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        if (isEnraged)
        {
            isEnraged = false;
            
            if (agent != null)
            {
                agent.speed /= enrageSpeedMultiplier;
                agent.acceleration /= enrageSpeedMultiplier;
            }
        }
    }

    private void CheckEnragePhase()
    {
        if (isEnraged || isDead) return;

        if (currentHealth <= maxHealth * enrageHealthPercentage)
        {
            isEnraged = true;
            
            // Optional: Play a roar animation
            // animator.SetTrigger(enrageHash);
            // attackLockTimer = 2.0f; // Lock while roaring

            if (agent != null)
            {
                agent.speed *= enrageSpeedMultiplier;
                agent.acceleration *= enrageSpeedMultiplier;
            }
        }
    }
    
}
