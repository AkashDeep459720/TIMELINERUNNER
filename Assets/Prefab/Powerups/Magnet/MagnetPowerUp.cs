using UnityEngine;

public class MagnetPowerUp : MonoBehaviour
{
    public float duration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMagnet pm = other.GetComponent<PlayerMagnet>();
        if (pm != null)
        {
            pm.ActivateMagnet(duration);
        }

        // ❌ Old (bad for pooling):
        // Destroy(gameObject);

        // ✅ New (safe with ObjectPoolManager)
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            // fallback if no pool manager is found
            gameObject.SetActive(false);
        }
    }
}
