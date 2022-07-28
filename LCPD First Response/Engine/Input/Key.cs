namespace LCPD_First_Response.Engine.Input
{
    using System.Windows.Forms;

    /// <summary>
    /// Describes a game key which is associated to the keyboard or controller.
    /// </summary>
    internal enum EGameKey
    {
        /// <summary>
        ///  No key.
        /// </summary>
        None = -1,

        /// <summary>
        /// The sprint key.
        /// </summary>
        Sprint = 1,

        /// <summary>
        /// The jump key.
        /// </summary>
        Jump = 2,

        /// <summary>
        /// They enter car key.
        /// </summary>
        EnterCar = 3,

        /// <summary>
        /// The attack key.
        /// </summary>
        Attack = 4,

        /// <summary>
        /// The look behind key.
        /// </summary>
        LookBehind = 7,

        /// <summary>
        /// The next weapon key.
        /// </summary>
        NextWeapon = 8,

        /// <summary>
        /// The last weapon key.
        /// </summary>
        LastWeapon = 9,

        /// <summary>
        /// The crouch key.
        /// </summary>
        Crouch = 20,

        /// <summary>
        /// They phone key.
        /// </summary>
        Phone = 21,

        /// <summary>
        /// The action key.
        /// </summary>
        Action = 23,

        /// <summary>
        /// The seek cover key.
        /// </summary>
        SeekCover = 28,

        /// <summary>
        /// The reload key.
        /// </summary>
        Reload = 29,

        /// <summary>
        /// The sound horn key.
        /// </summary>
        SoundHorn = 54,

        /// <summary>
        /// The Esc key.
        /// </summary>
        Esc = 61,

        /// <summary>
        /// The nav down key.
        /// </summary>
        NavDown = 64,

        /// <summary>
        /// The nav up key.
        /// </summary>
        NavUp = 65,

        /// <summary>
        /// They nav left key.
        /// </summary>
        NavLeft = 66,

        /// <summary>
        /// The nav right key.
        /// </summary>
        NavRight = 67,

        /// <summary>
        /// The nav leave key.
        /// </summary>
        NavLeave = 76,

        /// <summary>
        /// The nav enter key.
        /// </summary>
        NavEnter = 77,

        /// <summary>
        /// The nav back key
        /// </summary>
        NavBack = 78,

        /// <summary>
        /// The radar zoom key.
        /// </summary>
        RadarZoom = 86,

        /// <summary>
        /// The aim key.
        /// </summary>
        Aim = 87,

        /// <summary>
        /// The move forward key.
        /// </summary>
        MoveForward = 1090,

        /// <summary>
        /// The move backward key.
        /// </summary>
        MoveBackward = 1091,

        /// <summary>
        /// The move left key.
        /// </summary>
        MoveLeft = 1092,

        /// <summary>
        /// The move right key.
        /// </summary>
        MoveRight = 1093,
    }

    /// <summary>
    /// Class that represents a keyboard key.
    /// </summary>
    internal class Key
    {
        /// <summary>
        /// Gets a value indicating whether the key has been pressed since the last check
        /// </summary>
        public bool IsDown { get; private set; }

        /// <summary>
        /// Gets a value indicating whether key has been released since the last check
        /// </summary>
        public bool IsUp { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the key is down at the moment, doesn't matter if it's the same as before
            /// </summary>
            public bool IsStillDown
            {
                get
                {
                    return this.isDown;
                }
            }

        private bool hasBeenDownLastTime;
        private Keys key;
        // The real state of the key
        private bool isDown;

        public Key(Keys key)
        {
            this.key = key;
        }

        public void ClearIsDown()
        {
            this.IsDown = false;
        }

        public void Update(byte data)
        {
            bool down = (data & 0x80) != 0;

            // First, update the real state
            this.isDown = down;

            // Set IsDown to real state
            this.IsDown = this.isDown;
            this.IsUp = !this.isDown;

            // However, when it has been down before and is still down, set it to false
            if (this.hasBeenDownLastTime && this.isDown)
            {
                // Set IsDown to false, because key was down the last time already
                this.IsDown = false;
            }
            // When it has been up before and is still up, set to false
            if (!this.hasBeenDownLastTime && this.IsUp)
            {
                this.IsUp = false;
            }

            // Update hasBennDownLastTime
            this.hasBeenDownLastTime = this.isDown;

            if (this.IsDown)
            {
                Main.KeyWatchDog.InvokeKeyDown(this.key);
            }
        }
    }
}
