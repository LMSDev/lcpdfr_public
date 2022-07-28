
namespace LCPD_First_Response.LCPDFR.Input
{
    using System;
    using System.Collections.Generic;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;

    using SlimDX.XInput;

    using Controller = LCPD_First_Response.Engine.Input.Controller;

    /// <summary>
    /// Key handling class for LCPDFR.
    /// </summary>
    internal static class KeyHandler
    {
        /// <summary>
        /// All keys lcpdfr uses and their states
        /// </summary>
        private static Dictionary<ELCPDFRKeys, LCPDFRKey> keyStates;

        /// <summary>
        /// KeyDown event handler for <see cref="ELCPDFRKeys"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        public delegate void KeyDownEventHandler(ELCPDFRKeys key);

        /// <summary>
        /// Invoked when an <see cref="ELCPDFRKeys"/> is down.
        /// </summary>
        public static event KeyDownEventHandler KeyDown;

        /// <summary>
        /// Initializes static members of the <see cref="KeyHandler"/> class.
        /// </summary>
        public static void Initialize()
        {
            Log.Debug("Initializing...", "KeyHandler");
            keyStates = new Dictionary<ELCPDFRKeys, LCPDFRKey>();

            // Add all keys
            foreach (ELCPDFRKeys value in Enum.GetValues(typeof(ELCPDFRKeys)))
            {
                keyStates.Add(value, new LCPDFRKey(value));
            }

            // Initialize keys. This will look through the settings file and check if different hardware keys are assigned to the lcpdfr keys. If not, the default
            // hardware keys that are given through the KeyAttribute will be used
            foreach (KeyValuePair<ELCPDFRKeys, LCPDFRKey> keyValuePair in keyStates)
            {
                keyValuePair.Value.ReadAssignedKeys();
            }

            // Attach key down event handler
            Main.KeyWatchDog.GameKeyDown += new KeyWatchDog.GameKeyDownEventHandler(KeyWatchDog_GameKeyDown);
            Main.KeyWatchDog.KeyDown += new Engine.Input.KeyWatchDog.KeyDownEventHandler(KeyWatchDog_KeyDown);
            Log.Debug("Done", "KeyHandler");
        }

        /// <summary>
        /// Returns the key instance for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The key instance.</returns>
        public static LCPDFRKey GetKey(ELCPDFRKeys key)
        {
            return keyStates[key];
        }

        /// <summary>
        /// Returns a value indicating whether <paramref name="buttonFlags"/> is down.
        /// </summary>
        /// <param name="buttonFlags">The key.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsControllerKeyDown(GamepadButtonFlags buttonFlags)
        {
            return Controller.IsKeyDown(buttonFlags);
        }

        /// <summary>
        /// Returns a value indicating whether <paramref name="buttonFlags"/> is really down at the moment.
        /// </summary>
        /// <param name="buttonFlags">The key.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsControllerKeyStillDown(GamepadButtonFlags buttonFlags)
        {
            return Controller.IsKeyStillDown(buttonFlags);
        }

        /// <summary>
        /// Returns a value indicating whether <paramref name="gameKey"/> is down.
        /// </summary>
        /// <param name="gameKey">The game key.</param>
        /// <param name="ignoreConsole">If false, the key check will always return false if the console is active. If true, will return the real key state.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsGameKeyDown(EGameKey gameKey, bool ignoreConsole = false)
        {
            if (GTA.Game.Console.isActive && !ignoreConsole)
            {
                return false;
            }

            if (Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return false;
            }

            return Engine.Main.KeyWatchDog.IsGameKeyDown(gameKey);
        }

        /// <summary>
        /// Gets a value indicating whether <paramref name="keys"/> is down.
        /// </summary>
        /// <param name="keys">Windows.Forms key.</param>
        /// <param name="ignoreConsole">If false, the key check will always return false if the console is active. If true, will return the real key state.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsKeyboardKeyDown(System.Windows.Forms.Keys keys, bool ignoreConsole = false)
        {
            if (GTA.Game.Console.isActive && !ignoreConsole)
            {
                return false;
            }

            if (Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return false;
            }

            return Engine.Main.KeyWatchDog.IsKeyDown(keys);
        }

