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
    // Altura extra desde la que cae la cámara al empezar el nivel
    [SerializeField] private float introHeight = 40f;
    // Duración en segundos de esa caída inicial
    [SerializeField] private float introDuration = 2f;

    // Posición original en X y Z: el lateral se fija en el editor y la Z debe conservarse en 2D
    private float fixedX;
    private float fixedZ;

    // Cuánto dura una sacudida al recibir daño
    [SerializeField] private float shakeDuration = 0.25f;
    // Desplazamiento máximo de la sacudida, en unidades del mundo
    [SerializeField] private float shakeMagnitude = 0.35f;

    // Tiempo transcurrido de la animación de entrada
    private float introTimer;

    // Tiempo que le queda a la sacudida actual; en cero la cámara está quieta
    private float shakeTimer;

    // Llamar desde fuera cada vez que el personaje recibe daño
    public void Shake()
    {
        shakeTimer = shakeDuration;
    }

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

        // Altura de seguimiento normal, la que se usa una vez terminada la entrada
        float followY = target.position.y - verticalOffset;

        // Durante los primeros segundos la cámara desciende desde arriba hasta esa altura
        if (introTimer < introDuration)
        {
            introTimer += Time.deltaTime;
            // SmoothStep suaviza el arranque y la llegada para que no se sienta un salto seco
            float progress = Mathf.SmoothStep(0f, 1f, introTimer / introDuration);
            followY = Mathf.Lerp(followY + introHeight, followY, progress);
        }

        // Desplazamiento aleatorio que se suma encima del seguimiento, sin alterar su altura real
        Vector2 shakeOffset = Vector2.zero;

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            // La intensidad decae con el tiempo restante para que la sacudida se apague sola
            float intensity = shakeMagnitude * Mathf.Max(shakeTimer, 0f) / shakeDuration;
            shakeOffset = Random.insideUnitCircle * intensity;
        }

        transform.position = new Vector3(fixedX + shakeOffset.x, followY + shakeOffset.y, fixedZ);
    }
}