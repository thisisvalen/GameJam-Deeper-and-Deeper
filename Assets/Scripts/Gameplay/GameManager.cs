using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TimeManager timeManager;
    public float painScale;
    public float playerPain = 0f;
    public float maxPain = 100f;
    public bool gameOver = false;
    public float gameScore;
    void Start()
    {
        gameScore = 0f;
        painScale = 0.2f;
    }

    // Update is called once per frame
    void Update()
    {
        if(gameOver)
        {
            return;
        }

        
        if (playerPain > maxPain)
        {
            playerPain = maxPain;
            gameOver = true;
        }
    }

    public void IncreasePainPerSecond(float time)
    {
        playerPain += time * painScale;
    }
    public void IncreasePainScale()
    {
        painScale += 0.5f;
    }

    public void IncreasePain(float amount)
    {
        playerPain += amount;
        if (playerPain > maxPain)
        {
            playerPain = maxPain;
        }
    }

    public void ReducePain(float amount) 
    {         
        playerPain -= amount;
        if (playerPain < 0f)
        {
            playerPain = 0f;
        }
    }

    public void IncreaseScore(float amount)
    {
        gameScore += amount;
    }
}
