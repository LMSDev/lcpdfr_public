using System.Drawing;
using LCPD_First_Response.Engine.GUI;
using System;

namespace LCPD_First_Response.LCPDFR.Scripts.PoliceComputer
{
    /// <summary>
    /// Generic MessageBox class.
    /// </summary>
    internal class MessageBoxHandler : Form
    {
        public string Message { get; private set; }
        private Action<object> callback;

        /// <summary>
        /// The constructor for the MessageBox.
        /// </summary>
        /// <param name="message">The message to show. The message will be centered.</param>
        /// <param name="callback">Callback when the Message Box is dismissed or when 'OK' is clicked.</param>
        public MessageBoxHandler(string message, Action<object> callback)
        {
            this.CreateFormWindowsForm(new MessageBox());
            this.CenterOnScreen();

            this.callback = callback;
            this.Message = message;

            this.GetControlByName<Button>("button1").OnClick += MessageBoxHandler_OnClick;
            this.GetControlByName<Label>("label1").Text = message;

            this.Closed += MessageBoxHandler_Closed;
        }

        void MessageBoxHandler_Closed(object sender)
        {
            this.callback(sender);
        }

        void MessageBoxHandler_OnClick(object sender)
        {
            this.Close();
        }
    }
}
