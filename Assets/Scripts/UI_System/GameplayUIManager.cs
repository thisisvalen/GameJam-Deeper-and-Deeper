using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUIManager : MonoBehaviour
{
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

    private bool isPaused = false;
    private bool isMusicMuted = false;

    private void Start()
    {
        if (pausePanel != null) 
            pausePanel.SetActive(false);

        UpdatePauseButtons();
    }

    // ==========================================
    // 1. PAIN BAR CONTROL (GRADIENT)
    // ==========================================

    /// <summary>
    /// Single parameter method exposed to Unity Slider's 'On Value Changed (Dynamic float)'.
    /// Receives a normalized value between 0.0 and 1.0.
    /// </summary>
    public void UpdatePainBar(float fillAmount)
    {
        fillAmount = Mathf.Clamp01(fillAmount);

        if (painSlider != null && painSlider.value != fillAmount)
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