using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        // Don't take damage if already dead
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        Debug.Log($"Demon took {amount} damage. Current Health: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Inform AI about damage to trigger Damage Reaction (Hurt state)
            if (enemyAI != null)
            {
                enemyAI.OnDamageTaken();
            }
        }
    }

    private void Die()
    {
        if (enemyAI != null)
        {
            enemyAI.ChangeState(EnemyAI.State.Dead);
        }
        
        // Disable main collider and scripts to act as a corpse
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Assuming we have a ragdoll or a death animation
        // If Death animation:
        // GetComponent<EnemyAnimator>().TriggerDeath(); (Requires adding death trigger to animator)
        
        this.enabled = false;
    }
}
