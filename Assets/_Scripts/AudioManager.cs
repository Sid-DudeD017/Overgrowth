using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Music")]
    public AudioSource musicSource;

    [Header("Global SFX")]
    public AudioSource sfxSource; 
    public AudioClip growthSound;
    public AudioClip gameOverSound;
    public AudioClip winSound;

    void Awake()
    {

        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void PlayGrowth()
    {
        if(sfxSource && growthSound) sfxSource.PlayOneShot(growthSound);
    }

    public void PlayGameOver()
    {
        if(musicSource) musicSource.Stop(); 
        if(sfxSource && gameOverSound) sfxSource.PlayOneShot(gameOverSound);
    }
    
    public void PlayWin()
    {
        if(musicSource) musicSource.Stop();
        if(sfxSource && winSound) sfxSource.PlayOneShot(winSound);
    }
}