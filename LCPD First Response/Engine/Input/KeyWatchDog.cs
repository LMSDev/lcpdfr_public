namespace LCPD_First_Response.Engine.Input
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;

    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// Key watch dog class, handling all keyboard and controller input.
    /// </summary>
    internal class KeyWatchDog
    {
        /// <summary>
        /// All keys.
        /// </summary>
        private Key[] keys;

        /// <summary>
        /// Whether a controller is in use.
        /// </summary>
        private bool isUsingController;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyWatchDog"/> class.
        /// </summary>
        public KeyWatchDog()
        {
            this.keys = new Key[256];
            for (int i = 0; i < 256; i++)
            {
                this.keys[i] = new Key((Keys)i);
            }
        }

        /// <summary>
        /// KeyDown event handler, return false if key shouldn't be passed to other functions.
        /// </summary>
        /// <param name="gameKey">Pressed key.</param>
        /// <returns>Return false if key shouldn't be passed to other functions.</returns>
        public delegate bool GameKeyDownEventHandler(EGameKey gameKey);

        /// <summary>
        /// KeyDown event handler, return false if key shouldn't be passed to other functions.
        /// </summary>
        /// <param name="key">Pressed key.</param>
        /// <returns>Return false if key shouldn't be passed to other functions.</returns>
        public delegate bool KeyDownEventHandler(Keys key);

        /// <summary>
        /// KeyDown event, invoked when a game key is down.
        /// </summary>
        public event GameKeyDownEventHandler GameKeyDown;

        /// <summary>
        /// KeyDown event, invoked when a keyboard key is down.
        /// </summary>
        public event KeyDownEventHandler KeyDown;

        /// <summary>
        /// Gets or sets a value indicating whether keyboard input is forced. Works by overrinding <see cref="IsUsingController"/> to always return false.
        /// </summary>
        public bool AlwaysUseKeyboardInput { get; set; }

        /// <summary>
        /// Gets a value indicating whether a controller is used for input or not.
        /// </summary>
        public bool IsUsingController
        {
            get
            {
                return this.isUsingController;
            }
        }

        /// <summary>
        /// Fires the key down event for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key that is down.</param>
        public void InvokeKeyDown(Keys key)
        {
            KeyDownEventHandler handler = this.KeyDown;
            if (handler != null)
            {
                handler(key);
            }
        }

        /// <summary>
        /// Clears the state of <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        public void ClearKeyState(Keys key)
        {
            this.keys[(int) key].ClearIsDown();
        }

        public Key GetKey(Keys key)
        {
            return this.keys[(int) key];
        }

        public bool IsGameKeyDown(EGameKey eGameKey)
        {
            return GTA.Game.isGameKeyPressed((GTA.GameKey)(int)eGameKey);
        }

        public bool IsKeyDown(Keys key)
        {
            bool isDown =  this.keys[(int) key].IsDown;
            return isDown;
        }

        public bool IsKeyStillDown(Keys key)
        {
            return this.keys[(int) key].IsStillDown;
        }

        public bool IsKeyUp(Keys key)
        {
            return this.keys[(int) key].IsUp;
        }

        public void Update()
        {
            this.isUsingController = !this.AlwaysUseKeyboardInput && Natives.IsUsingController();

            // Get key states
            byte[] keyStates = new byte[256];
            if (!GetKeyboardState(keyStates))
            {
                // Throw exception when getting keys failed
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode);
            }

            // Update all keys with the state
            for (int i = 0; i < 256; i++)
            {
                this.keys[i].Update(keyStates[i]);
            }

            // Update all game keys
            if (this.GameKeyDown != null)
            {
                foreach (EGameKey gameKey in Enum.GetValues(typeof(EGameKey)))
                {
                    if (this.IsGameKeyDown(gameKey))
                    {
                        this.GameKeyDown(gameKey);
                    }
                }
            }

            // Update states by calling GetKeyState once
            GetKeyState(0);

            // Update controller keys
            Controller.Update();
        }

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetKeyboardState(byte[] keyState);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int virtualKey);
    }
}
