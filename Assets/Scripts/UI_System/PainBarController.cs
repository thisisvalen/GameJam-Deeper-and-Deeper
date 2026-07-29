using UnityEngine;
using UnityEngine.UI;

public class PainBarController : MonoBehaviour
{
    [Header("UI References")]
    public Slider painSlider;    // Asigna el Slider desde el Inspector
    public Image fillImage;      // Asigna la imagen 'Fill' dentro del Slider

    [Header("Color Thresholds")]
    public Color greenColor = new Color(0.45f, 0.75f, 0.1f);  // Verde
    public Color yellowColor = new Color(0.98f, 0.78f, 0.0f); // Amarillo
    public Color redColor = new Color(0.98f, 0.23f, 0.12f);  // Rojo

    public void UpdatePainBar(float currentPain, float maxPain)
    {
        // 1. Normalizar el valor entre 0 y 1
        float fillAmount = Mathf.Clamp01(currentPain / maxPain);
        painSlider.value = fillAmount;

        // 2. Cambiar color según el tercio del dolor
        if (fillAmount <= 0.33f)
        {
            // Primer tercio (0% a 33%): Verde
            fillImage.color = greenColor;
        }
        else if (fillAmount <= 0.66f)
        {
            // Segundo tercio (34% a 66%): Amarillo
            fillImage.color = yellowColor;
        }
        else
        {
            // Tercer tercio (67% a 100%): Rojo (Insupportable)
            fillImage.color = redColor;
        }
    }
}