using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("Maximum health of the enemy.")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Call this to deal damage to the enemy.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        // Spawn the floating text slightly above the enemy
        Vector3 popupPosition = transform.position + Vector3.up * 1.5f;
        DamagePopup.Create(popupPosition, damageAmount);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // TODO: Add death logic here (e.g., play death animation, drop loot)
        
        // Optional: Destroy the enemy GameObject after a short delay
        // Destroy(gameObject, 0.1f);
    }
}
