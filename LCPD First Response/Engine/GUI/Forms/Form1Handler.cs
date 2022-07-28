using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCPD_First_Response.Engine.GUI.Forms
{
    class Form1Handler : Form
    {
        public Form1Handler()
        {
            // Create form from windows forms 'Form1'
            this.CreateFormWindowsForm(new Form1());

            // Hook up events
            this.GetControlByName<Button>("button2").OnClick += new Button.OnClickEventHandler(Form1Handler_OnClick);
        }

        void Form1Handler_OnClick(object sender)
        {
            Button button = sender as Button;
            button.Text = " Clicked";
        }
    }
}
