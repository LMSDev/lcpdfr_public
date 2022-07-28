namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    /// <summary>
    /// The script managing the automatic license number reader.
    /// </summary>
    [ScriptInfo("LicenseNumberScanner", true)]
    internal class LicenseNumberScanner : GameScript
    {
        /// <summary>
        /// The blips.
        /// </summary>
        private Dictionary<CVehicle, Blip> blips;

        /// <summary>
        /// Whether ANPR is enabled.
        /// </summary>
        private bool enabled;

        /// <summary>
        /// The last used vehicles.
        /// </summary>
        private List<CVehicle> lastUsedVehicles;

        /// <summary>
        /// The playback control used for the beep sound.
        /// </summary>
        private PlaybackControl playbackControl;

        /// <summary>
        /// Timer to prevent scanning all the time.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseNumberScanner"/> class.
        /// </summary>
        public LicenseNumberScanner()
        {
            this.blips = new Dictionary<CVehicle, Blip>();
            this.lastUsedVehicles = new List<CVehicle>();
            this.timer = new NonAutomaticTimer(1000);
        }

        /// <summary>
        /// Gets or sets a value indicating whether ANPR is enabled.
        /// </summary>
        public bool Enabled 
        {
            get
            {
                return this.enabled;
            }

            set
            {
                this.enabled = value;

                // "Announce" enabled ANPR
                if (value)
                {
                    this.PlayBeep();
                    Stats.UpdateStat(Stats.EStatType.ANPREnabled, 1);
                }
                else
                {
                    // Play once because it is off
                    AudioHelper.PlayScannerEndSound();
                    Stats.UpdateStat(Stats.EStatType.ANPRDisabled, 1);
                }
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.Enabled)
            {
                return;
            }

            if (!this.timer.CanExecute())
            {
                return;
            }

            // Validate blips
            List<CVehicle> cleanup = new List<CVehicle>();
            foreach (KeyValuePair<CVehicle, Blip> keyValuePair in this.blips)
            {
                if (keyValuePair.Key.Exists())
                {
                    if (keyValuePair.Key.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 8)
                    {
                        keyValuePair.Value.Delete();
                        cleanup.Add(keyValuePair.Key);
                    }
                }
                else
                {
                    cleanup.Add(keyValuePair.Key);
                }
            }

            // Clean up list
            foreach (CVehicle vehicle in cleanup)
            {
                this.blips.Remove(vehicle);
            }

            // Doesnt work when player is busy
            if (LCPDFRPlayer.LocalPlayer.IsBusy || Main.PoliceComputer.IsActive)
            {
                return;
            }

            // Check for close vehicles
            if (CPlayer.LocalPlayer.Ped.IsInVehicle && CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed < 50)
            {
                if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                {
                    // Get vehicles around
                    CVehicle[] vehicles = CPlayer.LocalPlayer.Ped.Intelligence.GetVehiclesAround(12f, EVehicleSearchCriteria.NoCop);
                    foreach (CVehicle vehicle in vehicles)
                    {
                        if (vehicle == CPlayer.LocalPlayer.Ped.CurrentVehicle)
                        {
                           continue;
                        }

                        // If already reported, skip
                        if (vehicle.Flags.HasFlag(EVehicleFlags.GotTicket))
                        {
                            continue;
                        }

                        if (vehicle.LicenseNumber.State == ELicenseNumberState.None)
                        {
                            continue;
                        }

                        // Allow effects only if the vehicle wasn't used recently
                        if (!this.lastUsedVehicles.Contains(vehicle))
                        {
                            this.PlayBeep();
                            this.lastUsedVehicles.Add(vehicle);
                            string reason = string.Empty;

                            switch (vehicle.LicenseNumber.State)
                            {
                                case ELicenseNumberState.FledCrimeScene:
                                    reason = CultureHelper.GetText("ANPR_FLED_CRIME_SCENE");
                                    break;

                                case ELicenseNumberState.HitAndRunInvolvement:
                                    reason = CultureHelper.GetText("ANPR_HNR");
                                    break;

                                case ELicenseNumberState.OwnerWarrant:
                                    reason = CultureHelper.GetText("ANPR_OWNER_WARRANT");
                                    break;

                                case ELicenseNumberState.PlateExpired:
                                    reason = CultureHelper.GetText("ANPR_PLATE_EXPIRED");
                                    break;

                                case ELicenseNumberState.Stolen:
                                    reason = CultureHelper.GetText("ANPR_STOLEN");
                                    break;

                                case ELicenseNumberState.UnpaidTicket:
                                    reason = CultureHelper.GetText("ANPR_UNPAID_TICKET");
                                    break;

                                case ELicenseNumberState.StruckPedestrian:
                                    reason = CultureHelper.GetText("ANPR_STRUCK");
                                    break;
                            }

                            vehicle.Flags |= EVehicleFlags.WasScanned;

                            TextHelper.PrintText(string.Format("~r~ALPR detected vehicle ({0})", reason), 4000);
                            if (!vehicle.HasDriver)
                            {
                                DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ANPR_HANDLE_PARKED")); }, this, 3000);
                            }
                            else
                            {
                                DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ANPR_HANDLE")); }, this, 3000);
                            }

                            // Remove from collection in 10 seconds
                            CVehicle vehicle1 = vehicle;
                            DelayedCaller.Call(delegate { this.lastUsedVehicles.Remove(vehicle1); }, this, 10000);
                        }

                        // Add blip if close
                        if (vehicle.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 12)
                        {
                            if (!vehicle.HasBlip)
                            {
                                Blip b = vehicle.AttachBlip();
                                if (b != null && b.Exists())
                                {
                                    this.blips.Add(vehicle, b);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Plays the beep sound.
        /// </summary>
        private void PlayBeep()
        {
            // Check if old playback control has finished
            if (this.playbackControl != null)
            {
                // If not, don't play
                if (!this.playbackControl.IsDisposed)
                {
                    return;
                }
            }

            this.playbackControl = AudioHelper.PlayScannerInSound();
        }
    }
}