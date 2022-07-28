namespace LCPD_First_Response.LCPDFR.Input
{
    using System;
    using System.Windows.Forms;

    using LCPD_First_Response.Engine.Input;

    using SlimDX.XInput;

    /// <summary>
    /// Describes the type of the key, i.e. where it is used. Can be used as flags.
    /// </summary>
    [Flags]
    internal enum EKeyType
    {
        /// <summary>
        /// Key only works in the police department.
        /// </summary>
        InPD = 0x1,

        /// <summary>
        /// Key only works in a vehicle.
        /// </summary>
        InVehicle = 0x2,

        /// <summary>
        /// Key only works on foot.
        /// </summary>
        OnFoot = 0x4,
    }

    /// <summary>
    /// Attributes for lcpdfr keys.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class KeyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="keyType">
        /// The key type.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="gameKey">
        /// The game key (used as controller key).
        /// </param>
        /// <param name="settingsName">
        /// The name of the settings value.
        /// </param>
        public KeyAttribute(EKeyType keyType, Keys key, GamepadButtonFlags gameKey, string settingsName)
        {
            this.KeyType = keyType;
            this.Key = key;
            this.KeyModifierKey = Keys.None;
            this.ControllerKey = gameKey;
            this.ControllerModifierKey = GamepadButtonFlags.None;
            this.SettingsName = settingsName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
        /// </summary>
        /// <param name="keyType">
        /// The key type.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="keyModifierKey">
        /// The modifier keyboard key.
        /// </param>
        /// <param name="gameKey">
        /// The game key (used as controller key).
        /// </param>
        /// <param name="controllerModifierKey">
        /// The controller modifier key.
        /// </param>
        /// <param name="settingsName">
        /// The name of the settings value.
        /// </param>
        public KeyAttribute(EKeyType keyType, Keys key, Keys keyModifierKey, GamepadButtonFlags gameKey, GamepadButtonFlags controllerModifierKey, string settingsName)
        {
            this.KeyType = keyType;
            this.Key = key;
            this.KeyModifierKey = keyModifierKey;
            this.ControllerKey = gameKey;
            this.ControllerModifierKey = controllerModifierKey;
            this.SettingsName = settingsName;
        }

        /// <summary>
        /// Gets the Windows.Forms key.
        /// </summary>
        public Keys Key { get; private set; }

        /// <summary>
        /// Gets the modifier key for the key.
        /// </summary>
        public Keys KeyModifierKey { get; private set; }

        /// <summary>
        /// Gets the key type.
        /// </summary>
        public EKeyType KeyType { get; private set; }

        /// <summary>
        /// Gets the controller key.
        /// </summary>
        public GamepadButtonFlags ControllerKey { get; private set; }

        /// <summary>
        /// Gets the modifier key for the controller, so the second key that has to be down as well.
        /// </summary>
        public GamepadButtonFlags ControllerModifierKey { get; private set; }

        /// <summary>
        /// Gets the settings name of the key.
        /// </summary>
        public string SettingsName { get; private set; }
    }
}