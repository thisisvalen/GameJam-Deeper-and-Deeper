using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EndScreenUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro field inside the SCORE box.")]
    public TextMeshProUGUI scoreText;

    [Header("Scene Navigation")]
    public string mainMenuSceneName = "01_MainMenu";
    public string playAgainSceneName = "03_PlayerView";

    private void Start()
    {
        // Display accumulated score saved from Gameplay scene
        if (scoreText != null)
        {
            scoreText.text = ScoreManager.FinalScore.ToString();
        }
    }

    public void OnMainButtonClicked()
    {
        AudioManager.Instance.PlayButtonSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnPlayAgainButtonClicked()
    {
        AudioManager.Instance.PlayButtonSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene(playAgainSceneName);
    }
}