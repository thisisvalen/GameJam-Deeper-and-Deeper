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
    public Gradient painGradient;

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

    [Tooltip("Exact name of the independent Winner/Mission Completed scene in Build Settings.")]
    public string winnerSceneName = "06_WinnerView";

    private bool isPaused = false;
    private bool isMusicMuted = false;
    private bool hasTriggeredEndGame = false;

    private void Start()
    {
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
        if (gameManager != null && !hasTriggeredEndGame)
        {
            // 1. Sync active gameplay UI elements
            UpdatePainBar(gameManager.playerPain, gameManager.maxPain);
            UpdateSyringes(Mathf.RoundToInt(gameManager.numberOfSyringes));
            UpdateScore(Mathf.RoundToInt(gameManager.gameScore));

            // 2. Check for Game Over condition
            if (gameManager.gameOver)
            {
                hasTriggeredEndGame = true;
                SaveScoreAndLoadScene(gameOverSceneName);
            }
            // 3. Check for Mission Completed condition
            else if (gameManager.gameFinished)
            {
                hasTriggeredEndGame = true;
                SaveScoreAndLoadScene(winnerSceneName);
            }
        }
    }

    private void SaveScoreAndLoadScene(string targetScene)
    {
        Time.timeScale = 1f;

        // Save current score globally so end screens can display it
        ScoreManager.FinalScore = Mathf.RoundToInt(gameManager.gameScore);

        if (!string.IsNullOrEmpty(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogError($"Target scene name is empty in GameplayUIManager!");
        }
    }

    // ==========================================
    // UI UPDATES & TOGGLES
    // ==========================================

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

    public void UpdatePainBar(float currentPain, float maxPain)
    {
        if (maxPain <= 0f) return;
        UpdatePainBar(currentPain / maxPain);
    }

    public void UpdateSyringes(int remainingCount)
    {
        if (syringeIcons == null) return;

        for (int i = 0; i < syringeIcons.Length; i++)
        {
            if (syringeIcons[i] != null)
            {
                syringeIcons[i].SetActive(i < remainingCount);
            }
        }
    }

    public void UpdateScore(int currentScore)
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pausePanel != null) 
            pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
        if(isPaused)
        {
            AudioManager.Instance.PauseClips();
        }
        else
        {
            AudioManager.Instance.UnPauseClips();
        }
        UpdatePauseButtons();
    }

    private void UpdatePauseButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.SetActive(!isPaused);
        }
        if (playButton != null)
        {
            playButton.SetActive(isPaused);
        }
    }
    public void ToggleMusic()
    {
        isMusicMuted = !isMusicMuted;

        if (isMusicMuted)
        {
            AudioManager.Instance.MuteClips();
        }
        else
        {
            AudioManager.Instance.UnMuteClips();
        }

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