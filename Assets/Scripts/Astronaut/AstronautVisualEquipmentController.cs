using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls the interactive 360-degree turntable view of the astronaut in MissionPreparation,
/// and dynamically displays modular physical equipment attachments on the spacesuit
/// when items are equipped or unequipped in the loadout panel.
/// </summary>
public class AstronautVisualEquipmentController : MonoBehaviour
{
    public static AstronautVisualEquipmentController Instance { get; private set; }

    [Header("--- 360 Turntable View Configuration ---")]
    [Tooltip("Transform of the astronaut to rotate (defaults to this transform)")]
    public Transform astronautRoot;
    public float mouseDragSensitivity = 0.45f;
    public float keyboardTurnSpeed = 90f;
    public float autoSpinSpeed = 22f;
    public float defaultYRotation = 160f;
    public float smoothDamping = 10f;

    [Header("--- Interactive State ---")]
    public bool isAutoSpinActive = false;
    public bool isDragging = false;

    [Header("--- Visual Equipment Attachment Props ---")]
    public GameObject sampleCanistersProp;
    public GameObject multispectralCameraProp;
    public GameObject mineralScannerProp;
    public GameObject medicalKitProp;

    // Runtime rotation tracking
    private float currentRotY;
    private float targetRotY;
    private float dragPreviousMouseX;

    private void Awake()
    {
        Instance = this;
        if (astronautRoot == null)
            astronautRoot = transform;

        currentRotY = astronautRoot.eulerAngles.y;
        targetRotY = currentRotY;
    }

    private void Start()
    {
        // Ensure visual attachment props exist
        BuildOrFindAttachmentProps();

        // Bind to EquipmentManager events
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnLoadoutChanged += RefreshVisualEquipment;
        }

