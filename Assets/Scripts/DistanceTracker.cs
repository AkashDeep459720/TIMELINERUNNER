using UnityEngine;
using TMPro;

public class DistanceTracker : MonoBehaviour
{
    public static DistanceTracker Instance;

    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private Transform player;

    public int currentDistance { get; private set; }

    public static bool pauseHudUpdates = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        currentDistance = GetDistanceMeters();

        if (pauseHudUpdates || distanceText == null) return;

        distanceText.text = $"{FormatNumber(currentDistance)}m";
    }

    public static int GetDistanceMeters()
    {
        if (WorldScrollController.Instance != null)
            return Mathf.FloorToInt(WorldScrollController.Instance.DistanceTravelled);

        return 0;
    }

    private string FormatNumber(int num)
    {
        return string.Format("{0:n0}", num);
    }
}
