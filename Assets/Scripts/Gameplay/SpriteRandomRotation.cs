using UnityEngine;

public class SpriteRandomRotation : MonoBehaviour
{
    [SerializeField] private float minRotation = 0f, maxRotation = 360f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float randomRotation = Random.Range(minRotation, maxRotation);
        transform.rotation = Quaternion.Euler(0f, 0f, randomRotation);
    }
}
