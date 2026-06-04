using System;
using UnityEngine;

public static class PortalEvents
{
    /// <summary>Raised when a portal GameObject is instantiated on the robot runway.</summary>
    public static Action<Vector3> OnPortalSpawned;

    /// <summary>Raised when the player timeline swaps to Egypt (masked portal transition).</summary>
    public static Action OnEgyptTimelineEntered;
}