        RefreshVisualEquipment();
    }

    private void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnLoadoutChanged -= RefreshVisualEquipment;
        }
    }

    private void Update()
    {
        HandleTurntableInput();
        ApplyTurntableRotation();
    }

    // ===================================================================
    // 1. 360-DEGREE VIEW TURNTABLE CONTROLS
    // ===================================================================

    private void HandleTurntableInput()
    {
        // 1. Mouse Drag Rotation (Left-click & drag on left 65% of screen)
        Vector2 mousePos = AstroInput.mousePosition;
        bool inViewport = mousePos.x <= (Screen.width * 0.65f);

        if (AstroInput.GetMouseButtonDown(0) && inViewport)
        {
            isDragging = true;
            dragPreviousMouseX = mousePos.x;
            isAutoSpinActive = false; // Player interaction pauses auto-spin
        }
        else if (AstroInput.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && AstroInput.GetMouseButton(0))
        {
            float deltaX = mousePos.x - dragPreviousMouseX;
            dragPreviousMouseX = mousePos.x;
            targetRotY -= deltaX * mouseDragSensitivity;
        }

        // 2. Keyboard Rotation [A / D] or [Left / Right Arrow]
        float kbInput = 0f;
        if (AstroInput.GetKey(KeyCode.A) || AstroInput.GetKey(KeyCode.LeftArrow)) kbInput -= 1f;
        if (AstroInput.GetKey(KeyCode.D) || AstroInput.GetKey(KeyCode.RightArrow)) kbInput += 1f;

        if (Mathf.Abs(kbInput) > 0.01f)
        {
            targetRotY += kbInput * keyboardTurnSpeed * Time.deltaTime;
            isAutoSpinActive = false;
        }

        // 3. Auto-Spin Mode
        if (isAutoSpinActive && !isDragging)
        {
            targetRotY += autoSpinSpeed * Time.deltaTime;
        }

        // 4. Shortcut: [Space] to toggle auto-spin, [R] to reset
        if (AstroInput.GetKeyDown(KeyCode.Space))
        {
            ToggleAutoSpin();
        }
        if (AstroInput.GetKeyDown(KeyCode.R))
        {
            ResetView();
        }
    }

    private void ApplyTurntableRotation()
    {
        if (astronautRoot == null) return;

        // Smoothly interpolate current rotation to target rotation
        currentRotY = Mathf.Lerp(currentRotY, targetRotY, Time.deltaTime * smoothDamping);
        astronautRoot.rotation = Quaternion.Euler(0f, currentRotY, 0f);
    }

    public void RotateLeft(float degrees = 30f)
    {
        isAutoSpinActive = false;
        targetRotY -= degrees;
    }

    public void RotateRight(float degrees = 30f)
    {
        isAutoSpinActive = false;
        targetRotY += degrees;
    }

    public void ToggleAutoSpin()
    {
        isAutoSpinActive = !isAutoSpinActive;
    }

    public void ResetView()
    {
        isAutoSpinActive = false;
        targetRotY = defaultYRotation;
    }

    // ===================================================================
    // 2. VISUAL EQUIPMENT ATTACHMENT SUIT PROPS
    // ===================================================================

    public void RefreshVisualEquipment()
    {
        if (EquipmentManager.Instance == null) return;

        // 1. Cryogenic Sample Canisters (Right Waist/Belt)
        bool hasCanisters = EquipmentManager.Instance.IsEquipped("sample_containers");
        if (sampleCanistersProp != null) sampleCanistersProp.SetActive(hasCanisters);

        // 2. High-Res Multispectral Camera (Left Shoulder/Chest)
        bool hasCamera = EquipmentManager.Instance.IsEquipped("camera");
        if (multispectralCameraProp != null) multispectralCameraProp.SetActive(hasCamera);

        // 3. XRF Mineralogical Scanner (Right Thigh Holster)
        bool hasScanner = EquipmentManager.Instance.IsEquipped("scanner");
        if (mineralScannerProp != null) mineralScannerProp.SetActive(hasScanner);

        // 4. Field Repair & Medical Kit (Left Hip/Belt)
        bool hasMedKit = EquipmentManager.Instance.IsEquipped("emergency_kit");
        if (medicalKitProp != null) medicalKitProp.SetActive(hasMedKit);
    }

    public void BuildOrFindAttachmentProps()
    {
        if (astronautRoot == null) return;

        Transform existingCanisters = astronautRoot.Find("Attach_SampleCanisters");
        if (existingCanisters != null) sampleCanistersProp = existingCanisters.gameObject;
        else sampleCanistersProp = CreateCanistersProp(astronautRoot);

        Transform existingCam = astronautRoot.Find("Attach_MultispectralCamera");
        if (existingCam != null) multispectralCameraProp = existingCam.gameObject;
        else multispectralCameraProp = CreateCameraProp(astronautRoot);

        Transform existingScan = astronautRoot.Find("Attach_MineralScanner");
        if (existingScan != null) mineralScannerProp = existingScan.gameObject;
        else mineralScannerProp = CreateScannerProp(astronautRoot);

        Transform existingMed = astronautRoot.Find("Attach_MedicalKit");
        if (existingMed != null) medicalKitProp = existingMed.gameObject;
        else medicalKitProp = CreateMedicalKitProp(astronautRoot);
    }

    // -------------------------------------------------------------------
    // Procedural Prop Builders (High quality NASA aesthetic)
    // -------------------------------------------------------------------

    private GameObject CreateCanistersProp(Transform parent)
    {
        // Right Waist/Belt Mount
        GameObject root = new GameObject("Attach_SampleCanisters");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0.34f, 0.95f, 0.04f);
        root.transform.localRotation = Quaternion.Euler(0f, 15f, 5f);

        // Mounting bracket
        GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bracket.name = "Bracket";
        bracket.transform.SetParent(root.transform, false);
        bracket.transform.localPosition = Vector3.zero;
        bracket.transform.localScale = new Vector3(0.14f, 0.08f, 0.16f);
        DestroyCollider(bracket);
        ApplyMaterial(bracket, new Color(0.18f, 0.20f, 0.24f), 0.7f, 0.5f);

        // Dual Cryo Cylinders (Vacuum-insulated flasks)
        for (int i = 0; i < 2; i++)
        {
            float zOffset = (i == 0) ? -0.05f : 0.05f;
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = $"CryoCanister_{i}";
            cyl.transform.SetParent(root.transform, false);
            cyl.transform.localPosition = new Vector3(0.06f, -0.02f, zOffset);
            cyl.transform.localScale = new Vector3(0.075f, 0.14f, 0.075f);
            DestroyCollider(cyl);
            ApplyMaterial(cyl, new Color(0.85f, 0.88f, 0.92f), 0.85f, 0.8f);

            // Glowing cyan cryo status band
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "CryoGlowRing";
            ring.transform.SetParent(cyl.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            ring.transform.localScale = new Vector3(1.05f, 0.12f, 1.05f);
            DestroyCollider(ring);
            ApplyUnlitMaterial(ring, new Color(0.15f, 0.85f, 1.0f));
        }

        root.SetActive(false);
        return root;
    }

    private GameObject CreateCameraProp(Transform parent)
    {
        // Left Shoulder / Collar Harness Mount
        GameObject root = new GameObject("Attach_MultispectralCamera");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(-0.24f, 1.48f, 0.20f);
        root.transform.localRotation = Quaternion.Euler(8f, -12f, 0f);

        // Camera main body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "CameraBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.11f, 0.09f, 0.12f);
        DestroyCollider(body);
        ApplyMaterial(body, new Color(0.12f, 0.13f, 0.16f), 0.6f, 0.4f);

        // Lens Barrel (Facing forward)
        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lens.name = "LensBarrel";
        lens.transform.SetParent(root.transform, false);
        lens.transform.localPosition = new Vector3(0f, 0f, 0.08f);
        lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        lens.transform.localScale = new Vector3(0.065f, 0.05f, 0.065f);
        DestroyCollider(lens);
        ApplyMaterial(lens, new Color(0.85f, 0.75f, 0.25f), 0.9f, 0.85f); // Gold optical coating

        // Glowing Green Recording LED
        GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        led.name = "RecordLED";
        led.transform.SetParent(root.transform, false);
        led.transform.localPosition = new Vector3(0.04f, 0.035f, 0.065f);
        led.transform.localScale = Vector3.one * 0.02f;
        DestroyCollider(led);
        ApplyUnlitMaterial(led, new Color(0.2f, 1.0f, 0.4f));

        root.SetActive(false);
        return root;
    }

    private GameObject CreateScannerProp(Transform parent)
    {
        // Right Thigh Tactical Holster Mount
        GameObject root = new GameObject("Attach_MineralScanner");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0.32f, 0.70f, 0.04f);
        root.transform.localRotation = Quaternion.Euler(0f, 10f, -8f);

        // Holster base
        GameObject holster = GameObject.CreatePrimitive(PrimitiveType.Cube);
        holster.name = "Holster";
        holster.transform.SetParent(root.transform, false);
        holster.transform.localPosition = Vector3.zero;
        holster.transform.localScale = new Vector3(0.10f, 0.20f, 0.12f);
        DestroyCollider(holster);
        ApplyMaterial(holster, new Color(0.15f, 0.16f, 0.19f), 0.4f, 0.3f);

        // XRF Spectrometer Gun handle & emitter snout
        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.name = "ScannerBody";
        grip.transform.SetParent(root.transform, false);
        grip.transform.localPosition = new Vector3(0.02f, 0.08f, 0.01f);
        grip.transform.localScale = new Vector3(0.065f, 0.12f, 0.075f);
        DestroyCollider(grip);
        ApplyMaterial(grip, new Color(0.80f, 0.82f, 0.85f), 0.7f, 0.5f);

        // Glowing Amber/Yellow XRF Sensor Head
        GameObject sensor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sensor.name = "XRF_Emitter";
        sensor.transform.SetParent(grip.transform, false);
        sensor.transform.localPosition = new Vector3(0f, 0.55f, 0.2f);
        sensor.transform.localScale = Vector3.one * 0.4f;
        DestroyCollider(sensor);
        ApplyUnlitMaterial(sensor, new Color(1.0f, 0.75f, 0.2f));

        root.SetActive(false);
        return root;
    }

    private GameObject CreateMedicalKitProp(Transform parent)
    {
        // Left Hip / Belt Mount
        GameObject root = new GameObject("Attach_MedicalKit");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(-0.34f, 0.95f, 0.04f);
        root.transform.localRotation = Quaternion.Euler(0f, -15f, -5f);

        // Medical Utility Pack
        GameObject pouch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pouch.name = "MedicalPouch";
        pouch.transform.SetParent(root.transform, false);
        pouch.transform.localPosition = Vector3.zero;
        pouch.transform.localScale = new Vector3(0.12f, 0.14f, 0.16f);
        DestroyCollider(pouch);
        ApplyMaterial(pouch, new Color(0.85f, 0.18f, 0.18f), 0.2f, 0.4f); // Safety red

        // White Cross Emblem / Trim
        GameObject stripeH = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripeH.name = "CrossH";
        stripeH.transform.SetParent(pouch.transform, false);
        stripeH.transform.localPosition = new Vector3(-0.52f, 0f, 0f);
        stripeH.transform.localScale = new Vector3(0.05f, 0.22f, 0.65f);
        DestroyCollider(stripeH);
        ApplyMaterial(stripeH, Color.white, 0.1f, 0.5f);

        GameObject stripeV = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripeV.name = "CrossV";
        stripeV.transform.SetParent(pouch.transform, false);
        stripeV.transform.localPosition = new Vector3(-0.52f, 0f, 0f);
        stripeV.transform.localScale = new Vector3(0.05f, 0.65f, 0.22f);
        DestroyCollider(stripeV);
        ApplyMaterial(stripeV, Color.white, 0.1f, 0.5f);

        // Emergency Medical LED
        GameObject medLed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        medLed.name = "StatusBeacon";
        medLed.transform.SetParent(pouch.transform, false);
        medLed.transform.localPosition = new Vector3(-0.52f, 0.38f, 0.38f);
        medLed.transform.localScale = Vector3.one * 0.18f;
        DestroyCollider(medLed);
        ApplyUnlitMaterial(medLed, new Color(1.0f, 0.3f, 0.3f));

        root.SetActive(false);
        return root;
    }

    private static void DestroyCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);
    }

    private static void ApplyMaterial(GameObject go, Color col, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = col;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void ApplyUnlitMaterial(GameObject go, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = emission;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", emission);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
