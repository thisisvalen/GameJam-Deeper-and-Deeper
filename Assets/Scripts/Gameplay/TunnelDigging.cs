using UnityEngine;

// Deja el túnel excavado detrás del jugador colocando máscaras que perforan el terreno.
// Usa un buffer circular de tamaño fijo: como los agujeros se crean siempre en orden y solo
// dejan de verse al quedar por encima de la cámara, el más antiguo es siempre el que está más arriba.
// Reutilizarlo al colocar uno nuevo hace innecesarios el reciclado, el Destroy y un Update por agujero.
public class TunnelDigging : MonoBehaviour
{
    [Header("Tunnel Digging Settings")]
    [Tooltip("Prefab de la máscara que perfora el terreno.")]
    [SerializeField] private GameObject holeMaskPrefab;

    [Tooltip("Distancia que recorre el jugador entre un agujero y el siguiente. Menos = túnel más liso pero más máscaras.")]
    [SerializeField] private float distanceBetweenHoles = 0.2f;

    [Tooltip("Unidades de túnel que se mantienen detrás del jugador. Debe superar el alto de la pantalla con margen.")]
    [SerializeField] private float trailLength = 20f;

    // Buffer circular de máscaras reutilizadas
    private GameObject[] holes;
    // Siguiente posición del buffer a ocupar; al dar la vuelta reutiliza la máscara más antigua
    private int nextHoleIndex;
    // Última posición donde se dejó un agujero, origen del reparto de los siguientes
    private Vector2 lastHolePosition;
    // Contenedor para no ensuciar la jerarquía de la escena
    private Transform maskContainer;

    private void Start()
    {
        lastHolePosition = transform.position;

        // El tamaño sale de cuánto túnel se quiere conservar: no se configura a mano para que no se desincronice
        int poolSize = Mathf.Max(1, Mathf.CeilToInt(trailLength / Mathf.Max(0.01f, distanceBetweenHoles)));
        holes = new GameObject[poolSize];

        maskContainer = new GameObject("Holes container").transform;
    }

    private void Update()
    {
        Vector2 currentPosition = transform.position;
        float distanceMoved = Vector2.Distance(currentPosition, lastHolePosition);

        // Un salto mayor que el túnel entero solo puede venir de un reinicio o un teletransporte:
        // rellenarlo generaría miles de agujeros y machacaría el buffer, así que se descarta el tramo
        if (distanceMoved > trailLength)
        {
            lastHolePosition = currentPosition;
            return;
        }

        // Se rellena todo el tramo recorrido, no un solo agujero por fotograma:
        // así la separación es uniforme aunque caigan los fps o suba la velocidad
        while (distanceMoved >= distanceBetweenHoles)
        {
            lastHolePosition = Vector2.MoveTowards(lastHolePosition, currentPosition, distanceBetweenHoles);
            PlaceHole(lastHolePosition);
            distanceMoved = Vector2.Distance(currentPosition, lastHolePosition);
        }
    }

    // Coloca la siguiente máscara del buffer, creándola solo la primera vez que se usa
    private void PlaceHole(Vector2 position)
    {
        GameObject hole = holes[nextHoleIndex];

        // Instanciación diferida: reparte el coste de llenar el buffer durante el primer tramo
        // en lugar de provocar un tirón al cargar el nivel
        if (hole == null)
        {
            hole = Instantiate(holeMaskPrefab, position, Quaternion.identity, maskContainer);
            holes[nextHoleIndex] = hole;
        }
        else
        {
            hole.transform.position = position;

            // Solo hace falta reactivar tras un ClearTunnel
            if (!hole.activeSelf)
            {
                hole.SetActive(true);
            }
        }

        nextHoleIndex = (nextHoleIndex + 1) % holes.Length;
    }

    // Borra el túnel visible y reinicia el reparto. El nivel es rejugable y sin esto
    // los agujeros de la partida anterior seguirían perforando el terreno.
    public void ClearTunnel()
    {
        if (holes == null)
        {
            return;
        }

        foreach (GameObject hole in holes)
        {
            if (hole != null)
            {
                hole.SetActive(false);
            }
        }

        nextHoleIndex = 0;
        lastHolePosition = transform.position;
    }
}
