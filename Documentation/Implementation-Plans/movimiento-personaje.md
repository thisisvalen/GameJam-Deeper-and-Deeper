# Plan de Implementación: Movimiento del personaje (taladro dental)

**Estado:** implementado
**Archivo:** `Assets/Scripts/Gameplay/CharacterMovement.cs`
**Unity:** 6000.3.16f1 · Input Manager clásico (`activeInputHandler: 0`)

## Contexto

Game jam de un día, tema "deeper and deeper". Juego 2D en el que un ratón dentista monta un taladro y perfora hacia abajo dentro de un diente, atravesando secciones (esmalte → dentina → pulpa → zona radicular). Bajar aumenta la barra de dolor; destruir objetos y ser rápido aumentan la puntuación.

Este documento cubre **únicamente el movimiento del personaje**, que es la base sobre la que se apoyan el resto de sistemas.

## Convenciones del proyecto

- Identificadores en **inglés**; comentarios en **español**.
- C#: `PascalCase` para métodos y propiedades públicas, `camelCase` para campos y variables locales.

## Decisiones de diseño

| Tema | Decisión |
|---|---|
| Avance | Solo mientras se mantiene S, con aceleración/desaceleración (inercia) |
| Rotación | Rota el conjunto completo (ratón + taladro), un solo pivote |
| Límites laterales | Clamp numérico de X (`leftLimit`/`rightLimit`), ajustable en runtime |

## Convención de ángulos

`aimAngle` en grados, con **0° = apuntando recto hacia abajo**:

| Ángulo | Dirección resultante | Tecla |
|---|---|---|
| `-90°` | `(-1, 0)` → izquierda horizontal | A |
| `0°` | `(0, -1)` → recto hacia abajo | — |
| `+90°` | `(1, 0)` → derecha horizontal | D |

```csharp
float angleInRadians = aimAngle * Mathf.Deg2Rad;
return new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians));
```

Con `aimAngle` recortado a `[-90, +90]`, la componente `Y` de la dirección es **siempre ≤ 0** (verificado numéricamente: el máximo en todo el rango es exactamente `0.000000`). La imposibilidad de subir queda garantizada por la propia matemática, no por un parche.

El ángulo se aplica directo como rotación Z mediante `MoveRotation(aimAngle)`, asumiendo que el sprite se dibuja con el taladro hacia abajo en rotación 0.

## Parámetros afinables (Inspector)

| Campo | Valor inicial | Significado |
|---|---|---|
| `rotationSpeed` | `140` | Grados por segundo de giro |
| `maxAimAngle` | `90` | Apertura a cada lado → 180° totales |
| `maxDigSpeed` | `3.5` | Unidades por segundo de avance |
| `acceleration` | `12` | Unidades/s² al mantener S |
| `deceleration` | `18` | Unidades/s² al soltar S |
| `leftLimit` | `-2.5` | Borde izquierdo del túnel |
| `rightLimit` | `2.5` | Borde derecho del túnel |
| `canMove` | `true` | Interruptor de pausa / fin de nivel / cinemática |

## Algoritmo

**`Awake`** — cachea el `Rigidbody2D`, fuerza `bodyType = Kinematic`, guarda `startYPosition` como origen de la profundidad e inicializa `aimAngle = 0`.

**`Update`** — solo lectura de entrada (nunca física):
- `rotationInput = Input.GetAxisRaw("Horizontal")` → A = −1, D = +1
- `digInput = Input.GetKey(KeyCode.S)`
- Con `canMove == false` ambas quedan neutras, de modo que la desaceleración sigue corriendo y el frenado se ve natural.

**`FixedUpdate`** — cuatro pasos en orden:

