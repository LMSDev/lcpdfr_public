using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GTA;

namespace LCPD_First_Response.Engine.GUI
{
    abstract class Control
    {
        /// <summary>
        /// MouseDown event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        public delegate void MouseDownEventHandler(object sender);

        /// <summary>
        /// The mouse down event.
        /// </summary>
        public event MouseDownEventHandler MouseDown;

        public Color BackgroundColor { get; set; }
        public virtual GTA.Font Font
        {
            get { return this.font; }
            set
            {
                if (this.font != null)
                {
                    this.font.Dispose();
                    this.font = null;
                    Log.Debug("Font disposed", "Control");
                }
                this.font = value;
            }
        }
        public Color FontColor { get; set; }
        public string Name { get; private set; }
        public virtual Point Position
        {
            get { return this.position; }
            set
            {
                if (!isForm)
                {
                    this.position = new Point(value.X, value.Y + 5);
                }
                else
                {
                    this.position = value;
                }
                RecalcSizes();
            }
        }
        public Size Size
        {
            get { return this.size; }
            set
            {
                this.size = value;
                RecalcSizes();
            }
        }
        public string Text { get; set; }
        public bool Visible { get; set; }

        protected RectangleF DrawRectangle;
        protected Point PointTopLeft;
        protected Point PointTopRight;
        protected Point PointBottomLeft;
        protected Point PointBottomRight;

        private GTA.Font font;
        private bool isForm;
        private Point position;
        private Size positionOffset;
        private Size size;

        protected Control(Point position, Size size)
        {
            this.Position = position;
            this.Size = size;

            this.Initialize();
        }

        protected Control(int x, int y, int height, int width)
        {
            this.Position = new Point(x, y);
            this.Size = new Size(width, height);

            this.Initialize();
        }

        /// <summary>
        /// Gets the position offset.
        /// </summary>
        protected Size PositionOffset
        {
            get
            {
                return this.positionOffset;
            }
        }

        private void Initialize()
        {
            // Setup defaults
            this.BackgroundColor = Color.LightGray;
            this.font = null;
            this.FontColor = Color.Black;
            this.Name = "Control";
            this.Text = "Control";
            this.Visible = true;
        }

        public virtual void Dispose()
        {
            if (this.font != null)
            {
                this.font.Dispose();
            }
            this.font = null;
        }

        public virtual void ParentLocationChanged(Point location)
        {
            this.positionOffset = new Size(location);
            this.RecalcSizes();
        }

        public virtual void RecalcSizes()
        {
            PointF tempPosition = new PointF(this.position.X + 1, this.position.Y + 1);
            SizeF tempSize = new SizeF(this.size.Width -2, this.size.Height - 2);

            this.DrawRectangle = new RectangleF(tempPosition + this.positionOffset, tempSize);
            this.PointTopLeft = new Point(this.position.X + this.positionOffset.Width, this.position.Y + this.positionOffset.Height);
            this.PointTopRight = new Point(this.position.X + this.positionOffset.Width + this.size.Width, this.position.Y + this.positionOffset.Height);
            this.PointBottomLeft = new Point(this.position.X + this.positionOffset.Width, this.position.Y + this.positionOffset.Height + this.size.Height - 1);
            this.PointBottomRight = new Point(this.position.X + this.positionOffset.Width + this.size.Width, this.position.Y + this.positionOffset.Height + this.size.Height - 1);

        }
        /// <summary>
        /// For now, we do no drawing at all in control. This may change in the future. This would then e.g. draw background color
        /// </summary>
        /// <param name="graphics"></param>
        public abstract void Draw(GTA.Graphics graphics);

        /// <summary>
        /// Copies the given control. If isForm is false, will adjust position with offsets so 0 on our form is where 0 is on windows forms (right under the caption)
        /// </summary>
        /// <param name="winControl"></param>
        /// <param name="isWinForm"></param>
        public virtual void CopyControl(System.Windows.Forms.Control winControl, bool isWinForm = false)
        {
            this.isForm = isWinForm;

            // Forms are usually a little too long and big, we adjust this here
            if (this.isForm)
            {
                winControl.Size = new Size(winControl.Size.Width - 15, winControl.Size.Height - 30);
            }

            this.Position = winControl.Location;
            this.Size = winControl.Size;
            this.BackgroundColor = winControl.BackColor;
            this.Font = CopyFont(winControl.Font);
            this.FontColor = winControl.ForeColor;
            this.Name = winControl.Name;
            this.Text = winControl.Text;
        }

        public virtual void OnFocusLost() {}

        public virtual void OnMouseDown(Point point)
        {
            if (this.MouseDown != null)
            {
                this.MouseDown(this);
            }
        }
        public virtual void OnMouseFocusGot() {}
        public virtual void OnMouseFocusLost() {}

        public void DrawBackgroundRectangle(GTA.Graphics graphics)
        {
            // Draw background
            graphics.DrawRectangle(this.DrawRectangle, this.BackgroundColor);
        }

        public void DrawBorders(GTA.Graphics graphics, bool softEdges = true)
        {
            if (softEdges)
            {
                // Draw borders. Note: We don't want a rectangle but round edges
                graphics.DrawLine(this.PointTopLeft.X + 1, this.PointTopLeft.Y, this.PointTopRight.X - 1, this.PointTopRight.Y, 1, Color.Black);
                graphics.DrawLine(this.PointTopLeft.X, this.PointTopLeft.Y + 1, this.PointBottomLeft.X, this.PointBottomLeft.Y, 1, Color.Black);
                graphics.DrawLine(this.PointBottomLeft.X + 1, this.PointBottomLeft.Y, this.PointBottomRight.X - 1, this.PointBottomRight.Y, 1, Color.Black);
                graphics.DrawLine(this.PointTopRight.X - 1, this.PointTopRight.Y + 1, this.PointBottomRight.X - 1, this.PointBottomRight.Y, 1, Color.Black);
            }
            else
            {
                // Code for rectangle around control. Doesn't look so cool because of the edges
                graphics.DrawLine(this.PointTopLeft, this.PointTopRight, 1, Color.Black);
                graphics.DrawLine(this.PointTopLeft.X, this.PointTopLeft.Y, this.PointBottomLeft.X, this.PointBottomLeft.Y + 1, 1, Color.Black);
                graphics.DrawLine(this.PointBottomLeft, this.PointBottomRight, 1, Color.Black);
                graphics.DrawLine(this.PointTopRight.X - 1, this.PointTopRight.Y, this.PointBottomRight.X - 1, this.PointBottomRight.Y + 1, 1, Color.Black);
            }
        }

        public bool IsPointWithinControl(RectangleF rectangleF)
        {
            if (this.DrawRectangle.IntersectsWith(rectangleF))
            {
                return true;
            }
            return false;
        }

        public static GTA.Font CopyFont(System.Drawing.Font font)
        {
            GTA.Font gtaFont = new GTA.Font(font.FontFamily.Name, font.Height, GTA.FontScaling.Pixel, font.Bold, font.Italic);
            // Disable effects
            gtaFont.Effect = FontEffect.None;
            return gtaFont;
        }
    }
}
