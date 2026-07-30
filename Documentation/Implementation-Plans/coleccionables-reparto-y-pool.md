# Reparto y pool de coleccionables

Algoritmo de `CollectibleSpawner` y `Collectible`. Nivel finito de **200** unidades de profundidad.

## Por qué así

El nivel es finito y rejugable, así que el contenido no puede generarse por streaming infinito ni
crearse entero de golpe con `Instantiate`. La clave que lo simplifica todo: **el personaje nunca
puede subir** (`nextPosition.y = Mathf.Min(...)` en `CharacterMovement.ApplyMovement`), así que la
profundidad es monótona creciente y nada de lo que queda por encima hay que recuperarlo jamás.

## Dos fases separadas

### 1. Reparto (una vez, en `Start`)

Se genera una lista de `CollectiblePlacement` — structs planos con `depth`, `horizontalPosition`
y `typeIndex`. Son ~55 structs para todo el nivel: **ningún GameObject**.

```
depth = firstObjectDepth
mientras depth < levelDepth:
    tipo = uno al azar entre los válidos a esa profundidad (rango minDepth..maxDepth)
    añadir posición (depth, x al azar en la banda, tipo)
    depth += depthBetweenObjects ± depthJitter
```

La lista queda **ordenada por construcción**, sin necesidad de `Sort`. El `depthJitter` se recorta a
media separación (`Mathf.Clamp(depthJitter, 0, spacing * 0.5f)`): así el paso nunca es cero ni
negativo, lo que garantiza a la vez el orden y que el bucle termine aunque se configure un jitter
absurdo.

La banda en X se deriva de `player.LeftLimit`/`player.RightLimit` menos `horizontalMargin`, para que
el ancho jugable siga teniendo una sola fuente de verdad.

### 2. Streaming (en `Update`)

Un **único cursor** que solo avanza:

```
mientras placements[cursor].depth <= profundidad + activationLead:
    activar desde el pool; cursor++

para cada activo (recorriendo al revés):
    si depth < profundidad - despawnTrail: devolver al pool
```

Se recorre al revés porque retirar intercambia la entrada con la última de la lista.

**Lo que evita que un objeto aparezca dentro de la pantalla es `activationLead`, no la forma del
bucle.** Con `maxDigSpeed 3.5` un `if` daría el mismo resultado que el `while` a cualquier framerate
(medido: idéntico margen a 60, 30, 15, 10 y 5 fps), porque los objetos están a 3.5 unidades y el
jugador avanza como mucho 2.4 por fotograma. El `while` está por robustez ante otro afinado, no
porque hoy arregle un fallo.

## Recursos escasos: `fixedCount`

La anestesia no se sortea con los demás — es un recurso limitado.

Un tipo con `fixedCount > 0` **queda fuera del sorteo** y se coloca después: se divide su rango en
`fixedCount` bandas iguales y en cada una se elige un punto al azar dentro de su **mitad central**,
sustituyendo el tipo de la posición ya repartida más cercana.

Dos decisiones con motivo:

- **Sustituir en lugar de insertar** hace que el recurso escaso herede la separación del reparto:
  no puede solaparse con nada y no hace falta reordenar la lista.
- **Mitad central de la banda** porque con la banda entera dos unidades pueden caer pegadas justo en
  la frontera y salir en la misma pantalla. Medido: la separación mínima entre las dos anestesias
  sube de **4.5 a 46.8** unidades al aplicar el recorte.

## Pools: una pila por tipo

Cada tipo tiene su `Stack<Collectible>`, con instanciación diferida.

**Por qué no un buffer circular como el túnel.** Ahí el agujero N siempre se retira antes que el N+1,
así que la ranura a reutilizar es la siguiente en el índice. Los coleccionables rompen esa premisa
por dos motivos: se **consumen fuera de orden** (destruyes el séptimo antes de que el quinto salga
por arriba) y **no son intercambiables** entre tipos.

