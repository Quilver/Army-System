using UnityEngine;
using UnityEngine.InputSystem;

namespace BattlePrep.Tent
{
    // Input the tent reads directly (spec Docs/PreBattle/TentHub.md §1, §3). Clicks on objects go through the EventSystem.
    public static class TentInput
    {
        // Esc, right mouse button or gamepad East: close a panel, skip the briefing.
        public static bool CancelPressed =>
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        // Left click, Space, Enter or gamepad South: advance dialogue.
        public static bool AdvancePressed =>
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame ||
                                          Keyboard.current.enterKey.wasPressedThisFrame ||
                                          Keyboard.current.numpadEnterKey.wasPressedThisFrame)) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        // Arrow keys, WASD or the gamepad d-pad / left stick: start keyboard / controller navigation of the tent.
        public static bool NavigatePressed
        {
            get
            {
                var k = Keyboard.current;
                if (k != null && (k.leftArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame ||
                                  k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame ||
                                  k.aKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame))
                    return true;
                var g = Gamepad.current;
                return g != null && (g.dpad.left.wasPressedThisFrame || g.dpad.right.wasPressedThisFrame ||
                                     g.leftStick.left.wasPressedThisFrame || g.leftStick.right.wasPressedThisFrame);
            }
        }

        // Q / Page Up / left shoulder and E / Page Down / right shoulder: turn book pages.
        public static bool PagePrevPressed =>
            (Keyboard.current != null && (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.pageUpKey.wasPressedThisFrame)) ||
            (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame);

        public static bool PageNextPressed =>
            (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.pageDownKey.wasPressedThisFrame)) ||
            (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

        public static bool PointerMoved => Pointer.current != null && Pointer.current.delta.ReadValue().sqrMagnitude > 0.01f;

        public static Vector2 PointerPosition => Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
    }
}
