using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectOrb : MonoBehaviour
{
    [Header("Pickup FX (optional)")]
    [SerializeField] private AudioSource pickupSfxOnThisObject; // If set, we’ll use its clip/volume
    [SerializeField] private GameObject pickupVfxPrefab;        // Particle or simple VFX prefab

    [Header("Orb")]
    [SerializeField, Min(1)] private int orbValue = 1;
    [SerializeField] private string playerTag = "Player";

    [Header("Pooling / Hide")]
    [Tooltip("Objects to hide immediately on pickup (e.g., meshes). If empty, will try to hide all MeshRenderers under this object.")]
    [SerializeField] private Renderer[] hideRenderers;

    [Tooltip("Also disable the collider on pickup to prevent re-trigger while deactivating.")]
    [SerializeField] private bool disableColliderOnPickup = true;

    private Collider col;
    private bool consumed;

    private void Reset()
    {
        // Make sure we’re a trigger
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    private void OnValidate()
    {
        // Keep collider in trigger mode
        var c = GetComponent<Collider>();
        if (c && !c.isTrigger) c.isTrigger = true;
    }

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnEnable()
    {
        // Allow reuse if object is pooled
        consumed = false;

        // Ensure visuals are visible again if pooled
        if (hideRenderers != null && hideRenderers.Length > 0)
        {
            foreach (var r in hideRenderers) if (r) r.enabled = true;
        }
        else
        {
            // If the list is empty, auto-gather child renderers (one-time-ish)
            // (We don’t cache permanently to keep it simple & safe.)
        }
        if (col && disableColliderOnPickup) col.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        if (!other.CompareTag(playerTag)) return;

        consumed = true;

        // 1) Count it (fires OrbManager.OnOrbChanged → your UI popup)
        if (OrbManager.Instance != null)
        {
            OrbManager.Instance.AddOrb(orbValue);
        }

        // 2) VFX
        if (pickupVfxPrefab)
        {
            Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
        }

        // 3) SFX — PlayClipAtPoint so audio isn’t cut off when we disable this object
        if (pickupSfxOnThisObject && pickupSfxOnThisObject.clip)
        {
            AudioSource.PlayClipAtPoint(
                pickupSfxOnThisObject.clip,
                transform.position,
                pickupSfxOnThisObject.volume
            );
        }

        // 4) Hide visuals immediately (pool-friendly)
        if (hideRenderers != null && hideRenderers.Length > 0)
        {
            foreach (var r in hideRenderers) if (r) r.enabled = false;
        }
        else
        {
            // If not provided, try best-effort hide of all child renderers
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        // 5) Stop further triggers right away
        if (col && disableColliderOnPickup) col.enabled = false;

        // 6) Deactivate whole orb (works with pooling)
        gameObject.SetActive(false);
    }
}
