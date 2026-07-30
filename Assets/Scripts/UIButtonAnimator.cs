using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [Tooltip("Size when hovered over")]
    public float hoverScale = 1.1f;
    [Tooltip("Size when clicked down")]
    public float clickScale = 0.85f;
    [Tooltip("Speed of the scale animation")]
    public float animationSpeed = 25f; // Faster speed for punchier feel

    [Header("Audio Settings (Optional)")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    private AudioSource audioSource;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        targetScale = originalScale;

        // Auto add AudioSource if sounds are provided
        if ((hoverSound != null || clickSound != null) && GetComponent<AudioSource>() == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true; // Play even if game is paused
        }
        else
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        // Smoothly interpolate to the target scale using unscaled delta time 
        if (rectTransform.localScale != targetScale)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        if (audioSource != null && hoverSound != null) audioSource.PlayOneShot(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            targetScale = originalScale * hoverScale;
        }
        else
        {
            targetScale = originalScale;
        }
    }
}
