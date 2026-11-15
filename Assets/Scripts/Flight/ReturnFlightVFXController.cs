using UnityEngine;
using System.Collections;

/// <summary>
/// Controls all specialized visual effects for the Earth Return flight:
/// 1. Relativistic cosmic dust speedlines during Trans-Earth deep space cruise
/// 2. Multi-layered fiery atmospheric re-entry plasma envelope, sparks, and dynamic cockpit camera turbulence
/// 3. Procedural atmospheric re-entry wind/plasma rumble audio
/// 4. Touchdown tire smoke puffs upon runway landing
/// </summary>
public class ReturnFlightVFXController : MonoBehaviour
{
    [Header("--- Core Dependencies ---")]
    public SpacecraftController spacecraft;
    public EarthReentryAndLandingController reentryController;
    public Camera cockpitCamera;
    public Camera chaseCamera;

    [Header("--- Trans-Earth Speedlines VFX ---")]
    [Tooltip("Particle system streaming cosmic dust streaks past the spacecraft during deep space cruise")]
    public ParticleSystem cosmicSpeedlines;
    public float baseSpeedlineRate = 35f;
    public float maxSpeedlineRate = 180f;

    [Header("--- Re-entry Plasma & Fireball VFX ---")]
    public GameObject plasmaFXRoot;
    public ParticleSystem plasmaFireParticles;
    public ParticleSystem plasmaSparksParticles;
    public Light[] plasmaLights;
    public Renderer plasmaShieldRenderer;
    [Tooltip("Emissive color applied to the heat shield mesh as temperature increases")]
    public Color coldShieldColor = new Color(0.15f, 0.15f, 0.18f);
    public Color hotShieldColor = new Color(1.0f, 0.45f, 0.1f);

    [Header("--- Camera Turbulence Shake ---")]
    public bool enableCameraShake = true;
    public float maxReentryShakeIntensity = 0.22f;
    public float shakeFrequency = 24f;

    [Header("--- Touchdown Runway FX ---")]
    public ParticleSystem touchdownSmokeParticles;

    [Header("--- Procedural Audio FX ---")]
    public AudioSource reentryAudioSource;

    private Vector3 originalCockpitCamLocalPos = Vector3.zero;
    private Vector3 originalChaseCamLocalPos = Vector3.zero;
    private bool isReentryActive = false;
    private float shakeSeed = 0f;

    private void Awake()
    {
        if (spacecraft == null) spacecraft = GetComponent<SpacecraftController>();
        if (reentryController == null) reentryController = GetComponent<EarthReentryAndLandingController>();

        shakeSeed = Random.Range(0f, 100f);

        if (cockpitCamera != null) originalCockpitCamLocalPos = cockpitCamera.transform.localPosition;
        if (chaseCamera != null) originalChaseCamLocalPos = chaseCamera.transform.localPosition;

        SetupProceduralAudio();
    }

    private void Start()
    {
        // Start with cruise speedlines active
        SetSpeedlinesActive(true);
        SetReentryFXActive(false);
    }

    private void Update()
    {
        if (spacecraft == null || !spacecraft.IsAlive) return;

        UpdateCruiseSpeedlines();

        if (isReentryActive)
        {
            UpdateReentryFX();
            UpdateCameraShake();
        }
    }

    // =========================================================================
    // 1. TRANS-EARTH CRUISE SPEEDLINES
    // =========================================================================
    public void SetSpeedlinesActive(bool active)
    {
        if (cosmicSpeedlines == null) return;

        if (active)
        {
            if (!cosmicSpeedlines.isPlaying) cosmicSpeedlines.Play();
        }
        else
        {
            if (cosmicSpeedlines.isPlaying) cosmicSpeedlines.Stop();
        }
    }

    private void UpdateCruiseSpeedlines()
    {
        if (cosmicSpeedlines == null || !cosmicSpeedlines.isPlaying) return;

        // Scale emission rate with spacecraft forward speed
        float speedRatio = Mathf.Clamp01(spacecraft.CurrentSpeed / Mathf.Max(1f, spacecraft.maxForwardSpeed));
        var emission = cosmicSpeedlines.emission;
        emission.rateOverTime = Mathf.Lerp(baseSpeedlineRate, maxSpeedlineRate, speedRatio * (spacecraft.IsBoosting ? 1.5f : 1.0f));

        var main = cosmicSpeedlines.main;
        main.startSpeed = Mathf.Lerp(25f, 90f, speedRatio);
    }

    // =========================================================================
    // 2. ATMOSPHERIC RE-ENTRY PLASMA FIREBALL
    // =========================================================================
    public void SetReentryFXActive(bool active)
    {
        isReentryActive = active;

        if (plasmaFXRoot != null) plasmaFXRoot.SetActive(active);

        if (active)
        {
            // Dim/disable space speedlines to emphasize dense atmospheric plasma
            SetSpeedlinesActive(false);

            if (plasmaFireParticles != null && !plasmaFireParticles.isPlaying) plasmaFireParticles.Play();
            if (plasmaSparksParticles != null && !plasmaSparksParticles.isPlaying) plasmaSparksParticles.Play();
            if (reentryAudioSource != null && !reentryAudioSource.isPlaying) reentryAudioSource.Play();
        }
        else
        {
            if (plasmaFireParticles != null && plasmaFireParticles.isPlaying) plasmaFireParticles.Stop();
            if (plasmaSparksParticles != null && plasmaSparksParticles.isPlaying) plasmaSparksParticles.Stop();
            if (reentryAudioSource != null && reentryAudioSource.isPlaying) reentryAudioSource.Stop();

            ResetCameraPositions();
        }

        if (plasmaLights != null)
        {
            foreach (var l in plasmaLights)
            {
                if (l != null) l.enabled = active;
            }
        }
    }

