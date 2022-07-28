using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LCPD_First_Response.Engine.Input;

namespace LCPD_First_Response.Engine.GUI
{
    using LCPD_First_Response.Engine.Timers;

    class Mouse : BaseComponent, ITickable
    {
        public bool Enabled
        {
            get { return this.enabled; }
            set 
            { 
                GTA.Game.LocalPlayer.CanControlCharacter = !value;
                this.enabled = value;
            }
        }
        public int X
        {
            get { return Cursor.Position.X - 16; }
        }
        public int Y
        {
            get { return Cursor.Position.Y - 16; }
        }

        private EMouseCursorType cursorType;
        private bool enabled;
        private GTA.Texture texCursor;
        private GTA.Texture texCursorDefault;
        private GTA.Texture texCursorHand;
        private GTA.Texture texCursorIBeam;

        public Mouse()
        {
            this.texCursorDefault = GetCursorTexture(Cursors.Default);
            this.texCursorHand = GetCursorTexture(Cursors.Hand);
            this.texCursorIBeam = GetCursorTexture(Cursors.IBeam);
            this.cursorType = EMouseCursorType.Default;
            this.texCursor = this.texCursorDefault;

            Main.KeyWatchDog.KeyDown += new KeyWatchDog.KeyDownEventHandler(KeyWatchDog_KeyDown);
        }

        bool KeyWatchDog_KeyDown(Keys key)
        {
            //if (key == Keys.NumPad0)
            //{
            //    this.Enabled = !this.Enabled;
            //}
            //if (key == Keys.NumPad1)
            //{
            //    if (this.cursorType == EMouseCursorType.Default)
            //    {
            //        this.texCursor = texCursorIBeam;
            //        this.cursorType = EMouseCursorType.IBeam;
            //    }
            //    else
            //    {
            //        if (this.cursorType == EMouseCursorType.IBeam)
            //        {
            //            this.texCursor = texCursorDefault;
            //            this.cursorType = EMouseCursorType.Default;
            //        }
            //    }
            //}
            if (key == Keys.LButton)
            {
                if (this.Enabled)
                {
                    Main.FormsManager.TriggerMouseDown(new Point(this.X, this.Y));
                }
            }
            return true;
        }

        public void ChangeCursor(EMouseCursorType mouseCursorType)
        {
            if (mouseCursorType == EMouseCursorType.Default)
            {
                this.cursorType = mouseCursorType;
                this.texCursor = this.texCursorDefault;
            }
            if (mouseCursorType == EMouseCursorType.Hand)
            {
                this.cursorType = mouseCursorType;
                this.texCursor = this.texCursorHand;
            }
            if (mouseCursorType == EMouseCursorType.IBeam)
            {
                this.cursorType = mouseCursorType;
                this.texCursor = this.texCursorIBeam;
            }
        }

        public void Draw(GTA.Graphics graphics)
        {
            if (this.Enabled)
            {
                graphics.DrawSprite(this.texCursor, Cursor.Position.X, Cursor.Position.Y, Cursor.Current.Size.Width, Cursor.Current.Size.Height, 0, Color.White);
            }
        }

        private GTA.Texture GetCursorTexture(Cursor cursor)
        {
            Bitmap b = new Bitmap(cursor.Size.Width, cursor.Size.Height);
            Graphics graphics = Graphics.FromImage(b);
            cursor.Draw(graphics, new Rectangle(0, 0, cursor.Size.Width, cursor.Size.Height));
            graphics.Dispose();

            GTA.Texture texture;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                b.Save(ms, ImageFormat.Bmp);
                texture = new GTA.Texture(ms.ToArray());
            }
            b.Dispose();
            return texture;
        }

        public void Process()
        {
            if (this.Enabled)
            {
                // Process mousemovement
                Main.FormsManager.TriggerMouseMovement(new Point(this.X, this.Y));
            }
        }

        public override string ComponentName
        {
            get { return "Mouse"; }
        }
    }

    enum EMouseCursorType
    {
        Default,
        Hand,
        IBeam,
    }
}
