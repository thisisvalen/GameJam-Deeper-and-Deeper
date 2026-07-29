using UnityEngine;

// Cámara 2D que acompaña al taladro en su descenso.
// El personaje nunca puede subir y el túnel es angosto, así que basta con copiar su altura:
// sin suavizado no hay retraso ni rebote, y el jugador queda siempre en el mismo punto de la pantalla.
public class CameraFollow : MonoBehaviour
{
    // Personaje a seguir; si se deja vacío se busca automáticamente al arrancar
    [SerializeField] private Transform target;
    // Distancia a la que la cámara queda por debajo del jugador, para mostrar más del fondo
    [SerializeField] private float verticalOffset = 1f;

    // Posición original en X y Z: el lateral se fija en el editor y la Z debe conservarse en 2D
    private float fixedX;
    private float fixedZ;

    private void Awake()
    {
        fixedX = transform.position.x;
        fixedZ = transform.position.z;

        // Comodidad para la jam: si nadie asignó el objetivo, se toma el único personaje de la escena
        if (target == null)
        {
            CharacterMovement player = FindFirstObjectByType<CharacterMovement>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        // Sin objetivo la cámara se queda quieta en lugar de lanzar errores cada fotograma
        if (target == null)
        {
            return;
        }

        transform.position = new Vector3(fixedX, target.position.y - verticalOffset, fixedZ);
    }
}
