using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Singleton AudioManager that handles Background Music (BGM) and global sound effects.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Settings")]
        [Tooltip("The Background Music to play.")]
        [SerializeField] private AudioClip bgmClip;

        private AudioSource _bgmSource;

        private void Awake()
        {
            // Standard Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null); // Prevent Unity warning
                DontDestroyOnLoad(gameObject);
                
                _bgmSource = GetComponent<AudioSource>();
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;

                if (bgmClip != null)
                {
                    _bgmSource.clip = bgmClip;
                    _bgmSource.Play();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Global method to play a sound at a specific position in 3D space.
        /// Useful for sounds not tied to a specific moving object.
        /// </summary>
        public void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }
    }
}
