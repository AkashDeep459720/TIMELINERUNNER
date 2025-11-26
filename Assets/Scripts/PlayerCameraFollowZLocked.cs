using UnityEngine;

public class PlayerCameraFollowLookAhead : MonoBehaviour
{
    public Transform player;
    public float followHeight = 5f;
    public float cameraDistance = 10f;
    public float verticalSmoothTime = 0.05f;

    private float verticalVelocity = 0f;

    void LateUpdate()
    {
        if (player == null) return;

        // Lock camera Z directly behind player (no smoothing)
        float targetZ = player.position.z - cameraDistance;

        // Smooth camera Y to follow jump height
        float targetY = player.position.y + followHeight;
        float smoothY = Mathf.SmoothDamp(transform.position.y, targetY, ref verticalVelocity, verticalSmoothTime);

        // Use fixed X (optional: you can lock to player's X if needed)
        float x = transform.position.x;

        transform.position = new Vector3(x, smoothY, targetZ);
    }
}