        /// <summary>
        /// Gets a value indicating whether <paramref name="keys"/> is really down at the moment.
        /// </summary>
        /// <param name="keys">Windows.Forms key.</param>
        /// <param name="ignoreConsole">If false, the key check will always return false if the console is active. If true, will return the real key state.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsKeyboardKeyStillDown(System.Windows.Forms.Keys keys, bool ignoreConsole = false)
        {
            if (GTA.Game.Console.isActive && !ignoreConsole)
            {
                return false;
            }

            if (Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return false;
            }

            return Main.KeyWatchDog.IsKeyStillDown(keys);
        }


        /// <summary>
        /// Gets a value indicating whether <paramref name="keys"/> has been pressed since the last check.
        /// </summary>
        /// <param name="keys">The key to check.</param>
        /// <param name="ignoreConsole">If false, the key check will always return false if the console is active. If true, will return the real key state.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsKeyDown(ELCPDFRKeys keys, bool ignoreConsole = false)
        {
            if (GTA.Game.Console.isActive && !ignoreConsole)
            {
                return false;
            }

            if (Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return false;
            }

            return keyStates[keys].IsDown;
        }

        /// <summary>
        /// Gets a value indicating whether <paramref name="keys"/> is down.
        /// </summary>
        /// <param name="keys">The key to check.</param>
        /// <param name="ignoreConsole">If false, the key check will always return false if the console is active. If true, will return the real key state.</param>
        /// <returns>True if down, false if not.</returns>
        public static bool IsKeyStillDown(ELCPDFRKeys keys, bool ignoreConsole = false)
        {
            if (GTA.Game.Console.isActive && !ignoreConsole)
            {
                return false;
            }

            if (Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return false;
            }

            return keyStates[keys].IsStillDown;
        }

        /// <summary>
        /// Writes the default settings to the file.
        /// </summary>
        public static void WriteDefaultIniSettings()
        {
            foreach (KeyValuePair<ELCPDFRKeys, LCPDFRKey> keyValuePair in keyStates)
            {
                Settings.WriteValue("Keybindings", keyValuePair.Value.KeyAttribute.SettingsName, keyValuePair.Value.KeyAttribute.Key.ToString());
                Settings.WriteValue("Keybindings", keyValuePair.Value.KeyAttribute.SettingsName + "ModifierKey", keyValuePair.Value.KeyAttribute.KeyModifierKey.ToString());
                Settings.WriteValue("KeybindingsController", keyValuePair.Value.KeyAttribute.SettingsName, keyValuePair.Value.KeyAttribute.ControllerKey.ToString());
                Settings.WriteValue("KeybindingsController", keyValuePair.Value.KeyAttribute.SettingsName + "ModifierKey", keyValuePair.Value.KeyAttribute.ControllerModifierKey.ToString());
            }
        }

        /// <summary>
        /// Called when a game key is down.
        /// </summary>
        /// <param name="gameKey">The key pressed.</param>
        /// <returns>Return false if key shouldn't be passed to other functions.</returns>
        private static bool KeyWatchDog_GameKeyDown(EGameKey gameKey)
        {
            if (KeyDown == null)
            {
                return true;
            }

            if (Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return true;
            }

            // Check if key is assigned to lcpdfrkey
            foreach (KeyValuePair<ELCPDFRKeys, LCPDFRKey> keyValuePair in keyStates)
            {
                if (keyValuePair.Value.GameKey == gameKey)
                {
                    if (keyValuePair.Value.IsStillDown)
                    {
                        KeyDown(keyValuePair.Key);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Called when a key is down. If console is active, keys aren't passed.
        /// </summary>
        /// <param name="key">The key pressed.</param>
        /// <returns>Return false if key shouldn't be passed to other functions.</returns>
        private static bool KeyWatchDog_KeyDown(System.Windows.Forms.Keys key)
        {
            if (KeyDown == null)
            {
                return true;
            }

            if (GTA.Game.Console.isActive)
            {
                return false;
            }

            if (Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return true;
            }

            // Check if key is assigned to lcpdfrkey
            foreach (KeyValuePair<ELCPDFRKeys, LCPDFRKey> keyValuePair in keyStates)
            {
                if (keyValuePair.Value.Key == key)
                {
                    if (keyValuePair.Value.IsStillDown)
                    {
                        KeyDown(keyValuePair.Key);
                    }
                }
            }

            return true;    
        }
    }
}