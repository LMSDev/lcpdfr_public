using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GTA;
using Graphics = GTA.Graphics;
using KeyEventArgs = GTA.KeyEventArgs;
using LCPD_First_Response.Engine.Input;

namespace LCPD_First_Response.Engine.GUI
{
    class TextBox : Control
    {
        private bool hasKeyFocus;
        private RectangleF textRectangle;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBox"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="size">
        /// The size.
        /// </param>
        public TextBox(Point position, Size size) : base(position, size)
        {
            Main.KeyWatchDog.KeyDown += new KeyWatchDog.KeyDownEventHandler(this.KeyWatchDog_KeyDown);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBox"/> class.
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
        public TextBox(int x, int y, int height, int width) : base(x, y, height, width)
        {
            Main.KeyWatchDog.KeyDown += new KeyWatchDog.KeyDownEventHandler(this.KeyWatchDog_KeyDown);
        }

        /// <summary>
        /// Gets or sets the password char to override the textbox input.
        /// </summary>
        public string PasswordChar { get; set; }

        bool KeyWatchDog_KeyDown(System.Windows.Forms.Keys key)
        {
            if (!this.hasKeyFocus) return true;
            ParseInput(key);

            return true;
        }

        private void ParseInput(Keys key)
        {
            bool shift = (System.Windows.Forms.Control.ModifierKeys == Keys.Shift);
            if (!shift) shift = System.Windows.Forms.Control.IsKeyLocked(Keys.CapsLock);

            int keyCode = (int) key;
            string textToAdd = "";
            // Numbers
            if (keyCode >= 48 && keyCode <= 57)
            {
                textToAdd = ((Char) keyCode).ToString();
            }
            // Numpad
            if (keyCode >= 96 && keyCode <= 105)
            {
                textToAdd = ((Char) keyCode - 48).ToString();
            }
            // Lower key
            if (!shift)
            {
                if (keyCode >= 65 && keyCode <= 90)
                {
                    textToAdd = key.ToString().ToLower();
                }
            }
            else
            {
                if (keyCode >= 65 && keyCode <= 90)
                {
                    textToAdd = key.ToString().ToUpper();
                }
            }

            if (key == Keys.Space || key == Keys.Tab)
            {
                textToAdd = " ";
            }
            if (key == Keys.Back)
            {
                if (this.Text.Length > 0)
                {
                    this.Text = this.Text.Substring(0, this.Text.Length - 1);
                }
            }

            this.Text += textToAdd;
        }

        public override void RecalcSizes()
        {
            base.RecalcSizes();

            // Text should have a little offset from the left
            this.textRectangle = new RectangleF(new PointF(base.PointTopLeft.X + 3, base.PointTopLeft.Y),
                                                new SizeF(base.Size.Width - 5, base.Size.Height));
        }

        public override void Draw(Graphics graphics)
        {
            base.DrawBackgroundRectangle(graphics);
            // Draw text in the middle
            string text = base.Text;

            // Replace text with password char, if any
            if (this.PasswordChar != null)
            {
                int length = text.Length;
                text = string.Empty;
                for (int i = 0; i < length; i++)
                {
                    text += this.PasswordChar;
                }
            }

            if (this.hasKeyFocus)
            {
                text += "_";
            }
            graphics.DrawText(text, this.textRectangle, TextAlignment.VerticalCenter, base.FontColor, base.Font);
            base.DrawBorders(graphics);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (this.hasKeyFocus)
            {
                Gui.TextInputActive = false;
            }
        }

        public override void OnFocusLost()
        {
            base.OnFocusLost();

            this.hasKeyFocus = false;
            Gui.TextInputActive = false;
            Main.FormsManager.Mouse.ChangeCursor(EMouseCursorType.Default);
        }

        public override void OnMouseDown(Point point)
        {
            base.OnMouseDown(point);
            this.hasKeyFocus = true;

            // Enable text input mode
            Gui.TextInputActive = true;
        }

        public override void OnMouseFocusGot()
        {
            base.OnMouseFocusGot();

            Main.FormsManager.Mouse.ChangeCursor(EMouseCursorType.IBeam);
        }

        public override void OnMouseFocusLost()
        {
            base.OnMouseFocusLost();

            Main.FormsManager.Mouse.ChangeCursor(EMouseCursorType.Default);
        }
    }
}
