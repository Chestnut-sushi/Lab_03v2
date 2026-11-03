using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// First-person keyboard + mouse control for testing the scene without a headset.
// WASD moves, the mouse looks around, left click grabs / releases a Grabbable or toggles the temple doors.
// Esc frees the cursor, a click locks it again. Does nothing when a real headset is active.
[RequireComponent(typeof(CharacterController))]
public class DesktopFirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 2f;
    [SerializeField] float runSpeed = 4f;
    [SerializeField] float gravity = -9.81f;

    [Header("Look")]
    [SerializeField] Camera playerCamera;
    [SerializeField] float lookSensitivity = 0.1f;
    [SerializeField] float maxPitch = 85f;

    [Header("Interaction")]
    [SerializeField] float reachDistance = 3f;
    [Tooltip("The prompt (and the action) for each target only appears within this distance.")]
    [SerializeField] float doorPromptDistance = 2.5f;
    [SerializeField] float grabPromptDistance = 2f;
    [SerializeField] float burnerPromptDistance = 2.5f;
    [SerializeField] float readerPromptDistance = 2.5f;
    [SerializeField] float holdDistance = 0.9f;
    [SerializeField] float holdHeightOffset = -0.25f;
    [SerializeField] float holdSmoothing = 15f;
    [SerializeField] bool onlyWithoutHeadset = true;

    CharacterController controller;
    float pitch;
    float verticalVelocity;
    Grabbable held;
    Grabbable hoverGrabbable;
    TempleDoors hoverDoors;
    IncenseBurner hoverBurner;
    ScrollReader hoverReader;
    ScrollReader openReader;          // set while the reading sheet is on screen
    readonly List<RaycastHit> hits = new List<RaycastHit>();

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        if (onlyWithoutHeadset && XRSettings.isDeviceActive)
        {
            enabled = false;
            return;
        }

        // Without a headset nothing feeds the tracked pose, so the camera is driven from here instead.
        foreach (var b in playerCamera.GetComponents<Behaviour>())
            if (b.GetType().Name == "TrackedPoseDriver") b.enabled = false;
        playerCamera.transform.localPosition = Vector3.zero;
    }

    void Start() => LockCursor(true);

    void OnDisable()
    {
        if (held != null) Release();
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        // While the reading sheet is open the player is frozen; a click, Esc or E closes it.
        if (openReader != null)
        {
            if (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
                openReader = null;
            return;
        }

        if (kb.escapeKey.wasPressedThisFrame) LockCursor(false);

        bool locked = Cursor.lockState == CursorLockMode.Locked;
        if (!locked)
        {
            if (mouse.leftButton.wasPressedThisFrame) LockCursor(true);
            return;
        }

        Look(mouse);
        Move(kb);
        UpdateHover();
        if (mouse.leftButton.wasPressedThisFrame) Interact();
    }

    void LateUpdate()
    {
        // Applied after anything else that might write the camera pose.
        if (playerCamera != null) playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        if (held != null) MoveHeld();
    }

    void Look(Mouse mouse)
    {
        Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
        transform.Rotate(0f, delta.x, 0f, Space.World);
        pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
    }

    void Move(Keyboard kb)
    {
        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 dir = transform.right * x + transform.forward * z;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        verticalVelocity = controller.isGrounded ? -2f : verticalVelocity + gravity * Time.deltaTime;
        controller.Move((dir * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    // Closest hit along the view ray that does not belong to the player.
    bool Probe(float distance, out RaycastHit result, bool ignoreBurner = false)
    {
        hits.Clear();
        hits.AddRange(Physics.RaycastAll(playerCamera.transform.position, playerCamera.transform.forward, distance, ~0, QueryTriggerInteraction.Ignore));
        hits.Sort((a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (held != null && h.collider.transform.IsChildOf(held.transform)) continue;
            // a held piece may be brought over the incense burner, so it must not be stopped by it
            if (ignoreBurner && h.collider.transform.root.GetComponentInChildren<IncenseBurner>(true) != null) continue;
            result = h;
            return true;
        }
        result = default;
        return false;
    }

    void UpdateHover()
    {
        hoverGrabbable = null;
        hoverDoors = null;
        hoverBurner = null;
        hoverReader = null;
        if (!Probe(reachDistance, out var hit)) return;
        if (held != null)
        {
            if (hit.distance <= burnerPromptDistance)
                hoverBurner = hit.collider.transform.root.GetComponentInChildren<IncenseBurner>(true);
            return;
        }

        // Only the thing the cursor is exactly on, and only when close enough: the door leaves, a loose piece of wood.
        var piece = hit.collider.GetComponentInParent<Grabbable>();
        if (piece != null)
        {
            if (hit.distance <= grabPromptDistance) hoverGrabbable = piece;
            return;
        }
        var reader = hit.collider.GetComponentInParent<ScrollReader>();
        if (reader != null)
        {
            if (hit.distance <= readerPromptDistance) hoverReader = reader;
            return;
        }
        var doors = hit.collider.GetComponentInParent<TempleDoors>();
        if (doors != null && doors.IsLeaf(hit.collider.transform) && hit.distance <= doorPromptDistance)
            hoverDoors = doors;
    }

    void Interact()
    {
        if (held != null)
        {
            // looking at the incense burner: put the piece in it instead of dropping it
            if (hoverBurner != null)
            {
                var piece = held;
                held = null;
                hoverBurner.Accept(piece);
            }
            else Release();
            return;
        }
        if (hoverGrabbable != null) Grab(hoverGrabbable);
        else if (hoverReader != null && hoverReader.ModalImage != null) openReader = hoverReader;
        else if (hoverDoors != null) hoverDoors.Toggle();
    }

    void Grab(Grabbable g)
    {
        held = g;
        held.OnGrab(controller);
    }

    void Release()
    {
        held.OnRelease(controller);
        held = null;
    }

    void MoveHeld()
    {
        var cam = playerCamera.transform;
        float distance = holdDistance + held.GetBounds().extents.magnitude * 0.5f;
        Vector3 target = cam.position + cam.forward * distance + Vector3.up * holdHeightOffset;

        // Keep the object in front of walls instead of pushing it through them.
        if (Probe(distance + 0.3f, out var wall, ignoreBurner: true) && wall.distance < distance)
            target = cam.position + cam.forward * Mathf.Max(0.4f, wall.distance - 0.3f) + Vector3.up * holdHeightOffset;

        float t = 1f - Mathf.Exp(-holdSmoothing * Time.deltaTime);
        held.transform.position = Vector3.Lerp(held.transform.position, target, t);
        held.transform.rotation = Quaternion.Slerp(held.transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), t);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // Prompt look: white, readable text on a rounded, semi-transparent black box (like video subtitles).
    GUIStyle promptStyle;
    Font promptFont;
    Texture2D promptBox;
    int promptFontSize;

    void EnsurePromptStyle()
    {
        int size = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 28f), 18, 56);
        if (promptStyle != null && size == promptFontSize) return;
        promptFontSize = size;
        if (promptFont == null) promptFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Roboto", "Helvetica Neue", "Arial" }, size);
        if (promptBox == null) promptBox = MakeRoundedBox(32, 12, new Color(0f, 0f, 0f, 0.78f));
        promptStyle = new GUIStyle(GUI.skin.label)
        {
            font = promptFont,
            fontSize = size,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            clipping = TextClipping.Overflow,
            border = new RectOffset(12, 12, 12, 12),
            padding = new RectOffset(Mathf.RoundToInt(size * 0.8f), Mathf.RoundToInt(size * 0.8f), Mathf.RoundToInt(size * 0.35f), Mathf.RoundToInt(size * 0.4f)),
        };
        promptStyle.normal.background = promptBox;
        promptStyle.normal.textColor = Color.white;
    }

    static Texture2D MakeRoundedBox(int size, float radius, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - 0.5f - x, x - (size - 1 - radius + 0.5f), 0f);
                float dy = Mathf.Max(radius - 0.5f - y, y - (size - 1 - radius + 0.5f), 0f);
                float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    void DrawPrompt(string text, float centerX, float centerY)
    {
        EnsurePromptStyle();
        var size = promptStyle.CalcSize(new GUIContent(text));
        GUI.Label(new Rect(centerX - size.x / 2f, centerY - size.y / 2f, size.x, size.y), text, promptStyle);
    }

    // Full-screen reading sheet: dimmed background, the image fitted on screen, a close badge and a close hint.
    void DrawReadingSheet()
    {
        var tex = openReader.ModalImage;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float aspect = tex.width / (float)tex.height;
        float w = Screen.width * 0.94f, h = w / aspect, maxH = Screen.height * 0.84f;
        if (h > maxH) { h = maxH; w = h * aspect; }
        var r = new Rect((Screen.width - w) / 2f, Screen.height * 0.07f + (Screen.height * 0.84f - h) / 2f, w, h);
        GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, false);

        EnsurePromptStyle();
        float s = promptFontSize * 1.9f;
        var badge = new Rect(r.xMax - s * 0.5f, r.yMin - s * 0.5f, s, s);           // round "x" badge on the panel corner
        GUI.DrawTexture(badge, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Color.black, 0f, s * 0.5f);
        GUI.Label(badge, "×", new GUIStyle(promptStyle) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(promptFontSize * 1.5f), padding = new RectOffset(0, 0, 0, 4), normal = { background = null, textColor = Color.white } });
        DrawPrompt("Press Trigger / B: close", Screen.width / 2f, Screen.height * 0.955f);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) return;

        if (openReader != null)
        {
            DrawReadingSheet();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            DrawPrompt("Press Trigger to resume control", Screen.width / 2f, Screen.height / 2f);
            return;
        }

        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        GUI.color = Color.black; GUI.DrawTexture(new Rect(cx - 3f, cy - 3f, 6f, 6f), Texture2D.whiteTexture);   // outline
        GUI.color = Color.white; GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), Texture2D.whiteTexture);

        string hint = CurrentPrompt;
        if (hint != null) DrawPrompt(hint, cx, Screen.height * 0.88f);       // low and centred, where subtitles sit
    }

    // What the prompt says right now (null = nothing to show).
    public string CurrentPrompt
    {
        get
        {
            if (held != null) return hoverBurner != null ? "Press Trigger: put the wood in the incense burner" : "Press Trigger: drop the wood";
            if (hoverGrabbable != null) return "Press Trigger: pick up the wood";
            if (hoverReader != null) return "Press Trigger: read the scroll";
            if (hoverDoors != null) return hoverDoors.IsOpen ? "Press Trigger: close the door" : "Press Trigger: open the door";
            return null;
        }
    }
}
