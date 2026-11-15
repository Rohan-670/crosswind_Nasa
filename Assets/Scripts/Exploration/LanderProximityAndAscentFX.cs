using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random;

/// <summary>
/// Controls the Lunar Lander proximity feedback and cinematic ascent liftoff:
/// 1. Activates holographic proximity beacon & ground ring ONLY when astronaut is near the spacecraft
/// 2. Executes realistic Apollo-style lunar ascent liftoff sequence (engine ignition, radial regolith dust blast,
///    ascent stage separation from descent stage, and ascent into lunar orbit towards Earth).
/// </summary>
public class LanderProximityAndAscentFX : MonoBehaviour
{
    [Header("--- Proximity Triggering ---")]
    [Tooltip("Target astronaut transform to track distance")]
    public Transform astronautTransform;
    [Tooltip("Exploration manager tracking mission objectives")]
    public ExplorationManager explorationManager;
    [Tooltip("Maximum distance at which proximity beacon activates (meters)")]
    public float proximityRadius = 18f;

    [Header("--- Visual Proximity FX (Active ONLY when near) ---")]
    [Tooltip("Root object containing proximity holographic rings and guide lights")]
    public GameObject proximityVisualRoot;
    [Tooltip("Ground projection ring mesh or light")]
    public Renderer groundRingRenderer;
    [Tooltip("Vertical holographic beam pointing towards the ingress ladder")]
    public Renderer verticalGuideBeam;
    [Tooltip("Beacon light pulsing atop the lander hatch")]
    public Light ingressBeaconLight;

    [Header("--- Lunar Ascent Liftoff VFX ---")]
    [Tooltip("Ascent cabin transform that separates and flies into space")]
    public Transform ascentCabin;
    [Tooltip("Descent stage and legs that remain stationary on the Moon surface")]
    public Transform descentStage;
    [Tooltip("Ascent rocket engine exhaust plume particle system")]
    public ParticleSystem ascentFlameParticles;
    [Tooltip("Radial regolith dust blast ring expanding across the lunar crater ground")]
    public ParticleSystem radialDustBlastParticles;
    [Tooltip("Rocket ignition point light")]
    public Light engineIgnitionLight;
    [Tooltip("Cinematic camera that frames the lunar liftoff")]
    public Camera ascentCinematicCamera;

    [Header("--- Audio ---")]
    public AudioSource fxAudioSource;

    // Runtime state
    public bool IsAscending { get; private set; } = false;
    private bool wasNear = false;
    private float pulseTimer = 0f;

    private void Awake()
    {
        SetupProceduralAudio();
    }

    private void Start()
    {
        // Find astronaut automatically if not assigned
        if (astronautTransform == null)
        {
            var astro = FindFirstObjectByType<AstronautController>();
            if (astro != null) astronautTransform = astro.transform;
        }

        if (explorationManager == null)
        {
            explorationManager = ExplorationManager.Instance ?? FindFirstObjectByType<ExplorationManager>();
        }

        // Initially ensure proximity VFX are hidden
        if (proximityVisualRoot != null)
        {
            proximityVisualRoot.SetActive(false);
        }

        if (engineIgnitionLight != null) engineIgnitionLight.enabled = false;
        if (ascentFlameParticles != null && ascentFlameParticles.isPlaying) ascentFlameParticles.Stop();
        if (radialDustBlastParticles != null && radialDustBlastParticles.isPlaying) radialDustBlastParticles.Stop();
        if (ascentCinematicCamera != null) ascentCinematicCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsAscending) return;

