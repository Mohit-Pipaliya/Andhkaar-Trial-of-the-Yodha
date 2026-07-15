using UnityEngine;

/// <summary>
/// Generates a realistic sci-fi/magic energy beam sound procedurally.
/// Attach this to any GameObject to generate a humming, zapping laser tone.
/// No audio file needed!
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProceduralLaserAudio : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Synth Settings")]
    [Range(20f, 400f)] public float baseFrequency = 85f;
    [Range(0f, 1f)] public float distortion = 0.5f;
    [Range(0f, 1f)] public float volume = 0.4f;

    private float phase;
    private float sampleRate;
    private float time;

    // Perlin noise offsets for organic modulation
    private float noiseOffset1;
    private float noiseOffset2;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 30f;
        audioSource.loop = true;
        audioSource.playOnAwake = true;

        // Auto-play the procedural stream
        if (!audioSource.isPlaying)
            audioSource.Play();

        sampleRate = AudioSettings.outputSampleRate;

        noiseOffset1 = Random.Range(0f, 100f);
        noiseOffset2 = Random.Range(0f, 100f);
    }

    void Update()
    {
        time += Time.deltaTime;
    }

    // This is called by Unity's audio engine on a separate thread to fill the audio buffer!
    void OnAudioFilterRead(float[] data, int channels)
    {
        float t = time;

        for (int i = 0; i < data.Length; i += channels)
        {
            // Update time per sample
            t += 1f / sampleRate;

            // Frequency modulation (vibrato/humming effect)
            // Slower hum
            float lfo1 = Mathf.Sin(t * Mathf.PI * 4f);
            // Faster zap/crackle
            float lfo2 = Mathf.PerlinNoise(t * 15f + noiseOffset1, noiseOffset2) * 2f - 1f;

            // Dynamic frequency
            float freq = baseFrequency + (lfo1 * 15f) + (lfo2 * 35f);

            // Advance phase
            phase += (freq * 2f * Mathf.PI) / sampleRate;
            if (phase > 2f * Mathf.PI) phase -= 2f * Mathf.PI;

            // Generate Sawtooth/Pulse hybrid wave
            float wave = (phase / Mathf.PI) - 1f; // Sawtooth base
            
            // Add some distortion/harmonics
            wave = Mathf.Clamp(wave * (1f + distortion * 4f), -1f, 1f);

            // Amplitude modulation (crackle/volume flutter)
            float ampMod = 0.7f + 0.3f * Mathf.PerlinNoise(noiseOffset1, t * 25f + noiseOffset2);

            float sample = wave * volume * ampMod;

            // Copy to all channels (stereo/mono)
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }
}
