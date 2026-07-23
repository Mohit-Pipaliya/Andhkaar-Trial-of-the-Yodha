using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Managers;

/// <summary>
/// Attach this script to the enemy's attack colliders (e.g., sword or foot).
/// Requires a Collider with "Is Trigger" checked and a Rigidbody with "Is Kinematic" checked.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyWeaponDamage : MonoBehaviour
{
    [Tooltip("Amount of damage this weapon deals.")]
    public float damageAmount = 20f;

    [Header("Visual Effects")]
    [Tooltip("Optional: Drag your impact particle prefab here. Spawned when hitting the player or shield.")]
    public GameObject impactEffectPrefab;

    [Header("Combat Feel Settings")]
    [Tooltip("Sound played when hitting the player directly (flesh).")]
    public AudioClip hitFleshSound;
    
    [Tooltip("Sound played when hitting the player's shield (blocking).")]
    public AudioClip hitBlockSound;

    [Tooltip("Reference to a Cinemachine Impulse Source for screen shake on hit.")]
    public CinemachineImpulseSource impulseSource;

    private Collider _weaponCollider;
    private List<GameObject> _alreadyHitTargets = new List<GameObject>();
    private bool _canDealDamage = false;

    /// <summary>Set to true for boss weapon colliders to trigger knockback on the player.</summary>
    [HideInInspector] public bool IsFromBoss = false;

    /// <summary>The transform of the attacker — used to calculate knockback direction.</summary>
    [HideInInspector] public Transform AttackerTransform;

    private void Awake()
    {
        _weaponCollider = GetComponent<Collider>();
        _weaponCollider.enabled = false;
        _weaponCollider.isTrigger = true;
    }

    /// <summary>
    /// Enables the damage collider. Called by EnemyBase (usually via Animation Event).
    /// </summary>
    public void EnableDamage(float customDamage = -1f)
    {
        if (customDamage >= 0)
        {
            damageAmount = customDamage;
        }
        _alreadyHitTargets.Clear();
        _canDealDamage = true;
        if (_weaponCollider != null) _weaponCollider.enabled = true;
    }

    /// <summary>
    /// Disables the damage collider. Called by EnemyBase (usually via Animation Event).
    /// </summary>
    public void DisableDamage()
    {
        _canDealDamage = false;
        if (_weaponCollider != null) _weaponCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_canDealDamage) return;

        bool spawnedImpact = false;
        AudioClip soundToPlay = null;

        // Check if we hit the player's shield
        PlayerShield shield = other.GetComponent<PlayerShield>();
        if (shield != null && shield.playerController != null)
        {
            GameObject targetObj = shield.playerController.gameObject;
            if (_alreadyHitTargets.Contains(targetObj)) return;

            if (shield.playerController.IsBlocking)
            {
                // Add the root object to avoid multi-hits in the same swing
                _alreadyHitTargets.Add(targetObj);
                spawnedImpact = true;
                soundToPlay = hitBlockSound;
                
                TriggerCombatFeel(other, spawnedImpact, soundToPlay);
                return; // Deal no damage
            }
        }

        // Check if we hit the player directly
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            GameObject targetObj = player.gameObject;
            if (_alreadyHitTargets.Contains(targetObj)) return;

            _alreadyHitTargets.Add(targetObj);
            spawnedImpact = true;
            soundToPlay = hitFleshSound;

            // Boss weapons trigger a knockback slide on the player.
            // Regular enemy weapons just deal plain damage.
            if (IsFromBoss && AttackerTransform != null)
            {
                player.TakeDamageWithKnockback(damageAmount, AttackerTransform.position);
            }
            else
            {
                player.TakeDamage(damageAmount, AttackerTransform != null ? AttackerTransform.position : transform.position);
            }
            
            TriggerCombatFeel(other, spawnedImpact, soundToPlay);
        }
    }

    private void TriggerCombatFeel(Collider other, bool spawnedImpact, AudioClip soundToPlay)
    {
        if (spawnedImpact)
        {
            // Visuals
            if (impactEffectPrefab != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Instantiate(impactEffectPrefab, hitPoint, Quaternion.identity);
            }

            // Audio
            if (soundToPlay != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClipAtPoint(soundToPlay, transform.position);
            }

            // Game Feel
            if (CombatFeelManager.Instance != null)
            {
                // Slightly shorter hit stop for enemy attacks so the player isn't completely helpless
                CombatFeelManager.Instance.HitStop(0.02f, 0.2f);
            }
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse();
            }
        }
    }
}
