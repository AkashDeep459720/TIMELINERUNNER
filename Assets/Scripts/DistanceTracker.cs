using UnityEngine;
using TMPro;

public class DistanceTracker : MonoBehaviour
{
    public static DistanceTracker Instance;

    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private Transform player;

    public int currentDistance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (WorldScrollController.Instance != null)
            currentDistance = Mathf.RoundToInt(WorldScrollController.Instance.DistanceTravelled);
        else if (player != null)
            currentDistance = Mathf.RoundToInt(player.position.z);
        else
            currentDistance = 0;

        // update UI text
        if (distanceText)
            distanceText.text = $"{FormatNumber(currentDistance)}m";
    }

    private string FormatNumber(int num)
    {
        return string.Format("{0:n0}", num); // adds commas
    }
}
