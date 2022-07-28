namespace LCPD_First_Response.Engine.Input
{
    using System;
    using System.Collections.Generic;

    using SlimDX.XInput;

    /// <summary>
    /// Class to handle all controller input.
    /// </summary>
    internal static class Controller
    {
        /// <summary>
        /// The controller.
        /// </summary>
        private static SlimDX.XInput.Controller controller;

        /// <summary>
        /// All keys.
        /// </summary>
        private static Dictionary<GamepadButtonFlags, ControllerKey> keys;

        /// <summary>
        /// Gets the X position for the left thumb stick.
        /// </summary>
        public static short LeftThumbStickX { get; private set; }

        /// <summary>
        /// Gets the Y position for the left thumb stick.
        /// </summary>
        public static short LeftThumbStickY { get; private set; }

        /// <summary>
        /// Gets the X position for the right thumb stick.
        /// </summary>
        public static short RightThumbStickX { get; private set; }

        /// <summary>
        /// Gets the Y position for the right thumb stick.
        /// </summary>
        public static short RightThumbStickY { get; private set; }


        /// <summary>
        /// Initializes static members of the <see cref="Controller"/> class.
        /// </summary>
        static Controller()
        {
            controller = new SlimDX.XInput.Controller(UserIndex.One);
            keys = new Dictionary<GamepadButtonFlags, ControllerKey>();
            foreach (GamepadButtonFlags value in Enum.GetValues(typeof(GamepadButtonFlags)))
            {
                keys.Add(value, new ControllerKey(value));
            }
        }

        /// <summary>
        /// Returns whether <paramref name="gamepadButtonFlags"/> is down on the controller.
        /// </summary>
        /// <param name="gamepadButtonFlags">The button flags.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsKeyDown(GamepadButtonFlags gamepadButtonFlags)
        {
            if (controller.IsConnected)
            {
                return keys[gamepadButtonFlags].IsDown;
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="gamepadButtonFlags"/> is really down on the controller at the moment.
        /// </summary>
        /// <param name="gamepadButtonFlags">The button flags.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsKeyStillDown(GamepadButtonFlags gamepadButtonFlags)
        {
            if (controller.IsConnected)
            {
                return keys[gamepadButtonFlags].IsStillDown;
            }

            return false;
        }

        /// <summary>
        /// Updates the key states.
        /// </summary>
        public static void Update()
        {
            if (controller.IsConnected)
            {
                State state = controller.GetState();
                GamepadButtonFlags buttonFlags = state.Gamepad.Buttons;

                foreach (GamepadButtonFlags value in Enum.GetValues(typeof(GamepadButtonFlags)))
                {
                    if (buttonFlags.HasFlag(value))
                    {
                        keys[value].Update(true);
                    }
                    else
                    {
                        keys[value].Update(false);
                    }
                }

                // Update thumb stick values
                LeftThumbStickX = state.Gamepad.LeftThumbX;
                LeftThumbStickY = state.Gamepad.LeftThumbY;
                RightThumbStickX = state.Gamepad.RightThumbX;
                RightThumbStickY = state.Gamepad.RightThumbY;
            }
        }
    }
}