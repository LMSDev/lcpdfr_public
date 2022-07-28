namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Checkpoint that is displayed as an arrow.
    /// </summary>
    internal class ArrowCheckpoint : BaseComponent, ITickable
    {
        /// <summary>
        /// The actual blip.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The callback called when the player is close to the arrow.
        /// </summary>
        private Delegate callback;

        /// <summary>
        /// Whether the player distance check is enabled. The check is disabled when the callback has been invoked and the player hasn't yet moved out of the arrow.
        /// </summary>
        private bool checkEnabled;

        /// <summary>
        /// The position.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Whether the blip is visible.
        /// </summary>
        private bool visible;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrowCheckpoint"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="callback">
        /// The callback called when the player is close to the arrow.
        /// </param>
        public ArrowCheckpoint(Vector3 position, Action callback)
        {
            this.position = position;
            this.callback = callback;

            this.blip = Blip.AddBlip(position);
            this.DistanceToEnter = 0.8f;
            this.ArrowColor = System.Drawing.Color.Orange;
            this.blip.Color = BlipColor.Orange;
            this.checkEnabled = true;
            this.Enabled = true;
            this.Visible = true;
            this.HasInitializedProperly = true;
        }

        /// <summary>
        /// Gets or sets the color of the blip.
        /// </summary>
        public System.Drawing.Color ArrowColor { get; set; }

        /// <summary>
        /// Gets or sets the blip displays.
        /// </summary>
        public BlipDisplay BlipDisplay
        {
            get
            {
                return this.blip.Display;
            }

            set
            {
                this.blip.Display = value;
            }
        }

        /// <summary>
        /// Gets or sets the blip color.
        /// </summary>
        public BlipColor BlipColor
        {
            get
            {
                return this.blip.Color;
            }

            set
            {
                this.blip.Color = value;
            }
        }

        /// <summary>
        /// Gets or sets the blip icon.
        /// </summary>
        public BlipIcon BlipIcon
        {
            get
            {
                return this.blip.Icon;
            }

            set
            {
                this.blip.Icon = value;
            }
        }

        /// <summary>
        /// Gets or sets the distance required to enter the checkpoint. Default is .8f.
        /// </summary>
        public float DistanceToEnter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checkpoint is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets a value indicating whether the checkpoint has been initialized properly.
        /// </summary>
        public bool HasInitializedProperly { get; private set; }

        /// <summary>
        /// Sets a value indicating whether a route to the blip is active.
        /// </summary>
        public bool RouteActive
        {
            set
            {
                this.blip.RouteActive = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the checkpoint is visible. Setting to false doesn't disable the checkpoint!
        /// </summary>
        public bool Visible
        {
            get
            {
                return this.visible;
            }

            set
            {
                this.visible = value;

                if (!this.visible)
                {
                    this.blip.Display = BlipDisplay.Hidden;
                }
                else
                {
                    this.blip.Display = BlipDisplay.ArrowAndMap;
                }
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ArrowCheckpoint";
            }
        }

        /// <summary>
        /// Deletes the checkpoint.
        /// </summary>
        public new void Delete()
        {
            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }

            base.Delete();
        }

        /// <summary>
        /// Checks whether <paramref name="point"/> is in the checkpoint.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>True if in, false if not.</returns>
        public bool IsPointInCheckpoint(Vector3 point)
        {
            return this.blip.Position.DistanceTo2D(point) < this.DistanceToEnter;
        }

        /// <summary>
        /// Called every tick.
        /// </summary>
        public void Process()
        {
            if (!this.Enabled)
            {
                return;
            }

            if (this.checkEnabled)
            {
                // if check is enabled, check for close player.
                if (CPlayer.LocalPlayer.Ped.Position.DistanceTo2D(this.blip.Position) < this.DistanceToEnter)
                {
                    this.checkEnabled = false;
                    this.callback.DynamicInvoke();
                }
            }
            else
            {
                // if check is disabled, check if player is far away enough to re-enable the check
                if (CPlayer.LocalPlayer.Ped.Position.DistanceTo2D(this.blip.Position) > 6f)
                {
                    this.checkEnabled = true;
                }
            }

            if (this.visible)
            {
                GUI.Gui.DrawColouredCylinder(this.position, this.ArrowColor);
            }
        }

        /// <summary>
        /// Resets the distance check and allows the player to enter again even though he didn't move away.
        /// </summary>
        public void ResetDistanceCheck()
        {
            this.checkEnabled = true;
        }

        /// <summary>
        /// Simulates player being close to the arrow and so the distance check is started, that ensures player has to get a little away form the arrow in order to enter it again.
        /// </summary>
        /// <param name="alsoInvokeCallback">Whether the callback should be also invoked.</param>
        public void TriggerPlayerIsInArrow(bool alsoInvokeCallback)
        {
            this.checkEnabled = false;

            if (alsoInvokeCallback)
            {
                this.callback.DynamicInvoke();
            }
        }
    }
}
