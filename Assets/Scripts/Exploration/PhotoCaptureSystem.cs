using UnityEngine;
using System;

/// <summary>
/// Reusable scientific photography system:
/// toggles photo mode with [C], renders camera viewfinder overlay,
/// detects landmark subjects in frame, and captures photos with a shutter flash.
/// </summary>
public class PhotoCaptureSystem : MonoBehaviour
{
    [Header("--- Camera Reference ---")]
    public Camera photoCamera;

    [Header("--- Target Landmarks ---")]
    public Transform earthInSkyTarget;
    public Transform lunarLanderTarget;
    public Transform moonBaseHabitatTarget;

    // Runtime state
    public bool IsInPhotoMode { get; private set; } = false;
    public bool HasCapturedEarthPhoto { get; private set; } = false;
    public bool HasCapturedLanderPhoto { get; private set; } = false;
    public bool HasCapturedBasePhoto { get; private set; } = false;
    public string CurrentTargetInFrame { get; private set; } = "";

    public event Action<bool> OnPhotoModeToggled; // inPhotoMode
    public event Action<string> OnPhotoCaptured;  // targetName

    private void Start()
    {
        if (photoCamera == null)
            photoCamera = Camera.main;
    }

    private void Update()
    {
        // Toggle photo mode with C
        if (AstroInput.GetKeyDown(KeyCode.C))
        {
            TogglePhotoMode(!IsInPhotoMode);
        }

        if (IsInPhotoMode)
        {
            CheckTargetInFrame();

            // Capture photo with Space or Left Click
            if (AstroInput.GetMouseButtonDown(0) || AstroInput.GetKeyDown(KeyCode.Space))
            {
                CapturePhoto();
            }
        }
    }

    public void TogglePhotoMode(bool enable)
    {
        IsInPhotoMode = enable;
        OnPhotoModeToggled?.Invoke(enable);
    }

    private void CheckTargetInFrame()
    {
        CurrentTargetInFrame = "";

        if (photoCamera == null) return;

        // Check Earth target
        if (earthInSkyTarget != null && IsObjectInViewport(earthInSkyTarget.position))
        {
            CurrentTargetInFrame = "EARTH (LUNAR EARTHRISE)";
            return;
        }

        // Check Lander target
        if (lunarLanderTarget != null && IsObjectInViewport(lunarLanderTarget.position))
        {
            CurrentTargetInFrame = "LUNAR EXPEDITION LANDER";
            return;
        }

        // Check Moon Base Habitat target
        if (moonBaseHabitatTarget != null && IsObjectInViewport(moonBaseHabitatTarget.position))
        {
            CurrentTargetInFrame = "MOON BASE HABITAT COMPLEX";
            return;
        }
    }

    private bool IsObjectInViewport(Vector3 targetPos)
    {
        Vector3 vpPos = photoCamera.WorldToViewportPoint(targetPos);
        // Is in front of camera and within centered frame (0.2 to 0.8)
        return vpPos.z > 0 && vpPos.x >= 0.15f && vpPos.x <= 0.85f && vpPos.y >= 0.15f && vpPos.y <= 0.85f;
    }

    private void CapturePhoto()
    {
        string subject = string.IsNullOrEmpty(CurrentTargetInFrame) ? "LUNAR REGOLITH LANDSCAPE" : CurrentTargetInFrame;

        if (subject.Contains("EARTH"))
        {
            HasCapturedEarthPhoto = true;
        }
        else if (subject.Contains("LANDER"))
        {
            HasCapturedLanderPhoto = true;
        }
        else if (subject.Contains("HABITAT") || subject.Contains("BASE"))
        {
            HasCapturedBasePhoto = true;
        }

        Debug.Log($"[PhotoCaptureSystem] Scientific photo captured: {subject}");
        OnPhotoCaptured?.Invoke(subject);
    }
}
