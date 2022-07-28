namespace LCPD_First_Response.LCPDFR.Networking
{
    using System;
    using System.Threading;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Main networking class in the LCPDFR layer.
    /// </summary>
    internal class Networking
    {
        /// <summary>
        /// The main window handle
        /// </summary>
        private IntPtr handle;

        /// <summary>
        /// Whether we have blocked the input for the non-focused window
        /// </summary>
        private bool hasBlockedInput;

        /// <summary>
        /// Initializes a new instance of the <see cref="Networking"/> class.
        /// </summary>
        public Networking()
        {
            this.handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            EventJoinedNetworkGame.EventRaised += this.EventJoinedNetworkGame_EventRaised;
        }

        /// <summary>
        /// Gets a value indicating whether the main window has focus.
        /// </summary>
        /// <returns></returns>
        public bool HasFocus
        {
            get
            {
                return GetForegroundWindow() == this.handle;
            }
        }

        /// <summary>
        /// Processes LCPDFR's network logic.
        /// </summary>
        public void Process()
        {
            while (true)
            {
                // Ensure that game continues running when not focused to prevent connection loss
                if (!this.HasFocus)
                {
                    if (!AdvancedHookManaged.AGame.GetIsDeviceNotBlockedOnLostFocus())
                    {
                        AdvancedHookManaged.AGame.SetDeviceNotBlockedOnLostFocus(true);
                        Log.Debug("Process: Set game to run in non-focused mode", this);
                    }

                    if (!this.hasBlockedInput)
                    {
                        DelayedCaller.Call(
                            delegate
                                {     
                                    LCPDFRPlayer.LocalPlayer.IgnoredByAI = true;
                                    LCPDFRPlayer.LocalPlayer.IgnoredByEveryone = true;

                                    // To immediately force blackscreen use GTA.Native.Function.Call("DO_SCREEN_FADE_OUT_UNHACKED", 1); which does not have a minimum amount of fading time
                                    GTA.Game.SendChatMessage("is now tabbed-out");
                                    GTA.Game.FadeScreenOut(1, true);
                            },
                            this,
                            1);

                        this.hasBlockedInput = true;
                        Log.Debug("Process: Blocked input while not focused", this);
                    }
                }
                else
                {
                    if (AdvancedHookManaged.AGame.GetIsDeviceNotBlockedOnLostFocus())
                    {
                        AdvancedHookManaged.AGame.SetDeviceNotBlockedOnLostFocus(false);
                        Log.Debug("Process: Restored focused mode", this);
                    }


                    if (this.hasBlockedInput)
                    {
                        DelayedCaller.Call(
                            delegate
                            {
                                LCPDFRPlayer.LocalPlayer.IgnoredByAI = false;
                                LCPDFRPlayer.LocalPlayer.IgnoredByEveryone = false;
                                GTA.Game.SendChatMessage("is back in-game");
                                GTA.Game.FadeScreenIn(1, true);
                            },
                            this,
                            1);

                        this.hasBlockedInput = false;
                        Log.Debug("Process: Restored input", this);

                    }
                }

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Called when the player has joined a network game.
        /// </summary>
        /// <param name="event">The event arguments.</param>
        private void EventJoinedNetworkGame_EventRaised(EventJoinedNetworkGame @event)
        {
            if (Main.NetworkManager.IsNetworkSession)
            {
                // Set port.
                if (Main.NetworkManager.IsHost)
                {
                    Main.NetworkManager.Port = Settings.MultiplayerHostPort;
                    Main.NetworkManager.StartServer(Main.NetworkManager.LocalPlayerName, Main.NetworkManager.Port, null, null);
                }
                else
                {
                    Main.NetworkManager.Port = Settings.MultiplayerClientPort;
                }

                // For whatever reasons the native "SET_CHAR_AS_MISSION_CHAR" does some addition checks before setting the flag in MP and often does not set the ped as required
                // so peds can be disposed by game randomly even though we need them
                AdvancedHookManaged.APed.DisableNetworkCheckForMissionPeds(true);
                AdvancedHookManaged.AGame.DisableNetworkSlowPCWarning(true);
                Log.Info("EventJoinedNetworkGame_EventRaised: Network game code patched", this);

                Log.Debug("EventJoinedNetworkGame_EventRaised: Launching LCPDFR layer networking thread", this);
                Thread thread = new Thread(this.Process);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}