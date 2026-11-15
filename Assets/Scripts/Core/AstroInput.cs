using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Universal Input Abstraction for ASTROBASE:
/// natively supports Unity's New Input System (Keyboard.current, Mouse.current)
/// while safely falling back to legacy Input Manager without throwing
/// InvalidOperationException regardless of PlayerSettings.activeInputHandler mode.
/// </summary>
public static class AstroInput
{
    /// <summary>
    /// Checks whether a key is currently held down.
    /// </summary>
    public static bool GetKey(KeyCode key)
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            switch (key)
            {
                case KeyCode.W: return keyboard.wKey.isPressed;
                case KeyCode.S: return keyboard.sKey.isPressed;
                case KeyCode.A: return keyboard.aKey.isPressed;
                case KeyCode.D: return keyboard.dKey.isPressed;
                case KeyCode.UpArrow: return keyboard.upArrowKey.isPressed;
                case KeyCode.DownArrow: return keyboard.downArrowKey.isPressed;
                case KeyCode.LeftArrow: return keyboard.leftArrowKey.isPressed;
                case KeyCode.RightArrow: return keyboard.rightArrowKey.isPressed;
                case KeyCode.Space: return keyboard.spaceKey.isPressed;
                case KeyCode.LeftShift: case KeyCode.RightShift: return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                case KeyCode.LeftControl: case KeyCode.RightControl: return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
                case KeyCode.E: return keyboard.eKey.isPressed;
                case KeyCode.F: return keyboard.fKey.isPressed;
                case KeyCode.Q: return keyboard.qKey.isPressed;
                case KeyCode.C: return keyboard.cKey.isPressed;
                case KeyCode.V: return keyboard.vKey.isPressed;
                case KeyCode.R: return keyboard.rKey.isPressed;
                case KeyCode.I: return keyboard.iKey.isPressed;
                case KeyCode.K: return keyboard.kKey.isPressed;
                case KeyCode.J: return keyboard.jKey.isPressed;
                case KeyCode.L: return keyboard.lKey.isPressed;
                case KeyCode.Return: case KeyCode.KeypadEnter: return keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed;
                case KeyCode.Escape: return keyboard.escapeKey.isPressed;
            }
        }

        try
        {
            return Input.GetKey(key);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether a key was pressed down this frame.
    /// </summary>
    public static bool GetKeyDown(KeyCode key)
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            switch (key)
            {
                case KeyCode.W: return keyboard.wKey.wasPressedThisFrame;
                case KeyCode.S: return keyboard.sKey.wasPressedThisFrame;
                case KeyCode.A: return keyboard.aKey.wasPressedThisFrame;
                case KeyCode.D: return keyboard.dKey.wasPressedThisFrame;
                case KeyCode.Space: return keyboard.spaceKey.wasPressedThisFrame;
                case KeyCode.E: return keyboard.eKey.wasPressedThisFrame;
                case KeyCode.F: return keyboard.fKey.wasPressedThisFrame;
                case KeyCode.C: return keyboard.cKey.wasPressedThisFrame;
                case KeyCode.V: return keyboard.vKey.wasPressedThisFrame;
                case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
                case KeyCode.Return: case KeyCode.KeypadEnter: return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
                case KeyCode.Escape: return keyboard.escapeKey.wasPressedThisFrame;
                case KeyCode.LeftShift: return keyboard.leftShiftKey.wasPressedThisFrame;
            }
        }

        try
        {
            return Input.GetKeyDown(key);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether a mouse button is held down (0 = Left, 1 = Right, 2 = Middle).
    /// </summary>
    public static bool GetMouseButton(int button)
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (button == 0) return mouse.leftButton.isPressed;
            if (button == 1) return mouse.rightButton.isPressed;
            if (button == 2) return mouse.middleButton.isPressed;
        }

        try
        {
            return Input.GetMouseButton(button);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether a mouse button was clicked this frame.
    /// </summary>
    public static bool GetMouseButtonDown(int button)
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (button == 0) return mouse.leftButton.wasPressedThisFrame;
            if (button == 1) return mouse.rightButton.wasPressedThisFrame;
            if (button == 2) return mouse.middleButton.wasPressedThisFrame;
        }

        try
        {
            return Input.GetMouseButtonDown(button);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether a mouse button was released this frame.
    /// </summary>
    public static bool GetMouseButtonUp(int button)
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (button == 0) return mouse.leftButton.wasReleasedThisFrame;
            if (button == 1) return mouse.rightButton.wasReleasedThisFrame;
            if (button == 2) return mouse.middleButton.wasReleasedThisFrame;
        }

        try
        {
            return Input.GetMouseButtonUp(button);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Current mouse position in screen pixels.
    /// </summary>
    public static Vector2 mousePosition
    {
        get
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.position.ReadValue();
            }

            try
            {
                return Input.mousePosition;
            }
            catch
            {
                return Vector2.zero;
            }
        }
    }

    /// <summary>
    /// Reads normalized axis values ("Horizontal", "Vertical", "Mouse X", "Mouse Y").
    /// </summary>
    public static float GetAxis(string axisName)
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (axisName == "Horizontal")
        {
            float val = 0f;
            if (keyboard != null)
            {
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) val += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) val -= 1f;
                return val;
            }
        }
        else if (axisName == "Vertical")
        {
            float val = 0f;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) val += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) val -= 1f;
                return val;
            }
        }
        else if (axisName == "Mouse X")
        {
            if (mouse != null)
            {
                return mouse.delta.x.ReadValue() * 0.1f;
            }
        }
        else if (axisName == "Mouse Y")
        {
            if (mouse != null)
            {
                return mouse.delta.y.ReadValue() * 0.1f;
            }
        }

        try
        {
            return Input.GetAxis(axisName);
        }
        catch
        {
            return 0f;
        }
    }

    public static bool GetButtonDown(string buttonName)
    {
        if (buttonName == "Jump")
        {
            var keyboard = Keyboard.current;
            if (keyboard != null) return keyboard.spaceKey.wasPressedThisFrame;
        }

        try
        {
            return Input.GetButtonDown(buttonName);
        }
        catch
        {
            return false;
        }
    }
}
