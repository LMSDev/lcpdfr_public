using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GTA;
using Graphics = GTA.Graphics;

namespace LCPD_First_Response.Engine.GUI
{
    internal class ListBox : Control
    {
        public delegate void OnClickEventHandler(object sender);
        public event OnClickEventHandler OnClick;
        public List<String> Items = new List<string>();
        public int SelectionIndex = 0;
        public GTA.Font ItemFont;
        private int blockHeight = 20;
        private int maxItemsInDraw;
        private int SelectedIndex = -1;
        private int LastDrawnItemsCount;


        public ListBox(Point position, Size size) : base(position, size)
        {
            maxItemsInDraw = Convert.ToInt32(Math.Floor(((decimal)size.Height / blockHeight)));
            Log.Debug("ListBox: we can fit " + maxItemsInDraw + " in this box.", this);
        }

        public ListBox(int x, int y, int height, int width) : base(x, y, height, width)
        {
            maxItemsInDraw = Convert.ToInt32(Math.Floor(((decimal)height / blockHeight)));
            Log.Debug("ListBox: we can fit " + maxItemsInDraw + " in this box.", this);
        }

        public override void Draw(Graphics graphics)
        {
            base.DrawBackgroundRectangle(graphics);
            float curY = 0;
            int drawnItems = 0;
            int actualSelectionIdx = 0;
            if (SelectionIndex >= maxItemsInDraw)
            {
                actualSelectionIdx = SelectionIndex - maxItemsInDraw;
            }
            else
            {
                actualSelectionIdx = 0;
            }
            for (int x = actualSelectionIdx; x < Items.Count; x++)
            {
                if (drawnItems == maxItemsInDraw - 1) break; //If we're exceeding the amount of items, break the loop and don't draw any more.
                //Create the rectangle for this listbox entry.
                RectangleF metaRectangle = new RectangleF(base.DrawRectangle.X, base.DrawRectangle.Y + curY, base.DrawRectangle.Width, blockHeight);
                graphics.DrawRectangle(metaRectangle, SelectedIndex == x ? Color.Black : Color.White);
                //Draw the text of the entry
                graphics.DrawText(Items[x], metaRectangle, TextAlignment.VerticalCenter | TextAlignment.Left, SelectedIndex == x ? Color.White : Color.Black, ItemFont == null ? base.Font : ItemFont);
                drawnItems++;
                curY = curY + blockHeight;
            }
            base.DrawBorders(graphics);
        }

        public override void OnMouseDown(Point point)
        {
            base.OnMouseDown(point);

            if (OnClick != null)
            {
                OnClick(this);
            }

            //Find out which listbox entry was clicked and highlight it for the user.

            float curY = 0;
            int drawnItems = 0;
            int actualSelectionIdx = 0;
            if (SelectionIndex >= maxItemsInDraw)
            {
                actualSelectionIdx = SelectionIndex - maxItemsInDraw;
            }
            else
            {
                actualSelectionIdx = 0;
            }
            for (int x = actualSelectionIdx; x < Items.Count; x++)
            {
                if (drawnItems == maxItemsInDraw - 1) break;
                RectangleF metaRectangle = new RectangleF(base.DrawRectangle.X, base.DrawRectangle.Y + curY, base.DrawRectangle.Width, blockHeight);
                if (point.Y >= base.DrawRectangle.Y + curY && point.Y <= base.DrawRectangle.Y + curY + blockHeight)
                    SelectedIndex = x;
                drawnItems++;
                curY = curY + blockHeight;
            }
            
        }

       

    }
}
