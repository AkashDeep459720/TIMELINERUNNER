using System.Collections;
using UnityEngine;

/// <summary>
/// Re-snaps portal Y after spawn so colliders are ready (end-of-frame safety net).
/// </summary>
public class PortalGroundSnap : MonoBehaviour
{
    [SerializeField] private float portalBaseYOffset = 1.93f;
    [SerializeField] private LayerMask groundSnapMask = ~0;
    [SerializeField] private float groundSnapRayHeight = 6f;
    [SerializeField] private float groundSnapRayDistance = 18f;

    private Coroutine snapRoutine;

    public void Configure(float baseYOffset, LayerMask mask, float rayHeight, float rayDistance)
    {
        portalBaseYOffset = baseYOffset;
        groundSnapMask = mask;
        groundSnapRayHeight = rayHeight;
        groundSnapRayDistance = rayDistance;
    }

    public void SnapToGroundDeferred()
    {
        if (snapRoutine != null)
            StopCoroutine(snapRoutine);
        snapRoutine = StartCoroutine(SnapNextFrame());
    }

    private IEnumerator SnapNextFrame()
    {
        yield return null;
        Physics.SyncTransforms();
        ApplyGroundY();
        snapRoutine = null;
    }

    private void ApplyGroundY()
    {
        Vector3 pos = transform.position;
        float groundY = SampleGroundY(pos);
        if (!float.IsNaN(groundY))
            transform.position = new Vector3(pos.x, groundY + portalBaseYOffset, pos.z);
    }

    private float SampleGroundY(Vector3 worldPos)
    {
        Vector3 rayOrigin = worldPos + Vector3.up * groundSnapRayHeight;
        float maxDist = groundSnapRayHeight + groundSnapRayDistance;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, maxDist, groundSnapMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return float.NaN;

        float lowest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].collider.transform.IsChildOf(transform) || hits[i].collider.transform == transform)
                continue;
            if (hits[i].point.y < lowest)
                lowest = hits[i].point.y;
        }

        return lowest < float.MaxValue ? lowest : float.NaN;
    }
}
