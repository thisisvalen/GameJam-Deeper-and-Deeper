using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scenes
{
    MainMenu,
    CreditsView,
    PlayerView,
    Tutorial,
    GameOver,
    WinnerView
}
public class AudioManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip buttonClip; // Efecto de sonido al presionar un boton
    [SerializeField] private AudioClip syringeClip; // Efecto de sonido al usar una jeringa
    [SerializeField] private AudioClip obstacleClip; // Efecto de sonido al taladrar un nervio
    [SerializeField] private AudioClip collectableClip; // Efecto de sonido al taladrar un collectable

    [Header("Pistas de sonido")]
    [SerializeField] private AudioClip playerMusic; // Pista de sonido en loop del taladro del jugador
    [SerializeField] private AudioClip menuMusic; // Pista de sonido del menu
    [SerializeField] private AudioClip gameplayMusic; // Pista de sonido del juego

    [Header("Canales")]
    [SerializeField] private AudioSource sfxChannel; //Canal de efectos de sonido
    [SerializeField] private AudioSource musicChannel; //Canal de pistas de sonido
    [SerializeField] private AudioSource playerChannel; // Canal de pista de sonido del taladro

    public bool isMuted = false;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        sfxChannel = gameObject.AddComponent<AudioSource>();
        musicChannel = gameObject.AddComponent<AudioSource>();
        playerChannel = gameObject.AddComponent<AudioSource>();
    
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic();
    }
    public void PlaySoundEffect(ItemType itemType)
    {
        if(isMuted)
        {
            return;
        }
        switch (itemType)
        {
            case ItemType.Syringe:
                sfxChannel.PlayOneShot(syringeClip);
                break;
            case ItemType.Collectable:
                sfxChannel.PlayOneShot(collectableClip);
                break;
            case ItemType.Obstacle:
                sfxChannel.PlayOneShot(obstacleClip);
                break;
            default:
                break;
        }
    }

    public void PlayPlayerMusic()
    {
        if (playerChannel.clip == playerMusic && playerChannel.isPlaying)
            return;

        playerChannel.clip = playerMusic;
        playerChannel.loop = true;
        playerChannel.Play();
    }

    public void PlayMusic()
    {
        AudioClip clipToPlay = null;

        Scenes currentScene = (Scenes)SceneManager.GetActiveScene().buildIndex;

        if(currentScene == Scenes.MainMenu || currentScene == Scenes.CreditsView || currentScene == Scenes.Tutorial)
        {
            clipToPlay = menuMusic;
        }
        else if(currentScene == Scenes.PlayerView || currentScene == Scenes.GameOver || currentScene == Scenes.WinnerView)
        {
            clipToPlay = gameplayMusic;
        }
        else
        {
            return; // No hay música para reproducir en esta escena
        }

        if(musicChannel.clip == clipToPlay && musicChannel.isPlaying)
        {
            return;
        }
        else
        {
            musicChannel.clip = clipToPlay;
            musicChannel.loop = true;
            musicChannel.Play();
        }
    }

    public void StopPlayerMusic()
    {
        playerChannel.Stop();
    }

    public void StopMenuMusic()
    {
        musicChannel.Stop();
    }

    public void PauseClips()
    {
        playerChannel.Pause();
        musicChannel.Pause();
    }

    public void UnPauseClips()
    {
        playerChannel.UnPause();
        musicChannel.UnPause();
    }

    public void MuteClips()
    {
        playerChannel.mute = true;
        musicChannel.mute = true;
        isMuted = true;
    }

    public void UnMuteClips()
    {
        playerChannel.mute = false;
        musicChannel.mute = false;
        isMuted = false;
    }

    public void PlayButtonSound()
    {
        if(isMuted)
        {
            return;
        }
        sfxChannel.PlayOneShot(buttonClip);
    }


}
