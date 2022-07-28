namespace LCPD_First_Response.LCPDFR.API
{
    using System;

    using GTA;

    /// <summary>
    /// Represents a colored arrow checkpoint in a similar fashion to the original IV mission checkpoints.
    /// </summary>
    public class ArrowCheckpoint
    {
        /// <summary>
        /// The internal arrow checkpoint.
        /// </summary>
        private Engine.Scripting.Entities.ArrowCheckpoint arrowCheckpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrowCheckpoint"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="color">
        /// The color.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        public ArrowCheckpoint(Vector3 position, System.Drawing.Color color, Action callback)
        {
            this.arrowCheckpoint = new Engine.Scripting.Entities.ArrowCheckpoint(position, callback);
            this.arrowCheckpoint.ArrowColor = color;
        }

        /// <summary>
        /// Gets or sets the blip displays.
        /// </summary>
        public BlipDisplay BlipDisplay
        {
            get
            {
                return this.arrowCheckpoint.BlipDisplay;
            }

            set
            {
                this.arrowCheckpoint.BlipDisplay = value;
            }
        }

        /// <summary>
        /// Gets or sets the blip color.
        /// </summary>
        public BlipColor BlipColor
        {
            get
            {
                return this.arrowCheckpoint.BlipColor;
            }

            set
            {
                this.arrowCheckpoint.BlipColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the blip icon.
        /// </summary>
        public BlipIcon BlipIcon
        {
            get
            {
                return this.arrowCheckpoint.BlipIcon;
            }

            set
            {
                this.arrowCheckpoint.BlipIcon = value;
            }
        }

        /// <summary>
        /// Gets or sets the color of the blip.
        /// </summary>
        public System.Drawing.Color CheckpointColor
        {
            get
            {
                return this.arrowCheckpoint.ArrowColor;
            }

            set
            {
                this.arrowCheckpoint.ArrowColor = value;
            }
        }

        /// <summary>
        /// Deletes the checkpoint.
        /// </summary>
        public void Delete()
        {
            this.arrowCheckpoint.Delete();
            this.arrowCheckpoint = null;
        }

        /// <summary>
        /// Checks whether <paramref name="point"/> is in the checkpoint.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>True if in, false if not.</returns>
        public bool IsPointInCheckpoint(Vector3 point)
        {
            return this.arrowCheckpoint.IsPointInCheckpoint(point);
        }

        /// <summary>
        /// Resets the distance check and allows the player to enter again even though he didn't move away.
        /// </summary>
        public void ResetDistanceCheck()
        {
            this.arrowCheckpoint.ResetDistanceCheck();
        }
    }
}