1. **`ApplyRotation`** — `aimAngle = Mathf.Clamp(aimAngle + rotationInput * rotationSpeed * dt, -maxAimAngle, maxAimAngle)` seguido de `body.MoveRotation(aimAngle)`. Se puede girar aunque no se esté excavando.
2. **`UpdateSpeed`** — `Mathf.MoveTowards(currentSpeed, digInput ? maxDigSpeed : 0f, (digInput ? acceleration : deceleration) * dt)`.
3. **`ApplyMovement`** — si `currentSpeed <= 0` no hace nada; si no:
   - `next = body.position + GetDigDirection() * (currentSpeed * dt)`
   - `next.x = Mathf.Clamp(next.x, leftLimit, rightLimit)` → al topar con el borde el jugador **desliza** y conserva su avance vertical, en lugar de trabarse.
   - `next.y = Mathf.Min(next.y, currentPosition.y)` → red de seguridad ante cualquier empuje ascendente futuro.
   - `body.MovePosition(next)`
4. **`UpdateDepth`** — `maxDepthReached = Mathf.Max(maxDepthReached, startYPosition - body.position.y)`.

Se usa `MovePosition`/`MoveRotation` sobre un `Rigidbody2D` **kinemático** (no `transform`) para que los triggers de los objetos destruibles se disparen correctamente y la interpolación suavice el movimiento a los 50 Hz de la física.

## API pública (ganchos para el resto del juego)

| Miembro | Uso previsto |
|---|---|
| `AimAngle` | Animación y efectos visuales |
| `DigDirection` | Partículas y raycast de detección |
| `CurrentDepth` | **Barra de dolor** y contador de distancia en pantalla |
| `MaxDepthReached` | Secciones del diente y resultado final |
| `IsDigging` | Animación, audio del taladro, sacudida de cámara |
| `SpeedRatio` (0–1) | Intensidad de partículas y tono del taladro |
| `CanMove` (get/set) | Bloqueo desde pausa, fin de nivel o cinemática |
| `SetHorizontalLimits(left, right)` | El sistema de secciones angosta/ensancha el túnel |
| `StopImmediately()` | Fin de nivel o derrota por dolor |

Ningún sistema posterior necesita modificar `CharacterMovement`: todos consumen esta superficie.

## Ayuda de afinado

`OnDrawGizmosSelected` dibuja en la vista de escena los dos bordes del túnel (cian), el arco de 180° de apertura (amarillo) y la dirección actual del taladro (rojo).

## Setup de escena requerido

La escena base sigue siendo la 3D por defecto (`orthographic: 0`), así que hace falta:

1. **Main Camera** → Projection **Orthographic**, Size ≈ 5.
2. GameObject `Player` con:
   - `Rigidbody2D`: Body Type **Kinematic**, Interpolate **Interpolate**, Collision Detection **Continuous**.
   - `CircleCollider2D` en la punta del taladro con **Is Trigger** activado (los destruibles se detectan por trigger; el movimiento no se bloquea con ellos).
   - `CharacterMovement`.
   - Hijos: `Sprite_Mouse`, `Sprite_Drill` (dibujado apuntando hacia abajo en rotación 0) y un transform vacío `DrillTip` en la punta, para VFX y detección futura.

## Verificación

1. **Compilación** — verificada contra los ensamblados de Unity 6000.3.16f1: 0 errores, 0 advertencias.
2. **Invariante matemática** — comprobada la componente `Y` de `GetDigDirection()` en todo el rango `[-90, 90]`: máximo exactamente `0`.
3. **Rotación** — A/D giran el conjunto y se detienen exactamente en ±90°; el taladro nunca apunta por encima de la horizontal.
4. **Inercia** — mantener S acelera progresivamente hasta `maxDigSpeed`; al soltar frena suave hasta detenerse.
5. **Caso límite** — con el taladro a exactamente ±90° y S mantenido, `position.y` no cambia mientras `position.x` avanza.
6. **Límites laterales** — al avanzar en diagonal contra `rightLimit` el jugador desliza por el borde y sigue bajando, sin trabarse ni salirse.
7. **Profundidad** — `MaxDepthReached` crece al bajar y nunca decrece.
8. **Bloqueo** — con `canMove = false` en runtime el jugador desacelera hasta detenerse y deja de responder a A/D/S.

## Fuera de alcance de esta tarea

Barra de dolor, anestesia, puntuación, destrucción de objetos, secciones del diente y seguimiento de cámara.
