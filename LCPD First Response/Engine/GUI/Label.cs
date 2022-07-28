using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Graphics = GTA.Graphics;

namespace LCPD_First_Response.Engine.GUI
{
    class Label : Control
    {
        public GTA.TextAlignment TextAlign { get; set; }
        public Label(Point position, Size size) : base(position, size)
        {
        }

        public Label(int x, int y, int height, int width) : base(x, y, height, width)
        {
        }

        public override void Draw(Graphics graphics)
        {
            // Draw text in the middle
            graphics.DrawText(base.Text, base.DrawRectangle, this.TextAlign, base.FontColor, base.Font);
        }

        public override void CopyControl(System.Windows.Forms.Control winControl, bool isWinForm = false)
        {
            var labelControl = (System.Windows.Forms.Label)winControl;
            switch (labelControl.TextAlign)
            {
                case ContentAlignment.BottomCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.TopCenter:
                    this.TextAlign = GTA.TextAlignment.Center;
                    break;
                case ContentAlignment.BottomRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.TopRight:
                    this.TextAlign = GTA.TextAlignment.Right;
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.TopLeft:
                default:
                    this.TextAlign = GTA.TextAlignment.Left;
                    break;
            }
            base.CopyControl(winControl, isWinForm);
        }
    }
}
