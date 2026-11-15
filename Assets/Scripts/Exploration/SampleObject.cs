using UnityEngine;
using System;

/// <summary>
/// Reusable geological sample object:
/// represents collectible core samples, volatile ice, or minerals
/// on planetary exploration surfaces.
/// </summary>
public class SampleObject : MonoBehaviour
{
    [Header("--- Sample Identification ---")]
    public string sampleId = "sample_01";
    public string sampleName = "Regolith Core Sample";
    public string scientificDescription = "Enriched with ilmenite (FeTiO3) and volatile lunar ice trapped in permanently shadowed micro-craters.";
    public float interactionRadius = 2.8f;

    [Header("--- Visual & Audio FX ---")]
    public Renderer sampleMeshRenderer;
    public Light sampleGlowLight;
    public Color glowColor = new Color(0.35f, 0.90f, 1.0f);

    public bool IsCollected { get; private set; } = false;

    public static event Action<SampleObject> OnSampleCollectedGlobal;

    private void Start()
    {
        if (sampleGlowLight != null)
        {
            sampleGlowLight.color = glowColor;
        }
    }

    private void Update()
    {
        if (IsCollected) return;

        // Subtle hovering rotation
        transform.Rotate(Vector3.up * (Time.deltaTime * 30f), Space.World);

        if (sampleGlowLight != null)
        {
            sampleGlowLight.intensity = 1.5f + Mathf.Sin(Time.time * 3f) * 0.5f;
        }
    }

    public bool CanInteract(Vector3 astronautPos)
    {
        return !IsCollected && Vector3.Distance(transform.position, astronautPos) <= interactionRadius;
    }

    public void Collect()
    {
        if (IsCollected) return;
        IsCollected = true;

        OnSampleCollectedGlobal?.Invoke(this);

        // Hide or destroy sample visual
        gameObject.SetActive(false);
    }
}
