using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    public bool musicOn = true;
    public bool sfxOn = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioPrefs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ToggleMusic(bool state)
    {
        musicOn = state;
        musicSource.mute = !state;
        PlayerPrefs.SetInt("Music", state ? 1 : 0);
    }

    public void ToggleSFX(bool state)
    {
        sfxOn = state;
        sfxSource.mute = !state;
        PlayerPrefs.SetInt("SFX", state ? 1 : 0);
    }

    void LoadAudioPrefs()
    {
        musicOn = PlayerPrefs.GetInt("Music", 1) == 1;
        sfxOn = PlayerPrefs.GetInt("SFX", 1) == 1;

        musicSource.mute = !musicOn;
        sfxSource.mute = !sfxOn;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxOn && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayMusicClip(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        if (musicOn)
            musicSource.Play();
    }
}
