namespace LCPD_First_Response.LCPDFR.Scripts.QuickActionMenu
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Timers;

    using Font = GTA.Font;
    using Vector2 = SlimDX.Vector2;

    class QuickActionMenuGroupStyle : MenuRendererBase
    {
        private const int BottomMargin = 140;

        /// <summary>
        /// The default height of an item.
        /// </summary>
        private int itemHeight = 40;

        /// <summary>
        /// The default width of an item.
        /// </summary>
        private int itemWidth = 350;

        /// <summary>
        /// The default spacing between items.
        /// </summary>
        private int itemSpacing = 10;

        /// <summary>
        /// The current Y value controlled by controller movement.
        /// </summary>
        private float controllerOffsetY = 0;

        public override QuickActionMenuItemBase OnProcess(bool downFirstTime, bool controllerUsed)
        {
            if (this.CurrentGroup.VisibleItems.Count == 0) return null;

            // Reset cursor to vertical screen center.
            if (downFirstTime)
            {
                int yPos = (int)this.CurrentGroup.VisibleItems.First().Position.Y;
                Cursor.Position = new Point(Cursor.Position.X, yPos);
                this.controllerOffsetY = yPos;
            }

            // Locate mouse.
            Vector2 position = new Vector2(0, 0);
            if (!controllerUsed)
            {
                position = new Vector2(Cursor.Position.X, Cursor.Position.Y);
            }
            else
            {
                GTA.Vector2 firstItemPosition = this.CurrentGroup.VisibleItems.First().Position;

                position = new Vector2((int)firstItemPosition.X, controllerOffsetY);
                int controllerY = Engine.Input.Controller.RightThumbStickY;
                int positiveY = Common.EnsureValueIsPositive(controllerY);
                int deadZone = 10000;

                // Has to be at least 10000 to leave the dead zone.
                if (positiveY > deadZone)
                {
                    // Get direction.
                    int change = positiveY / 3000;
                    if (controllerY < 0)
                    {
                        // Move down.
                        position = new Vector2(position.X, position.Y + change);

                    }
                    else if (controllerY > 0)
                    {
                        // Move up.
                        position = new Vector2(position.X, position.Y - change);
                    }

                    // Limit position.
                    if (position.Y >= firstItemPosition.Y && position.Y <= Game.Resolution.Height)
                    {
                        controllerOffsetY = position.Y;
                    }
                }
            }

            QuickActionMenuItemBase item = this.GetItemForPosition(position, this.itemHeight + this.itemSpacing);
            return item;
        }

        public override Font OnCreateFont()
        {
            Font f = new Font(25f, FontScaling.Pixel, true, false);
            f.Effect = FontEffect.None;
            return f;
        }

        public override void OnDraw(GraphicsEventArgs e)
        {
            // TODO: Sam please refactor this. Consider moving the rendering into a separate call and removing the hacky x/y stuff or use proper commenting. Thanks.

            // Draw group.
            Color color = Color.Gray;

            color = this.CurrentGroup.HighlightColour;
            int height = 30;
            int width = 140;

            RectangleF radar = e.Graphics.GetRadarRectangle(FontScaling.Pixel);
            float radarOffset = (Game.Resolution.Width - radar.X) - (width / 2);

            float yPadding = Game.Resolution.Height / 50;
            e.Graphics.DrawRectangle(radarOffset, radar.Y + radar.Height + yPadding, 5, 5, Color.Green);

            float xx = radarOffset;
            float yy = radar.Y + radar.Height + yPadding;

            float x = xx;
            float y = yy;


            float xModifier = 2;
            xModifier = 3.4f;

            x -= width / xModifier;
            y -= height / 2;

            // Render item
            e.Graphics.DrawRectangle(xx, yy, width, height, Color.FromArgb(180, Color.Black));

            int textAlpha = 225;

            e.Graphics.DrawText(this.CurrentGroup.Name.ToString().ToUpper(),
                    new RectangleF(x + width / 17f, y, width, height),
                    TextAlignment.Left | TextAlignment.VerticalCenter, Color.FromArgb(textAlpha, 230, 230, 230),
                    this.DefaultFont);

            e.Graphics.DrawRectangle(x - width / 12f, y + height / 2, 18, 18,
                Color.FromArgb(120, color.R, color.G, color.B));

            // Draw items.
            foreach (QuickActionMenuItemBase quickActionMenuItem in this.CurrentGroup.VisibleItems)
            {
                x = quickActionMenuItem.Position.X;
                y = quickActionMenuItem.Position.Y;
                xModifier = 2.4f;

                x -= this.itemWidth / xModifier;
                y -= this.itemHeight / 2;

                if (quickActionMenuItem == this.SelectedItem)
                {
                    if (this.DrawSelection)
                    {
                        color = quickActionMenuItem.Group.SelectedColour;
                    }
                    else
                    {
                        color = quickActionMenuItem.Group.HighlightColour;
                    }
                }
                else
                {
                    // Don't draw other items when selection should be drawn
                    if (this.DrawSelection)
                    {
                        continue;
                    }

                    color = quickActionMenuItem.Group.BackColour;
                }

                // Render item
                e.Graphics.DrawRectangle(quickActionMenuItem.Position.X, quickActionMenuItem.Position.Y, this.itemWidth, this.itemHeight, Color.FromArgb(180, color.R, color.G, color.B));

                // Border
                Point topLeft = new Point((int)quickActionMenuItem.Position.X - (this.itemWidth / 2), (int)quickActionMenuItem.Position.Y - (this.itemHeight / 2));
                Point topRight = new Point((int)quickActionMenuItem.Position.X + (this.itemWidth / 2) + 2, (int)quickActionMenuItem.Position.Y - (this.itemHeight / 2));
                Point bottomLeft = new Point((int)quickActionMenuItem.Position.X - (this.itemWidth / 2), (int)quickActionMenuItem.Position.Y + (this.itemHeight / 2));
                Point bottomRight = new Point((int)quickActionMenuItem.Position.X + (this.itemWidth / 2) + 2, (int)quickActionMenuItem.Position.Y + (this.itemHeight / 2));

                e.Graphics.DrawLine(topLeft, topRight, 2, Color.Black);
                e.Graphics.DrawLine(topLeft, bottomLeft, 2, Color.Black);
                e.Graphics.DrawLine(bottomLeft, bottomRight, 2, Color.Black);
                e.Graphics.DrawLine(bottomRight, topRight, 2, Color.Black);

                color = quickActionMenuItem.Group.HighlightColour;

                textAlpha = 180;
                if (quickActionMenuItem == this.SelectedItem) textAlpha = 225;

                e.Graphics.DrawText(quickActionMenuItem.Name,
                    new RectangleF(x + this.itemWidth / 40f, y, this.itemWidth, this.itemHeight),
                    TextAlignment.Left | TextAlignment.VerticalCenter, Color.FromArgb(textAlpha, 230, 230, 230),
                    this.DefaultFont);

                e.Graphics.DrawRectangle(x - this.itemWidth / 27.5f, y + this.itemHeight / 2, 18, 18,
                    Color.FromArgb(120, color.R, color.G, color.B));
            }

            // Draw options.
            if (this.CurrentGroup.HasOptions)
            {
                if (this.CurrentGroup.CurrentOption != null)
                {
                    GTA.Vector2 lastItemPos = this.CurrentGroup.VisibleItems.Last().Position;
                    x = lastItemPos.X;
                    y = lastItemPos.Y + itemHeight * 1.5f + itemSpacing;

                    color = this.CurrentGroup.CurrentOption.Color;
                    e.Graphics.DrawRectangle(x, y, this.itemWidth, this.itemHeight, Color.FromArgb(120, color.R, color.G, color.B));

                    // Border
                    Point topLeft = new Point((int)x - (this.itemWidth / 2), (int)y - (this.itemHeight / 2));
                    Point topRight = new Point((int)x + (this.itemWidth / 2) + 2, (int)y - (this.itemHeight / 2));
                    Point bottomLeft = new Point((int)x - (this.itemWidth / 2), (int)y + (this.itemHeight / 2));
                    Point bottomRight = new Point((int)x + (this.itemWidth / 2) + 2, (int)y + (this.itemHeight / 2));

                    e.Graphics.DrawLine(topLeft, topRight, 2, Color.Black);
                    e.Graphics.DrawLine(topLeft, bottomLeft, 2, Color.Black);
                    e.Graphics.DrawLine(bottomLeft, bottomRight, 2, Color.Black);
                    e.Graphics.DrawLine(bottomRight, topRight, 2, Color.Black);

                    xModifier = 2.4f;

                    x -= this.itemWidth / xModifier;
                    y -= this.itemHeight / 2;

                    e.Graphics.DrawText(this.CurrentGroup.CurrentOption.Name,
                        new RectangleF(x + this.itemWidth / 40f, y, this.itemWidth, this.itemHeight),
                        TextAlignment.Left | TextAlignment.VerticalCenter, Color.FromArgb(255, 230, 230, 230),
                        this.DefaultFont);

                    e.Graphics.DrawRectangle(x - this.itemWidth / 27.5f, y + this.itemHeight / 2, 18, 18,
                        Color.FromArgb(120, color.R, color.G, color.B));
                }
            }
        }

        public override void OnGroupChanged(QuickActionMenuGroup @group)
        {
            this.UpdatePositions();

            // Reset cursor to vertical screen center.
            if (@group.VisibleItems.Count > 0)
            {
                int yPos = (int)this.CurrentGroup.VisibleItems.First().Position.Y;
                Cursor.Position = new Point(Cursor.Position.X, yPos);
                this.controllerOffsetY = yPos;
            }
        }

        public override void OnItemAdded(QuickActionMenuItemBase item)
        {
            this.UpdatePositions();
        }

        public override void OnItemSelected(QuickActionMenuItemBase item)
        {
            this.DrawSelection = true;
            DelayedCaller.Call(delegate { this.DrawSelection = false; }, this, 125);
        }
        public override void OnItemsVisibilityChanged()
        {
            this.UpdatePositions();
        }

        private QuickActionMenuItemBase GetItemForPosition(Vector2 position, float range)
        {
            float distance = float.MaxValue;
            QuickActionMenuItemBase closest = null;

            // Return item angle is closest too
            foreach (QuickActionMenuItemBase quickActionMenuItem in this.CurrentGroup.VisibleItems.Where(item => item.CanBeSelected))
            {
                float dist = position.Y - quickActionMenuItem.Position.Y;
                dist = Common.EnsureValueIsPositive(dist);

                if (dist < distance && dist < range)
                {
                    distance = dist;
                    closest = quickActionMenuItem;
                }
            }

            return closest;
        }

        private void UpdatePositions()
        {
            int totalItems = this.CurrentGroup.VisibleItems.Count;
            int startPosition = (Game.Resolution.Height - BottomMargin) - (totalItems * (itemHeight + itemSpacing));

            // Get screen center.
            Vector2 center = new Vector2(Game.Resolution.Width / 2.0f, Game.Resolution.Height / 2.0f);

            // Only render items from current group.
            foreach (QuickActionMenuItemBase item in this.CurrentGroup.VisibleItems)
            {
                item.UpdatePosition(new GTA.Vector2(center.X + (center.X / 1.575f), startPosition));
                startPosition += (itemHeight + itemSpacing);
            }
        }
    }
}