using UnityEngine;

public class Latidos : MonoBehaviour
{
   
    [SerializeField] private float escalaMinima = 0.13f;
    [SerializeField] private float escalaMaxima = 0.2f;
    [SerializeField] private float velocidad = 2f;

    void Update()
    {
        float t = (Mathf.Sin(Time.time * velocidad) + 1f) / 2f;
        float nuevoTamano = Mathf.Lerp(escalaMinima, escalaMaxima, t);

        transform.localScale = new Vector3(nuevoTamano, nuevoTamano, 1f);
    }
}
