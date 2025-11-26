using System;
using UnityEngine;

public static class SegmentEvents
{
    /// <summary>
    /// Raised whenever a new segment is spawned.
    /// </summary>
    public static Action<GameObject> OnSegmentSpawned;
}
