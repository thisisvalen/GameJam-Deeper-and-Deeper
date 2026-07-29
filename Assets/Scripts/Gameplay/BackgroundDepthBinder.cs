using UnityEngine;

// Vuelca los límites del nivel en el material del fondo.
// El shader muestrea el gradiente por la Y de mundo del fragmento, así que solo necesita saber
// dónde empieza y dónde termina el nivel: se escribe una vez al arrancar y no cuesta nada por fotograma.
// De este modo la profundidad vive únicamente en ToothSectionManager y deja de estar duplicada en el material.
public class BackgroundDepthBinder : MonoBehaviour
{
    [Header("Sources")]
    // Dueño de la profundidad total del nivel
    [SerializeField] private ToothSectionManager sectionManager;
    // Personaje del que se toma la altura de inicio, que equivale a la profundidad 0
    [SerializeField] private CharacterMovement player;

    [Header("Target")]
    // Renderer del fondo; si se deja vacío se toma el de este mismo objeto
    [SerializeField] private Renderer backgroundRenderer;

    // Identificadores de las propiedades del Shader Graph, resueltos una sola vez
    private static readonly int startDepthPropertyId = Shader.PropertyToID("_Start_Depth_Y");
    private static readonly int maxDepthPropertyId = Shader.PropertyToID("_Max_Depth_Y");

    // Se usa Start y no Awake para que CharacterMovement ya haya registrado su altura de inicio
    private void Start()
    {
        ResolveReferences();
        ApplyLevelBounds();
    }

    // Rellena lo que no se haya asignado a mano en el Inspector
    private void ResolveReferences()
    {
        if (sectionManager == null)
        {
            sectionManager = FindFirstObjectByType<ToothSectionManager>();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<CharacterMovement>();
        }

        if (backgroundRenderer == null)
        {
            backgroundRenderer = GetComponent<Renderer>();
        }
    }

    // Escribe en el material el tramo de Y de mundo que debe cubrir el gradiente de profundidad
    public void ApplyLevelBounds()
    {
        // Sin alguna de las piezas el fondo seguiría funcionando con los valores guardados en el material,
        // que es justo la desincronización que este componente existe para evitar: conviene avisar
        if (sectionManager == null || player == null || backgroundRenderer == null)
        {
            Debug.LogWarning("BackgroundDepthBinder: faltan referencias, el fondo usará los valores guardados en el material.", this);
            return;
        }

        // La altura de inicio del jugador es la profundidad 0 y el fondo del nivel queda esa profundidad más abajo
        float levelTopY = player.StartYPosition;
        float levelBottomY = levelTopY - sectionManager.TotalLevelDepth;

        // Se escribe en material y no en sharedMaterial para no modificar el asset en disco
        Material instanceMaterial = backgroundRenderer.material;
        instanceMaterial.SetFloat(startDepthPropertyId, levelTopY);
        instanceMaterial.SetFloat(maxDepthPropertyId, levelBottomY);
    }
}
