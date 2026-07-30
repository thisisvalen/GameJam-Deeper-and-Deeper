using System;
using System.Collections.Generic;
using UnityEngine;

// Datos de un tipo de coleccionable. Cada tipo tiene su propia pila de instancias:
// una palomita no puede reutilizarse como carie, así que los pools no se mezclan.
[Serializable]
public class CollectibleType
{
    // Nombre para reconocer el tipo en el Inspector
    public string typeName = "Nuevo objeto";
    // Prefab a instanciar. Lo que haga al tocarlo es cosa de sus propios componentes:
    // este sistema solo lo coloca, lo activa y lo retira.
    public GameObject prefab;
    // Profundidad mínima a la que puede aparecer
    public float minDepth;
    // Profundidad máxima a la que puede aparecer
    public float maxDepth = 200f;
    // Cantidad exacta por partida para los recursos escasos, repartida en bandas iguales
    // dentro de su rango. En 0 el tipo entra en el sorteo normal y sale tantas veces como toque.
    public int fixedCount;
    // Instancias que se esperan vivas a la vez; pasarse no rompe nada pero se avisa.
    // La cota es (activationLead + despawnTrail) / separación mínima + 1: con los valores por
    // defecto son 22 / 2.3 + 1 = 10 objetos de un mismo tipo en el peor caso.
    public int poolSize = 10;
}

// Reparte los coleccionables por el nivel y los va prestando desde un pool según baja el jugador.
//
// El reparto se genera una sola vez como datos planos (unas decenas de structs, ningún GameObject)
// y luego un único cursor lo recorre hacia adelante. Eso vale porque el personaje nunca puede subir,
// así que la profundidad es monótona creciente y nada de lo que queda arriba hay que recuperarlo.
//
// A diferencia del túnel, aquí NO sirve un buffer circular: los objetos se consumen fuera de orden
// (destruyes el séptimo antes de que el quinto salga por arriba) y no son intercambiables entre tipos.
// Por eso cada tipo tiene su pila libre.
public class CollectibleSpawner : MonoBehaviour
{
    // Posición prevista de un objeto: dato puro, sin GameObject detrás
    private struct CollectiblePlacement
    {
        public float depth;
        public float horizontalPosition;
        public int typeIndex;
    }

    // Objeto en juego, con lo necesario para retirarlo sin volver a buscar componentes
    private struct ActiveCollectible
    {
        public GameObject instance;
        public float depth;
        public int typeIndex;
    }

    [Header("References")]
    // Personaje del que se lee todo lo del nivel: profundidad actual, ancho del túnel y fondo.
    // Si se deja vacío se busca al arrancar.
    [SerializeField] private CharacterMovement player;

    [Header("Types")]
    // Tipos disponibles. Los rangos de profundidad son lo que hace progresar el nivel:
    // arriba comida, abajo caries, y anestesia repartida casi por todo el recorrido.
    [SerializeField]
    private CollectibleType[] collectibleTypes =
    {
        new CollectibleType { typeName = "Palomita", minDepth = 0f, maxDepth = 90f },
        new CollectibleType { typeName = "Pollo", minDepth = 20f, maxDepth = 140f },
        new CollectibleType { typeName = "Carie", minDepth = 80f, maxDepth = 200f },
        // La anestesia es un recurso escaso: cantidad fija en lugar de entrar al sorteo
        new CollectibleType { typeName = "Anestesia", minDepth = 15f, maxDepth = 200f, fixedCount = 2, poolSize = 2 }
    };

    [Header("Layout")]
    // Separación media entre objetos consecutivos, en unidades de profundidad
    [SerializeField] private float depthBetweenObjects = 3.5f;
    // Variación aleatoria de esa separación, para que el reparto no se sienta regular
    [SerializeField] private float depthJitter = 1.2f;
    // Primera profundidad con objetos: evita colocar algo encima del punto de partida
    [SerializeField] private float firstObjectDepth = 8f;
    // Margen respecto a los bordes del túnel, para que ningún objeto quede pegado a la pared
    [SerializeField] private float horizontalMargin = 1f;
    // Semilla del reparto; -1 deja un nivel distinto en cada partida
    [SerializeField] private int randomSeed = -1;

    [Header("Streaming")]
    // Distancia por delante del jugador a la que se activan los objetos, antes de ser visibles
    [SerializeField] private float activationLead = 14f;
    // Distancia por encima del jugador a la que se devuelven al pool
    [SerializeField] private float despawnTrail = 8f;

