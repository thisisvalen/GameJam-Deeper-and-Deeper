using UnityEngine;

public class PlayDiggingVFX : MonoBehaviour
{
    //Declare variables
    [SerializeField] private ParticleSystem diggingVFX;

    // Update is called once per frame
    void Update()
    {
        PlayDiggingEffect();
    }

    void PlayDiggingEffect()
    {
        // El personaje se mueve/cava mientras se mantiene presionada la tecla S
        bool isDigging = Input.GetKey(KeyCode.S);

        if (diggingVFX != null && !diggingVFX.isPlaying && isDigging)
        {
            diggingVFX.Play();
        }
        else if (diggingVFX != null && diggingVFX.isPlaying && !isDigging)
        {
            diggingVFX.Stop();
        }
    }
}
