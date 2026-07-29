using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI painText;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private GameManager gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.gameOver)
        {
            return;
        }

        ActualizarCanvas();
    }

    public void ActualizarCanvas()
    {
        timerText.text = "Timer: " + Mathf.Round(timeManager.currentTimeOfGame).ToString();
        difficultyText.text = "Dificultad:  " + gameManager.painScale.ToString();
        painText.text = "Dolor:  " + Mathf.Round(gameManager.playerPain).ToString();
    }
}
