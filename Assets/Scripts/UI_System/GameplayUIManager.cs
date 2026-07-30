using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls gameplay HUD elements (Pain bar gradient, syringe array, score text, pause state with toggleable play/pause buttons, and music toggle).
/// </summary>
public class GameplayUIManager : MonoBehaviour
{
    [Header("Pain Bar Settings")]
    public Slider painSlider;
    public Image painFillImage;
    public Gradient painGradient;

    [Header("Syringe UI Slots (Array of 3 Syringe Icons)")]
    public GameObject[] syringeIcons;

    [Header("Score UI")]
    public TextMeshProUGUI scoreText;

    [Header("Overlay Panels & Toggle Buttons")]
    public GameObject pausePanel;
    public GameObject pauseButton; // Button GameObject for Pause (❚❚)
    public GameObject playButton;  // Button GameObject for Play (▶)

    [Header("Audio Toggles")]
    public AudioSource musicAudioSource;
    public Image musicToggleIcon;

    private bool isPaused = false;
    private bool isMusicMuted = false;

    private void Start()
    {
        if (pausePanel != null) 
            pausePanel.SetActive(false);

        // Ensure game starts in Play state (Pause button visible, Play button hidden)
        UpdatePauseButtons();
    }

    // ==========================================
    // 1. PAIN BAR CONTROL
    // ==========================================

    public void UpdatePainBar(float currentPain, float maxPain)
    {
        float fillAmount = Mathf.Clamp01(currentPain / maxPain);

        if (painSlider != null)
            painSlider.value = fillAmount;

        if (painFillImage != null && painGradient != null)
        {
            painFillImage.color = painGradient.Evaluate(fillAmount);
        }
    }

    // ==========================================
    // 2. SYRINGE VISIBILITY CONTROL
    // ==========================================

    public void UpdateSyringes(int remainingCount)
    {
        for (int i = 0; i < syringeIcons.Length; i++)
        {
            if (syringeIcons[i] != null)
            {
                syringeIcons[i].SetActive(i < remainingCount);
            }
        }
    }

    // ==========================================
    // 3. SCORE TEXT CONTROL
    // ==========================================

    public void UpdateScore(int currentScore)
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }

    // ==========================================
    // 4. PAUSE & PLAY TOGGLE CONTROL
    // ==========================================

    /// <summary>
    /// Toggles between Pause and Play states, switching button visibility.
    /// </summary>
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
        // When paused: Show PLAY button, hide PAUSE button
        // When playing: Show PAUSE button, hide PLAY button
        if (pauseButton != null) pauseButton.SetActive(!isPaused);
        if (playButton != null) playButton.SetActive(isPaused);
    }

    // ==========================================
    // 5. MUSIC TOGGLE CONTROL
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