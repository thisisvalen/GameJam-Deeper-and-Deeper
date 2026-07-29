using UnityEngine;

public class AutoDestroyHole : MonoBehaviour
{
    private Transform camTransform;
    
    // Distancia arriba de la cámara a la que el agujero se destruirá
    private float destroyOffset = 15f; 

    void Start()
    {
        // Cacheamos la cámara principal por rendimiento
        camTransform = Camera.main.transform;
    }

    void Update()
    {
        // Si la posición en Y del agujero está muy por encima de la cámara actual, lo destruimos.
        if (transform.position.y > camTransform.position.y + destroyOffset)
        {
            Destroy(gameObject);
        }
    }
}