using UnityEngine;

/// <summary>
/// Fades in a UI element over time using a CanvasGroup's alpha.
/// Attach this to any UI panel or group that should appear gradually.
/// Requires a CanvasGroup component (one will be added automatically if missing).
/// </summary>
public class TitleFadeIn : MonoBehaviour
{
    [Tooltip("Seconds to wait before the fade begins")]
    public float delay = 1f;

    [Tooltip("How many seconds the fade-in animation takes")]
    public float fadeDuration = 2.5f;

    // CanvasGroup controls the transparency of this element and all its children
    private CanvasGroup canvasGroup;
    private float timer = 0f;

    void Awake()
    {
        // Get or add a CanvasGroup so we can control alpha (transparency)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Start fully invisible
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // After the delay, smoothly increase alpha from 0 to 1
        if (timer > delay)
        {
            float elapsed = timer - delay;
            // Clamp01 ensures the value stays between 0 and 1
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
        }
    }
}
