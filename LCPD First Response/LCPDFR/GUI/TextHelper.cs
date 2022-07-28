namespace LCPD_First_Response.LCPDFR.GUI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.LCPDFR.Input;

    using SlimDX.XInput;

    /// <summary>
    /// Provides functions to format and display text.
    /// </summary>
    internal class TextHelper
    {
        /// <summary>
        /// Gets a value indicating whether a helpbox is being displayed.
        /// </summary>
        public static bool IsHelpboxBeingDisplayed
        {
            get
            {
                return HelpBox.IsBeingDisplayed();
            }
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the news scrollbar.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void AddStringToNewsScrollbar(string text)
        {
            Gui.AddStringToNewsScrollBar(text);
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the text wall.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="allTextModeOnly">Whether this text will only be added if all text mode is enabled.</param>
        public static void AddTextToTextWall(string text, bool allTextModeOnly = false)
        {
            if (allTextModeOnly && !Settings.AllTextModeEnabled)
            {
                return;
            }

            Main.TextWall.AddText(text);
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the text wall.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="reporter">The reporter.</param>
        /// <param name="allTextModeOnly">Whether this text will only be added if all text mode is enabled.</param>
        public static void AddTextToTextWall(string text, string reporter, bool allTextModeOnly = false)
        {
            if (allTextModeOnly && !Settings.AllTextModeEnabled)
            {
                return;
            }

            Main.TextWall.AddText("[" + reporter + "] " + text);
        }

        /// <summary>
        /// Clears the help box.
        /// </summary>
        public static void ClearHelpbox()
        {
            HelpBox.Clear();
        }

        /// <summary>
        /// Formats and prints <paramref name="text"/> as helpbox.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="force">
        /// Whether the help box is forced and can replace a possible current helpbox.
        /// </param>
        public static unsafe void PrintFormattedHelpBox(string text, bool force = true)
        {
            if (IsHelpboxBeingDisplayed)
            {
                if (!force)
                {
                    return;
                }
            }

            HelpBox.Clear();

            // TODO: Format text using Engine.GUI.TextBuilder

            // Search for a key variable
            if (text.Contains("~KEY_"))
            {
                // The keys we override in memory. IDs: 33, 34, 23, 32, 39, 30, 16, 25
                string[] keysUsedToReplace = new string[]
                    {
                        "INPUT_FRONTEND_REPLAY_CYCLEMARKERLEFT", "INPUT_FRONTEND_REPLAY_CYCLEMARKERRIGHT",
                        "INPUT_FRONTEND_REPLAY_HIDEHUD", "INPUT_FRONTEND_REPLAY_NEWMARKER", "INPUT_FRONTEND_REPLAY_PAUSE",
                        "INPUT_FRONTEND_REPLAY_RESTART", "INPUT_FRONTEND_REPLAY_SCREENSHOT",
                        "INPUT_FRONTEND_REPLAY_SHOWHOTKEY"
                    };
                Dictionary<string, int> keysUsedToReplaceDic = new Dictionary<string, int>();
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_CYCLEMARKERLEFT", 0x33);
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_CYCLEMARKERRIGHT", 0x34);
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_HIDEHUD", 0x23);
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_NEWMARKER", 0x32);
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_PAUSE", 0x39);
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_RESTART", 0x30);
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_SCREENSHOT", 0x16);
                keysUsedToReplaceDic.Add("INPUT_FRONTEND_REPLAY_SHOWHOTKEY", 0x25);

                // Split by "~KEY_"
                Regex regex = new Regex("~KEY_");
                string[] strings = regex.Split(text);
                int stringsReplaced = 0;
                for (int index = 1; index < strings.Length; index++)
                {
                    string s = strings[index];

                    // Get key
                    string keyName = s;
                    keyName = keyName.Substring(0, keyName.IndexOf("~"));

                    // Since KEY_ARREST_DRIVE_TO_PD is much better readable than KEY_ARRESTDRIVETOPD we use it in the translations and thus remove the _ remove
                    string strippedKeyName = keyName.Replace("_", string.Empty);

                    // Get assigned key
                    ELCPDFRKeys lcpdfrKeys;
                    if (Enum.TryParse(strippedKeyName, true, out lcpdfrKeys))
                    {
                        LCPDFRKey key = KeyHandler.GetKey(lcpdfrKeys);
                        Keys keyBoardKey = key.Key;
                        GamepadButtonFlags controllerKey = key.ControllerKey;

                        // If using the keyboard, simply write the key
                        if (!Engine.Main.KeyWatchDog.IsUsingController)
                        {
                            string newText = keyBoardKey.ToString();

                            // Check if the key text can be parsed, since some special keys that do not consist of one letter, such as UP should rather be an "up-arrow"
                            string parsedKeyText = ParseKeyStringToKey(newText);
                            if (parsedKeyText != string.Empty)
                            {
                                // Replace and append ~ again to make sure the right key is overwritten and not another key just starting with the same name
                                // So by using ~ we ensure the name is ending
                                text = text.Replace("KEY_" + keyName + "~", parsedKeyText + "~");
                                continue;
                            }

                            // Localize
                            newText = LocalizeKey(newText);

                            // For every string we have to replace, we take another ingame value
                            string replaceString = keysUsedToReplace[stringsReplaced];
                            int id = keysUsedToReplaceDic[replaceString];
                            stringsReplaced++;
                            AdvancedHookManaged.AGame.RegisterReplacementText(id, newText);

                            // Write text using our replacement string
                            text = text.Replace("KEY_" + keyName, replaceString);

                            // If key has modifier keys, append
                            if (key.HasModifierKeys)
                            {
                                string modifierKeyString = GetStringRepresentationOfModifierKeys(key);
                                string replaceStringModifier = keysUsedToReplace[stringsReplaced];
                                id = keysUsedToReplaceDic[replaceStringModifier];
                                stringsReplaced++;
                                AdvancedHookManaged.AGame.RegisterReplacementText(id, modifierKeyString);
                                text = text.Replace("~" + replaceString + "~", "~" + replaceStringModifier + "~ ~" + replaceString + "~");
                            }
                        }
                        else
                        {
                            // Using a controller, insert PAD_KEY
                            string parsedControllerKey = ParseControllerKeyStringToKey(controllerKey.ToString());
                            text = text.Replace("~KEY_" + keyName + "~", "~" + parsedControllerKey + "~");

                            // If there is a modifier key, append it
                            if (key.ControllerModifierKey != GamepadButtonFlags.None)
                            {
                                string parsedModifierKey = ParseControllerKeyStringToKey(key.ControllerModifierKey.ToString());
                                text = text.Replace("~" + parsedControllerKey + "~", "~" + parsedControllerKey + "~ ~" + parsedModifierKey + "~");
                            }
                        }
                    }
                    else
                    {
                        // Not a registered key, so simply write text
                        // For every string we have to replace, we take another ingame value
                        string replaceString = keysUsedToReplace[stringsReplaced];
                        int id = keysUsedToReplaceDic[replaceString];
                        stringsReplaced++;
                        AdvancedHookManaged.AGame.RegisterReplacementText(id, keyName);

                        // Couldn't find a suitable replacement for the key, so now we basically just insert the letter
                        text = text.Replace("KEY_" + keyName, replaceString);
                    }
                }
            }

            HelpBox.Print(text);
        }

        /// <summary>
        /// Prints <paramref name="text"/> in the lower center of the screen.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="duration">The duration.</param>
        public static void PrintText(string text, int duration)
        {
            Gui.PrintText(text, duration);
        }

        /// <summary>
        /// Sets the message for the loading screen to <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void SetLoadingScreenMessage(string text)
        {
            AdvancedHookManaged.AGame.RegisterReplacementText(0x33, text);
            Natives.SetMsgForLoadingScreen("INPUT_FRONTEND_REPLAY_CYCLEMARKERLEFT");
        }

        /// <summary>
        /// Gets the string representation of modifier keys.
        /// </summary>
        /// <param name="lcpdfrKey">The key.</param>
        /// <returns>The string.</returns>
        private static string GetStringRepresentationOfModifierKeys(LCPDFRKey lcpdfrKey)
        {
            string append = string.Empty;

            if (lcpdfrKey.HasModifierKeys)
            {
                if (lcpdfrKey.KeyAttribute.KeyModifierKey == Keys.Menu)
                {
                    append += LocalizeKey("ALT");
                }

                if (lcpdfrKey.KeyAttribute.KeyModifierKey == Keys.ControlKey)
                {
                    append += LocalizeKey("CONTROLKEY");
                }

                if (lcpdfrKey.KeyAttribute.KeyModifierKey == Keys.ShiftKey)
                {
                    append += LocalizeKey("SHIFTKEY");
                }
            }

            return append;
        }

        /// <summary>
        /// Localizes a key that doesn't consist of a single letter such as SPACE or RETURN.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The localized key.</returns>
        private static string LocalizeKey(string key)
        {
            key = key.ToUpper();

            switch (key)
            {
                case "SHIFTKEY":
                    return CultureHelper.GetText("INPUT_SHIFT");

                case "SPACE":
                    return CultureHelper.GetText("INPUT_SPACE");

                case "RETURN":
                    return CultureHelper.GetText("INPUT_RETURN");
            }

            return key;
        }

        /// <summary>
        /// Parses <paramref name="key"/> into a key that is useable ingame, e.g. Up will become PAD_UP
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The parsed key.</returns>
        private static string ParseKeyStringToKey(string key)
        {
            key = key.ToLower();

            switch (key)
            {
                case "up":
                    return "PAD_UP";
            }

            switch (key)
            {
                case "down":
                    return "PAD_DOWN";
            }

            switch (key)
            {
                case "left":
                    return "PAD_LEFT";
            }

            switch (key)
            {
                case "right":
                    return "PAD_RIGHT";
            }

            return string.Empty;
        }

        /// <summary>
        /// Parses <paramref name="key"/> into a key that is useable ingame, e.g. DPadDown will become PAD_DPAD_DOWN
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The parsed key.</returns>
        private static string ParseControllerKeyStringToKey(string key)
        {
            switch (key)
            {
                case "A":
                    return "PAD_A";

                case "B":
                    return "PAD_B";

                case "X":
                    return "PAD_X";

                case "Y":
                    return "PAD_Y";

                case "DPadDown":
                    return "PAD_DPAD_DOWN";

                case "DPadLeft":
                    return "PAD_DPAD_LEFT";

                case "DPadRight":
                    return "PAD_DPAD_RIGHT";

                case "DPadUp":
                    return "PAD_DPAD_UP";

                case "LeftShoulder":
                    return "PAD_LB";

                case "LeftThumb":
                    return "PAD_LSTICK_NONE";

                case "RightThumb":
                    return "PAD_RSTICK_NONE";
            }

            return string.Empty;
        }
    }
}