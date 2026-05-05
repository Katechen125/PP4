using UnityEngine;

using UnityEngine.InputSystem;

using System.Collections.Generic;

public static class SimpleInput
{
    static Dictionary<string, float> axisValues = new Dictionary<string, float>();
    static Dictionary<string, float> axisVelocities = new Dictionary<string, float>();
    static Dictionary<string, bool> lastButtonStates = new Dictionary<string, bool>();
    static Dictionary<string, bool> currentButtonStates = new Dictionary<string, bool>();
    static Dictionary<KeyCode, bool> lastKeyStates = new Dictionary<KeyCode, bool>();
    const float gravity = 3f;
    const float sensitivity = 3f;
    const float dead = 0.001f;
    static float deltaTime => Time.unscaledDeltaTime;

    // -------- AXIS -------- 
    public static float GetAxisRaw(string axis)
    {
        return axis switch { "Horizontal" => GetHorizontalRaw(), "Vertical" => GetVerticalRaw(), "Mouse X" => Mouse.current?.delta.x.ReadValue() ?? 0f, "Mouse Y" => Mouse.current?.delta.y.ReadValue() ?? 0f, _ => 0f };
    }
    public static float GetAxis(string axis)
    {
        float raw = GetAxisRaw(axis);
        if (!axisValues.ContainsKey(axis))
        {
            axisValues[axis] = 0;
            axisVelocities[axis] = 0;
        }
        float target = raw;
        float current = axisValues[axis];
        if (Mathf.Abs(target) > dead)
        {
            current = Mathf.MoveTowards(current, target, sensitivity * deltaTime);
        }
        else
        {
            current = Mathf.MoveTowards(current, 0, gravity * deltaTime);
        }
        axisValues[axis] = current;
        return current;
    }
    static float GetHorizontalRaw()
    {
        float value = 0;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) value -= 1;
            if (Keyboard.current.dKey.isPressed) value += 1;
            if (Keyboard.current.leftArrowKey.isPressed) value -= 1;
            if (Keyboard.current.rightArrowKey.isPressed) value += 1;
        }
        if (Gamepad.current != null) value += Gamepad.current.leftStick.x.ReadValue();
        if (Joystick.current != null) value += Joystick.current.stick.x.ReadValue();
        return Mathf.Clamp(value, -1f, 1f);
    }
    static float GetVerticalRaw()
    {
        float value = 0;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.sKey.isPressed) value -= 1;
            if (Keyboard.current.wKey.isPressed) value += 1;
            if (Keyboard.current.downArrowKey.isPressed) value -= 1;
            if (Keyboard.current.upArrowKey.isPressed) value += 1;
        }
        if (Gamepad.current != null) value += Gamepad.current.leftStick.y.ReadValue();
        if (Joystick.current != null) value += Joystick.current.stick.y.ReadValue();
        return Mathf.Clamp(value, -1f, 1f);
    }
    // -------- BUTTONS -------- 
    public static bool GetButton(string button)
    {
        bool state = GetButtonState(button);
        currentButtonStates[button] = state;
        return state;
    }
    public static bool GetButtonDown(string button)
    {
        bool current = GetButtonState(button);
        bool last = lastButtonStates.ContainsKey(button) && lastButtonStates[button];
        lastButtonStates[button] = current;
        return current && !last;
    }
    public static bool GetButtonUp(string button)
    {
        bool current = GetButtonState(button);
        bool last = lastButtonStates.ContainsKey(button) && lastButtonStates[button];
        lastButtonStates[button] = current;
        return !current && last;
    }
    static bool GetButtonState(string button)
    {
        switch (button)
        {
            case "Jump":
                if (Keyboard.current?.spaceKey.isPressed == true) return true;
                if (Gamepad.current?.buttonSouth.isPressed == true) return true;
                if (Joystick.current?.trigger.isPressed == true) return true;
                break;
            case "Fire1":
                if (Mouse.current?.leftButton.isPressed == true) return true;
                if (Gamepad.current?.rightTrigger.ReadValue() > 0.5f) return true;
                break;
            case "Fire2":
                if (Mouse.current?.rightButton.isPressed == true) return true;
                if (Gamepad.current?.leftTrigger.ReadValue() > 0.5f) return true;
                break;
        }
        return false;
    }
    // -------- KEYS --------
    public static bool GetKey(KeyCode key)
    {
        return GetKeyState(key);
    }
    public static bool GetKeyDown(KeyCode key)
    {
        bool current = GetKeyState(key);
        bool last = lastKeyStates.ContainsKey(key) && lastKeyStates[key];
        lastKeyStates[key] = current;
        return current && !last;
    }
    public static bool GetKeyUp(KeyCode key)
    {
        bool current = GetKeyState(key);
        bool last = lastKeyStates.ContainsKey(key) && lastKeyStates[key];
        lastKeyStates[key] = current;
        return !current && last;
    }
    static bool GetKeyState(KeyCode key)
    {
        if (Keyboard.current == null) return false;
        return key switch
        {
            // Letters
            KeyCode.A => Keyboard.current.aKey.isPressed,
            KeyCode.B => Keyboard.current.bKey.isPressed,
            KeyCode.C => Keyboard.current.cKey.isPressed,
            KeyCode.D => Keyboard.current.dKey.isPressed,
            KeyCode.E => Keyboard.current.eKey.isPressed,
            KeyCode.F => Keyboard.current.fKey.isPressed,
            KeyCode.G => Keyboard.current.gKey.isPressed,
            KeyCode.H => Keyboard.current.hKey.isPressed,
            KeyCode.I => Keyboard.current.iKey.isPressed,
            KeyCode.J => Keyboard.current.jKey.isPressed,
            KeyCode.K => Keyboard.current.kKey.isPressed,
            KeyCode.L => Keyboard.current.lKey.isPressed,
            KeyCode.M => Keyboard.current.mKey.isPressed,
            KeyCode.N => Keyboard.current.nKey.isPressed,
            KeyCode.O => Keyboard.current.oKey.isPressed,
            KeyCode.P => Keyboard.current.pKey.isPressed,
            KeyCode.Q => Keyboard.current.qKey.isPressed,
            KeyCode.R => Keyboard.current.rKey.isPressed,
            KeyCode.S => Keyboard.current.sKey.isPressed,
            KeyCode.T => Keyboard.current.tKey.isPressed,
            KeyCode.U => Keyboard.current.uKey.isPressed,
            KeyCode.V => Keyboard.current.vKey.isPressed,
            KeyCode.W => Keyboard.current.wKey.isPressed,
            KeyCode.X => Keyboard.current.xKey.isPressed,
            KeyCode.Y => Keyboard.current.yKey.isPressed,
            KeyCode.Z => Keyboard.current.zKey.isPressed,
            // Top-row numbers
            KeyCode.Alpha0 => Keyboard.current.digit0Key.isPressed,
            KeyCode.Alpha1 => Keyboard.current.digit1Key.isPressed,
            KeyCode.Alpha2 => Keyboard.current.digit2Key.isPressed,
            KeyCode.Alpha3 => Keyboard.current.digit3Key.isPressed,
            KeyCode.Alpha4 => Keyboard.current.digit4Key.isPressed,
            KeyCode.Alpha5 => Keyboard.current.digit5Key.isPressed,
            KeyCode.Alpha6 => Keyboard.current.digit6Key.isPressed,
            KeyCode.Alpha7 => Keyboard.current.digit7Key.isPressed,
            KeyCode.Alpha8 => Keyboard.current.digit8Key.isPressed,
            KeyCode.Alpha9 => Keyboard.current.digit9Key.isPressed,
            // Numpad
            KeyCode.Keypad0 => Keyboard.current.numpad0Key.isPressed,
            KeyCode.Keypad1 => Keyboard.current.numpad1Key.isPressed,
            KeyCode.Keypad2 => Keyboard.current.numpad2Key.isPressed,
            KeyCode.Keypad3 => Keyboard.current.numpad3Key.isPressed,
            KeyCode.Keypad4 => Keyboard.current.numpad4Key.isPressed,
            KeyCode.Keypad5 => Keyboard.current.numpad5Key.isPressed,
            KeyCode.Keypad6 => Keyboard.current.numpad6Key.isPressed,
            KeyCode.Keypad7 => Keyboard.current.numpad7Key.isPressed,
            KeyCode.Keypad8 => Keyboard.current.numpad8Key.isPressed,
            KeyCode.Keypad9 => Keyboard.current.numpad9Key.isPressed,
            KeyCode.KeypadDivide   => Keyboard.current.numpadDivideKey.isPressed,
            KeyCode.KeypadMultiply => Keyboard.current.numpadMultiplyKey.isPressed,
            KeyCode.KeypadMinus    => Keyboard.current.numpadMinusKey.isPressed,
            KeyCode.KeypadPlus     => Keyboard.current.numpadPlusKey.isPressed,
            KeyCode.KeypadPeriod   => Keyboard.current.numpadPeriodKey.isPressed,
            KeyCode.KeypadEnter    => Keyboard.current.numpadEnterKey.isPressed,
            // Function keys
            KeyCode.F1  => Keyboard.current.f1Key.isPressed,
            KeyCode.F2  => Keyboard.current.f2Key.isPressed,
            KeyCode.F3  => Keyboard.current.f3Key.isPressed,
            KeyCode.F4  => Keyboard.current.f4Key.isPressed,
            KeyCode.F5  => Keyboard.current.f5Key.isPressed,
            KeyCode.F6  => Keyboard.current.f6Key.isPressed,
            KeyCode.F7  => Keyboard.current.f7Key.isPressed,
            KeyCode.F8  => Keyboard.current.f8Key.isPressed,
            KeyCode.F9  => Keyboard.current.f9Key.isPressed,
            KeyCode.F10 => Keyboard.current.f10Key.isPressed,
            KeyCode.F11 => Keyboard.current.f11Key.isPressed,
            KeyCode.F12 => Keyboard.current.f12Key.isPressed,
            // Arrow keys
            KeyCode.UpArrow    => Keyboard.current.upArrowKey.isPressed,
            KeyCode.DownArrow  => Keyboard.current.downArrowKey.isPressed,
            KeyCode.LeftArrow  => Keyboard.current.leftArrowKey.isPressed,
            KeyCode.RightArrow => Keyboard.current.rightArrowKey.isPressed,
            // Modifiers
            KeyCode.LeftShift    => Keyboard.current.leftShiftKey.isPressed,
            KeyCode.RightShift   => Keyboard.current.rightShiftKey.isPressed,
            KeyCode.LeftControl  => Keyboard.current.leftCtrlKey.isPressed,
            KeyCode.RightControl => Keyboard.current.rightCtrlKey.isPressed,
            KeyCode.LeftAlt      => Keyboard.current.leftAltKey.isPressed,
            KeyCode.RightAlt     => Keyboard.current.rightAltKey.isPressed,
            KeyCode.LeftCommand  => Keyboard.current.leftCommandKey.isPressed,
            KeyCode.RightCommand => Keyboard.current.rightCommandKey.isPressed,
            // Navigation cluster
            KeyCode.Insert   => Keyboard.current.insertKey.isPressed,
            KeyCode.Delete   => Keyboard.current.deleteKey.isPressed,
            KeyCode.Home     => Keyboard.current.homeKey.isPressed,
            KeyCode.End      => Keyboard.current.endKey.isPressed,
            KeyCode.PageUp   => Keyboard.current.pageUpKey.isPressed,
            KeyCode.PageDown => Keyboard.current.pageDownKey.isPressed,
            // Common keys
            KeyCode.Space     => Keyboard.current.spaceKey.isPressed,
            KeyCode.Return    => Keyboard.current.enterKey.isPressed,
            KeyCode.Escape    => Keyboard.current.escapeKey.isPressed,
            KeyCode.Tab       => Keyboard.current.tabKey.isPressed,
            KeyCode.Backspace => Keyboard.current.backspaceKey.isPressed,
            KeyCode.CapsLock  => Keyboard.current.capsLockKey.isPressed,
            KeyCode.Numlock   => Keyboard.current.numLockKey.isPressed,
            KeyCode.Print     => Keyboard.current.printScreenKey.isPressed,
            KeyCode.ScrollLock => Keyboard.current.scrollLockKey.isPressed,
            KeyCode.Pause     => Keyboard.current.pauseKey.isPressed,
            // Punctuation
            KeyCode.BackQuote     => Keyboard.current.backquoteKey.isPressed,
            KeyCode.Minus         => Keyboard.current.minusKey.isPressed,
            KeyCode.Equals        => Keyboard.current.equalsKey.isPressed,
            KeyCode.LeftBracket   => Keyboard.current.leftBracketKey.isPressed,
            KeyCode.RightBracket  => Keyboard.current.rightBracketKey.isPressed,
            KeyCode.Backslash     => Keyboard.current.backslashKey.isPressed,
            KeyCode.Semicolon     => Keyboard.current.semicolonKey.isPressed,
            KeyCode.Quote         => Keyboard.current.quoteKey.isPressed,
            KeyCode.Comma         => Keyboard.current.commaKey.isPressed,
            KeyCode.Period        => Keyboard.current.periodKey.isPressed,
            KeyCode.Slash         => Keyboard.current.slashKey.isPressed,
            _                     => false
        };
    }
    // Optional helper 
    public static Vector2 GetVector2(string horizontal = "Horizontal", string vertical = "Vertical")
    {
        return new Vector2(GetAxis(horizontal), GetAxis(vertical));
    }
}
