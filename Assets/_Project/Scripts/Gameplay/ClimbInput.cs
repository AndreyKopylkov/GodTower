using System;
using GodTower.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GodTower.Gameplay
{
    /// <summary>Player intent for one frame.</summary>
    public readonly struct ClimbInputFrame
    {
        public static readonly ClimbInputFrame None = default;

        /// <summary>The screen is held: climb.</summary>
        public readonly bool IsHolding;

        /// <summary>-1 = swipe left, +1 = swipe right, 0 = no swipe this frame.</summary>
        public readonly int Swipe;

        public ClimbInputFrame(bool isHolding, int swipe)
        {
            IsHolding = isHolding;
            Swipe = swipe;
        }
    }

    /// <summary>Source of climbing input, sampled once per frame by the level runner.</summary>
    public interface IClimbInput
    {
        ClimbInputFrame Read(float time);
    }

    /// <summary>
    /// Hold + swipe input from any pointer: touch on device, mouse in the editor.
    /// Uses code-defined Input System actions bound to <c>&lt;Pointer&gt;</c> (primary touch for touchscreens).
    /// A press that starts over UI (pause button, panels) is ignored until it is released.
    /// </summary>
    public sealed class PointerClimbInput : IClimbInput, IDisposable
    {
        private readonly InputAction _press = new("Press", InputActionType.Button, "<Pointer>/press");
        private readonly InputAction _position = new("Position", InputActionType.PassThrough, "<Pointer>/position", expectedControlType: "Vector2");
        private readonly SwipeDetector _swipes = new();
        private readonly UiPointerFilter _uiFilter;
        private bool _wasPressed;
        private bool _pressStartedOverUi;

        public PointerClimbInput(UiPointerFilter uiFilter)
        {
            _uiFilter = uiFilter;
            _press.Enable();
            _position.Enable();
        }

        public ClimbInputFrame Read(float time)
        {
            bool pressed = _press.IsPressed();
            Vector2 position = _position.ReadValue<Vector2>();
            if (pressed && !_wasPressed)
                _pressStartedOverUi = _uiFilter != null && _uiFilter.IsOverUi(position);
            if (_pressStartedOverUi)
            {
                _wasPressed = pressed;
                _pressStartedOverUi = pressed;
                return ClimbInputFrame.None;
            }

            float x = position.x;
            int swipe = 0;

            if (pressed && !_wasPressed)
                _swipes.Begin(time, x);
            else if (pressed)
                swipe = _swipes.Update(time, x, Screen.width);
            else if (_wasPressed)
                _swipes.End();

            _wasPressed = pressed;
            return new ClimbInputFrame(pressed, swipe);
        }

        public void Dispose()
        {
            _press.Dispose();
            _position.Dispose();
        }
    }
}
