using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GTA;
using LCPD_First_Response.Engine.GUI.Forms;
using Graphics = System.Drawing.Graphics;

namespace LCPD_First_Response.Engine.GUI
{
    using LCPD_First_Response.Engine.Timers;

    class FormsManager : BaseComponent, ITickable
    {
        public Mouse Mouse { get; private set; }

        /// <summary>
        /// Keyboard focus
        /// </summary>
        private Control focusKeyControl;
        private Control focusMouseControl;
        private List<Form> forms;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormsManager"/> class.
        /// </summary>
        public FormsManager()
        {
            this.Mouse = new Mouse();
            this.forms = new List<Form>();

            LCPDFR_Loader.PublicScript.PerFrameDrawing += new GTA.GraphicsEventHandler(PublicScript_PerFrameDrawing);
        }

        /// <summary>
        /// Adds the form.
        /// </summary>
        /// <param name="form">The form.</param>
        public void AddForm(Form form)
        {
            this.forms.Add(form);
        }

        void PublicScript_PerFrameDrawing(object sender, GTA.GraphicsEventArgs e)
        {
            Draw(e);
        }

        private void Draw(GTA.GraphicsEventArgs e)
        {
            e.Graphics.Scaling = FontScaling.Pixel;

            // Draw forms
            for (int i = 0; i < forms.Count; i++)
            {
                if (forms[i] != null)
                {
                    Form form = forms[i];
                    if (form.Alive)
                    {
                        if (form.Visible)
                        {
                            form.Draw(e.Graphics);
                        }
                    }
                    else
                    {
                        forms[i] = null;
                    }
                }
            }

            // Draw mouse
            if (Mouse.Enabled)
            {
                Mouse.Draw(e.Graphics);
            }
        }

        public void Process()
        {
            //if (Main.KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.NumPad2))
            //{
            //    //this.form.Opacity--;
            //    //Game.DisplayText(this.form.Opacity.ToString());
            //    if (this.forms.Count > 0)
            //    {
            //        for (int i = 0; i < forms.Count; i++)
            //        {
            //            forms[i].Dispose();
            //            forms[i] = null;
            //        }
            //        this.forms.Clear();
            //    }
            //}
            //if (Main.KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.NumPad3))
            //{
            //    if (this.forms.Count <= 0)
            //    {
            //        //this.form.Opacity++;
            //        //Game.DisplayText(this.form.Opacity.ToString());
            //        Form1Handler form1Handler = new Form1Handler();
            //        this.forms.Add(form1Handler);
            //    }
            //}
        }

        public void TriggerMouseDown(Point point)
        {
            // TODO: GetFormAt
            // Reverse loop so forms drawn last are processed first
            for (int i = this.forms.Count - 1; i >= 0; i--)
            {
                Form form = this.forms[i];
                if (form != null && form.Alive && form.Visible)
                {
                    Control c = form.GetControlAt(point);
                    if (c != this.focusKeyControl)
                    {
                        if (this.focusKeyControl != null)
                        {
                            this.focusKeyControl.OnFocusLost();
                        }
                    }
                    if (c != null && c.Visible)
                    {
                        this.focusKeyControl = c;
                        c.OnMouseDown(point);
                        break;
                    }
                }
            }
        }

        public void TriggerMouseMovement(Point point)
        {
            // TODO: GetFormAt

            foreach (Form form in forms)
            {
                if (form != null && form.Alive && form.Visible)
                {
                    Control c = form.GetControlAt(point);
                    if (c != this.focusMouseControl)
                    {
                        if (this.focusMouseControl != null)
                        {
                            this.focusMouseControl.OnMouseFocusLost();
                        }
                    }
                    if (c != null && c.Visible)
                    {
                        this.focusMouseControl = c;
                        c.OnMouseFocusGot();
                    }
                }
            }
        }

        public override string ComponentName
        {
            get { return "GUI"; }
        }
    }
}
