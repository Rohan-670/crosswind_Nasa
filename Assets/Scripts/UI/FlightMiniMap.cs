using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Top-Right Navigation Mini-Map / Radar:
/// displays the planned flight route, player spacecraft position and heading,
/// active and cleared checkpoints, final destination beacon, and hazard danger zones.
/// Optimized for browser/WebGL performance using lightweight UI elements.
/// </summary>
public class FlightMiniMap : MonoBehaviour
{
    [Header("--- Mini-Map Geometry ---")]
    public RectTransform mapContainer;
    [Tooltip("World distance in meters represented from map center to edge")]
    public float mapWorldRange = 220f;
    public bool centerOnPlayer = true;

    [Header("--- Target References ---")]
    public SpacecraftController spacecraft;
    public RouteManager routeManager;
    public CheckpointManager checkpointManager;
    public HazardManager hazardManager;

    [Header("--- Map Blip Prefabs / Prototypes ---")]
    public RectTransform playerBlip;
    public RectTransform checkpointBlipTemplate;
    public RectTransform destinationBlip;
    public RectTransform hazardBlipTemplate;
    public RectTransform routeDotTemplate;

    private List<RectTransform> spawnedCheckpointBlips = new List<RectTransform>();
    private List<RectTransform> spawnedHazardBlips = new List<RectTransform>();
    private List<RectTransform> spawnedRouteDots = new List<RectTransform>();

    private float mapPixelRadius = 110f;

    private void Start()
    {
        if (mapContainer != null)
        {
            mapPixelRadius = Mathf.Min(mapContainer.rect.width, mapContainer.rect.height) * 0.46f;
        }

        InitializeMapElements();
    }

    private void LateUpdate()
    {
        if (spacecraft == null || mapContainer == null) return;

        UpdateMapTelemetry();
    }

    public void InitializeMapElements()
    {
        // 1. Initialize Route Dots
        if (routeManager != null && routeDotTemplate != null)
        {
            routeDotTemplate.gameObject.SetActive(false);
            List<Vector3> waypoints = routeManager.GetWaypointPositions();

            // Sample points along route segments
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 pA = waypoints[i];
                Vector3 pB = waypoints[i + 1];
                float dist = Vector3.Distance(pA, pB);
                int samples = Mathf.Max(2, Mathf.RoundToInt(dist / 20f));

                for (int s = 0; s <= samples; s++)
                {
                    Vector3 samplePos = Vector3.Lerp(pA, pB, (float)s / samples);
                    RectTransform dot = Instantiate(routeDotTemplate, mapContainer);
                    dot.gameObject.SetActive(true);
                    dot.name = $"RouteDot_{i}_{s}";
                    spawnedRouteDots.Add(dot);
                }
            }
        }

        // 2. Initialize Checkpoint Blips
        if (checkpointManager != null && checkpointBlipTemplate != null)
        {
            checkpointBlipTemplate.gameObject.SetActive(false);

            for (int i = 0; i < checkpointManager.checkpoints.Count; i++)
            {
                Checkpoint cp = checkpointManager.checkpoints[i];
                if (cp == null) continue;

                RectTransform blip = Instantiate(checkpointBlipTemplate, mapContainer);
                blip.gameObject.SetActive(true);
                blip.name = $"Blip_Checkpoint_{i}";
                spawnedCheckpointBlips.Add(blip);
            }
        }

