namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// Non-ELS lights for the player's police vehicle.
    /// </summary>
    [ScriptInfo("Lights", true)]
    internal class Lights : GameScript
    {
        /// <summary>
        /// The internal counter for switching the lights.
        /// </summary>
        private int counter;

        /// <summary>
        /// The timer used for the key checks.
        /// </summary>
        private Engine.Timers.Timer keyTimer;

        /// <summary>
        /// The current light mode.
        /// </summary>
        private int lightmode;

        private bool modeTwoLightsActivated;

        /// <summary>
        /// Timer to delay execution.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lights"/> class.
        /// </summary>
        public Lights()
        {
            this.timer = new NonAutomaticTimer(20);
        }

        /// <summary>
        /// Gets a value indicating whether a lights mode is activated.
        /// </summary>
        public bool IsUsingLightsMode
        {
            get
            {
                return this.lightmode != 0;
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.ToggleSirenMode))
            {
                if (CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager == null)
                {
                    Log.Warning("Process: Not a valid police vehicle, hence siren manager instance is missing. Forcing creation", this);
                    CPlayer.LocalPlayer.Ped.CurrentVehicle.AddSirenManager();
                }

                if (this.keyTimer == null)
                {
                    this.keyTimer = new Engine.Timers.Timer(10, this.PlayerPressedKeyTimer, DateTime.Now);
                    this.keyTimer.Start();

                    this.vehicle = CPlayer.LocalPlayer.Ped.CurrentVehicle;
                }

                return;
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.ToggleSirenSound))
            {
                if (CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager != null)
                {
                    if (CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.IsSirenMuted)
                    {
                        CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.UnmuteSiren();
                    }
                    else
                    {
                        CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.MuteSiren();
                    }
                }
            }

            if (!this.timer.CanExecute())
            {
                return;
            }

            bool isKeyHeldDown = KeyHandler.IsKeyStillDown(ELCPDFRKeys.ToggleSirenMode) && this.keyTimer == null && CPlayer.LocalPlayer.Ped.IsInVehicle;

            if (this.lightmode == 1)
            {
                if (this.vehicle != null && this.vehicle.Exists())
                {
                    /*
                    this.counter += 1;

                    if (this.counter == 0)
                    {
                        CPlayer.LocalPlayer.LastVehicle.SirenActive = true;
                    }

                    if (this.counter == 4)
                    {
                        CPlayer.LocalPlayer.LastVehicle.SirenActive = false;
                    }

                    if (this.counter == 8)
                    {
                        CPlayer.LocalPlayer.LastVehicle.SirenActive = true;
                    }

                    if (this.counter == 12)
                    {
                        CPlayer.LocalPlayer.LastVehicle.SirenActive = false;
                        this.counter = 0;
                    }

                    if (!isKeyHeldDown)
                    {
                        CPlayer.LocalPlayer.LastVehicle.SirenManager.MuteSiren();
                    }
                     * */

                    if (!isKeyHeldDown)
                    {
                        this.vehicle.SirenManager.MuteSiren();
                    }
                }
            }

            if (this.lightmode == 2)
            {
                if (this.vehicle != null && this.vehicle.Exists())
                {
                    if (modeTwoLightsActivated)
                    {
                        this.vehicle.SirenManager.DeactivateAll();
                        modeTwoLightsActivated = false;
                    }
                    else
                    {
                        this.vehicle.SirenManager.ActivateAll();
                        modeTwoLightsActivated = true;
                    }

                    if (!isKeyHeldDown)
                    {
                        this.vehicle.SirenManager.MuteSiren();
                    }
                }
            }

            if (this.lightmode == 3)
            {
                if (this.vehicle != null && this.vehicle.Exists())
                {
                    // Cancel siren sound
                    this.vehicle.SirenActive = true;

                    if (!isKeyHeldDown)
                    {
                        this.vehicle.SirenManager.MuteSiren();
                    }
                    else
                    {
                        this.vehicle.SirenManager.UnmuteSiren();
                    }

                    this.counter = 0;
                }
            }
        }

        /// <summary>
        /// Called when the player has pressed the siren mode key.
        /// </summary>
        /// <param name="parameter">The time the key has been pressed.</param>
        private void PlayerPressedKeyTimer(params object[] parameter)
        {
            TimeSpan timeElasped = DateTime.Now - (DateTime)parameter[0];

            if (!KeyHandler.IsKeyStillDown(ELCPDFRKeys.ToggleSirenMode))
            {
                // If key is no longer held down, cancel timer and either disable siren, change modes or unmute siren audio
                this.keyTimer.Stop();
                this.keyTimer = null;

                // If held down for less than 750 milliseconds, either disable siren or change mode
                if (timeElasped.TotalMilliseconds < 750)
                {
                    // Less than 250 ms down, so toggle mode
                    if (timeElasped.TotalMilliseconds < 250)
                    {
                        if (this.lightmode == 3)
                        {
                            this.lightmode = 0;
                            this.vehicle.SirenActive = false;
                            this.vehicle.SirenManager.UnmuteSiren();
                        }
                        else
                        {
                            this.vehicle.SirenActive = true;
                            this.vehicle.SirenManager.ActivateAll();
                            this.vehicle.SirenManager.MuteSiren();
                            this.lightmode++;

                            if (this.lightmode == 1)
                            {
                                this.vehicle.SirenManager.LightingMode = ELightingMode.WhiteThenRed;
                            }
                            else
                            {
                                this.vehicle.SirenManager.LightingMode = ELightingMode.Default;
                            }
                        }
                    }
                    else
                    {
                        // Disable siren because at least 250 ms down
                        this.lightmode = 0;
                        this.vehicle.SirenActive = false;
                    }
                }
            }
            else
            {
                // If still down and longer than 750 milliseconds, cancel timer so siren audio can be played
                if (timeElasped.TotalMilliseconds > 750)
                {
                    this.vehicle.SirenManager.UnmuteSiren();
                    this.keyTimer.Stop();
                    this.keyTimer = null;
                }
            }
        }
    }
}