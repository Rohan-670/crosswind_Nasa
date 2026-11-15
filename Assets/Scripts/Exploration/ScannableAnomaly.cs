using UnityEngine;

/// <summary>
/// Scannable geological structure / scientific anomaly:
/// provides scientific data revealed when analyzed with the astronaut scanner tool.
/// </summary>
public class ScannableAnomaly : MonoBehaviour
{
    [Header("--- Anomaly Identification ---")]
    public string anomalyId = "anomaly_subsurface_ice";
    public string anomalyName = "Subsurface Volatiles Deposit";
    [TextArea(2, 4)]
    public string telemetryAnalysis = "Multispectral analysis indicates 14.8% H2O volatile concentration under 40cm of fractured regolith crust.";

    [Header("--- Scanning Parameters ---")]
    public float scanRadius = 14f;
    public float scanDurationRequired = 1.8f;

    [Header("--- Visual Elements ---")]
    public Renderer anomalyRenderer;
    public Light anomalyMarkerLight;

    public bool IsScanned { get; private set; } = false;

    public void MarkScanned()
    {
        IsScanned = true;

        if (anomalyMarkerLight != null)
        {
            anomalyMarkerLight.color = new Color(0.35f, 1.0f, 0.55f); // Turn green when scanned
        }
    }
}
