using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Settings")]
    public float attackCooldown = 1.5f;
    public Collider attackHitbox; // Assign a trigger collider in inspector (e.g. on the hand or weapon)

    private EnemyAnimator enemyAnimator;
    private EnemyAI enemyAI;
    
    private float lastAttackTime;
    private int lastAttackIndex = -1;
    private int sameAttackCount = 0;
    
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        enemyAnimator = GetComponent<EnemyAnimator>();
        enemyAI = GetComponent<EnemyAI>();
        
        if (attackHitbox != null)
        {
            attackHitbox.isTrigger = true;
            attackHitbox.enabled = false;
        }
    }

    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown && !IsAttacking;
    }

    public void StartAttack()
    {
        if (!CanAttack()) return;

        IsAttacking = true;
        enemyAnimator.SetAttacking(true);

        // Randomly choose between Attack (0) and Attack1 (1)
        int attackIndex = ChooseAttackIndex();
        enemyAnimator.SetAttackIndex(attackIndex);
    }

    private int ChooseAttackIndex()
    {
        int newAttack = Random.Range(0, 2); // 0 or 1

        if (newAttack == lastAttackIndex)
        {
            sameAttackCount++;
            if (sameAttackCount >= 2) // Prevent same attack repeating more than 2 times
            {
                newAttack = (lastAttackIndex == 0) ? 1 : 0;
                sameAttackCount = 0;
            }
        }
        else
        {
            sameAttackCount = 0;
        }

        lastAttackIndex = newAttack;
        return newAttack;
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        enemyAnimator.SetAttacking(false);
        DisableHitbox();
    }

    #region Animation Events
    // ---------------------------------------------------------
    // These methods MUST be called via Animation Events on the 
    // Attack and Attack1 animation clips in the Unity Editor.
    // ---------------------------------------------------------

    public void EnableHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    public void FinishAttack()
    {
        IsAttacking = false;
        enemyAnimator.SetAttacking(false);
        lastAttackTime = Time.time;
        
        // Notify AI that the attack has finished so it can evaluate next state
        if (enemyAI != null)
        {
            enemyAI.OnAttackFinished();
        }
    }
    #endregion
}
