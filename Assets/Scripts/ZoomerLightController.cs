using UnityEngine;
using System.Collections;

public class ZoomerLightController : MonoBehaviour
{
    [Header("Zoomer Lights Setup")]
    public Light[] pointLights;
    public float turnOnDelay = 0.1f; // Delay between each light turning on
    
    private bool isPlayerInside = false;
    private bool areLightsOn = false;
    private PlayerController playerController;
    private Coroutine lightCoroutine;

    void Start()
    {
        // Shuru me sab lights off rahengi
        if (pointLights != null)
        {
            foreach (Light l in pointLights)
            {
                if (l != null) l.enabled = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (playerController == null)
            {
                playerController = other.GetComponent<PlayerController>();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // Lights tabhi jalengi jab player andar ho AUR vo kisi bhi enemy ke sath fight (combat) me ho
        bool shouldBeOn = isPlayerInside && playerController.activeCombatEngagements > 0;

        if (shouldBeOn && !areLightsOn)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
            lightCoroutine = StartCoroutine(ToggleLights(true));
        }
        else if (!shouldBeOn && areLightsOn)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
            lightCoroutine = StartCoroutine(ToggleLights(false));
        }
    }

    IEnumerator ToggleLights(bool state)
    {
        areLightsOn = state;
        
        if (pointLights != null)
        {
            foreach (Light l in pointLights)
            {
                if (l != null) 
                {
                    l.enabled = state;
                    yield return new WaitForSeconds(turnOnDelay); // Ek sath nahi jalengi, thodi thodi der me
                }
            }
        }
    }
}
