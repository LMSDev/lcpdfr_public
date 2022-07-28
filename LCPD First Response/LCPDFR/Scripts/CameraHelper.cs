namespace LCPD_First_Response.LCPDFR.Scripts
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    /// <summary>
    /// Camera class for advanced (mostly timed) camera controls.
    /// </summary>
    internal class CameraHelper
    {
        /// <summary>
        /// Makes the game camera focus on <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="fade">Whether the cameras should nicely fade between each others position.</param>
        /// <param name="fadeTime">The time to fade. The shorter the sharper the fade is.</param>
        /// <param name="focusTime">The time to focus the ped.</param>
        public static void FocusGameCamOnPed(CPed ped, bool fade, int fadeTime, int focusTime)
        {
            // Make camera look at ped
            GTA.Camera camera = new GTA.Camera();
            camera.Position = Game.CurrentCamera.Position;
            camera.Rotation = Game.CurrentCamera.Rotation;
            camera.FOV = Game.CurrentCamera.FOV;
            Vector3 offset = Game.CurrentCamera.Position - CPlayer.LocalPlayer.Ped.Position;
            Natives.AttachCamToPed(camera, CPlayer.LocalPlayer.Ped);
            Natives.SetCamAttachOffset(camera, offset);
            Natives.SetInterpFromGameToScript(fade, fadeTime);
            camera.Activate();
            camera.LookAt(ped);

            DelayedCaller.Call(
                delegate
                {
                    Natives.SetInterpFromScriptToGame(fade, fadeTime);
                    camera.Deactivate();
                },
                focusTime);
        }

        /// <summary>
        /// Performs an camera focus on a given event given by the <paramref name="eventPed"/> for <paramref name="focusTime"/>.
        /// </summary>
        /// <param name="eventPed">The ped.</param>
        /// <param name="fade">Whether the cameras should nicely fade between each others position.</param>
        /// <param name="fadeTime">The time to fade. The shorter the sharper the fade is.</param>
        /// <param name="focusTime">The time to focus the ped.</param>
        /// <param name="onlyIfSpotted">When set to true, the player has to spot the ped before the camera focus is executed.</param>
        /// <param name="ignoreGlobals">If set to true, the global flag whether the helpbox has been already displayed (<seealso cref="Globals.HasHelpboxDisplayedWorldEvents"/>) is ignored.</param>
        /// <param name="displayHelp">If true, a helpbox will be displayed (if <seealso cref="Globals.HasHelpboxDisplayedWorldEvents"/> is false).</param>
        /// <returns></returns>
        public static bool PerformEventFocus(CPed eventPed, bool fade, int fadeTime, int focusTime, bool onlyIfSpotted, bool ignoreGlobals, bool displayHelp)
        {
            if (!Settings.DisableCameraFocusOnWorldEvents && !LCPDFRPlayer.LocalPlayer.IsBusy && (CPlayer.LocalPlayer.Ped.HasSpottedCharInFront(eventPed) || !onlyIfSpotted))
            {
                // If on foot, get really close
                if ((!CPlayer.LocalPlayer.Ped.IsInVehicle && CPlayer.LocalPlayer.Ped.Position.DistanceTo(eventPed.Position) < 12 && !CPlayer.LocalPlayer.Ped.IsInCombat && !CPlayer.LocalPlayer.Ped.IsAiming) ||
        (CPlayer.LocalPlayer.Ped.IsInVehicle && CPlayer.LocalPlayer.Ped.Position.DistanceTo(eventPed.Position) < 25 && CPlayer.LocalPlayer.Ped.Speed < 5.0f))
                {
                    if (eventPed.IsOnScreen)
                    {
                        CameraHelper.FocusGameCamOnPed(eventPed, true, 1000, 3500);

                        if ((!Globals.HasHelpboxDisplayedWorldEvents || ignoreGlobals) && displayHelp)
                        {
                            Globals.HasHelpboxDisplayedWorldEvents = true;
                            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("SCENARIO_WORLD_EVENT")); }, null, 750);
                        }

                        return true;
                    }
                }
            }

            return false;
        }
    }
}