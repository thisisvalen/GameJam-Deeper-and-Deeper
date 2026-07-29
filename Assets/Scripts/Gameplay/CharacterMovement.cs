using UnityEngine;

// Movimiento del jugador (ratón dentista sobre el taladro).
// Rota con A/D dentro de una apertura de 180 grados mirando hacia abajo y excava manteniendo S.
// Por diseño el personaje nunca puede subir: el ángulo recortado a [-90, 90] lo garantiza.
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Rotation")]
    // Velocidad de giro del taladro en grados por segundo
    [SerializeField] private float rotationSpeed = 140f;
    // Apertura máxima hacia cada lado: 90 grados por lado equivalen a los 180 grados totales
    [SerializeField] private float maxAimAngle = 90f;

    [Header("Digging")]
    // Velocidad máxima de excavación en unidades por segundo
    [SerializeField] private float maxDigSpeed = 3.5f;
    // Ganancia de velocidad por segundo mientras se mantiene S
    [SerializeField] private float acceleration = 12f;
    // Pérdida de velocidad por segundo al soltar S
    [SerializeField] private float deceleration = 18f;

    [Header("Horizontal limits")]
    // Borde izquierdo del túnel en coordenadas de mundo
    [SerializeField] private float leftLimit = -2.5f;
    // Borde derecho del túnel en coordenadas de mundo
    [SerializeField] private float rightLimit = 2.5f;

    [Header("State")]
    // Interruptor externo para pausa, fin de nivel o cinemáticas
    [SerializeField] private bool canMove = true;

    // Cuerpo cinemático: mueve el collider y dispara los triggers de los objetos destruibles
    private Rigidbody2D body;
    // Ángulo actual del taladro en grados, donde 0 apunta recto hacia abajo
    private float aimAngle;
    // Rapidez actual de avance, siempre positiva
    private float currentSpeed;
    // Entrada de giro leída en Update: A vale -1 y D vale +1
    private float rotationInput;
    // Entrada de excavación leída en Update
    private bool digInput;
    // Altura inicial usada como origen para medir la profundidad
    private float startYPosition;
    // Profundidad máxima alcanzada, monótona creciente
    private float maxDepthReached;

    // Ángulo actual del taladro, para animación y efectos visuales
    public float AimAngle => aimAngle;
    // Dirección unitaria hacia donde apunta el taladro
    public Vector2 DigDirection => GetDigDirection();
    // Profundidad actual respecto al inicio: entrada de la barra de dolor y del contador de distancia
    public float CurrentDepth => startYPosition - transform.position.y;
    // Altura de mundo donde arrancó el nivel, es decir la profundidad 0
    public float StartYPosition => startYPosition;
    // Profundidad récord de la partida, para las secciones del diente y el resultado final
    public float MaxDepthReached => maxDepthReached;
    // Indica si el taladro está avanzando, para animación, audio y sacudida de cámara
    public bool IsDigging => currentSpeed > 0.01f;
    // Rapidez normalizada entre 0 y 1, para intensidad de partículas y tono del taladro
    public float SpeedRatio => maxDigSpeed > 0f ? currentSpeed / maxDigSpeed : 0f;

    // Permite a otros sistemas bloquear o devolver el control al jugador
    public bool CanMove
    {
        get => canMove;
        set => canMove = value;
    }

    private void Awake()
    {
        // Se cachea el cuerpo y se fuerza el modo cinemático: el movimiento lo controla este script, no la simulación
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        // El origen de la medición de profundidad es la posición donde arranca el nivel
        startYPosition = body.position.y;

        // El taladro arranca apuntando recto hacia abajo
        aimAngle = 0f;
        body.MoveRotation(aimAngle);
    }

    private void Update()
    {
        // Con el control bloqueado se neutraliza la entrada para que la desaceleración siga corriendo
        if (!canMove)
        {
            rotationInput = 0f;
            digInput = false;
            return;
        }

        // Toda la lectura de entrada vive en Update; la física se aplica en FixedUpdate
        rotationInput = Input.GetAxisRaw("Horizontal");
        digInput = Input.GetKey(KeyCode.S);
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        ApplyRotation(deltaTime);
        UpdateSpeed(deltaTime);
        ApplyMovement(deltaTime);
        UpdateDepth();
    }

    // Gira el conjunto completo y recorta el ángulo para que el taladro jamás apunte por encima de la horizontal
    private void ApplyRotation(float deltaTime)
    {
        aimAngle = Mathf.Clamp(aimAngle + rotationInput * rotationSpeed * deltaTime, -maxAimAngle, maxAimAngle);
        body.MoveRotation(aimAngle);
    }

    // Acelera mientras se mantiene S y frena de forma gradual al soltarlo
    private void UpdateSpeed(float deltaTime)
    {
        float targetSpeed = digInput ? maxDigSpeed : 0f;
        float changeRate = digInput ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, changeRate * deltaTime);
    }

    // Desplaza el cuerpo en la dirección del taladro respetando los límites del túnel
    private void ApplyMovement(float deltaTime)
    {
        // Sin rapidez no hay nada que mover
        if (currentSpeed <= 0f)
        {
            return;
        }

        Vector2 currentPosition = body.position;
        Vector2 nextPosition = currentPosition + GetDigDirection() * (currentSpeed * deltaTime);

        // Al topar con un borde el jugador se desliza sobre él en lugar de trabarse, conservando el avance vertical
        nextPosition.x = Mathf.Clamp(nextPosition.x, leftLimit, rightLimit);

        // Red de seguridad ante cualquier empuje ascendente futuro: la altura nunca puede aumentar
        nextPosition.y = Mathf.Min(nextPosition.y, currentPosition.y);

        body.MovePosition(nextPosition);
    }

    // Registra el punto más profundo alcanzado en la partida
    private void UpdateDepth()
    {
        maxDepthReached = Mathf.Max(maxDepthReached, startYPosition - body.position.y);
    }

    // Convierte el ángulo en un vector unitario: 0 grados apunta abajo, +90 a la derecha y -90 a la izquierda
    private Vector2 GetDigDirection()
    {
        float angleInRadians = aimAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians));
    }

    // Permite al sistema de secciones angostar o ensanchar el túnel sin tocar este script
    public void SetHorizontalLimits(float left, float right)
    {
        leftLimit = Mathf.Min(left, right);
        rightLimit = Mathf.Max(left, right);
    }

    // Corta el avance de golpe, para el fin de nivel o la derrota por dolor
    public void StopImmediately()
    {
        currentSpeed = 0f;
        digInput = false;
    }

    // Dibuja en el editor los bordes del túnel y el arco de apertura para facilitar el afinado
    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        float lineHeight = 6f;

        // Bordes laterales del túnel
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(leftLimit, position.y - lineHeight, 0f), new Vector3(leftLimit, position.y + lineHeight, 0f));
        Gizmos.DrawLine(new Vector3(rightLimit, position.y - lineHeight, 0f), new Vector3(rightLimit, position.y + lineHeight, 0f));

        // Arco de apertura del taladro trazado con segmentos entre -maxAimAngle y +maxAimAngle
        Gizmos.color = Color.yellow;
        float arcRadius = 1.5f;
        int segmentCount = 24;
        Vector3 previousPoint = position + AngleToDirection(-maxAimAngle) * arcRadius;

        for (int i = 1; i <= segmentCount; i++)
        {
            float segmentAngle = Mathf.Lerp(-maxAimAngle, maxAimAngle, i / (float)segmentCount);
            Vector3 currentPoint = position + AngleToDirection(segmentAngle) * arcRadius;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        // Extremos del arco y dirección actual del taladro
        Gizmos.DrawLine(position, position + AngleToDirection(-maxAimAngle) * arcRadius);
        Gizmos.DrawLine(position, position + AngleToDirection(maxAimAngle) * arcRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + AngleToDirection(aimAngle) * arcRadius);
    }

    // Versión del cálculo de dirección utilizable desde los gizmos con un ángulo arbitrario
    private static Vector3 AngleToDirection(float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians), 0f);
    }
}
