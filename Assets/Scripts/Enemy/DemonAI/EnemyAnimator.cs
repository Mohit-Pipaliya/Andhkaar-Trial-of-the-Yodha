using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    [Header("Settings")]
    public float animationDampTime = 0.15f;

    private Animator animator;
    private float targetSpeed;
    private float currentSpeedVelocity;

    // Animator Hashes for optimization
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isAttackingHash = Animator.StringToHash("IsAttacking");
    private readonly int damageHash = Animator.StringToHash("Damage");
    private readonly int attackIndexHash = Animator.StringToHash("AttackIndex");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found in children. Please ensure the Demon model has an Animator component.");
        }
    }

    private void Update()
    {
        if (animator == null) return;

        // Smooth transition for speed using SmoothDamp to prevent snapping/foot sliding
        float currentSpeed = animator.GetFloat(speedHash);
        float smoothSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref currentSpeedVelocity, animationDampTime);
        
        // Ensure we cleanly stop at 0 if extremely close
        if (targetSpeed == 0f && smoothSpeed < 0.01f) smoothSpeed = 0f;

        animator.SetFloat(speedHash, smoothSpeed);
    }

    public void UpdateSpeed(float target)
    {
        targetSpeed = target;
    }

    public void SetAttacking(bool isAttacking)
    {
        if (animator != null)
        {
            animator.SetBool(isAttackingHash, isAttacking);
        }
    }

    public void TriggerDamage()
    {
        if (animator != null)
        {
            animator.SetTrigger(damageHash);
        }
    }

    public void SetAttackIndex(int index)
    {
        if (animator != null)
        {
            animator.SetFloat(attackIndexHash, (float)index);
        }
    }
}
