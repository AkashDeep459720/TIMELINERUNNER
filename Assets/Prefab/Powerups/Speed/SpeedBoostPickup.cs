using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpeedBoostPickup : MonoBehaviour
{
    [Header("Boost")]
    [SerializeField] private float boostDuration = 5f;

    [Header("Audio/VFX (optional)")]
    [SerializeField] private AudioClip boostSfx;     // use Clip, not a local AudioSource (prevents cut-off)
    [SerializeField] private GameObject pickupVFX;   // turn off or play a one-shot effect

    [Header("Cleanup")]
    [SerializeField] private float fallbackDestroyDelay = 0.1f;

    private Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true; // ensure trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Prefer a dedicated speed-boost controller on/under the player
        var speed = other.GetComponentInParent<PlayerSpeedBoost>();
        if (speed != null)
        {
            speed.Activate(boostDuration);
        }
        else
        {
            // Fallback: if you really keep it on PlayerMovement (not recommended), uncomment:
            // var pm = other.GetComponentInParent<PlayerMovement>();
            // if (pm != null) pm.ActivateSpeedBoost(boostDuration);
            Debug.LogWarning("SpeedBoostPickup: No PlayerSpeedBoost found on player.");
        }

        // SFX from a temporary, auto-destroyed source (won't be cut off)
        float killDelay = fallbackDestroyDelay;
        if (boostSfx != null)
        {
            AudioSource.PlayClipAtPoint(boostSfx, transform.position);
            killDelay = Mathf.Max(killDelay, boostSfx.length);
        }

        // Optional VFX toggle/one-shot
        if (pickupVFX != null) pickupVFX.SetActive(false);

        // Disable pickup immediately (no double triggers)
        if (_col) _col.enabled = false;
        foreach (Transform child in transform) child.gameObject.SetActive(false);

        // Destroy after audio finishes (or fallback)
        Destroy(gameObject, killDelay);
    }
}