    // Reparto completo del nivel, ordenado por profundidad por construcción
    private readonly List<CollectiblePlacement> placements = new List<CollectiblePlacement>();
    // Objetos actualmente en juego
    private readonly List<ActiveCollectible> active = new List<ActiveCollectible>();
    // Reutilizado al elegir tipo para no asignar memoria durante la generación
    private readonly List<int> candidateBuffer = new List<int>();
    // Posiciones ya ocupadas por un recurso escaso, para no ponerle dos encima
    private readonly HashSet<int> usedSlots = new HashSet<int>();

    // Una pila de instancias libres por tipo
    private Stack<GameObject>[] pools;
    // Instancias creadas por tipo, para detectar cuándo se supera el tamaño previsto
    private int[] createdCount;
    // Evita repetir el aviso de pool corto en cada fotograma
    private bool[] poolWarningShown;

    // Siguiente posición del reparto pendiente de activarse; solo avanza
    private int nextPlacement;
    // Contenedor para no ensuciar la jerarquía de la escena
    private Transform container;
    // Profundidad total del nivel, resuelta al arrancar
    private float levelDepth;

    // Objetos en juego ahora mismo, útil para depurar el reparto
    public int ActiveCount => active.Count;
    // Posiciones que quedan por activarse en el descenso
    public int RemainingCount => Mathf.Max(0, placements.Count - nextPlacement);

    private void Start()
    {
        // Comodidad para la jam: si nadie asignó las referencias, se toman las únicas de la escena
        if (player == null)
        {
            player = FindFirstObjectByType<CharacterMovement>();
        }

        // Sin personaje no hay profundidad que seguir ni ancho del que repartir
        if (player == null)
        {
            Debug.LogWarning("CollectibleSpawner: no hay CharacterMovement en la escena, no se reparte nada.", this);
            enabled = false;
            return;
        }

        // El fondo del nivel es el tope del personaje: repartir más allá sería colocar
        // objetos donde nadie puede llegar
        levelDepth = player.MaxDepth;

        container = new GameObject("Collectibles container").transform;

        BuildPools();
        GenerateLayout();
    }

