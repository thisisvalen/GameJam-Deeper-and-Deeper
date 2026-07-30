using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TimeManager timeManager;
    private CharacterMovement characterMovement;
    public float playerPositionY;

    [Header("Pain Settings")]
    public float painScale;
    public float maxPain = 100f;
    public float yBandToIncreasePain = 30f;

    [Header("Game Settings")]
    public bool gameOver = false;
    public float yLimit = -170f;

    [Header("Player Stats")]
    public float gameScore;
    public float numberOfSyringes;
    public float playerPain = 0f;

    [Header ("Animation Control")]
    [SerializeField] private Animator _animator;
    public TinteRojo _tinteRojo;

    void Start()
    {
        gameScore = 0f;
        painScale = 0.2f;
        characterMovement = FindAnyObjectByType<CharacterMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if(gameOver)
        {
            return;
        }

        playerPositionY = characterMovement.transform.position.y;
        ChangePainScale();

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
    public void ChangePainScale()
    {
        painScale = 0.5f + (Mathf.Abs(playerPositionY / yBandToIncreasePain) * 0.5f);
    }

    public void CollectItem(ItemData itemData)
    {
        switch (itemData.itemType)
        {
            case ItemType.Syringe:
                AddSyringe();
                _animator.Play("MouseCelebrating");
                break;
            case ItemType.Collectable:
                IncreaseScore(itemData.itemEffectValue);
                _animator.Play("MouseCelebrating");
                break;
            case ItemType.Obstacle:
                IncreasePain(itemData.itemEffectValue);
                _tinteRojo.ActivarFlash();
                break;
            default:
                Debug.LogWarning("Unknown item type collected: " + itemData.itemType);
                break;
        }
    }

    public void IncreasePain(float amount)
    {
        playerPain += amount;
        if (playerPain > maxPain)
        {
            playerPain = maxPain;
        }
    }

    public void ApplySyringe() 
    {         
        if(numberOfSyringes <= 0)
        {
            return;
        }
        numberOfSyringes--;
        float syringeEffect = 20f; // Amount of pain reduced by a syringe
        playerPain -= syringeEffect;
        if (playerPain < 0f)
        {
            playerPain = 0f;
        }
    }

    public void AddSyringe()
    {
        if(numberOfSyringes < 3)
        {
            numberOfSyringes++;
        }
    }

    public void IncreaseScore(float amount)
    {
        gameScore += amount;
    }
}
