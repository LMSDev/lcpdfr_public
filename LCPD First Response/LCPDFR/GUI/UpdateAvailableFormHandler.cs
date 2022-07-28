
using System.Collections.Generic;
using System.Drawing;

using LCPD_First_Response.Engine.GUI;
using LCPD_First_Response.Engine.Timers;
using LCPD_First_Response.LCPDFR.Input;
using System.Diagnostics;

namespace LCPD_First_Response.LCPDFR.GUI
{
    internal class UpdateAvailableFormHandler : Form
    {
        /// <summary>
        /// Initalize the update available form and add an event handler to our buttons.
        /// </summary>
        public UpdateAvailableFormHandler()
        {
            this.CreateFormWindowsForm(new UpdateAvailableForm());
            this.GetControlByName<Button>("button1").OnClick += DoUpdate_OnClick;
            this.GetControlByName<Button>("button2").OnClick += Dismiss_OnClick;
            this.DontDrawCloseButton = true;
            this.CenterOnScreen();
        }
        /// <summary>
        /// Populate the form with the current version.
        /// </summary>
        /// <param name="latestVersion">The latest version as provided by the update server</param>
        public void ProvideVersion(string latestVersion)
        {
            this.GetControlByName<Label>("label2").Text = "New version: " + latestVersion;
        }
        /// <summary>
        /// Handle when the user wishes to exit and update the game.
        /// </summary>
        /// <param name="sender"></param>
        void DoUpdate_OnClick(object sender)
        {
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.FileName = "http://www.lcpdfr.com";
            proc.Start();
            System.Environment.Exit(0);
        }
        /// <summary>
        /// Handle when the user wishes to just close the form and continue.
        /// </summary>
        /// <param name="sender"></param>
        void Dismiss_OnClick(object sender)
        {
            this.Close();
        }
    }
}
