using System.Collections;
using UnityEngine;
using TMPro;

public class LevelTriggerArea : MonoBehaviour
{
    [Header("=== Notification Panel ===")]
    [Tooltip("Drag karo Notification Panel GameObject")]
    public GameObject notificationPanel;

    [Tooltip("Drag karo Notification Text (TextMeshPro)")]
    public TextMeshProUGUI notificationText;

    [Header("=== Notification Settings ===")]
    [Tooltip("Yeh text notification me show hoga")]
    [TextArea(2, 3)]
    public string messageToShow = "Level 1 Complete!";

    [Tooltip("Kitne seconds tak dikhana hai notification")]
    public float showDuration = 3f;

    [Header("=== Health Panel Level Text ===")]
    [Tooltip("Level text jo Health Panel me hai (e.g. 'Level : 1') — woh yahan update hoga")]
    public string updatedLevelText = "Level : 2";

    // Private
    private bool hasTriggered = false;
    private UIManager uiManager;

    private void Start()
    {
        // UIManager auto dhundhega scene me (updated to avoid deprecation warning)
        uiManager = Object.FindFirstObjectByType<UIManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(ShowNotification());
        }
    }

    private IEnumerator ShowNotification()
    {
        // Health Panel wala "Level : 1" text update karo
        if (uiManager != null && uiManager.levelText != null)
        {
            uiManager.levelText.text = updatedLevelText;
        }

        // Notification text set karo aur panel dikhao
        if (notificationText != null)
            notificationText.text = messageToShow;

        if (notificationPanel != null)
            notificationPanel.SetActive(true);

        // ── GAME FREEZE ──
        Time.timeScale = 0f; 

        // Real time mein wait karo (kyunki game time freeze hai)
        yield return new WaitForSecondsRealtime(showDuration);

        // ── GAME UNFREEZE ──
        Time.timeScale = 1f;

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
}
