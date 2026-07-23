using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AAAVoiceEffect : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // 1. Faster speed!
        audioSource.pitch = 1.4f; 
        audioSource.spatialBlend = 0.8f; // Make it 3D so it sounds like it's in the world

        // 2. Add Reverb (Echoing inside a huge temple/cave)
        AudioReverbFilter reverb = gameObject.AddComponent<AudioReverbFilter>();
        reverb.reverbPreset = AudioReverbPreset.Cave;
        reverb.dryLevel = 0f;
        reverb.room = -1000f;
        reverb.roomHF = -100f;
        reverb.decayTime = 3.0f;
        
        // 3. Add Chorus (Makes the voice sound like multiple demonic voices overlapping)
        AudioChorusFilter chorus = gameObject.AddComponent<AudioChorusFilter>();
        chorus.dryMix = 0.5f;
        chorus.wetMix1 = 0.5f;
        chorus.wetMix2 = 0.5f;
        chorus.wetMix3 = 0.5f;
        chorus.delay = 40f;
        chorus.rate = 0.8f;
        chorus.depth = 0.2f;

        // 4. Add subtle Echo for a trailing scary effect
        AudioEchoFilter echo = gameObject.AddComponent<AudioEchoFilter>();
        echo.delay = 200f;
        echo.decayRatio = 0.3f;
        echo.wetMix = 0.4f;
        echo.dryMix = 1.0f;
    }
}