Cota del tamaño de pila: `(activationLead + despawnTrail) / separación mínima + 1`. Con los valores
por defecto son `22 / 2.3 + 1 = 10` objetos de un mismo tipo en el peor caso — de ahí el
`poolSize = 10`. Si una pila se agota se crea una instancia extra y se avisa una sola vez.

## Límite de responsabilidad

Este sistema cubre **aparición, colocación por profundidad y pool**. Nada más. No detecta la recogida,
no aplica efectos y no conoce a `GameManager`.

`CollectibleType` es **identidad y colocación**: `typeName`, `prefab`, `minDepth`, `maxDepth`,
`fixedCount` y `poolSize`. Ni puntos, ni dolor, ni tipo de efecto.

Queda **fuera** a propósito: la detección del contacto con el jugador, los puntos, el dolor, el dolor
por zona del diente (y con él las fases esmalte/dentina/pulpa/zona radicular) y el inventario de
anestesia con su activación por botón. Aquí la anestesia solo *aparece* como recurso escaso.

Hubo un `Collectible`, un `ToothSectionManager` y un `BackgroundDepthBinder` para parte de eso, y los
tres **se eliminaron**; siguen en el historial de git.

## Cómo convive con el sistema de ítems

Los prefabs llevan `SpriteRenderer` + `CircleCollider2D` en modo trigger, y **ningún componente de
comportamiento**: quien lo maneje añade el suyo (`ItemBehaviour`, con su `ItemData`).

No hace falta ninguna coordinación entre los dos sistemas, y merece la pena entender por qué:

- El pool **retira por profundidad, no por recogida.** Un objeto sale de la lista de activos cuando
  su profundidad queda `despawnTrail` por encima del jugador, lo hayan cogido o no.
- Que otro componente haga `SetActive(false)` al recogerlo **no rompe nada**: el objeto ya está en la
  lista de activos y volverá a su pila igual cuando pase por arriba. `Release` vuelve a llamar
  `SetActive(false)`, que sobre algo inactivo no hace nada.
- Al reutilizarlo, `Activate` hace `SetActive(true)`, que deshace el ocultado anterior.
- **El tamaño del pool no cambia:** el tiempo que un objeto ocupa su hueco es el mismo lo hayan
  recogido o no, así que la cota de 10 por tipo se mantiene exacta.

Los coleccionables **no llevan `Rigidbody2D`**: en 2D basta con que uno de los dos cuerpos tenga uno,
y el jugador ya aporta el suyo cinemático.

## Valores por defecto

| Campo | Valor | Nota |
|---|---|---|
| `depthBetweenObjects` | 3.5 | ~55 objetos por nivel |
| `depthJitter` | 1.2 | separación resultante 2.3 – 4.7 |
| `firstObjectDepth` | 8 | nada encima del punto de partida |
| `horizontalMargin` | 1 | banda ±6.75 con límites ±7.75 |
| `activationLead` | 14 | borde inferior de pantalla a +6 |
| `despawnTrail` | 8 | borde superior a −4 |
| `randomSeed` | −1 | −1 = nivel distinto cada partida |

Tipos: Palomita 0–90, Pollo 20–140, Carie 80–200, Anestesia 15–200 con `fixedCount 2`.

## Dependencias

La única es **`CharacterMovement`**, que aporta `CurrentDepth`, `StartYPosition`, `LeftLimit`,
`RightLimit` y `MaxDepth`.

`CharacterMovement.maxDepth` es el fondo donde el personaje se detiene y el límite hasta donde se
reparten objetos: esas dos cosas se mantienen en sincronía solas.

El gradiente del fondo **no** se deriva de ahí: sus límites están horneados a mano en
`_Start_Depth_Y` y `_Max_Depth_Y` de los materiales del fondo. Al cambiar `maxDepth` hay que
actualizar `_Max_Depth_Y` en los dos materiales (`_Inside` y `_Outside`) con el valor negado, y si
alguna vez se mueve la altura de inicio del jugador, `_Start_Depth_Y` debe igualarla.

`RestartLevel()` queda público y sin conectar: no existe flujo de reinicio todavía.
