using System.Collections;
using UnityEngine;

public class TinteRojo : MonoBehaviour
{
    private SpriteRenderer miSpriteRenderer;

    [Header("Configuración del Flash")]
    public Color colorDeFlash = Color.red; // O 'new Color(1f, 0f, 0f)'
    public float duracionFlash = 0.2f;

    void Start()
    {
        // Obtenemos el componente al iniciar
        miSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Llama a esta función cuando el personaje reciba daño o haga una acción
    public void ActivarFlash()
    {
        StartCoroutine(EfectoFlash());
    }

    private IEnumerator EfectoFlash()
    {
        // 1. Guardamos el color original
        Color colorOriginal = miSpriteRenderer.color;

        // 2. Cambiamos al color de flash
        miSpriteRenderer.color = colorDeFlash;

        // 3. Esperamos el tiempo indicado
        yield return new WaitForSeconds(duracionFlash);

        // 4. Regresamos al color original
        miSpriteRenderer.color = colorOriginal;
    }
}