        // 3. Initialize Hazard Blips
        if (hazardManager != null && hazardBlipTemplate != null)
        {
            hazardBlipTemplate.gameObject.SetActive(false);

            for (int i = 0; i < hazardManager.hazardZones.Count; i++)
            {
                HazardZone hz = hazardManager.hazardZones[i];
                if (hz == null) continue;

                RectTransform hBlip = Instantiate(hazardBlipTemplate, mapContainer);
                hBlip.gameObject.SetActive(true);
                hBlip.name = $"Blip_Hazard_{i}";

                // Size hazard circle proportional to world radius
                float blipPixelSize = (hz.radius / mapWorldRange) * (mapPixelRadius * 2f);
                hBlip.sizeDelta = new Vector2(blipPixelSize, blipPixelSize);

                spawnedHazardBlips.Add(hBlip);
            }
        }
    }

    private void UpdateMapTelemetry()
    {
        Vector3 shipPos = spacecraft.transform.position;
        Vector3 mapCenterWorldPos = centerOnPlayer ? shipPos : Vector3.zero;

        // 1. Update Player Blip
        if (playerBlip != null)
        {
            if (centerOnPlayer)
            {
                playerBlip.anchoredPosition = Vector2.zero;
                // Face upward, rotate with ship yaw
                playerBlip.localRotation = Quaternion.Euler(0f, 0f, -spacecraft.transform.eulerAngles.y);
            }
            else
            {
                playerBlip.anchoredPosition = WorldToMapPos(shipPos, mapCenterWorldPos);
                playerBlip.localRotation = Quaternion.Euler(0f, 0f, -spacecraft.transform.eulerAngles.y);
            }
        }

        // 2. Update Route Dots
        if (routeManager != null && spawnedRouteDots.Count > 0)
        {
            List<Vector3> waypoints = routeManager.GetWaypointPositions();
            int dotIdx = 0;

            for (int i = 0; i < waypoints.Count - 1 && dotIdx < spawnedRouteDots.Count; i++)
            {
                Vector3 pA = waypoints[i];
                Vector3 pB = waypoints[i + 1];
                float dist = Vector3.Distance(pA, pB);
                int samples = Mathf.Max(2, Mathf.RoundToInt(dist / 20f));

                for (int s = 0; s <= samples && dotIdx < spawnedRouteDots.Count; s++)
                {
                    Vector3 samplePos = Vector3.Lerp(pA, pB, (float)s / samples);
                    spawnedRouteDots[dotIdx].anchoredPosition = WorldToMapPos(samplePos, mapCenterWorldPos);
                    dotIdx++;
                }
            }
        }

        // 3. Update Checkpoint Blips
        if (checkpointManager != null)
        {
            for (int i = 0; i < checkpointManager.checkpoints.Count && i < spawnedCheckpointBlips.Count; i++)
            {
                Checkpoint cp = checkpointManager.checkpoints[i];
                RectTransform blip = spawnedCheckpointBlips[i];
                if (cp == null || blip == null) continue;

                blip.anchoredPosition = WorldToMapPos(cp.transform.position, mapCenterWorldPos);

                // Update visual color
                Image img = blip.GetComponent<Image>();
                if (img != null)
                {
                    if (cp.isFinalDestination)
                    {
                        img.color = new Color(1.0f, 0.85f, 0.25f); // Gold destination
                    }
                    else if (cp.State == Checkpoint.CheckpointState.Completed)
                    {
                        img.color = new Color(0.25f, 1.0f, 0.45f); // Green completed
                    }
                    else if (cp.State == Checkpoint.CheckpointState.Active)
                    {
                        img.color = new Color(0.25f, 0.85f, 1.0f); // Cyan active
                    }
                    else
                    {
                        img.color = new Color(0.5f, 0.55f, 0.65f, 0.6f); // Grey pending
                    }
                }
            }
        }

        // 4. Update Hazard Blips
        if (hazardManager != null)
        {
            for (int i = 0; i < hazardManager.hazardZones.Count && i < spawnedHazardBlips.Count; i++)
            {
                HazardZone hz = hazardManager.hazardZones[i];
                RectTransform hBlip = spawnedHazardBlips[i];
                if (hz == null || hBlip == null) continue;

                hBlip.anchoredPosition = WorldToMapPos(hz.transform.position, mapCenterWorldPos);
            }
        }
    }

    private Vector2 WorldToMapPos(Vector3 worldPos, Vector3 centerWorldPos)
    {
        // Project onto X (horizontal) and Z (depth) plane
        float dx = worldPos.x - centerWorldPos.x;
        float dz = worldPos.z - centerWorldPos.z;

        float mapX = (dx / mapWorldRange) * mapPixelRadius;
        float mapY = (dz / mapWorldRange) * mapPixelRadius;

        // Clamp inside radar boundary
        Vector2 pos = new Vector2(mapX, mapY);
        if (pos.magnitude > mapPixelRadius)
        {
            pos = pos.normalized * mapPixelRadius;
        }

        return pos;
    }
}
