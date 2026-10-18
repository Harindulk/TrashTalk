using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// One place for input: mouse/touch pointer + keyboard (new Input System).
public static class Inp
{
    public static Vector2 ScreenPos => Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

    public static Vector2 WorldPos
    {
        get
        {
            var cam = CameraRig.I != null ? CameraRig.I.Cam : null;
            if (cam == null) return Vector2.zero;
            Vector3 p = ScreenPos;
            p.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(p);
        }
    }

    public static bool OverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    public static bool Pressed => Pointer.current != null && Pointer.current.press.wasPressedThisFrame && !OverUI;
    public static bool Held => Pointer.current != null && Pointer.current.press.isPressed;
    public static bool Released => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;

    // Drop trash from the keyboard (mouse taps are handled by Rocket).
    public static bool DropKey => Key(Keyboard.current?.spaceKey);

    // -1..1 from A/D or arrows.
    public static float Steer
    {
        get
        {
            var k = Keyboard.current;
            if (k == null) return 0;
            float s = 0;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) s -= 1;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) s += 1;
            return s;
        }
    }

    public static bool Restart => Key(Keyboard.current?.rKey);
    public static bool Pause => Key(Keyboard.current?.escapeKey) || Key(Keyboard.current?.pKey);

    static bool Key(KeyControl k) => k != null && k.wasPressedThisFrame;
}
