using System.Collections;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Handles global combat effects like Hit Stop.
    /// </summary>
    public class CombatFeelManager : MonoBehaviour
    {
        public static CombatFeelManager Instance { get; private set; }

        private bool _isHitStopping = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null); // Prevent Unity warning
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Briefly slows down time to give impacts more weight.
        /// </summary>
        /// <param name="duration">How long (in real seconds) the hit stop lasts.</param>
        /// <param name="timeScale">The time scale during the hit stop (e.g., 0.1 for very slow).</param>
        public void HitStop(float duration = 0.1f, float timeScale = 0.1f)
        {
            if (_isHitStopping) return;
            StartCoroutine(DoHitStop(duration, timeScale));
        }

        private IEnumerator DoHitStop(float duration, float targetTimeScale)
        {
            _isHitStopping = true;
            float originalTimeScale = Time.timeScale;
            
            // Set the slow time scale
            Time.timeScale = targetTimeScale;
            
            // Wait for the duration using unscaled time so it actually completes in 'duration' seconds
            yield return new WaitForSecondsRealtime(duration);
            
            // Restore normal time
            Time.timeScale = originalTimeScale;
            _isHitStopping = false;
        }
    }
}
