using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayAudioOnce : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        // Automatically grab the AudioSource on this GameObject
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource or clip missing on " + gameObject.name);
        }
    }
}
