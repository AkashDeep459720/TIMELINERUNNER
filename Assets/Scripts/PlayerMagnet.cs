using UnityEngine;
using System.Collections;

public class PlayerMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    public float magnetRadius = 10f;
    public float magnetForce = 15f;
    [SerializeField] private float forwardCaptureDistance = 25f;
    [SerializeField] private float behindCaptureDistance = 4f;
    [SerializeField] private float extraLaneCapturePadding = 2.5f;
    [SerializeField] private Vector3 attractionPointOffset = new Vector3(0f, 1f, 0f);

    [Header("Visuals & UI")]
    public GameObject magnetVFX;
    public MagnetBarUI magnetBarUI;

    private bool isMagnetActive = false;

    private void Start()
    {
        if (magnetVFX != null)
            magnetVFX.SetActive(false);
    }

    public void ActivateMagnet(float duration)
    {
        if (!isMagnetActive)
        {
            if (magnetBarUI != null)
                magnetBarUI.StartTimer(duration);

            StartCoroutine(MagnetRoutine(duration));
        }
    }

    private IEnumerator MagnetRoutine(float duration)
    {
        isMagnetActive = true;

        if (magnetVFX != null)
            magnetVFX.SetActive(true);

        float timer = 0f;

        while (timer < duration)
        {
            // Find all active coins by tag instead of relying on Layer
            GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
            float laneDistance = PlayerMovement.Instance != null ? PlayerMovement.Instance.LaneDistanceValue : 5f;
            float horizontalCapture = laneDistance + extraLaneCapturePadding;
            Vector3 attractionPoint = transform.position + attractionPointOffset;

            foreach (GameObject coin in coins)
            {
                if (coin == null || !coin.activeInHierarchy) continue;

                Vector3 delta = coin.transform.position - transform.position;
                bool insideLaneWindow = Mathf.Abs(delta.x) <= horizontalCapture;
                bool insideForwardWindow = delta.z <= forwardCaptureDistance && delta.z >= -behindCaptureDistance;
                float dist = delta.magnitude;

                if ((insideLaneWindow && insideForwardWindow) || dist <= magnetRadius)
                {
                    float step = magnetForce * Time.deltaTime;
                    coin.transform.position = Vector3.MoveTowards(coin.transform.position, attractionPoint, step);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        isMagnetActive = false;

        if (magnetVFX != null)
            magnetVFX.SetActive(false);
    }
}
