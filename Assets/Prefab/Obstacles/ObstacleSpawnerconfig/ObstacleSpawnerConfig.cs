using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleSpawnerConfig", menuName = "Configs/ObstacleSpawnerConfig")]
public class ObstacleSpawnerConfig : ScriptableObject
{
    [Header("Spawn Settings")]
    public int maxObstaclesPerSegment = 2;
    public int cleanSegmentInterval = 4; // every X segments one is empty

    [Header("Probability")]
    [Range(0f, 1f)] public float baseSpawnChance = 0.5f; // 0.5 = 50%
    public float difficultyRampRate = 0.05f; // linear ramp per 100 distance units

    [Header("Spacing")]
    public float minZGap = 5f; // space between obstacles in same segment

    [Header("Obstacle Prefabs")]
    public GameObject[] jumpOverObstacles;
    public GameObject[] slideUnderObstacles;
    public GameObject[] avoidLaneObstacles;
    public GameObject[] fullLaneObstacles;
}