        UpdateProximityDetection();
    }

    private void UpdateProximityDetection()
    {
        if (astronautTransform == null) return;

        float dist = Vector3.Distance(transform.position, astronautTransform.position);
        bool isNear = dist <= proximityRadius;

        // When entering or exiting proximity
        if (isNear != wasNear)
        {
            wasNear = isNear;
            if (proximityVisualRoot != null)
            {
                proximityVisualRoot.SetActive(isNear);
            }

            if (isNear)
            {
                PlayProximityChime();
            }
        }

        // Animate glowing pulse ONLY when astronaut is near
        if (isNear)
        {
            pulseTimer += Time.deltaTime * 3.2f;
            float pulse = 0.65f + (Mathf.Sin(pulseTimer) * 0.35f);

            if (ingressBeaconLight != null)
            {
                ingressBeaconLight.intensity = 2.5f * pulse;
            }

            if (groundRingRenderer != null && groundRingRenderer.material != null)
            {
                Color ringCol = new Color(0.35f, 0.95f, 1.0f, 0.75f * pulse);
                groundRingRenderer.material.color = ringCol;
                if (groundRingRenderer.material.HasProperty("_BaseColor"))
                    groundRingRenderer.material.SetColor("_BaseColor", ringCol);
            }

            if (verticalGuideBeam != null && verticalGuideBeam.material != null)
            {
                Color beamCol = new Color(0.3f, 0.85f, 1.0f, 0.35f * pulse);
                verticalGuideBeam.material.color = beamCol;
                if (verticalGuideBeam.material.HasProperty("_BaseColor"))
                    verticalGuideBeam.material.SetColor("_BaseColor", beamCol);
            }
        }
    }

    // =========================================================================
    // LUNAR ASCENT LIFTOFF SEQUENCE
    // =========================================================================
    public void PlayAscentSequence(Action onComplete)
    {
        if (IsAscending) return;
        StartCoroutine(AscentLiftoffRoutine(onComplete));
    }

    private IEnumerator AscentLiftoffRoutine(Action onComplete)
    {
        IsAscending = true;
        Debug.Log("[LanderFX] Initiating Lunar Ascent Liftoff Sequence...");

        // 1. Turn off proximity rings and guide lights
        if (proximityVisualRoot != null) proximityVisualRoot.SetActive(false);

        // 2. Hide Astronaut character (crew boarded inside ascent cabin)
        if (astronautTransform != null)
        {
            var astro = astronautTransform.GetComponent<AstronautController>();
            if (astro != null) astro.SetControlsLocked(true);
            astronautTransform.gameObject.SetActive(false);
        }

        // 3. Switch to Cinematic Ascent Camera
        if (ascentCinematicCamera != null)
        {
            ascentCinematicCamera.gameObject.SetActive(true);
        }

        // 4. Detach Ascent Cabin from Descent Stage
        if (ascentCabin != null)
        {
            ascentCabin.SetParent(null, true);
        }

        // 5. ENGINE IGNITION BLAST
        if (engineIgnitionLight != null)
        {
            engineIgnitionLight.enabled = true;
            engineIgnitionLight.intensity = 8.0f;
        }

        if (ascentFlameParticles != null)
        {
            ascentFlameParticles.Play();
        }

        if (radialDustBlastParticles != null)
        {
            radialDustBlastParticles.Play();
        }

        PlayAscentRoar();

        // 6. Smooth upward acceleration into lunar orbit (3.5 seconds)
        float duration = 3.6f;
        float elapsed = 0f;
        Vector3 startPos = ascentCabin != null ? ascentCabin.position : transform.position;
        Quaternion startRot = ascentCabin != null ? ascentCabin.rotation : transform.rotation;

        Vector3 targetPos = startPos + new Vector3(25f, 260f, 120f); // Climb vertically and tip towards orbital trajectory
        Quaternion targetRot = startRot * Quaternion.Euler(22f, 15f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth exponential acceleration curve
            float easeIn = t * t;

            if (ascentCabin != null)
            {
                ascentCabin.position = Vector3.Lerp(startPos, targetPos, easeIn);
                ascentCabin.rotation = Quaternion.Slerp(startRot, targetRot, easeIn);
            }

            if (engineIgnitionLight != null)
            {
                engineIgnitionLight.intensity = Mathf.Lerp(8.0f, 14.0f, easeIn) * (0.85f + (Random.value * 0.3f));
            }

            // Keep cinematic camera focused on ascending stage
            if (ascentCinematicCamera != null && ascentCabin != null)
            {
                ascentCinematicCamera.transform.LookAt(ascentCabin.position);
            }

            yield return null;
        }

        Debug.Log("[LanderFX] Lunar Ascent complete. Earth orbit trajectory achieved.");
        onComplete?.Invoke();
    }

    // =========================================================================
    // AUDIO
    // =========================================================================
    private void PlayProximityChime()
    {
        if (fxAudioSource != null)
        {
            fxAudioSource.pitch = 1.25f;
            fxAudioSource.PlayOneShot(GetOrCreateBeepClip());
        }
    }

    private void PlayAscentRoar()
    {
        if (fxAudioSource != null)
        {
            fxAudioSource.pitch = 1.0f;
            fxAudioSource.volume = 0.95f;
            fxAudioSource.loop = true;
            fxAudioSource.clip = GetOrCreateRoarClip();
            fxAudioSource.Play();
        }
    }

    private void SetupProceduralAudio()
    {
        if (fxAudioSource == null)
        {
            fxAudioSource = gameObject.AddComponent<AudioSource>();
            fxAudioSource.playOnAwake = false;
            fxAudioSource.spatialBlend = 0.5f;
        }
    }

    private AudioClip GetOrCreateBeepClip()
    {
        int sampleRate = 22050;
        int length = sampleRate / 3;
        float[] samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * (1f - (float)i / length) * 0.4f;
        }
        AudioClip clip = AudioClip.Create("Lander_Proximity_Chime", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GetOrCreateRoarClip()
    {
        int sampleRate = 22050;
        int length = sampleRate * 2;
        float[] samples = new float[length];
        float last = 0f;
        for (int i = 0; i < length; i++)
        {
            float white = (Random.value * 2f) - 1f;
            last = (last * 0.90f) + (white * 0.10f);
            samples[i] = last * 0.85f;
        }
        AudioClip clip = AudioClip.Create("Ascent_Engine_Roar", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
