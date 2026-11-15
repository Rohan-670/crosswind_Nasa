using UnityEngine;

/// <summary>
/// Procedural audio generator for spacecraft launch and descent sequences:
/// synthesizes countdown telemetry beeps and deep rocket engine combustion rumble
/// without requiring external audio assets. Also supports assigned AudioClips.
/// Fully thread-safe for Unity's audio DSP thread (no UnityEngine.Object calls in OnAudioFilterRead).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProceduralEngineAudio : MonoBehaviour
{
    [Header("--- Engine Sound Overrides (Optional) ---")]
    [Tooltip("Optional external engine audio clip. If null, procedural synthesis is used.")]
    public AudioClip engineRumbleClip;
    [Tooltip("Optional external countdown beep audio clip.")]
    public AudioClip countdownBeepClip;

    [Header("--- Engine Synthesis Parameters ---")]
    [Range(0f, 1f)]
    public float thrustLevel = 0f;
    [Range(0f, 1f)]
    public float masterVolume = 0.75f;
    public float baseRumbleFrequency = 48f;

    private AudioSource audioSource;
    private double sampleRate = 48000.0;
    private double phase1 = 0.0;
    private double phase2 = 0.0;
    private double beepPhase = 0.0;
    private float currentBeepTime = 0f;
    private float beepDuration = 0f;
    private float beepFrequency = 1000f;
    private float smoothedThrust = 0f;

    // Thread-safe cached state for OnAudioFilterRead (avoids EnsureRunningOnMainThread)
    private bool hasExternalClip = false;
    private uint rngState = 2463534242u;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D sound for immersion
        sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000.0;
        hasExternalClip = (engineRumbleClip != null);
    }

    private void Start()
    {
        hasExternalClip = (engineRumbleClip != null);

        if (hasExternalClip)
        {
            audioSource.clip = engineRumbleClip;
            audioSource.Play();
        }
        else
        {
            // Start audio source to activate OnAudioFilterRead
            audioSource.Play();
        }
    }

    private void Update()
    {
        smoothedThrust = Mathf.MoveTowards(smoothedThrust, thrustLevel, Time.deltaTime * 0.8f);

        if (currentBeepTime > 0f)
        {
            currentBeepTime -= Time.deltaTime;
        }

        if (hasExternalClip)
        {
            audioSource.volume = smoothedThrust * masterVolume;
            audioSource.pitch = 0.8f + (smoothedThrust * 0.5f);
        }
    }

    /// <summary>
    /// Set engine thrust level between 0.0 (silent) and 1.0 (full main engine burn).
    /// </summary>
    public void SetThrust(float targetThrust)
    {
        thrustLevel = Mathf.Clamp01(targetThrust);
    }

    /// <summary>
    /// Play a telemetry countdown beep tone.
    /// </summary>
    public void TriggerCountdownBeep(float freq = 1000f, float duration = 0.12f)
    {
        if (countdownBeepClip != null)
        {
            audioSource.PlayOneShot(countdownBeepClip, masterVolume);
            return;
        }

        beepFrequency = freq;
        beepDuration = duration;
        currentBeepTime = duration;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        // Thread-safe check: do NOT access UnityEngine.Object on audio thread
        if (hasExternalClip) return;

        double sampleIncrement1 = (baseRumbleFrequency * (1.0 + smoothedThrust * 0.6)) * 2.0 * System.Math.PI / sampleRate;
        double sampleIncrement2 = (baseRumbleFrequency * 2.05 * (1.0 + smoothedThrust * 0.4)) * 2.0 * System.Math.PI / sampleRate;
        double beepIncrement = beepFrequency * 2.0 * System.Math.PI / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            phase1 += sampleIncrement1;
            phase2 += sampleIncrement2;
            if (phase1 > 2.0 * System.Math.PI) phase1 -= 2.0 * System.Math.PI;
            if (phase2 > 2.0 * System.Math.PI) phase2 -= 2.0 * System.Math.PI;

            // Thread-safe fast pseudo-random noise (Xorshift32)
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;
            float noise = ((float)(rngState & 0x00FFFFFF) / 16777216f) * 2f - 1f;

            float osc1 = (float)System.Math.Sin(phase1);
            float osc2 = (float)System.Math.Sin(phase2) * 0.6f;

            float rumble = (osc1 + osc2 + noise * 0.85f) * smoothedThrust * 0.65f * masterVolume;

            // Beep tone
            float beepSample = 0f;
            if (currentBeepTime > 0f && beepDuration > 0.0001f)
            {
                beepPhase += beepIncrement;
                if (beepPhase > 2.0 * System.Math.PI) beepPhase -= 2.0 * System.Math.PI;
                float beepEnvelope = currentBeepTime / beepDuration;
                if (beepEnvelope > 1f) beepEnvelope = 1f;
                else if (beepEnvelope < 0f) beepEnvelope = 0f;
                beepSample = (float)System.Math.Sin(beepPhase) * 0.28f * beepEnvelope * masterVolume;
            }

            float finalSample = rumble + beepSample;
            if (finalSample > 1f) finalSample = 1f;
            else if (finalSample < -1f) finalSample = -1f;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = finalSample;
            }
        }
    }
}