    private void UpdateReentryFX()
    {
        float temp = reentryController != null ? reentryController.HeatShieldTemperature : 1400f;
        float heatRatio = Mathf.Clamp01((temp - 600f) / 1600f); // 0 at 600C, 1 at 2200C

        // 1. Modulate Re-entry Plasma Lights with high-speed flicker
        if (plasmaLights != null)
        {
            float flicker = Mathf.PerlinNoise(Time.time * 28f, shakeSeed);
            float baseIntensity = Mathf.Lerp(2.5f, 7.0f, heatRatio);

            foreach (var l in plasmaLights)
            {
                if (l != null)
                {
                    l.intensity = baseIntensity * (0.8f + flicker * 0.4f);
                    l.color = Color.Lerp(new Color(1.0f, 0.85f, 0.4f), new Color(1.0f, 0.35f, 0.1f), heatRatio);
                }
            }
        }

        // 2. Heat Shield Mesh Color & Emission
        if (plasmaShieldRenderer != null && plasmaShieldRenderer.material != null)
        {
            Color currentHeatCol = Color.Lerp(coldShieldColor, hotShieldColor, heatRatio);
            plasmaShieldRenderer.material.color = currentHeatCol;
            if (plasmaShieldRenderer.material.HasProperty("_BaseColor"))
                plasmaShieldRenderer.material.SetColor("_BaseColor", currentHeatCol);
        }

        // 3. Audio Volume and Pitch Modulation
        if (reentryAudioSource != null)
        {
            reentryAudioSource.volume = Mathf.Lerp(0.35f, 0.95f, heatRatio);
            reentryAudioSource.pitch = Mathf.Lerp(0.85f, 1.25f, heatRatio);
        }

        // 4. Particle Emission Modulation
        if (plasmaFireParticles != null)
        {
            var emission = plasmaFireParticles.emission;
            emission.rateOverTime = Mathf.Lerp(50f, 160f, heatRatio);
        }
    }

    private void UpdateCameraShake()
    {
        if (!enableCameraShake) return;

        float temp = reentryController != null ? reentryController.HeatShieldTemperature : 1200f;
        float intensityFactor = Mathf.Clamp01((temp - 800f) / 1400f);

        // Extra shake if out of pitch corridor (turbulent buffeting)
        float pitch = spacecraft.transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        float pitchAngle = -pitch;
        bool outOfCorridor = (pitchAngle < 4.0f || pitchAngle > 18.0f);
        if (outOfCorridor) intensityFactor = Mathf.Min(1.0f, intensityFactor * 1.6f);

        float shake = maxReentryShakeIntensity * intensityFactor;
        float time = Time.time * shakeFrequency;

        Vector3 offset = new Vector3(
            (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * shake,
            (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * shake,
            (Mathf.PerlinNoise(time, time) - 0.5f) * shake * 0.5f
        );

        if (cockpitCamera != null && cockpitCamera.gameObject.activeInHierarchy)
        {
            cockpitCamera.transform.localPosition = originalCockpitCamLocalPos + offset;
        }

        if (chaseCamera != null && chaseCamera.gameObject.activeInHierarchy)
        {
            chaseCamera.transform.localPosition = originalChaseCamLocalPos + offset * 1.3f;
        }
    }

    private void ResetCameraPositions()
    {
        if (cockpitCamera != null) cockpitCamera.transform.localPosition = originalCockpitCamLocalPos;
        if (chaseCamera != null) chaseCamera.transform.localPosition = originalChaseCamLocalPos;
    }

    // =========================================================================
    // 3. RUNWAY TOUCHDOWN TIRE SMOKE
    // =========================================================================
    public void TriggerTouchdownSmoke(Vector3 contactPosition)
    {
        if (touchdownSmokeParticles != null)
        {
            touchdownSmokeParticles.transform.position = contactPosition;
            touchdownSmokeParticles.Play();
        }
    }

    // =========================================================================
    // 4. PROCEDURAL ATMOSPHERIC AUDIO GENERATOR
    // =========================================================================
    private void SetupProceduralAudio()
    {
        if (reentryAudioSource == null)
        {
            reentryAudioSource = gameObject.AddComponent<AudioSource>();
            reentryAudioSource.loop = true;
            reentryAudioSource.playOnAwake = false;
            reentryAudioSource.spatialBlend = 0.0f; // 2D cockpit audio
        }

        // If audio source lacks an audio clip, generate a 2-second looped atmospheric rumble buffer
        if (reentryAudioSource.clip == null)
        {
            int sampleRate = 22050;
            int length = sampleRate * 2;
            float[] samples = new float[length];

            // Filtered Brownian / pink noise for deep atmospheric aerodynamic roar
            float lastVal = 0f;
            for (int i = 0; i < length; i++)
            {
                float white = (Random.value * 2f) - 1f;
                lastVal = (lastVal * 0.94f) + (white * 0.06f); // Low-pass
                samples[i] = lastVal * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Reentry_Rumble", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            reentryAudioSource.clip = clip;
        }
    }
}
