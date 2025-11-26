using UnityEngine;

public class CollectCoin : MonoBehaviour
{
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private GameObject visuals;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        MasterInfo.coinCount++;

        if (visuals != null)
            visuals.SetActive(false);

        // Play sound decoupled from this object
        CoinAudioPlayer.PlaySoundAt(transform.position, coinClip);

        // Instead of destroying, just disable
        gameObject.SetActive(false);

        // Re-enable visuals when reused from pool
        if (visuals != null)
            Invoke(nameof(ResetVisuals), 0.01f);
    }

    private void ResetVisuals()
    {
        if (visuals != null)
            visuals.SetActive(true);
    }
}
