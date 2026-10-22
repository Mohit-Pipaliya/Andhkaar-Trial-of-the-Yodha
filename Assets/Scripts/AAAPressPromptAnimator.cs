using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// AAA Game Style Animated Press/Interaction Prompts.
/// Provides punchy spring entrance, organic floating bob, gentle glowing pulse,
/// and smooth fade-out. Automatically attaches to all "Press" UI elements.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AAAPressPromptAnimator : MonoBehaviour
{
    [Header("AAA Animation Settings")]
    [Tooltip("Spring pop-in duration (seconds)")]
    public float entranceDuration = 0.28f;
    [Tooltip("Smooth fade-out duration (seconds)")]
    public float exitDuration = 0.16f;
    [Tooltip("Spring pop-in scale overshoot")]
    public float overshootScale = 1.18f;
    [Tooltip("Vertical floating bob amplitude in pixels")]
    public float bobAmplitude = 4.5f;
    [Tooltip("Floating bob frequency")]
    public float bobSpeed = 3.2f;
    [Tooltip("Gentle breathing pulse amount")]
    public float pulseAmount = 0.05f;
    [Tooltip("Breathing pulse frequency")]
    public float pulseSpeed = 2.0f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseLocalScale = Vector3.one;

    private Coroutine activeAnimation;
    private bool isVisible = false;
    private float currentAnimScale = 1f;
    private float randomPhaseOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseAnchoredPosition = rectTransform.anchoredPosition;
        baseLocalScale = rectTransform.localScale == Vector3.zero ? Vector3.one : rectTransform.localScale;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        randomPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (baseAnchoredPosition == Vector2.zero && rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        // Auto trigger entrance animation whenever activated
        StartEntrance();
    }

    private void Update()
    {
        if (!isVisible && (canvasGroup != null && canvasGroup.alpha <= 0.01f))
            return;

        // Use unscaledTime so UI animates even when game is paused or on GameOver screen
        float time = Time.unscaledTime + randomPhaseOffset;

        // 1. Organic floating bob
        float bobY = Mathf.Sin(time * bobSpeed) * bobAmplitude;
        rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(0f, bobY);

        // 2. Subtle breathing pulse (heartbeat feeling)
        float pulse = 1f + (Mathf.Sin(time * pulseSpeed) * 0.5f + 0.5f) * pulseAmount;
        rectTransform.localScale = baseLocalScale * (currentAnimScale * pulse);
    }

    /// <summary>
    /// Smoothly show the prompt with a punchy AAA spring pop-in.
    /// </summary>
    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return; // OnEnable will handle entrance
        }

        StartEntrance();
    }

    /// <summary>
    /// Smoothly hide the prompt with a quick scale-down fade-out.
    /// </summary>
    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(ExitRoutine());
    }

    public void SetVisible(bool show)
    {
        if (show)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void StartEntrance()
    {
        isVisible = true;
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(EntranceRoutine());
    }

    private IEnumerator EntranceRoutine()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        currentAnimScale = 0.5f;

        float elapsed = 0f;
        Vector2 startPos = baseAnchoredPosition - new Vector2(0f, 15f); // Slide up slightly from below

        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);

            // Elastic Overshoot ease curve
            float easeScale;
            if (t < 0.65f)
            {
                float t1 = t / 0.65f;
                // Fast smooth step from 0.5 to overshoot (1.18)
                easeScale = Mathf.Lerp(0.5f, overshootScale, Mathf.Sin(t1 * Mathf.PI * 0.5f));
            }
            else
            {
                float t2 = (t - 0.65f) / 0.35f;
                // Settle from overshoot (1.18) to 1.0 with damping
                easeScale = Mathf.Lerp(overshootScale, 1f, Mathf.Sin(t2 * Mathf.PI * 0.5f));
            }

            currentAnimScale = easeScale;
            canvasGroup.alpha = Mathf.Clamp01(t * 1.5f); // Fade in faster than scale

            yield return null;
        }

        canvasGroup.alpha = 1f;
        currentAnimScale = 1f;
        activeAnimation = null;
    }

    private IEnumerator ExitRoutine()
    {
        isVisible = false;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        float startScale = currentAnimScale;
        float elapsed = 0f;

        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);

            // Quick shrink and fade out
            currentAnimScale = Mathf.Lerp(startScale, 0.75f, t * t);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        currentAnimScale = 0.75f;
        gameObject.SetActive(false);
        activeAnimation = null;
    }

    /// <summary>
    /// Utility function to smoothly toggle any prompt with AAA animation.
    /// </summary>
    public static void AnimatePrompt(GameObject promptObj, bool show)
    {
        if (promptObj == null) return;

        AAAPressPromptAnimator animator = promptObj.GetComponent<AAAPressPromptAnimator>();
        if (animator == null)
        {
            animator = promptObj.AddComponent<AAAPressPromptAnimator>();
        }

        animator.SetVisible(show);
    }

    /// <summary>
    /// Auto-discovers and enhances all "Press ..." prompts across any newly loaded scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnhanceAllPromptsInScene()
    {
        // Find all TextMeshProUGUI prompts containing "Press" (both active and inactive)
        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI tmp in allTexts)
        {
            if (tmp == null || tmp.gameObject == null) continue;
            // Skip assets/prefabs not in scene
            if (!tmp.gameObject.scene.isLoaded) continue;

            string name = tmp.gameObject.name.ToLower();
            string text = tmp.text.ToLower();

            if (name.Contains("press ") || text.Contains("press "))
            {
                if (tmp.GetComponent<AAAPressPromptAnimator>() == null)
                {
                    tmp.gameObject.AddComponent<AAAPressPromptAnimator>();
                }
            }
        }
    }
}