    private void Update()
    {
        float depth = player.CurrentDepth;

        // Bucle y no un if para que la colocación no dependa del framerate. Con los valores por
        // defecto un if bastaría (los objetos están a 3.5 y el jugador avanza como mucho 2.4 por
        // fotograma a 5 fps), pero dejaría de bastar al bajar la separación o subir la velocidad.
        // Lo que de verdad evita que un objeto aparezca en pantalla es activationLead, no este bucle.
        while (nextPlacement < placements.Count && placements[nextPlacement].depth <= depth + activationLead)
        {
            Activate(placements[nextPlacement]);
            nextPlacement++;
        }

        // Se recorre al revés porque retirar intercambia la entrada con la última de la lista
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].depth < depth - despawnTrail)
            {
                Release(i);
            }
        }
    }

    // Prepara una pila vacía por tipo; las instancias se crean sobre la marcha
    private void BuildPools()
    {
        pools = new Stack<GameObject>[collectibleTypes.Length];
        createdCount = new int[collectibleTypes.Length];
        poolWarningShown = new bool[collectibleTypes.Length];

        for (int i = 0; i < collectibleTypes.Length; i++)
        {
            pools[i] = new Stack<GameObject>(Mathf.Max(1, collectibleTypes[i].poolSize));
        }
    }

    // Genera el reparto completo del nivel de una pasada
    private void GenerateLayout()
    {
        placements.Clear();
        nextPlacement = 0;

        // Semilla fija reproduce el mismo nivel en cada partida; -1 lo deja al azar
        System.Random random = randomSeed >= 0 ? new System.Random(randomSeed) : new System.Random();

        float leftEdge = player.LeftLimit + horizontalMargin;
        float rightEdge = player.RightLimit - horizontalMargin;

        // Con un margen mayor que el propio túnel la banda se invertiría; centrarla es más útil que fallar
        if (leftEdge > rightEdge)
        {
            float center = (player.LeftLimit + player.RightLimit) * 0.5f;
            leftEdge = center;
            rightEdge = center;
        }

        float spacing = Mathf.Max(0.1f, depthBetweenObjects);
        // El jitter se recorta a media separación: así el paso nunca es cero ni negativo,
        // lo que garantiza que la lista quede ordenada y que este bucle siempre termine
        float jitter = Mathf.Clamp(depthJitter, 0f, spacing * 0.5f);
        float depth = Mathf.Max(0f, firstObjectDepth);

        while (depth < levelDepth)
        {
            int typeIndex = PickTypeForDepth(depth, random);

            // Sin ningún tipo válido a esta altura simplemente queda un hueco
            if (typeIndex >= 0)
            {
                placements.Add(new CollectiblePlacement
                {
                    depth = depth,
                    horizontalPosition = Mathf.Lerp(leftEdge, rightEdge, (float)random.NextDouble()),
                    typeIndex = typeIndex
                });
            }

            depth += spacing + (float)(random.NextDouble() * 2d - 1d) * jitter;
        }

        // Los recursos escasos se colocan aparte, encima del reparto ya hecho
        PlaceFixedCountTypes(random);

        // Un reparto vacío casi siempre significa prefabs sin asignar, y en silencio cuesta encontrarlo
        if (placements.Count == 0)
        {
            Debug.LogWarning("CollectibleSpawner: el reparto salió vacío. Revisa que los tipos tengan prefab y que sus rangos de profundidad entren en el nivel.", this);
        }
    }

    // Elige al azar entre los tipos cuyo rango incluye esta profundidad; -1 si ninguno encaja
    private int PickTypeForDepth(float depth, System.Random random)
    {
        candidateBuffer.Clear();

        for (int i = 0; i < collectibleTypes.Length; i++)
        {
            CollectibleType type = collectibleTypes[i];

            // Un tipo sin prefab no se puede colocar
            if (type.prefab == null)
            {
                continue;
            }

            // Los de cantidad fija no entran en el sorteo: se colocan después y por separado
            if (type.fixedCount > 0)
            {
                continue;
            }

            if (depth < type.minDepth || depth > type.maxDepth)
            {
                continue;
            }

            candidateBuffer.Add(i);
        }

        return candidateBuffer.Count > 0 ? candidateBuffer[random.Next(candidateBuffer.Count)] : -1;
    }

    // Coloca los tipos de cantidad fija repartiéndolos en bandas iguales dentro de su rango.
    // En lugar de insertar posiciones nuevas se reemplaza el tipo de la posición ya repartida
    // más cercana: así el recurso escaso hereda la separación del reparto y no puede solaparse
    // con nada, sin necesidad de reordenar la lista.
    private void PlaceFixedCountTypes(System.Random random)
    {
        usedSlots.Clear();

        for (int typeIndex = 0; typeIndex < collectibleTypes.Length; typeIndex++)
        {
            CollectibleType type = collectibleTypes[typeIndex];

            if (type.prefab == null || type.fixedCount <= 0)
            {
                continue;
            }

            // El rango efectivo es la intersección del rango del tipo con el tramo que tiene objetos
            float rangeStart = Mathf.Max(type.minDepth, firstObjectDepth);
            float rangeEnd = Mathf.Min(type.maxDepth, levelDepth);

            if (rangeEnd <= rangeStart)
            {
                Debug.LogWarning($"CollectibleSpawner: '{type.typeName}' no cabe en el nivel (rango {type.minDepth}-{type.maxDepth}), no se coloca ninguno.", this);
                continue;
            }

            // Una banda por unidad pedida garantiza que no salgan todas juntas al principio
            float bandSize = (rangeEnd - rangeStart) / type.fixedCount;
            int placed = 0;

            for (int band = 0; band < type.fixedCount; band++)
            {
                // Punto al azar dentro de la mitad central de la banda. Usando la banda entera
                // dos unidades podrían caer pegadas justo en la frontera entre bandas y aparecer
                // en la misma pantalla, que es lo contrario de repartir un recurso escaso.
                float targetDepth = rangeStart + bandSize * (band + 0.25f + 0.5f * (float)random.NextDouble());
                int slot = FindFreeSlotNear(targetDepth, rangeStart, rangeEnd);

                if (slot < 0)
                {
                    continue;
                }

                CollectiblePlacement placement = placements[slot];
                placement.typeIndex = typeIndex;
                placements[slot] = placement;

                usedSlots.Add(slot);
                placed++;
            }

            // Quedarse corto significa que no había posiciones libres suficientes en el rango
            if (placed < type.fixedCount)
            {
                Debug.LogWarning($"CollectibleSpawner: solo se colocaron {placed} de {type.fixedCount} '{type.typeName}'. Amplía su rango de profundidad o baja depthBetweenObjects.", this);
            }
        }
    }

    // Posición del reparto más cercana a esa profundidad que siga libre y dentro del rango; -1 si no queda
    private int FindFreeSlotNear(float targetDepth, float rangeStart, float rangeEnd)
    {
        int bestSlot = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < placements.Count; i++)
        {
            // Una posición ya tomada por otro recurso escaso no se reutiliza
            if (usedSlots.Contains(i))
            {
                continue;
            }

            float depth = placements[i].depth;

            if (depth < rangeStart || depth > rangeEnd)
            {
                continue;
            }

            float distance = Mathf.Abs(depth - targetDepth);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSlot = i;
            }
        }

        return bestSlot;
    }

    // Saca un objeto del pool y lo coloca en su sitio
    private void Activate(CollectiblePlacement placement)
    {
        GameObject instance = Take(placement.typeIndex);

        if (instance == null)
        {
            return;
        }

        // La profundidad se traduce a altura de mundo con el mismo origen que fijó el personaje al arrancar
        instance.transform.position = new Vector3(placement.horizontalPosition, player.StartYPosition - placement.depth, 0f);

        // Reactivar aquí también deshace el SetActive(false) de quien lo haya recogido antes
        instance.SetActive(true);

        active.Add(new ActiveCollectible
        {
            instance = instance,
            depth = placement.depth,
            typeIndex = placement.typeIndex
        });
    }

    // Devuelve una instancia libre del tipo pedido, creándola si la pila está vacía
    private GameObject Take(int typeIndex)
    {
        Stack<GameObject> pool = pools[typeIndex];

        if (pool.Count > 0)
        {
            return pool.Pop();
        }

        CollectibleType type = collectibleTypes[typeIndex];
        createdCount[typeIndex]++;

        // Pasarse del tamaño previsto se autocorrige, pero conviene saberlo para subirlo en el Inspector
        if (createdCount[typeIndex] > type.poolSize && !poolWarningShown[typeIndex])
        {
            poolWarningShown[typeIndex] = true;
            Debug.LogWarning($"CollectibleSpawner: el pool de '{type.typeName}' se quedó corto (poolSize {type.poolSize}). Se crean instancias extra; sube el valor en el Inspector.", this);
        }

        // Instanciación diferida: el pool se llena durante el primer tramo en lugar de dar un tirón al cargar
        GameObject instance = Instantiate(type.prefab, container);
        instance.SetActive(false);
        return instance;
    }

    // Aparta el objeto de la lista de activos y lo devuelve a su pila.
    // Se llama por profundidad, no por recogida: da igual si alguien ya lo desactivó al recogerlo,
    // porque el tiempo que ocupa su hueco es el mismo lo hayan cogido o no.
    private void Release(int activeIndex)
    {
        ActiveCollectible entry = active[activeIndex];

        entry.instance.SetActive(false);
        pools[entry.typeIndex].Push(entry.instance);

        // Intercambio con el último elemento para no desplazar el resto de la lista
        int lastIndex = active.Count - 1;
        active[activeIndex] = active[lastIndex];
        active.RemoveAt(lastIndex);
    }

    // Devuelve todo al pool y genera un reparto nuevo. El nivel es rejugable y sin esto
    // los objetos de la partida anterior seguirían colocados.
    public void RestartLevel()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Release(i);
        }

        GenerateLayout();
    }

    // Dibuja la banda de reparto y el fondo del nivel para revisar la distribución sin jugar
    private void OnDrawGizmosSelected()
    {
        if (player == null)
        {
            return;
        }

        float topY = Application.isPlaying ? player.StartYPosition : player.transform.position.y;
        float depthToDraw = levelDepth > 0f ? levelDepth : player.MaxDepth;
        float leftEdge = player.LeftLimit + horizontalMargin;
        float rightEdge = player.RightLimit - horizontalMargin;

        // Bordes de la banda donde pueden caer los objetos
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(leftEdge, topY, 0f), new Vector3(leftEdge, topY - depthToDraw, 0f));
        Gizmos.DrawLine(new Vector3(rightEdge, topY, 0f), new Vector3(rightEdge, topY - depthToDraw, 0f));

        // Fondo del nivel
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(leftEdge, topY - depthToDraw, 0f), new Vector3(rightEdge, topY - depthToDraw, 0f));

        // Primera profundidad con objetos
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(leftEdge, topY - firstObjectDepth, 0f), new Vector3(rightEdge, topY - firstObjectDepth, 0f));
    }

    // Mantiene los valores en rangos que no rompan la generación ni el streaming
    private void OnValidate()
    {
        depthBetweenObjects = Mathf.Max(0.1f, depthBetweenObjects);
        depthJitter = Mathf.Max(0f, depthJitter);
        firstObjectDepth = Mathf.Max(0f, firstObjectDepth);
        horizontalMargin = Mathf.Max(0f, horizontalMargin);
        activationLead = Mathf.Max(0f, activationLead);
        despawnTrail = Mathf.Max(0f, despawnTrail);

        if (collectibleTypes == null)
        {
            return;
        }

        foreach (CollectibleType type in collectibleTypes)
        {
            type.poolSize = Mathf.Max(1, type.poolSize);
            type.fixedCount = Mathf.Max(0, type.fixedCount);
            type.minDepth = Mathf.Max(0f, type.minDepth);
            type.maxDepth = Mathf.Max(type.minDepth, type.maxDepth);
        }
    }
}