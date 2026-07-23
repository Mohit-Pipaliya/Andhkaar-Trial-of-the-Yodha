using UnityEngine;

public class SwordDamage : MonoBehaviour
{
    public int damageAmount = 100;
    
    private PlayerController player;
    private float lastHitTime = 0f;
    private float hitCooldown = 0.5f; // Ek hit ke baad thodi der tak wapas nahi lagega (taaki ek hi swing me 5 hit na lag jaye)

    void Start()
    {
        // PlayerController script dhundh rahe hain taaki pata chale ki player abhi attack kar raha hai ya nahi
        player = GetComponentInParent<PlayerController>();
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider other)
    {
        // Agar player swing (attack) nahi kar raha hai, to sirf touch hone se damage nahi lagega
        if (player != null && !player.IsAttacking) return;

        // Cooldown check
        if (Time.time < lastHitTime + hitCooldown) return;

        // Check if we hit an enemy
        EnemyAi enemy = other.GetComponent<EnemyAi>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyAi>();

        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
            Debug.Log("<color=cyan>Sword Collider ne Enemy ko " + damageAmount + " damage diya!</color>");
            lastHitTime = Time.time;
        }
        else
        {
            // Agar EnemyAi nahi mila, to check karo ki naya EnemyAiNoPatrol wala dushman to nahi hai
            EnemyAiNoPatrol enemyNoPatrol = other.GetComponent<EnemyAiNoPatrol>();
            if (enemyNoPatrol == null) enemyNoPatrol = other.GetComponentInParent<EnemyAiNoPatrol>();

            if (enemyNoPatrol != null)
            {
                enemyNoPatrol.TakeDamage(damageAmount);
                Debug.Log("<color=cyan>Sword Collider ne Stationary Enemy ko " + damageAmount + " damage diya!</color>");
                lastHitTime = Time.time;
            }
            else
            {
                BossEnemyAi boss = other.GetComponent<BossEnemyAi>();
                if (boss == null) boss = other.GetComponentInParent<BossEnemyAi>();
                
                if (boss != null)
                {
                    boss.TakeDamage(300); // Player boss ko seedha 300 damage dega ek hit me
                    Debug.Log("<color=cyan>Sword Collider ne Boss ko 300 damage diya!</color>");
                    lastHitTime = Time.time;
                }
            }
        }
    }
}
