using System;
using UnityEngine;

public static class PortalEvents
{
    /// <summary>Raised when a portal GameObject is instantiated.</summary>
    public static Action<Vector3> OnPortalSpawned;
}
