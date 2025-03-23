using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
namespace TransformHandles.Utils
{
    // -------------------- Utility Input Methods ----------------------
    public static class InputUtils
    {
        public static void EnableEnhancedTouch()
        {
#if !UNITY_EDITOR
            EnhancedTouchSupport.Enable();
#endif
        }

        public static Vector2 GetInputScreenPosition()
        {
#if UNITY_EDITOR
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Touchscreen.current != null && Touch.activeTouches.Count > 0
            ? Touch.activeTouches[0].screenPosition
            : Vector2.zero;
#endif
        }

        public static bool IsPrimaryPressed()
        {
#if UNITY_EDITOR
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
    return Touch.activeTouches.Count > 0 &&
            Touch.activeTouches[0].phase == TouchPhase.Moved;
#endif
        }

        public static bool IsPrimaryPressedThisFrame()
        {
#if UNITY_EDITOR
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Touchscreen.current != null && Touch.activeTouches.Count > 0 &&
                Touch.activeTouches[0].phase == TouchPhase.Began;
#endif
        }

        public static bool IsSecondaryPressedThisFrame()
        {
#if UNITY_EDITOR
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
        return false;
#endif
        }

        public static bool IsMiddlePressed()
        {
#if UNITY_EDITOR
            return Mouse.current != null && Mouse.current.middleButton.isPressed;
#else
        return false;
#endif
        }

        public static bool IsControlPressed()
        {
#if UNITY_EDITOR
            return Keyboard.current != null &&
                    (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
#else
        return false;
#endif
        }

        public static bool IsPrimaryReleasedThisFrame()
        {
#if UNITY_EDITOR
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return Touch.activeTouches.Count > 0 &&
                Touch.activeTouches[0].phase == TouchPhase.Ended;
#endif
        }

    }
}
