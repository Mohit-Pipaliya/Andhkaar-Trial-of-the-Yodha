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
    private static bool hasStartedOnce = false;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        hasStartedOnce = false;
    }
#endif

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

    // ================= SINGLETON & PERSISTENCE =================
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1 frame delay taaki naye scene ka PlayerController.Start() pehle save load kar le
        StartCoroutine(DelayedSetupSceneUI(scene.name));
    }

    private IEnumerator DelayedSetupSceneUI(string sceneName)
    {
        yield return null;
        SetupSceneUI(sceneName);
    }

    private void OnEnable()
    {
        ResubscribePlayerEvents();
    }

    private void OnDisable()
    {
        UnsubscribePlayerEvents();
    }

    private void ResubscribePlayerEvents()
    {
        UnsubscribePlayerEvents();
        PlayerController.OnHealthChanged += UpdateHealthSmooth;
        PlayerController.OnOilChanged += UpdateOilSmooth;
        PlayerController.OnTorchStateChanged += ToggleOilSlider;
        PlayerController.OnPlayerDied += TriggerGameOver;
        PlayerController.OnPlayerRespawned += HandlePlayerRespawn;
    }

    private void UnsubscribePlayerEvents()
    {
        PlayerController.OnHealthChanged -= UpdateHealthSmooth;
        PlayerController.OnOilChanged -= UpdateOilSmooth;
        PlayerController.OnTorchStateChanged -= ToggleOilSlider;
        PlayerController.OnPlayerDied -= TriggerGameOver;
        PlayerController.OnPlayerRespawned -= HandlePlayerRespawn;
    }

    private void Start()
    {
        SetupSceneUI(SceneManager.GetActiveScene().name);
    }

    public void SetupSceneUI(string currentSceneName)
    {
        if (string.IsNullOrEmpty(currentSceneName))
            currentSceneName = SceneManager.GetActiveScene().name;

        // Force hide other panels first
        HidePanel(mainMenuPanel, false);
        HidePanel(pauseMenuPanel, false);
        HidePanel(optionsMenuPanel, false);
        HidePanel(notificationPanel, false);
        HidePanel(damagePanel, false);
        HidePanel(finishPanel, false);
        HidePanel(gameOverPanel, false);
        HidePanel(loadingPanel, false);

        // Sliders & Player setup
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        float pMaxHealth = (pc != null) ? pc.maxHealth : 1000f;
        float pCurrentHealth = (pc != null && pc.currentHealth > 0) ? pc.currentHealth : pMaxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = pMaxHealth;
            healthSlider.value = pCurrentHealth;
            targetHealth = pCurrentHealth;
        }

        bool playerHasTorch = (pc != null && pc.hasTorch);
        if (oilLampSlider != null)
        {
            oilLampSlider.maxValue = 100f;
            oilLampSlider.gameObject.SetActive(playerHasTorch);
            if (playerHasTorch && pc != null && pc.handLampLight != null)
            {
                targetOil = pc.handLampLight.intensity;
                oilLampSlider.value = targetOil;
            }
            else
            {
                targetOil = 100f;
                oilLampSlider.value = 100f;
            }
        }

        if (levelText != null)
        {
            if (currentSceneName == "Level 2") levelText.text = "Level : 2";
            else if (currentSceneName == "Level 3") levelText.text = "Level : 3";
            else levelText.text = "Level : 1";
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = AudioListener.volume;
        }

        // Re-subscribe events to current PlayerController
        ResubscribePlayerEvents();

        if (currentSceneName == "Level 1" && !hasStartedOnce)
        {
            isGameActive = false; // Game starts locked in loading/menu
            ShowPanel(loadingPanel, false);
            StartCoroutine(LoadingRoutine());
            hasStartedOnce = true;
        }
        else
        {
            // Skip loading and main menu, go straight to gameplay
            ShowPanel(gamePlayPanel, false);
            if (gamePlayPanel != null)
            {
                CanvasGroup cg = gamePlayPanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
            Time.timeScale = 1f; 
            isGamePaused = false;
            isGameOver = false;
            isGameActive = true; // Unlock player input
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // Smooth Slider Updates (60+ FPS responsive UI)
        if (healthSlider != null)
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, Time.unscaledDeltaTime * 5f);
            
        if (oilLampSlider != null)
            oilLampSlider.value = Mathf.Lerp(oilLampSlider.value, targetOil, Time.unscaledDeltaTime * 5f);

        // R dabane par respawn / restart logic
        if (((gameOverPanel != null && gameOverPanel.activeSelf) || isGameOver) && IsRPressedThisFrame())
        {
            RespawnPlayer();
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
    private System.Collections.Generic.Dictionary<GameObject, Coroutine> activeFadeRoutines = new System.Collections.Generic.Dictionary<GameObject, Coroutine>();

    private void ShowPanel(GameObject panel, bool animate = true)
    {
        if (panel == null) return;

        if (activeFadeRoutines.TryGetValue(panel, out Coroutine existing) && existing != null)
        {
            StopCoroutine(existing);
            activeFadeRoutines.Remove(panel);
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling(); // Bring panel to the front to prevent Raycast blocking
        
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        if (animate)
        {
            cg.alpha = 0f;
            activeFadeRoutines[panel] = StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, uiFadeSpeed, panel));
        }
        else
        {
            cg.alpha = 1f;
        }
    }

    private void HidePanel(GameObject panel, bool animate = true)
    {
        if (panel == null) return;

        if (activeFadeRoutines.TryGetValue(panel, out Coroutine existing) && existing != null)
        {
            StopCoroutine(existing);
            activeFadeRoutines.Remove(panel);
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (animate && panel.activeSelf)
        {
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            activeFadeRoutines[panel] = StartCoroutine(FadeCanvasGroupAndHide(cg, panel, uiFadeSpeed));
        }
        else
        {
            if (cg != null) cg.alpha = 0f;
            panel.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration, GameObject panel = null)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // Unscaled so it animates even when Time.timeScale is 0
            if (cg != null) cg.alpha = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }
        if (cg != null) cg.alpha = end;
        if (panel != null && activeFadeRoutines.ContainsKey(panel))
            activeFadeRoutines.Remove(panel);
    }

    private IEnumerator FadeCanvasGroupAndHide(CanvasGroup cg, GameObject panel, float duration)
    {
        yield return StartCoroutine(FadeCanvasGroup(cg, cg != null ? cg.alpha : 1f, 0f, duration, panel));
        if (panel != null) panel.SetActive(false);
        if (panel != null && activeFadeRoutines.ContainsKey(panel))
            activeFadeRoutines.Remove(panel);
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
        Debug.Log("QuitGame called!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); 
#endif
    }

    public void BackToMainMenu()
    {
        Debug.Log("BackToMainMenu called!");
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
        Debug.Log("PauseGame called!");
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
        Debug.Log("ResumeGame called!");
        isGamePaused = false;
        isGameActive = true; // Unlock player
        Time.timeScale = 1f; 
        
        HidePanel(pauseMenuPanel, true);
        HidePanel(optionsMenuPanel, true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool openedOptionsFromMainMenu = false;

    public void OpenOptionsMenu()
    {
        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            openedOptionsFromMainMenu = true;
            HidePanel(mainMenuPanel, true);
        }
        else if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            openedOptionsFromMainMenu = false;
            HidePanel(pauseMenuPanel, true);
        }
        
        ShowPanel(optionsMenuPanel, true);
    }

    public void CloseOptionsMenu()
    {
        HidePanel(optionsMenuPanel, true);
        
        if (openedOptionsFromMainMenu)
        {
            ShowPanel(mainMenuPanel, true);
        }
        else
        {
            ShowPanel(pauseMenuPanel, true);
        }
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

        // Auto trigger AAA animation for Press R prompt inside GameOver panel
        if (gameOverPanel != null)
        {
            TextMeshProUGUI[] tmps = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t != null && (t.gameObject.name.ToLower().Contains("press") || t.text.ToLower().Contains("press")))
                {
                    AAAPressPromptAnimator.AnimatePrompt(t.gameObject, true);
                }
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        // Agar player mar chuka hai ya GameOver screen open hai, toh nearest spawn point par respawn karo taaki weapon/torch lost na ho
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && (player.currentHealth <= 0 || isGameOver))
        {
            player.RespawnNearDeathPoint();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RespawnPlayer()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.RespawnNearDeathPoint();
        }
        else
        {
            RestartGame();
        }
    }

    private void HandlePlayerRespawn()
    {
        isGameOver = false;
        isGamePaused = false;
        isGameActive = true;
        Time.timeScale = 1f;

        HidePanel(gameOverPanel, false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        ShowPanel(gamePlayPanel, false);
        if (gamePlayPanel != null)
        {
            gamePlayPanel.SetActive(true);
            CanvasGroup cg = gamePlayPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        // Healthbar and Oil lamp bar full restore & force visible
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            targetHealth = pc.maxHealth;
            if (healthSlider != null)
            {
                healthSlider.gameObject.SetActive(true);
                healthSlider.maxValue = pc.maxHealth;
                healthSlider.value = pc.maxHealth;
            }

            if (pc.hasTorch)
            {
                targetOil = pc.maxLampIntensity > 0 ? pc.maxLampIntensity : 100f;
                if (oilLampSlider != null)
                {
                    oilLampSlider.gameObject.SetActive(true);
                    oilLampSlider.maxValue = targetOil;
                    oilLampSlider.value = targetOil;
                }
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool IsRPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) return true;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R)) return true;
#endif
        return false;
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
            StartCoroutine(ShowFinalBossNotificationDelay("Final Boss Fight"));
        }
    }

    public void TriggerFinalBossSequence(string bossText)
    {
        StartCoroutine(ShowFinalBossNotificationDelay(bossText));
    }

    private IEnumerator ShowFinalBossNotificationDelay(string bossText)
    {
        yield return new WaitForSeconds(notificationShowTime + 0.5f);
        ShowNotification(bossText);
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
