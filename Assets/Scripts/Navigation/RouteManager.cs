using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Reusable RouteManager:
/// manages the planned flight corridor, computes cross-track deviation distance,
/// renders the 3D route trajectory line, tracks corridor recovery countdown,
/// and triggers mission failure if the spacecraft deviates excessively.
/// </summary>
public class RouteManager : MonoBehaviour
{
    public static RouteManager Instance { get; private set; }

    [Header("--- Route Geometry ---")]
    [Tooltip("Transforms marking sequential route waypoints")]
    public List<Transform> routeWaypoints = new List<Transform>();

    [Header("--- Corridor Tolerances ---")]
    [Tooltip("Deviation distance in meters that triggers a recovery warning")]
    public float warningCorridorRadius = 150f;
    [Tooltip("Maximum allowable deviation distance in meters before immediate failure")]
    public float criticalCorridorRadius = 350f;
    [Tooltip("Seconds permitted to recover before mission failure when deviating")]
    public float recoveryTimeLimit = 30f;

    [Header("--- In-World Route Visualization ---")]
    public LineRenderer routeLineRenderer;
    public Color routeColor = new Color(0.25f, 0.85f, 1.0f, 0.65f);
    public float lineWidth = 1.2f;

    [Header("--- Target Spacecraft ---")]
    public SpacecraftController spacecraft;

    // Runtime Deviation Telemetry
    public float CurrentDeviationDistance { get; private set; }
    public bool IsDeviating { get; private set; }
    public float RecoverySecondsRemaining { get; private set; }
    public Vector3 NearestRoutePoint { get; private set; }

    public event Action<bool, float, float> OnDeviationStatusChanged; // isDeviating, deviationDist, secondsLeft

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RecoverySecondsRemaining = recoveryTimeLimit;
        SetupRouteVisualizer();
    }

    private void Update()
    {
        if (spacecraft == null || !spacecraft.IsAlive || routeWaypoints.Count < 2) return;

        UpdateDeviationTracking();
    }

    private void SetupRouteVisualizer()
    {
        if (routeLineRenderer == null)
        {
            routeLineRenderer = gameObject.GetComponent<LineRenderer>();
            if (routeLineRenderer == null)
                routeLineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (routeLineRenderer.sharedMaterial == null)
        {
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                Material lineMat = new Material(lineShader);
                lineMat.color = routeColor;
                routeLineRenderer.sharedMaterial = lineMat;
            }
        }

        if (routeWaypoints.Count >= 2)
        {
            routeLineRenderer.positionCount = routeWaypoints.Count;
            routeLineRenderer.startWidth = lineWidth;
            routeLineRenderer.endWidth = lineWidth;
            routeLineRenderer.startColor = routeColor;
            routeLineRenderer.endColor = routeColor;
            routeLineRenderer.useWorldSpace = true;

            for (int i = 0; i < routeWaypoints.Count; i++)
            {
                if (routeWaypoints[i] != null)
                {
                    routeLineRenderer.SetPosition(i, routeWaypoints[i].position);
                }
            }
        }
    }

    private void UpdateDeviationTracking()
    {
        Vector3 shipPos = spacecraft.transform.position;
        NearestRoutePoint = GetNearestPointOnPolyline(shipPos, out float minDistance);
        CurrentDeviationDistance = minDistance;

        // Check if deviating
        if (minDistance > warningCorridorRadius)
        {
            IsDeviating = true;
            RecoverySecondsRemaining = Mathf.Max(0f, RecoverySecondsRemaining - Time.deltaTime);

            OnDeviationStatusChanged?.Invoke(true, CurrentDeviationDistance, RecoverySecondsRemaining);

            // Check failure conditions
            if (minDistance >= criticalCorridorRadius)
            {
                spacecraft.TriggerFailure($"MISSION ABORT // CRITICAL TRAJECTORY DEVIATION ({minDistance:F0}M OFF CORRIDOR)");
            }
            else if (RecoverySecondsRemaining <= 0f)
            {
                spacecraft.TriggerFailure($"MISSION ABORT // FAILED TO RECOVER TO PLANNED CORRIDOR WITHIN {recoveryTimeLimit:F0}S");
            }
        }
        else
        {
            if (IsDeviating)
            {
                IsDeviating = false;
                RecoverySecondsRemaining = recoveryTimeLimit;
                OnDeviationStatusChanged?.Invoke(false, CurrentDeviationDistance, RecoverySecondsRemaining);
            }
        }
    }

    /// <summary>
    /// Computes the nearest point and distance to the piecewise linear route segments.
    /// </summary>
    private Vector3 GetNearestPointOnPolyline(Vector3 point, out float minDistance)
    {
        minDistance = float.MaxValue;
        Vector3 closestPoint = point;

        for (int i = 0; i < routeWaypoints.Count - 1; i++)
        {
            if (routeWaypoints[i] == null || routeWaypoints[i + 1] == null) continue;

            Vector3 a = routeWaypoints[i].position;
            Vector3 b = routeWaypoints[i + 1].position;
            Vector3 nearestOnSegment = GetNearestPointOnSegment(a, b, point);
            float dist = Vector3.Distance(point, nearestOnSegment);

            if (dist < minDistance)
            {
                minDistance = dist;
                closestPoint = nearestOnSegment;
            }
        }

        return closestPoint;
    }

    private Vector3 GetNearestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float abSqr = ab.sqrMagnitude;
        if (abSqr < 0.0001f) return a;

        float t = Vector3.Dot(p - a, ab) / abSqr;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    public List<Vector3> GetWaypointPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (Transform t in routeWaypoints)
        {
            if (t != null) positions.Add(t.position);
        }
        return positions;
    }
}
