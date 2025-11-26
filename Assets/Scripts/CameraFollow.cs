using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 4f, -10f);
    [SerializeField] float followSpeed = 10f;
    [SerializeField] float yFollowSmooth = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Follow target's X and Z exactly
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            transform.position.y, // keep current Y
            target.position.z + offset.z
        );

        // Lerp forward movement smoothly
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Smooth Y separately (jump/slide bounce)
        float targetY = target.position.y + offset.y;
        float smoothedY = Mathf.Lerp(transform.position.y, targetY, yFollowSmooth * Time.deltaTime);

        transform.position = new Vector3(smoothedPosition.x, smoothedY, smoothedPosition.z);
    }
}
