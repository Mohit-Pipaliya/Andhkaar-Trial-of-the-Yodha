using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    // === Game State for PlayerController ===
    public static bool isGameActive = false;

    [Header("--- UI Panels ---")]
    public GameObject loadingPanel;
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject optionsMenuPanel;
    public GameObject gamePlayPanel; 
    public GameObject notificationPanel;
    public GameObject damagePanel;
    public GameObject finishPanel;
    public GameObject gameOverPanel; 

    [Header("--- Sliders ---")]
    public Slider loadingSlider;
    public Slider volumeSlider;
    public Slider healthSlider;
    public Slider oilLampSlider;

    [Header("--- Texts (TextMeshPro) ---")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI notificationText;

    [Header("--- Settings ---")]
    public float loadingTime = 3f; 
    public float damagePanelShowTime = 0.2f; 
    public float notificationShowTime = 3f; 
    public float uiFadeSpeed = 0.3f;

    private bool isGamePaused = false;
    private bool isGameOver = false;

    // Target values for smooth sliders
    private float targetHealth = 100f;
    private float targetOil = 100f;

    // ================= EVENT SUBSCRIPTION =================
    private void Awake()
    {
        // Auto-fix EventSystem for the New Input System so buttons work!
        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
        {
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                var oldModule = es.GetComponent<UnityEngine.EventSystems.BaseInputModule>();
                if (oldModule != null) Destroy(oldModule);
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("UIManager: Automatically updated EventSystem for the New Input System!");
            }
        }
    }

    private void OnEnable()
    {
        // Subscribe to PlayerController events (Event-Driven Architecture)
        PlayerController.OnHealthChanged += UpdateHealthSmooth;
        PlayerController.OnOilChanged += UpdateOilSmooth;
        PlayerController.OnTorchStateChanged += ToggleOilSlider;
        PlayerController.OnPlayerDied += TriggerGameOver;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        PlayerController.OnHealthChanged -= UpdateHealthSmooth;
        PlayerController.OnOilChanged -= UpdateOilSmooth;
        PlayerController.OnTorchStateChanged -= ToggleOilSlider;
        PlayerController.OnPlayerDied -= TriggerGameOver;
    }

    private void Start()
    {
        isGameActive = false; // Game starts locked in loading/menu

        // Force hide all panels first instantly
        HidePanel(mainMenuPanel, false);
        HidePanel(gamePlayPanel, false);
        HidePanel(pauseMenuPanel, false);
        HidePanel(optionsMenuPanel, false);
        HidePanel(notificationPanel, false);
        HidePanel(damagePanel, false);
        HidePanel(finishPanel, false);
        HidePanel(gameOverPanel, false);

        ShowPanel(loadingPanel, false);

        // Sliders setup
        healthSlider.maxValue = 100f;
        healthSlider.value = 100f;
        targetHealth = 100f;

        oilLampSlider.maxValue = 100f;
        oilLampSlider.value = 100f;
        targetOil = 100f;
        oilLampSlider.gameObject.SetActive(false); // Start me hide rakho

        levelText.text = "Level : 1";

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = AudioListener.volume;
        }

        StartCoroutine(LoadingRoutine());
    }

    private void Update()
    {
        // Smooth Slider Updates (60+ FPS responsive UI)
        if (healthSlider != null)
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, Time.unscaledDeltaTime * 5f);
            
        if (oilLampSlider != null)
            oilLampSlider.value = Mathf.Lerp(oilLampSlider.value, targetOil, Time.unscaledDeltaTime * 5f);

        // R dabane par restart logic
        if (gameOverPanel != null && gameOverPanel.activeSelf && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }

        // ESC dabane par Pause / Resume
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !isGameOver && !mainMenuPanel.activeSelf && !loadingPanel.activeSelf)
        {
            if (isGamePaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    // ================= HELPER: FADE ANIMATIONS =================
    private void ShowPanel(GameObject panel, bool animate = true)
    {
        if (panel == null) return;
        panel.SetActive(true);
        
        if (animate)
        {
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, uiFadeSpeed));
        }
    }

    private void HidePanel(GameObject panel, bool animate = true)
    {
        if (panel == null || !panel.activeSelf) return;
        
        if (animate)
        {
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            StartCoroutine(FadeCanvasGroupAndHide(cg, panel, uiFadeSpeed));
        }
        else
        {
            panel.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // Unscaled so it animates even when Time.timeScale is 0
            cg.alpha = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    private IEnumerator FadeCanvasGroupAndHide(CanvasGroup cg, GameObject panel, float duration)
    {
        yield return StartCoroutine(FadeCanvasGroup(cg, cg.alpha, 0f, duration));
        panel.SetActive(false);
    }

    // ================= 1. Loading Screen =================
    private IEnumerator LoadingRoutine()
    {
        float timer = 0f;
        loadingSlider.value = 0f;

        while (timer < loadingTime)
        {
            timer += Time.deltaTime;
            loadingSlider.value = Mathf.Lerp(0, 1, timer / loadingTime);
            yield return null;
        }

        HidePanel(loadingPanel, true);
        OpenMainMenu();
    }

    // ================= 2. Main Menu =================
    public void OpenMainMenu()
    {
        ShowPanel(mainMenuPanel, true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayGame()
    {
        HidePanel(mainMenuPanel, true);
        ShowPanel(gamePlayPanel, true);
        Time.timeScale = 1f; 
        isGamePaused = false;
        isGameActive = true; // Unlock player input
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        Application.Quit(); 
    }

    public void BackToMainMenu()
    {
        isGamePaused = false;
        Time.timeScale = 1f; 
        isGameActive = false; // Lock player input
        
        HidePanel(pauseMenuPanel, true);
        HidePanel(optionsMenuPanel, true);
        HidePanel(gamePlayPanel, true);
        OpenMainMenu();
    }

    // ================= 3. Pause & Options Menu =================
    public void PauseGame()
    {
        isGamePaused = true;
        isGameActive = false; // Lock player input
        Time.timeScale = 0f; // Game freeze
        
        ShowPanel(pauseMenuPanel, true);
        HidePanel(optionsMenuPanel, false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isGamePaused = false;
        isGameActive = true; // Unlock player
        Time.timeScale = 1f; 
        
        HidePanel(pauseMenuPanel, true);
        HidePanel(optionsMenuPanel, true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenOptionsMenu()
    {
        HidePanel(pauseMenuPanel, true);
        ShowPanel(optionsMenuPanel, true);
    }

    public void CloseOptionsMenu()
    {
        HidePanel(optionsMenuPanel, true);
        ShowPanel(pauseMenuPanel, true);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    // ================= 4. UI Events Handlers =================
    private void UpdateHealthSmooth(float newHealth)
    {
        targetHealth = newHealth;
        if (newHealth < healthSlider.value && newHealth > 0)
        {
            // Show damage panel briefly if taking damage (but not dead yet)
            StartCoroutine(ShowDamagePanelRoutine());
        }
    }

    private void UpdateOilSmooth(float newOil)
    {
        targetOil = newOil;
    }

    private void ToggleOilSlider(bool hasTorch)
    {
        if (oilLampSlider != null)
        {
            oilLampSlider.gameObject.SetActive(hasTorch);
        }
    }

    private IEnumerator ShowDamagePanelRoutine()
    {
        ShowPanel(damagePanel, true);
        yield return new WaitForSeconds(damagePanelShowTime);
        HidePanel(damagePanel, true);
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        isGameActive = false;
        Time.timeScale = 0f; 
        
        HidePanel(gamePlayPanel, true);
        ShowPanel(gameOverPanel, true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ================= 6. Level Triggers & Notifications =================
    public void CompleteLevel(int completedLevel)
    {
        if (completedLevel == 1)
        {
            levelText.text = "Level : 2";
            ShowNotification("Level 1 Completed");
        }
        else if (completedLevel == 2)
        {
            levelText.text = "Level : 3";
            ShowNotification("Level 2 Completed");
        }
        else if (completedLevel == 3)
        {
            ShowNotification("Level 3 Completed");
            StartCoroutine(ShowFinalBossNotificationDelay());
        }
    }

    private IEnumerator ShowFinalBossNotificationDelay()
    {
        yield return new WaitForSeconds(notificationShowTime + 0.5f);
        ShowNotification("Final Boss Fight");
    }

    public void ShowNotification(string message)
    {
        StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        ShowPanel(notificationPanel, true);
        
        yield return new WaitForSeconds(notificationShowTime);
        
        HidePanel(notificationPanel, true);
    }

    // ================= 7. Finish Game =================
    public void GameFinished()
    {
        isGameOver = true;
        isGameActive = false;
        Time.timeScale = 0f; 
        
        HidePanel(gamePlayPanel, true);
        ShowPanel(finishPanel, true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
