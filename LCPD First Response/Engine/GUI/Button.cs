using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GTA;
using Graphics = GTA.Graphics;

namespace LCPD_First_Response.Engine.GUI
{
    class Button : Control
    {
        public delegate void OnClickEventHandler(object sender);
        public event OnClickEventHandler OnClick;

        public Button(Point position, Size size) : base(position, size)
        {

        }

        public Button(int x, int y, int height, int width) : base(x, y, height, width)
        {
        }

        public override void Draw(Graphics graphics)
        {
            base.DrawBackgroundRectangle(graphics);
            // Draw text in the middle
            graphics.DrawText(base.Text, base.DrawRectangle, TextAlignment.Center | TextAlignment.VerticalCenter, base.FontColor, base.Font);
            base.DrawBorders(graphics);
        }

        public override void OnMouseDown(Point point)
        {
            base.OnMouseDown(point);

            if (OnClick != null)
            {
                OnClick(this);
            }
        }

        public override void OnMouseFocusGot()
        {
            base.OnMouseFocusGot();

            Main.FormsManager.Mouse.ChangeCursor(EMouseCursorType.Hand);
        }

        public override void OnMouseFocusLost()
        {
            base.OnMouseFocusLost();

            Main.FormsManager.Mouse.ChangeCursor(EMouseCursorType.Default);
        }
    }
}
