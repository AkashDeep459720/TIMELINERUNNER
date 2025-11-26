using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShieldPickup : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var shield = other.GetComponent<PlayerShield>();
        if (shield != null)
        {
            shield.ActivateShield();
            Debug.Log("🛡️ Shield activated for 5 seconds!");
            Destroy(gameObject);
        }
    }
}
