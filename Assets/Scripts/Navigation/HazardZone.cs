using UnityEngine;

/// <summary>
/// Individual hazard zone in space:
/// defines a spherical danger zone (e.g. micrometeorite swarm or solar radiation pocket)
/// that inflicts continuous hull degradation when entered.
/// </summary>
public class HazardZone : MonoBehaviour
{
    public enum HazardType
    {
        RadiationStorm,
        SpaceDebrisField,
        MicrometeoriteCluster
    }

    [Header("--- Specifications ---")]
    public string hazardName = "Solar Radiation Pocket";
    public HazardType hazardType = HazardType.RadiationStorm;
    public float radius = 55f;
    [Tooltip("Hull damage inflicted per second inside the zone")]
    public float damagePerSecond = 8.5f;

    [Header("--- Visual Telemetry ---")]
    public Renderer boundaryRenderer;
    public Light dangerLight;
    public Color warningColor = new Color(1.0f, 0.25f, 0.20f, 0.35f);

    private void Start()
    {
        transform.localScale = Vector3.one * (radius * 2f);

        if (boundaryRenderer != null)
        {
            Material mat = boundaryRenderer.material;
            if (mat != null)
            {
                mat.color = warningColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", warningColor);
            }
        }

        if (dangerLight != null)
        {
            dangerLight.color = warningColor;
            dangerLight.range = radius * 1.5f;
        }
    }

    private void Update()
    {
        // Gentle rotation of hazard debris / visual volume
        transform.Rotate(Vector3.up * (Time.deltaTime * 6f), Space.World);

        if (dangerLight != null)
        {
            dangerLight.intensity = 1.8f + Mathf.Sin(Time.time * 3f) * 0.8f;
        }
    }

    public bool IsPositionInside(Vector3 pos)
    {
        return Vector3.Distance(transform.position, pos) <= radius;
    }
}
