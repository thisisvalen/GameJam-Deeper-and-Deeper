using UnityEngine;

public class TunnelDigging : MonoBehaviour
{
    [Header("Tunnel Digging Settings")]
    [Tooltip("Hole Mask Prefab to instantiate as the player digs through the tunnel.")]
    public GameObject holeMaskPrefab;
    
    [Tooltip("The distance the player must move to leave another hole. (Less = smoother tunnel but uses more resources)")]
    public float distanceBetweenHoles = 0.2f;

    private Vector2 lastHolePosition;
    private Transform maskContainer;

    void Start()
    {
        // Guardamos la posición inicial
        lastHolePosition = transform.position;

        // Opcional para organizar la jerarquía: creamos una carpeta vacía para meter la basura
        maskContainer = new GameObject("Contenedor_Agujeros").transform;
    }

    void Update()
    {
        // Calculamos cuánta distancia ha recorrido el jugador desde el último agujero
        float distanceMoved = Vector2.Distance(transform.position, lastHolePosition);

        // Si se movió lo suficiente, instanciamos un nuevo agujero
        if (distanceMoved >= distanceBetweenHoles)
        {
            LeaveHole();
        }
    }

    void LeaveHole()
    {
        // Creamos la máscara en la posición actual del taladro
        GameObject newHole = Instantiate(holeMaskPrefab, transform.position, Quaternion.identity);
        
        // Lo metemos al contenedor para no ensuciar la jerarquía de Unity
        newHole.transform.SetParent(maskContainer);

        // Actualizamos la posición del último agujero creado
        lastHolePosition = transform.position;
    }
}