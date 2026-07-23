using UnityEngine;
using Unity.Cinemachine;

namespace PlayerSystems
{
    /// <summary>
    /// Bridges the TargetingSystem with Cinemachine 3.
    /// Uses a standard Third-Person lock-on: Follows the Player, Looks at the Enemy.
    /// </summary>
    public class CinemachineTargetingBridge : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The TargetingSystem on your Player.")]
        public TargetingSystem targetingSystem;
        
        [Tooltip("The Virtual Camera designed for Lock-On mode.")]
        public CinemachineCamera lockOnCamera;
        
        [Tooltip("The point on the player to track (e.g., Spine or Head).")]
        public Transform playerTarget;

        [Header("Shoulder Offset")]
        [Tooltip("Offset relative to the player to position the camera (e.g., over the right shoulder) to avoid the player blocking the view of the target.")]
        public Vector3 shoulderOffset = new Vector3(1.0f, 0.5f, -1.0f);

        private Transform _previousTarget;
        private Transform _shoulderProxy;

        private void Start()
        {
            // Ensure the lock-on camera is turned off by default
            if (lockOnCamera != null)
            {
                lockOnCamera.gameObject.SetActive(false);
            }

            // Create a proxy target for the camera to follow so it sits offset to the shoulder
            if (targetingSystem != null)
            {
                _shoulderProxy = new GameObject("TargetingShoulderProxy").transform;
                // Unparent it so it doesn't spin wildly during player attack animations
                _shoulderProxy.SetParent(null); 
            }
        }

        private void LateUpdate()
        {
            if (targetingSystem == null || lockOnCamera == null) return;

            Transform currentTarget = targetingSystem.CurrentTarget;

            // Handle lock-on state changes
            if (currentTarget != _previousTarget)
            {
                // Intentionally keep the lock-on camera disabled so the player
                // can move the mouse freely using the standard free-look camera.
                // The character will still auto-aim attacks at the target!
                lockOnCamera.gameObject.SetActive(false);
                
                _previousTarget = currentTarget;
            }
        }

        private void UpdateProxyPosition(float lerpSpeed)
        {
            Transform pTarget = playerTarget != null ? playerTarget : targetingSystem.transform;
            Transform enemy = targetingSystem.CurrentTarget;

            // 1. Direction from player to enemy (flat on ground)
            Vector3 dirToEnemy = enemy.position - pTarget.position;
            dirToEnemy.y = 0;
            if (dirToEnemy.sqrMagnitude < 0.001f) dirToEnemy = pTarget.forward;
            dirToEnemy.Normalize();

            // 2. Calculate local axes based on the direction to the enemy
            Vector3 right = Vector3.Cross(Vector3.up, dirToEnemy).normalized;
            Vector3 up = Vector3.up;

            // 3. Ideal position (shoulderOffset: X = right, Y = up, Z = forward towards enemy)
            // Note: shoulderOffset.z should be negative to sit behind the player
            Vector3 idealPosition = pTarget.position 
                                  + right * shoulderOffset.x 
                                  + up * shoulderOffset.y 
                                  + dirToEnemy * shoulderOffset.z;

            // 4. Move proxy smoothly
            if (lerpSpeed >= 1f)
            {
                _shoulderProxy.position = idealPosition;
            }
            else
            {
                _shoulderProxy.position = Vector3.Lerp(_shoulderProxy.position, idealPosition, lerpSpeed);
            }
            
            _shoulderProxy.rotation = Quaternion.LookRotation(dirToEnemy);
        }
    }
}
