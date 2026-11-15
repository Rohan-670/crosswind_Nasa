using UnityEngine;
using System;

/// <summary>
/// Reusable astronaut geological scanner:
/// scans nearby anomalies when holding [F] or [Right Click],
/// fills scan progress, and broadcasts telemetry to the exploration HUD.
/// </summary>
public class AstronautScanner : MonoBehaviour
{
    [Header("--- Target Scanning Range ---")]
    public float maxScanDistance = 16f;
    public LayerMask scannableLayer = ~0;

    [Header("--- Visual & Audio FX ---")]
    public LineRenderer scanBeamRenderer;
    public Light scanConeLight;
    public Transform scanEmitterOrigin;

    // Runtime state
    public bool IsScanning { get; private set; } = false;
    public float CurrentScanProgress { get; private set; } = 0f;
    public ScannableAnomaly TargetedAnomaly { get; private set; } = null;

    public event Action<ScannableAnomaly, float> OnScanProgressUpdated; // anomaly, progress01
    public event Action<ScannableAnomaly> OnAnomalyScanCompleted;

    private void Start()
    {
        if (scanEmitterOrigin == null)
            scanEmitterOrigin = transform;

        SetScanFXActive(false);
    }

    private void Update()
    {
        bool scanInput = AstroInput.GetKey(KeyCode.F) || AstroInput.GetMouseButton(1);

        if (scanInput)
        {
            PerformScanningCycle();
        }
        else
        {
            if (IsScanning)
            {
                CancelScan();
            }
        }
    }

    private void PerformScanningCycle()
    {
        IsScanning = true;
        SetScanFXActive(true);

        ScannableAnomaly hitAnomaly = FindAnomalyInView();

        if (hitAnomaly != null && !hitAnomaly.IsScanned)
        {
            TargetedAnomaly = hitAnomaly;
            CurrentScanProgress += Time.deltaTime / hitAnomaly.scanDurationRequired;
            OnScanProgressUpdated?.Invoke(TargetedAnomaly, Mathf.Clamp01(CurrentScanProgress));

            // Aim beam at anomaly
            if (scanBeamRenderer != null)
            {
                scanBeamRenderer.positionCount = 2;
                scanBeamRenderer.SetPosition(0, scanEmitterOrigin.position);
                scanBeamRenderer.SetPosition(1, hitAnomaly.transform.position);
            }

            if (CurrentScanProgress >= 1.0f)
            {
                hitAnomaly.MarkScanned();
                OnAnomalyScanCompleted?.Invoke(hitAnomaly);
                CancelScan();
            }
        }
        else
        {
            TargetedAnomaly = null;
            CurrentScanProgress = 0f;
            OnScanProgressUpdated?.Invoke(null, 0f);

            // Project beam forward into empty space
            if (scanBeamRenderer != null)
            {
                scanBeamRenderer.positionCount = 2;
                scanBeamRenderer.SetPosition(0, scanEmitterOrigin.position);
                scanBeamRenderer.SetPosition(1, scanEmitterOrigin.position + scanEmitterOrigin.forward * (maxScanDistance * 0.75f));
            }
        }
    }

    private ScannableAnomaly FindAnomalyInView()
    {
        Collider[] hits = Physics.OverlapSphere(scanEmitterOrigin.position, maxScanDistance, scannableLayer);
        ScannableAnomaly nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            ScannableAnomaly anomaly = col.GetComponent<ScannableAnomaly>() ?? col.GetComponentInParent<ScannableAnomaly>();
            if (anomaly != null && !anomaly.IsScanned)
            {
                Vector3 toAnomaly = (anomaly.transform.position - scanEmitterOrigin.position).normalized;
                float angle = Vector3.Angle(scanEmitterOrigin.forward, toAnomaly);

                // Cone of detection (60 degrees)
                if (angle < 60f)
                {
                    float d = Vector3.Distance(scanEmitterOrigin.position, anomaly.transform.position);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearest = anomaly;
                    }
                }
            }
        }

        return nearest;
    }

    private void CancelScan()
    {
        IsScanning = false;
        CurrentScanProgress = 0f;
        TargetedAnomaly = null;
        SetScanFXActive(false);
        OnScanProgressUpdated?.Invoke(null, 0f);
    }

    private void SetScanFXActive(bool active)
    {
        if (scanBeamRenderer != null)
            scanBeamRenderer.enabled = active;

        if (scanConeLight != null)
            scanConeLight.enabled = active;
    }
}
