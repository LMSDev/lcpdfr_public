using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LCPD_First_Response.Engine.GUI;

namespace LCPD_First_Response.Engine.GUI
{
    using System.Drawing;

    using Graphics = GTA.Graphics;

    /// <summary>
    /// The group box control.
    /// </summary>
    internal class GroupBox : Control, ICanHaveControls
    {
        /// <summary>
        /// The controls.
        /// </summary>
        private List<Control> controls;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupBox"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="size">
        /// The size.
        /// </param>
        public GroupBox(Point position, Size size) : base(position, size)
        {
            this.controls = new List<Control>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupBox"/> class.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        public GroupBox(int x, int y, int height, int width) : base(x, y, height, width)
        {
            this.controls = new List<Control>();
        }

        /// <summary>
        /// Adds <paramref name="control"/> to the group box.
        /// </summary>
        /// <param name="control">The control.</param>
        public void AddControl(Control control)
        {
            this.controls.Add(control);

            // Adjust position by 10 so controls are closer to the top
            control.ParentLocationChanged(new Point(this.PositionOffset + new Size(this.Position.X, this.Position.Y - 10)));
        }

        /// <summary>
        /// Gets all controls.
        /// </summary>
        /// <returns>All controls.</returns>
        public Control[] GetControls()
        {
            return this.controls.ToArray();
        }

        /// <summary>
        /// Draw the group box.
        /// </summary>
        /// <param name="graphics">The graphics instance.</param>
        public override void Draw(Graphics graphics)
        {
            this.DrawBorders(graphics);

            graphics.DrawText(this.Text, this.PointTopLeft.X + 5, this.PointTopLeft.Y - 1, this.FontColor, this.Font);

            // Draw all controls
            foreach (Control control in this.controls)
            {
                control.Draw(graphics);
            }
        }

        /// <summary>
        /// When position of the parent (in most cases a form) has changed, also adjust position of all controls of this groupbox.
        /// </summary>
        /// <param name="location">The new location.</param>
        public override void ParentLocationChanged(Point location)
        {
            base.ParentLocationChanged(location);

            // Let all controls know, position has changed
            foreach (Control control in this.controls)
            {
                // Adjust position by 10 so controls are closer to the top
                control.ParentLocationChanged(new Point(this.PositionOffset + new Size(this.Position.X, this.Position.Y - 10)));
            }
        }
    }
}
