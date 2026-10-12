using System.Collections.Generic;
using UnityEngine;

public sealed class WaypointPath : MonoBehaviour
{
    [SerializeField] private List<Transform> points = new();

    public int Count => points.Count;

    public Transform GetPoint(int index) => points[index];

    public void SetPoints(params Transform[] waypoints)
    {
        points.Clear();
        points.AddRange(waypoints);
    }
}
