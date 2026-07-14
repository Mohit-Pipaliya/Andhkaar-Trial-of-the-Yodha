using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [Tooltip("Size when hovered over")]
    public float hoverScale = 1.05f;
    [Tooltip("Size when clicked down")]
    public float clickScale = 0.95f;
    [Tooltip("Speed of the scale animation")]
    public float animationSpeed = 15f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        // Smoothly interpolate to the target scale using unscaled delta time 
        // so it works even when game is paused (Time.timeScale = 0)
        if (rectTransform.localScale != targetScale)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // When mouse is released, if we are still hovering over it, go back to hover scale
        // Otherwise, go back to original scale
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
