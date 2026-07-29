using UnityEngine;

public class ItemBehaviour : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private ItemData itemData;
    [SerializeField] private GameManager gameManager;
    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        gameManager.CollectItem(itemData);
        gameObject.SetActive(false);
    }

}
