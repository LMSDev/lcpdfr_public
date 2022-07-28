namespace LCPD_First_Response.Engine.GUI
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using GTA;

    using LCPD_First_Response.Engine.Input;

    using Font = GTA.Font;
    using Graphics = GTA.Graphics;

    /// <summary>
    /// Represents a LCPDFR Form which is based on System.Windows.Forms.
    /// </summary>
    internal class Form : Control, ICanHaveControls
    {
        /// <summary>
        /// Default top bar height.
        /// </summary>
        private const int DefaultTopBarHeight = 25;

        /// <summary>
        /// Default size.
        /// </summary>
        private const int DefaultSizeHeight = 400;

        /// <summary>
        /// Default width.
        /// </summary>
        private const int DefaultSizeWidth = 400;

        /// <summary>
        /// Close button.
        /// </summary>
        private Button closeButton;

        /// <summary>
        /// Whether or not close button is drawn.
        /// </summary>
        private bool dontDrawCloseButton;

        /// <summary>
        /// Whether or not the form is drawn. Does not affect the controls.
        /// </summary>
        private bool dontDrawForm;

        /// <summary>
        /// List of all controls.
        /// </summary>
        private List<Control> controls;

        /// <summary>
        /// Form opacity.
        /// </summary>
        private int opacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="Form"/> class.
        /// </summary>
        public Form() : base(0, 0, DefaultSizeHeight, DefaultSizeWidth)
        {
            this.controls = new List<Control>();
            this.opacity = 100;
            this.Alive = true;
            this.AddCloseButton();

            // Add events
            Main.KeyWatchDog.KeyDown += this.OnKeyDown;

            // Add form to gui
            Main.FormsManager.AddForm(this);
        }

        /// <summary>
        /// Delegate when form has been closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        public delegate void ClosedEventHandler(object sender);

        /// <summary>
        /// Invoked when the form has been closed.
        /// </summary>
        public event ClosedEventHandler Closed;

        /// <summary>
        /// Gets a value indicating whether the form is still alive.
        /// </summary>
        public bool Alive { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the caption is drawn.
        /// </summary>
        public bool DontDrawCaption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the close button is drawn.
        /// </summary>
        public bool DontDrawCloseButton
        {
            get
            {
                return this.dontDrawCloseButton;
            }

            set
            {
                this.dontDrawCloseButton = value;
                this.closeButton.Visible = !this.dontDrawCloseButton;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the form (borders and background) is drawn. This does not affect the controls.
        /// </summary>
        public bool DontDrawForm
        {
            get
            {
                return this.dontDrawForm;
            }

            set
            {
                this.dontDrawForm = value;
                
                if (this.dontDrawForm)
                {
                    this.closeButton.Visible = false;
                }
                else
                {
                    if (!this.DontDrawCloseButton)
                    {
                        this.closeButton.Visible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the form borders are drawn.
        /// </summary>
        public bool DontDrawFormBorders { get; set; }

        /// <summary>
        /// Gets or sets the opacity of the form.
        /// </summary>
        public int Opacity
        {
            get
            {
                return this.opacity;
            }

            set
            {
                // Correct input
                if (value < 0)
                {
                    value = 0;
                }
                if (value > 100)
                {
                    value = 100;
                }

                this.opacity = value;

                // Calc new color
                double alpha = this.opacity * 2.5;
                this.BackgroundColor = Color.FromArgb(Convert.ToInt32(alpha), this.BackgroundColor);
            }
        }

        /// <summary>
        /// Gets or sets the position of the form.
        /// </summary>
        public new Point Position
        {
            get
            {
                return base.Position;
            }

            set
            {
                // Let all controls know, position has changed
                foreach (Control control in this.controls)
                {
                    control.ParentLocationChanged(value);
                }

                base.Position = value;
            }
        }

        /// <summary>
        /// Adds <paramref name="control"/>.
        /// </summary>
        /// <param name="control">The control.</param>
        public void AddControl(Control control)
        {
            control.ParentLocationChanged(this.Position);
            this.controls.Add(control);
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
        /// Centers the form.
        /// </summary>
        public void CenterOnScreen()
        {
            // Get resolution
            Size resolution = Gui.Resolution;

            // Calculate new positions
            int x = (resolution.Width / 2) - (this.Size.Width / 2);
            int y = (resolution.Height / 2) - (this.Size.Height / 2);

            this.Position = new Point(x, y);
        }

        /// <summary>
        /// Closes the form.
        /// </summary>
        public void Close()
        {
            // Flush controls
            this.Dispose();

            if (this.Closed != null)
            {
                this.Closed(this);
            }
        }

        /// <summary>
        /// Gets the control at <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The control or null if not found.</returns>
        public Control GetControlAt(Point point)
        {
            RectangleF cursorRectangle = new RectangleF(point, new SizeF(16, 16));

            // Check if point is in rectangle
            if (this.DrawRectangle.IntersectsWith(cursorRectangle))
            {
                foreach (Control control in this.controls)
                {
                    if (control.IsPointWithinControl(cursorRectangle))
                    {
                        return control;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Because the form has no local variables for the controls like the windows forms, we have to access controls by their name (given in windows form designer)
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="name">The name.</param>
        /// <returns>The control with the name. Null if not found.</returns>
        public T GetControlByName<T>(string name) where T : Control
        {
            foreach (Control control in this.controls)
            {
                if (control is ICanHaveControls)
                {
                    ICanHaveControls canHaveControls = control as ICanHaveControls;
                    foreach (Control subControl in canHaveControls.GetControls())
                    {
                        if (subControl.Name == name)
                        {
                            return (T)subControl;
                        }
                    }
                }

                if (control.Name == name)
                {
                    return (T)control;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes <paramref name="control"/> from the form.
        /// </summary>
        /// <param name="control">The control</param>
        public void RemoveControl(Control control)
        {
            this.controls.Remove(control);
        }

        /// <summary>
        /// Copies the System.Windows.Forms control.
        /// </summary>
        /// <param name="winControl">The control.</param>
        /// <param name="isWinForm">Whether or not the control is a form.</param>
        public override void CopyControl(System.Windows.Forms.Control winControl, bool isWinForm = false)
        {
            base.CopyControl(winControl, isWinForm);

            // Opacity is given in double, so we have to convert it
            double doubleOpacity = ((System.Windows.Forms.Form)winControl).Opacity;
            doubleOpacity *= 100;
            this.Opacity = (int)doubleOpacity;
        }

        /// <summary>
        /// Relases all resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            foreach (Control control in this.controls)
            {
                control.Dispose();
            }

            this.controls.Clear();
            this.Alive = false;
            Main.KeyWatchDog.KeyDown -= this.OnKeyDown;

            // Hack: Ensure cursor resets properly
            Main.FormsManager.Mouse.ChangeCursor(EMouseCursorType.Default);
        }

        /// <summary>
        /// Draws the form.
        /// </summary>
        /// <param name="graphics">Graphics to draw on.</param>
        public override void Draw(Graphics graphics)
        {
            // Draw caption background
            if (!this.DontDrawForm && !this.DontDrawCaption)
            {
                for (int i = 0; i < DefaultTopBarHeight; i++)
                {
                    Color captioBackground = Color.FromArgb(100, 152, 180, 208);
                    graphics.DrawLine(this.PointTopLeft.X + 1, this.PointTopLeft.Y + i + 1, this.PointTopRight.X, this.PointTopLeft.Y + i + 1, 1, captioBackground);
                }
            }

            // Draw background
            if (!this.DontDrawForm)
            {
                this.DrawBackgroundRectangle(graphics);
            }

            // Draw caption text. Note: Caption is not affected by font
            if (!this.DontDrawForm && !this.DontDrawCaption)
            {
                graphics.DrawText(this.Text, this.PointTopLeft.X + 6, this.PointTopLeft.Y + 6, this.FontColor, this.Font);
            }

            // Draw controls
            foreach (Control control in this.controls)
            {
                if (control.Visible)
                {
                    control.Draw(graphics);
                }
            }

            // Draw borders
            if (!this.DontDrawForm && !this.DontDrawFormBorders)
            {
                this.DrawBorders(graphics, false);
            }
        }

        /// <summary>
        /// Recalculates all sizes.
        /// </summary>
        public override void RecalcSizes()
        {
            // We don't call base since forms act really different. DrawRectangle is only the grey main rectangle, without border and bar at the top
            this.DrawRectangle = new RectangleF(base.Position, this.Size);

            // However, the points refer to the real size, so including borders and bar at the top
            this.PointTopLeft = new Point(base.Position.X, base.Position.Y - DefaultTopBarHeight);
            this.PointTopRight = new Point(base.Position.X + this.Size.Width, base.Position.Y - DefaultTopBarHeight);
            this.PointBottomLeft = new Point(base.Position.X, base.Position.Y + this.Size.Height);
            this.PointBottomRight = new Point(base.Position.X + this.Size.Width, base.Position.Y + this.Size.Height);

            // Update close button
            if (this.closeButton != null)
            {
                this.AddCloseButton();
            }
        }

        /// <summary>
        /// Called when a key is down.
        /// </summary>
        /// <param name="key">The key down.</param>
        /// <returns>Return false if key shouldn't be passed to other functions.</returns>
        public virtual bool OnKeyDown(System.Windows.Forms.Keys key)
        {
            return true;
        }

        /// <summary>
        /// Creates the form out of <paramref name="form"/>.
        /// </summary>
        /// <param name="form">The System.Windows.Forms.Form.</param>
        protected void CreateFormWindowsForm(System.Windows.Forms.Form form)
        {
            this.CopyControl(form, true);

            foreach (System.Windows.Forms.Control control in form.Controls)
            {
                this.CopyControl(control, this);
            }
        }

        /// <summary>
        /// Copies <paramref name="control"/>.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="controlOwner">The owner of the control.</param>
        private void CopyControl(System.Windows.Forms.Control control, ICanHaveControls controlOwner)
        {
            // Check if type is button
            if (control is System.Windows.Forms.Button)
            {
                Button button = new Button(control.Location, control.Size);
                button.CopyControl(control);

                // Add control
                controlOwner.AddControl(button);
            }

            if (control is System.Windows.Forms.GroupBox)
            {
                GroupBox groupBox = new GroupBox(control.Location, control.Size);
                groupBox.CopyControl(control);

                controlOwner.AddControl(groupBox);

                foreach (System.Windows.Forms.Control subControl in ((System.Windows.Forms.GroupBox)control).Controls)
                {
                    this.CopyControl(subControl, groupBox);
                }
            }

            if (control is System.Windows.Forms.Label)
            {
                Label label = new Label(control.Location, control.Size);
                label.CopyControl(control);

                controlOwner.AddControl(label);
            }

            if (control is System.Windows.Forms.TextBox)
            {
                TextBox textBox = new TextBox(control.Location, control.Size);
                textBox.CopyControl(control);

                controlOwner.AddControl(textBox);
            }

            if (control is System.Windows.Forms.ListBox)
            {
                ListBox groupbox = new ListBox(control.Location, control.Size);
                groupbox.CopyControl(control);

                controlOwner.AddControl(groupbox);
            }
        }

        /// <summary>
        /// Adds/Updates the close button.
        /// </summary>
        private void AddCloseButton()
        {
            if (this.closeButton != null)
            {
                this.closeButton.OnClick -= new Button.OnClickEventHandler(this.CloseButton_OnClick);
                this.closeButton.Dispose();
                this.RemoveControl(this.closeButton);
            }

            // Add close button to right upper corner
            this.closeButton = new Button(this.Size.Width - 20, -25, 16, 16)
            {
                Text = "X",
                BackgroundColor = Color.DarkRed,
                Font = new Font("Arial", 16, FontScaling.Pixel),
                FontColor = Color.White
            };
            this.closeButton.OnClick += new Button.OnClickEventHandler(this.CloseButton_OnClick);
            this.AddControl(this.closeButton);

            // Hide if already hidden
            if (this.dontDrawCloseButton)
            {
                this.closeButton.Visible = false;
            }
        }

        /// <summary>
        /// Invoked when the close button has been clicked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void CloseButton_OnClick(object sender)
        {
            this.Close();
        }

        //public static Form CreateFromWindowsForm(System.Windows.Forms.Form form)
        //{
        //    // Copy form itself
        //    Form guiForm = new Form();
        //    guiForm.CopyControl(form, true);

        //    foreach (System.Windows.Forms.Control control in form.Controls)
        //    {
        //        // Check if type is button
        //        if (control is System.Windows.Forms.Button)
        //        {
        //            Button button = new Button(control.Location, control.Size);
        //            button.CopyControl(control);

        //            // Add control
        //            guiForm.AddControl(button);
        //        }
        //        if (control is System.Windows.Forms.Label)
        //        {
        //            Label label = new Label(control.Location, control.Size);
        //            label.CopyControl(control);

        //            guiForm.AddControl(label);
        //        }
        //        if (control is System.Windows.Forms.TextBox)
        //        {
        //            TextBox textBox = new TextBox(control.Location, control.Size);
        //            textBox.CopyControl(control);

        //            guiForm.AddControl(textBox);
        //        }
        //    }
        //    return guiForm;
        //}
    }
}
