using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableRotate : MonoBehaviour
{
    [SerializeField] int rotateSpeed = 4;

    void Update()
    {
        if (GameState.IsPaused) return;

        transform.Rotate(0, rotateSpeed, 0, Space.World);
    }
}
