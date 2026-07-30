using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this script to your Canvas or a Manager GameObject in the scene.
/// Select its methods directly from UI Button OnClick events.
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    [Header("Default Scene Names")]
    public string mainMenuSceneName = "01_MainMenu";
    public string gameplaySceneName = "03_PlayerView";

    // ==========================================
    // PUBLIC METHODS FOR UI BUTTON ONCLICK EVENTS
    // ==========================================

    /// <summary>
    /// Loads the main gameplay scene.
    /// </summary>
    public void GoToGameplay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Loads any scene dynamically by passing its exact name string from the Inspector button.
    /// </summary>
    /// <param name="sceneName">Name of the target scene in Build Settings.</param>
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Restarts the currently active scene.
    /// </summary>
    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Quits the application build.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Exiting game...");
        Application.Quit();
    }
}