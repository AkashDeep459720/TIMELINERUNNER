using UnityEngine;

public class CoinAudioPlayer : MonoBehaviour
{
    public static void PlaySoundAt(Vector3 position, AudioClip clip)
    {
        if (clip == null) return;

        GameObject tempAudio = new GameObject("TempCoinSound");
        tempAudio.transform.position = position;

        AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.spatialBlend = 0f; // 2D
        audioSource.Play();

        Object.Destroy(tempAudio, clip.length);
    }
}
