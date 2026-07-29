using UnityEngine;
using System.Collections;
public class TimeManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float currentTimeOfGame;
    [SerializeField] private float timeToIncreasePain = 10f;
    [SerializeField] private GameManager gameManager;
    void Start()
    {
        currentTimeOfGame = 0f;

        InvokeRepeating("IncreasePainScale", timeToIncreasePain, timeToIncreasePain);
    }

    // Update is called once per frame
    void Update()
    {
        if(gameManager.gameOver)
        {
            CancelInvoke("IncreasePainScale");
            return;
        }
        currentTimeOfGame += Time.deltaTime;
        gameManager.IncreasePainPerSecond(Time.deltaTime);
    }

    private void IncreasePainScale()
    {
        gameManager.IncreasePainScale();
    }
}
