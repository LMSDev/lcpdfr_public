using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using LCPD_First_Response.Engine.GUI;
using LCPD_First_Response.Engine;
using LCPD_First_Response.Engine.Networking;
using LCPD_First_Response.Engine.Timers;
using LCPD_First_Response.LCPDFR.GUI;
using LCPD_First_Response.Engine.Scripting.Entities;

namespace LCPD_First_Response.LCPDFR
{
    internal class UpdateCheck
    {
        internal UpdateAvailableFormHandler UpdateForm;
        internal Version CurrentVersion;
        internal string CurrentVersionString;
        public void Initalize()
        {
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersionString = String.Format("{0}.{1}.{2}.{3}", CurrentVersion.Major, CurrentVersion.Minor, CurrentVersion.Build, CurrentVersion.Revision);
            if (Engine.Main.LCPDFRServer.ConnectionState != ENetworkConnectionState.Connected) return;
            // Spawn thread
            var thread = new Thread(DoUpdateCheck);
            thread.IsBackground = true;
            thread.Start();
        }
        public void DoUpdateCheck()
        {
            Log.Info("DoUpdateCheck: Checking for updates", "UpdateCheck");
            string latestVersion = Engine.Main.LCPDFRServer.GetLatestVersionString(
                CurrentVersionString,
                Engine.Authentication.IsTesterBuild,
                Engine.Main.DEBUG_MODE);
            if (CurrentVersionString != latestVersion && latestVersion != "")
            {
                DelayedCaller.Call(delegate { ShowUpdateForm(latestVersion); }, this, 1);
            }
            else
            {
                Log.Info("DoUpdateCheck: No updates available", "UpdateCheck");
            }
        }
        public void ShowUpdateForm(string latestVersion)
        {
            Log.Info("ShowUpdateForm: Update is available. Current: " + CurrentVersionString + ", Latest: " + latestVersion, "UpdateCheck");
            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = true;
            CPlayer.LocalPlayer.CanControlCharacter = false;
            CPlayer.LocalPlayer.IgnoredByEveryone = true;
            CPlayer.LocalPlayer.IgnoredByAI = true;
            UpdateForm = new UpdateAvailableFormHandler();
            UpdateForm.ProvideVersion(latestVersion);
            Engine.Main.FormsManager.Mouse.Enabled = true;
            UpdateForm.Visible = true;
            UpdateForm.Closed += CloseUpdateForm;
        }

        public void CloseUpdateForm(object sender)
        {
            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = false;
            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.IgnoredByEveryone = false;
            CPlayer.LocalPlayer.IgnoredByAI = false;
            if (UpdateForm != null)
            {
                UpdateForm.Closed -= CloseUpdateForm;
                UpdateForm.Dispose();
            }
            Engine.Main.FormsManager.Mouse.Enabled = false;
        }
    }
}
