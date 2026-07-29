using System;
using UnityEngine;

// Datos de una fase del diente. La profundidad de inicio es el único umbral del juego:
// fondo, barra de dolor e interfaz leen de aquí en lugar de guardar copias propias.
[Serializable]
public class ToothSection
{
    // Nombre mostrado al entrar en la fase
    public string sectionName = "Nueva fase";
    // Profundidad a la que empieza esta fase; la primera debe empezar en 0
    public float startDepth;
    // Cuánto se multiplica el dolor mientras se está en esta fase
    public float painMultiplier = 1f;
}

// Dueño único de las fases del diente (esmalte, dentina, pulpa y zona radicular).
// Traduce la profundidad del jugador en fase actual y progreso normalizado, y avisa de los cambios.
public class ToothSectionManager : MonoBehaviour
{
    [Header("Target")]
    // Personaje del que se lee la profundidad; si se deja vacío se busca al arrancar
    [SerializeField] private CharacterMovement target;

    [Header("Level")]
    // Profundidad total del nivel: marca el fondo y sirve de referencia para el progreso normalizado
    [SerializeField] private float totalLevelDepth = 170f;

    [Header("Sections")]
    // Fases ordenadas por profundidad de inicio; se reordenan solas al arrancar por si se editaron sueltas
    [SerializeField]
    private ToothSection[] sections =
    {
        new ToothSection { sectionName = "Esmalte", startDepth = 0f, painMultiplier = 1f },
        new ToothSection { sectionName = "Dentina", startDepth = 40f, painMultiplier = 1.6f },
        new ToothSection { sectionName = "Pulpa", startDepth = 90f, painMultiplier = 2.4f },
        new ToothSection { sectionName = "Zona radicular", startDepth = 130f, painMultiplier = 3.5f }
    };

    // Fase en la que está el jugador; arranca en -1 para que el primer Update siempre notifique la inicial
    private int currentSectionIndex = -1;
    // Evita que el aviso de fondo alcanzado se dispare más de una vez
    private bool bottomReached;

    // Se dispara al entrar en una fase nueva, incluida la primera del nivel
    public event Action<ToothSection> OnSectionChanged;
    // Se dispara una sola vez al alcanzar el fondo del nivel
    public event Action OnBottomReached;

    // Fase actual; antes del primer Update devuelve la primera para que nadie reciba un nulo
    public ToothSection CurrentSection => sections != null && sections.Length > 0
        ? sections[Mathf.Clamp(currentSectionIndex, 0, sections.Length - 1)]
        : null;
    // Índice de la fase actual, útil para mostrar progreso del tipo "fase 2 de 4"
    public int CurrentSectionIndex => Mathf.Max(currentSectionIndex, 0);
    // Número total de fases configuradas
    public int SectionCount => sections != null ? sections.Length : 0;
    // Profundidad actual del jugador en unidades de mundo
    public float CurrentDepth => target != null ? target.CurrentDepth : 0f;
    // Progreso de 0 a 1 sobre el nivel completo: entrada del gradiente de fondo y de la barra de dolor
    public float NormalizedDepth => totalLevelDepth > 0f ? Mathf.Clamp01(CurrentDepth / totalLevelDepth) : 0f;
    // Profundidad total configurada para el nivel
    public float TotalLevelDepth => totalLevelDepth;
    // Indica si ya se llegó al fondo del nivel
    public bool HasReachedBottom => bottomReached;

    private void Awake()
    {
        // Comodidad para la jam: si nadie asignó el objetivo, se toma el único personaje de la escena
        if (target == null)
        {
            target = FindFirstObjectByType<CharacterMovement>();
        }

        // Ordenar en runtime hace fiable la búsqueda de fase sin estorbar la edición en el Inspector
        if (sections != null && sections.Length > 1)
        {
            Array.Sort(sections, (first, second) => first.startDepth.CompareTo(second.startDepth));
        }
    }

    private void Update()
    {
        // Sin objetivo no hay profundidad que traducir
        if (target == null || sections == null || sections.Length == 0)
        {
            return;
        }

        float depth = CurrentDepth;

        // El primer Update ocurre después de todos los Start, así que aquí ya nadie se pierde el aviso inicial
        int newSectionIndex = GetSectionIndexForDepth(depth);
        if (newSectionIndex != currentSectionIndex)
        {
            currentSectionIndex = newSectionIndex;
            OnSectionChanged?.Invoke(sections[currentSectionIndex]);
        }

        // Llegar al fondo cierra el nivel y solo debe avisarse una vez
        if (!bottomReached && depth >= totalLevelDepth)
        {
            bottomReached = true;
            OnBottomReached?.Invoke();
        }
    }

    // Devuelve la fase más profunda cuyo inicio ya se haya superado
    private int GetSectionIndexForDepth(float depth)
    {
        for (int i = sections.Length - 1; i >= 0; i--)
        {
            if (depth >= sections[i].startDepth)
            {
                return i;
            }
        }

        // Por encima del inicio de la primera fase se considera que se está en ella
        return 0;
    }

    // Evita valores negativos que romperían el progreso normalizado
    private void OnValidate()
    {
        totalLevelDepth = Mathf.Max(0.01f, totalLevelDepth);

        if (sections == null)
        {
            return;
        }

        foreach (ToothSection section in sections)
        {
            section.startDepth = Mathf.Max(0f, section.startDepth);
            section.painMultiplier = Mathf.Max(0f, section.painMultiplier);
        }
    }
}
