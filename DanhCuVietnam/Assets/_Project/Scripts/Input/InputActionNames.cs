namespace DanhCu.Input
{
    /// <summary>Names in Settings/Input/DanhCuInputActions.inputactions. Change both together.</summary>
    public static class InputActionNames
    {
        public const string GameplayMap = "Gameplay";

        /// <summary>Vector2 screen position of mouse / primary touch.</summary>
        public const string PointerPosition = "PointerPosition";

        /// <summary>Button: mouse left / touch contact. Hold to wind the rope, release to throw.</summary>
        public const string PointerPress = "PointerPress";

        /// <summary>Vector2 gamepad aim direction.</summary>
        public const string Aim = "Aim";

        /// <summary>Axis 0..1: gamepad wind-up strength (right trigger).</summary>
        public const string Charge = "Charge";

        /// <summary>Vector2 camera orbit delta.</summary>
        public const string CameraOrbit = "CameraOrbit";

        /// <summary>Button: open / close pause menu.</summary>
        public const string Pause = "Pause";
    }
}
