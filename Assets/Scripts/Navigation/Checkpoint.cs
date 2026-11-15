using UnityEngine;

/// <summary>
/// Individual flight checkpoint / navigation gate:
/// provides a 3D visual holographic ring that changes state between
/// Pending (dim), Active Target (pulsing cyan), and Cleared (emerald green).
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("--- Identification ---")]
    public int checkpointIndex = 0;
    public string checkpointName = "Nav Waypoint";
    public bool isFinalDestination = false;
    public float triggerRadius = 22f;

    [Header("--- Visual Components ---")]
    public Renderer ringRenderer;
    public Light beaconLight;

    public enum CheckpointState
    {
        Pending,
        Active,
        Completed
    }

    public CheckpointState State { get; private set; } = CheckpointState.Pending;

    private Material ringMaterial;
    private Color activeColor = new Color(0.25f, 0.85f, 1.0f);
    private Color completedColor = new Color(0.25f, 1.0f, 0.45f);
    private Color pendingColor = new Color(0.5f, 0.55f, 0.65f, 0.35f);

    private void Awake()
    {
        if (ringRenderer != null)
        {
            ringMaterial = ringRenderer.material;
        }
    }

    private void Update()
    {
        // Gentle holographic ring rotation and pulse
        transform.Rotate(Vector3.forward * (Time.deltaTime * 20f), Space.Self);

        if (State == CheckpointState.Active)
        {
            float pulse = 1.0f + Mathf.Sin(Time.time * 4f) * 0.08f;
            transform.localScale = Vector3.one * (triggerRadius * pulse);

            if (beaconLight != null)
            {
                beaconLight.intensity = 2.5f + Mathf.Sin(Time.time * 4f) * 1.0f;
            }
        }
    }

    public void SetState(CheckpointState newState)
    {
        State = newState;

        Color targetColor = pendingColor;
        bool lightActive = false;

        switch (newState)
        {
            case CheckpointState.Pending:
                targetColor = pendingColor;
                lightActive = false;
                transform.localScale = Vector3.one * triggerRadius;
                break;

            case CheckpointState.Active:
                targetColor = isFinalDestination ? new Color(1.0f, 0.85f, 0.25f) : activeColor;
                lightActive = true;
                break;

            case CheckpointState.Completed:
                targetColor = completedColor;
                lightActive = false;
                transform.localScale = Vector3.one * triggerRadius;
                break;
        }

        if (ringMaterial != null)
        {
            ringMaterial.color = targetColor;
            if (ringMaterial.HasProperty("_BaseColor")) ringMaterial.SetColor("_BaseColor", targetColor);
            if (ringMaterial.HasProperty("_EmissionColor")) ringMaterial.SetColor("_EmissionColor", targetColor * 1.5f);
        }

        if (beaconLight != null)
        {
            beaconLight.enabled = lightActive;
            beaconLight.color = targetColor;
        }
    }
}
