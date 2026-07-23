using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Abstract base class for all enemy AI types.
/// Handles core logic such as health, taking damage, blocking, and component references.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the enemy.")]
    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    [Header("Attack Settings")]
    [Tooltip("Damage dealt by the enemy's attack.")]
    [SerializeField] protected float attackDamage = 20f;
    [Tooltip("Minimum time (in seconds) the enemy idles between attacks.")]
    [SerializeField] protected float minAttackCooldown = 2.0f;
    [Tooltip("Maximum time (in seconds) the enemy idles between attacks.")]
    [SerializeField] protected float maxAttackCooldown = 3.0f;
    [Tooltip("Distance within which the enemy can attack the player.")]
    [SerializeField] protected float attackRange = 2f;
    [Tooltip("Delay in seconds before the attack damage is applied (no animation events required).")]
    [SerializeField] protected float attackHitDelay = 0.5f;
    [Tooltip("How long the weapon colliders stay active during an attack (in seconds).")]
    [SerializeField] protected float attackActiveDuration = 0.3f;
    [Tooltip("Distance the enemy keeps when the player is blocking.")]
    [SerializeField] protected float blockingStoppingDistance = 3.0f;

    [Header("Movement & Rotation")]
    [Tooltip("Speed at which the enemy rotates towards its target or movement direction.")]
    [SerializeField] protected float rotationSpeed = 5f;

    [Header("Advanced AI Settings")]
    [Tooltip("Speed while strafing.")]
    [SerializeField] protected float strafeSpeed = 2f;
    [Tooltip("Distance to jump backwards during a backstep.")]
    [SerializeField] protected float backstepDistance = 3f;
    [Tooltip("Time it takes to complete a backstep.")]
    [SerializeField] protected float backstepDuration = 0.4f;

    [Header("Weapon Colliders")]
    [Tooltip("Assign the GameObjects for the sword, foot, etc. The EnemyWeaponDamage script will be added automatically if missing.")]
    [SerializeField] protected GameObject[] weaponObjects;

    protected EnemyWeaponDamage[] weaponColliders;

    [Header("Audio Settings")]
    [SerializeField] protected AudioClip footstepSound;
    [SerializeField] protected AudioClip impactSound;
    [SerializeField] protected AudioClip deathSound;
    [Tooltip("The swash/slash sound of the weapon swinging.")]
    [SerializeField] protected AudioClip swingSound;
    [SerializeField] protected AudioClip growlSound;
    [SerializeField] protected float footstepInterval = 0.5f;
    
    [Header("Loot Drop")]
    [Tooltip("The item prefab (like an Elixir) to spawn when this enemy dies.")]
    [SerializeField] protected GameObject dropPrefab;
    
    [Tooltip("Chance (0 to 1) that the item drops on death.")]
    [Range(0f, 1f)]
    [SerializeField] protected float dropChance = 1f;

    protected AudioSource audioSource;
    protected float nextFootstepTime;

    protected float attackTimer;
    protected float attackLockTimer;
    
    // Components
    protected Animator animator;
    protected NavMeshAgent agent;
    protected Collider enemyCollider;

    // References
    protected Transform playerTransform;
    protected PlayerController playerController;
    protected float defaultStoppingDistance;

    // State
    protected bool isDead = false;
    protected bool isSliding = false;

    /// <summary>
    /// Override in subclasses to mark this enemy as a boss so its hits
    /// trigger a knockback slide on the player.
    /// </summary>
    protected virtual bool IsBoss => false;

    // Animator Hashes for performance
    protected readonly int speedHash = Animator.StringToHash("Speed");
    protected readonly int attack1Hash = Animator.StringToHash("Attack1");
    protected readonly int attack2Hash = Animator.StringToHash("Attack2");
    protected readonly int hitHash = Animator.StringToHash("Hit");
    protected readonly int deathHash = Animator.StringToHash("Death");
    protected readonly int isAlertHash = Animator.StringToHash("IsAlert");

    // Combat State
    private bool _isAlert;
    protected bool IsAlert
    {
        get => _isAlert;
        set
        {
            if (_isAlert != value)
            {
                _isAlert = value;
                if (animator != null)
                {
                    animator.SetBool(isAlertHash, _isAlert);
                }
                if (_isAlert && growlSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(growlSound);
                }
            }
        }
    }

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        
        if (agent != null)
        {
            // Disable auto rotation so we can smoothly rotate it via script using velocity
            agent.updateRotation = false;
        }

        enemyCollider = GetComponentInChildren<Collider>();

        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
        
        if (agent != null)
        {
            defaultStoppingDistance = agent.stoppingDistance;
        }

        if (weaponObjects != null && weaponObjects.Length > 0)
        {
            weaponColliders = new EnemyWeaponDamage[weaponObjects.Length];
            for (int i = 0; i < weaponObjects.Length; i++)
            {
                if (weaponObjects[i] != null)
                {
                    EnemyWeaponDamage dmg = weaponObjects[i].GetComponent<EnemyWeaponDamage>();
                    if (dmg == null)
                    {
                        dmg = weaponObjects[i].AddComponent<EnemyWeaponDamage>();
                    }
                    // Tag the weapon so it knows who owns it and whether to apply boss knockback
                    dmg.AttackerTransform = transform;
                    dmg.IsFromBoss = IsBoss;
                    weaponColliders[i] = dmg;
                }
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
        if (attackLockTimer > 0)
        {
            attackLockTimer -= Time.deltaTime;
        }

        if (agent != null && agent.isOnNavMesh && playerController != null)
        {
            if (playerController.IsBlocking)
            {
                agent.stoppingDistance = blockingStoppingDistance;
            }
            else
            {
                agent.stoppingDistance = defaultStoppingDistance;
            }
        }

        UpdateAI();
        UpdateMovementRotation();
        UpdateAnimator();

        // Prevent footstep sounds from playing while the enemy is actively swinging/attacking
        if (attackLockTimer <= 0 && agent != null && agent.velocity.magnitude > 0.1f && Time.time >= nextFootstepTime)
        {
            nextFootstepTime = Time.time + footstepInterval;
            if (footstepSound != null) audioSource.PlayOneShot(footstepSound, 0.4f);
        }
    }

    /// <summary>
    /// Rotates the enemy to face its movement velocity if it is moving.
    /// </summary>
    protected virtual void UpdateMovementRotation()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // Only rotate when the agent is actually moving (velocity threshold)
            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                Vector3 direction = agent.velocity.normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }
    }

    /// <summary>
    /// Smoothly rotates the enemy to face a specific target position.
    /// Useful for attacking or facing the player when not moving.
    /// </summary>
    protected virtual void SmoothRotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// Implemented by child classes to define specific AI state machines.
    /// </summary>
    protected abstract void UpdateAI();

    /// <summary>
    /// Updates common animator parameters.
    /// </summary>
    protected virtual void UpdateAnimator()
    {
        if (agent != null && agent.enabled)
        {
            if (isSliding)
            {
                animator.SetFloat(speedHash, 0f);
            }
            else
            {
                // Set speed for animator (0 = idle, 1 = walking)
                animator.SetFloat(speedHash, agent.velocity.magnitude > 0.1f ? 1f : 0f);
            }
        }
        else if (animator != null)
        {
            animator.SetFloat(speedHash, 0f);
        }
    }

    /// <summary>
    /// Called when the enemy receives damage.
    /// Plays hit animation and instantiates damage popups.
    /// </summary>
    /// <param name="amount">Amount of damage to inflict.</param>
    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        // Hit taken
        currentHealth -= amount;
        animator.SetTrigger(hitHash);
        if (impactSound != null && audioSource != null) audioSource.PlayOneShot(impactSound);

        // Interrupt any current attack!
        attackLockTimer = 0f;
        CancelInvoke(nameof(EnableWeapons));
        DisableWeapons();
        // Reset attack timer so they don't immediately attack after getting stunned
        attackTimer = maxAttackCooldown;

        // Briefly stop movement for a "stagger" effect and apply knockback
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            
            // Push the enemy backwards away from the player's direction smoothly
            if (playerTransform != null)
            {
                Vector3 pushDirection = (transform.position - playerTransform.position).normalized;
                pushDirection.y = 0; // Keep knockback flat on the ground
                
                // Stop any existing knockback coroutines to prevent them from stacking
                StopAllCoroutines(); 
                StartCoroutine(SmoothKnockback(pushDirection, 1.5f, 0.2f));
            }

            CancelInvoke(nameof(ResumeMovement));
            Invoke(nameof(ResumeMovement), 1.0f); // Stun duration
        }

        // Use the top of the enemy's collider bounds so the popup always appears
        // above the head, even for large/scaled enemies like the Boss.
        float popupHeight = (enemyCollider != null)
            ? enemyCollider.bounds.max.y + 0.3f
            : transform.position.y + 1.5f;
        Vector3 damagePopupPosition = new Vector3(transform.position.x, popupHeight, transform.position.z);
        DamagePopup.Create(damagePopupPosition, Mathf.RoundToInt(amount));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the death process of the enemy, including animations and disabling components.
    /// </summary>
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetTrigger(deathHash);
        if (deathSound != null && audioSource != null) audioSource.PlayOneShot(deathSound);

        if (agent != null) agent.enabled = false;
        if (enemyCollider != null) enemyCollider.enabled = false;

        if (dropPrefab != null && Random.value <= dropChance)
        {
            // Spawn the drop slightly above the enemy's root so it doesn't clip into the ground
            Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // Enemies no longer disappear after death
        // Destroy(gameObject, 3f);
    }

    protected void ResumeMovement()
    {
        if (!isDead && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private System.Collections.IEnumerator SmoothKnockback(Vector3 direction, float distance, float duration)
    {
        isSliding = true;
        float time = 0;
        float speed = distance / duration;

        while (time < duration)
        {
            if (isDead || agent == null || !agent.isOnNavMesh) break;
            
            agent.Move(direction * speed * Time.deltaTime);
            time += Time.deltaTime;
            
            yield return null;
        }
        isSliding = false;
    }



    protected int currentWeaponIndex = -1;

    /// <summary>
    /// Executes the attack animation and schedules the weapon collider activation.
    /// </summary>
    protected virtual void ExecuteAttack(int animHash)
    {
        currentWeaponIndex = -1;
        animator.SetTrigger(animHash);
        if (swingSound != null && audioSource != null) audioSource.PlayOneShot(swingSound);
        // Enable colliders after the delay to sync with animation swing
        Invoke(nameof(EnableWeapons), attackHitDelay);
    }

    /// <summary>
    /// Executes the attack animation and specifies which weapon index to enable.
    /// </summary>
    protected virtual void ExecuteAttack(int animHash, int weaponIndex)
    {
        currentWeaponIndex = weaponIndex;
        animator.SetTrigger(animHash);
        if (swingSound != null && audioSource != null) audioSource.PlayOneShot(swingSound);
        Invoke(nameof(EnableWeapons), attackHitDelay);
    }

    /// <summary>
    /// Enables assigned weapon colliders to deal damage.
    /// </summary>
    public virtual void EnableWeapons()
    {
        if (isDead) return;

        if (weaponColliders != null && weaponColliders.Length > 0)
        {
            if (currentWeaponIndex == -1)
            {
                // Enable all weapons
                foreach (var weapon in weaponColliders)
                {
                    if (weapon != null) weapon.EnableDamage(attackDamage);
                }
            }
            else if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponColliders.Length)
            {
                // Enable only the specified weapon
                if (weaponColliders[currentWeaponIndex] != null) 
                {
                    weaponColliders[currentWeaponIndex].EnableDamage(attackDamage);
                }
            }

            // Automatically disable colliders after a short duration 
            // if you don't use an Animation Event to disable them.
            Invoke(nameof(DisableWeapons), attackActiveDuration);
        }
    }

    protected virtual void OnDestroy()
    {
#if UNITY_EDITOR
        // Ensure we deselect the object at the EXACT moment it gets destroyed
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            UnityEditor.Selection.activeGameObject = null;
        }
#endif
    }

    /// <summary>
    /// Disables all assigned weapon colliders.
    /// </summary>
    public virtual void DisableWeapons()
    {
        if (weaponColliders != null)
        {
            foreach (var weapon in weaponColliders)
            {
                if (weapon != null)
                {
                    weapon.DisableDamage();
                }
            }
        }
    }
}
