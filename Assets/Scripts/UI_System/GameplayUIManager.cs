using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameplayUIManager : MonoBehaviour
{
    [Header("GameManager Connection")]
    [Tooltip("Optional: Drag GameManager here. If empty, it will be found automatically at Start.")]
    public GameManager gameManager;

    [Header("Pain Bar Settings")]
    public Slider painSlider;
    public Image painFillImage;

    [Header("Pain Bar Color Gradient")]
    public Gradient painGradient; // Evaluates fill amount (0 to 1) to set color dynamically

    [Header("Syringe UI Slots (Array of 3 Syringe Icons)")]
    public GameObject[] syringeIcons;

    [Header("Score UI")]
    public TextMeshProUGUI scoreText;

    [Header("Overlay Panels & Toggle Buttons")]
    public GameObject pausePanel;
    public GameObject pauseButton;
    public GameObject playButton;

    [Header("Audio Toggles")]
    public AudioSource musicAudioSource;
    public Image musicToggleIcon;

    [Header("Scene Transition Settings")]
    [Tooltip("Exact name of the independent Game Over scene in Build Settings.")]
    public string gameOverSceneName = "05_GameOver";

    private bool isPaused = false;
    private bool isMusicMuted = false;
    private bool hasLoadedGameOver = false;

    private void Start()
    {
        // Automatically find GameManager in scene if not assigned in Inspector
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (pausePanel != null) 
            pausePanel.SetActive(false);

        UpdatePauseButtons();
    }

    private void Update()
    {
        // Continuously read values from GameManager and sync with UI
        if (gameManager != null)
        {
            // 1. Update Pain Bar & Gradient
            UpdatePainBar(gameManager.playerPain, gameManager.maxPain);

            // 2. Update Syringe UI (Converts float numberOfSyringes to int)
            UpdateSyringes(Mathf.RoundToInt(gameManager.numberOfSyringes));

            // 3. Update Score UI (Converts float gameScore to int)
            UpdateScore(Mathf.RoundToInt(gameManager.gameScore));

            // 4. Trigger Scene Transition when Pain is Maxed out (Game Over)
            if (gameManager.gameOver && !hasLoadedGameOver)
            {
                hasLoadedGameOver = true;
                LoadGameOverScene();
            }
        }
    }

    // ==========================================
    // 1. GAME OVER SCENE CONTROL
    // ==========================================

    private void LoadGameOverScene()
    {
        // Reset time scale to normal before changing scenes so the new scene isn't paused
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
        else
        {
            Debug.LogError("Game Over scene name is empty in GameplayUIManager!");
        }
    }

    // ==========================================
    // 2. PAIN BAR CONTROL (GRADIENT)
    // ==========================================

    /// <summary>
    /// Single parameter method exposed to Unity Slider's 'On Value Changed (Dynamic float)'.
    /// Receives a normalized value between 0.0 and 1.0.
    /// </summary>
    public void UpdatePainBar(float fillAmount)
    {
        fillAmount = Mathf.Clamp01(fillAmount);

        if (painSlider != null && !Mathf.Approximately(painSlider.value, fillAmount))
        {
            painSlider.value = fillAmount;
        }

        if (painFillImage != null && painGradient != null)
        {
            painFillImage.color = painGradient.Evaluate(fillAmount);
        }
    }

    /// <summary>
    /// Overload method for external gameplay logic passing current and maximum values.
    /// Example usage: uiManager.UpdatePainBar(currentPain, maxPain);
    /// </summary>
    public void UpdatePainBar(float currentPain, float maxPain)
    {
        if (maxPain <= 0f) return;
        UpdatePainBar(currentPain / maxPain);
    }

    // ==========================================
    // 3. SYRINGE VISIBILITY CONTROL
    // ==========================================

    public void UpdateSyringes(int remainingCount)
    {
        if (syringeIcons == null) return;

        for (int i = 0; i < syringeIcons.Length; i++)
        {
            if (syringeIcons[i] != null)
            {
                // Shows syringe if its index is less than remainingCount
                syringeIcons[i].SetActive(i < remainingCount);
            }
        }
    }

    // ==========================================
    // 4. SCORE TEXT CONTROL
    // ==========================================

    public void UpdateScore(int currentScore)
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }

    // ==========================================
    // 5. PAUSE & PLAY TOGGLE CONTROL
    // ==========================================

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel != null) 
            pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        UpdatePauseButtons();
    }

    private void UpdatePauseButtons()
    {
        if (pauseButton != null) pauseButton.SetActive(!isPaused);
        if (playButton != null) playButton.SetActive(isPaused);
    }

    // ==========================================
    // 6. MUSIC TOGGLE CONTROL
    // ==========================================

    public void ToggleMusic()
    {
        isMusicMuted = !isMusicMuted;

        if (musicAudioSource != null)
        {
            musicAudioSource.mute = isMusicMuted;
        }

        if (musicToggleIcon != null)
        {
            musicToggleIcon.color = isMusicMuted ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
        }
    }
}