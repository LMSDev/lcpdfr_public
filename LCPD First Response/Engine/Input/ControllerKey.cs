namespace LCPD_First_Response.Engine.Input
{
    using SlimDX.XInput;

    /// <summary>
    /// A key on a controller.
    /// </summary>
    internal class ControllerKey
    {
        /// <summary>
        /// Whether key has been down before.
        /// </summary>
        private bool hasBeenDownLastTime;

        /// <summary>
        /// Whether key is down.
        /// </summary>
        private bool isDown;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerKey"/> class.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        public ControllerKey(GamepadButtonFlags key)
        {
            this.Key = key;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        public GamepadButtonFlags Key { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the key has been pressed since the last check.
        /// </summary>
        public bool IsDown { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the key is down at the moment.
        /// </summary>
        public bool IsStillDown
        {
            get
            {
                return this.isDown;
            }
        }

        /// <summary>
        /// Updates the key.
        /// </summary>
        /// <param name="down">Whether key is down or not.</param>
        public void Update(bool down)
        {
            // First, update the real state
            this.isDown = down;

            // Set IsDown to real state
            this.IsDown = this.isDown;

            // However, when it has been down before and is still down, set it to false
            if (this.hasBeenDownLastTime && this.isDown)
            {
                // Set IsDown to false, because key was down the last time already
                this.IsDown = false;
            }

            // Update hasBennDownLastTime
            this.hasBeenDownLastTime = this.isDown;
            this.isDown = down;
        }
    }
}