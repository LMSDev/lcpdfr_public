namespace LCPD_First_Response.LCPDFR.Scripts
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.Engine.Scripting;

    /// <summary>
    /// The script responsible for handing out parking tickets.
    /// </summary>
    [ScriptInfo("ParkingTickets", true)]
    internal class ParkingTickets : GameScript
    {
        /// <summary>
        /// Whether the helpbox can be shown.
        /// </summary>
        private bool allowHelpbox;

        /// <summary>
        /// The clip board object.
        /// </summary>
        private GTA.Object clipboard;

        /// <summary>
        /// Whether the player is issuing a ticket.
        /// </summary>
        private bool isIssuing;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParkingTickets"/> class.
        /// </summary>
        public ParkingTickets()
        {
            this.allowHelpbox = true;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.isIssuing)
            {
                return;
            }

            // Player mustn't be busy and mustn't be aiming or targetting anyone
            if (!CPlayer<LCPDFRPlayer>.LocalPlayer.IsBusy && !CPlayer.LocalPlayer.Ped.IsAiming && !CPlayer.LocalPlayer.IsTargettingAnything)
            {
                if (!CPlayer.LocalPlayer.Ped.IsInVehicle && !CPlayer.LocalPlayer.Ped.IsInCombat && !CPlayer.LocalPlayer.Ped.IsGettingIntoAVehicle)
                {
                    if (!CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle) && CPlayer.LocalPlayer.Ped.IsStandingStill)
                    {
                        // Get close vehicles that have no driver, are stopped, are not the last car of the player and not a cop vehicle
                        CVehicle vehicle = CPlayer.LocalPlayer.Ped.Intelligence.GetClosestVehicle(
                            EVehicleSearchCriteria.NoDriverOnly | EVehicleSearchCriteria.StoppedOnly |
                            EVehicleSearchCriteria.NoPlayersLastVehicle | EVehicleSearchCriteria.NoCop,
                            3.0f);

                        if (vehicle != null && !vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsEmergencyServicesVehicle))
                        {
                            if (CPlayer.LocalPlayer.Ped.Intelligence.CanSeePosition(vehicle.Position, 60f))
                            {
                                if (!vehicle.HasOwner && !vehicle.IsRequiredForMission && !vehicle.IsOnFire && vehicle.IsOnAllWheels
                                    && !vehicle.Flags.HasFlag(EVehicleFlags.GotTicket))
                                {
                                    if (this.allowHelpbox)
                                    {
                                        if (vehicle.LicenseNumber.State != ELicenseNumberState.None)
                                        {
                                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ANPR_TRANSMIT"));
                                        }
                                        else
                                        {
                                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PARKING_TICKET_ISSUE"));
                                        }

                                        this.allowHelpbox = false;
                                        DelayedCaller.Call(delegate { this.allowHelpbox = true; }, 10000);
                                    }

                                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.IssueParkingTicket))
                                    {
                                        if (vehicle.LicenseNumber.State != ELicenseNumberState.None && vehicle.Flags.HasFlag(EVehicleFlags.WasScanned))
                                        {
                                            if (!CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie))
                                            {
                                                TaskWalkieTalkie taskWalkie = new TaskWalkieTalkie("BLOCKED_VEHICLE");
                                                taskWalkie.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.MainTask);

                                                vehicle.Flags |= EVehicleFlags.GotTicket;

                                                DelayedCaller.Call(
                                                    delegate
                                                    {
                                                        // Build phrase
                                                        string intro = "THIS_IS_CONTROL";
                                                        if (Common.GetRandomBool(0, 4, 1))
                                                        {
                                                            intro = "UNITS_PLEASE_BE_ADVISED";
                                                        }

                                                        string crime = "INS_WEVE_GOT";
                                                        if (Common.GetRandomBool(0, 2, 1))
                                                        {
                                                            crime = "INS_WE_HAVE";
                                                        }
                                                        else if (Common.GetRandomBool(0, 4, 1))
                                                        {
                                                            crime = "INS_WE_HAVE_A_REPORT_OF_ERRR";
                                                        }

                                                        if (vehicle.LicenseNumber.State == ELicenseNumberState.Stolen)
                                                        {
                                                            crime += " CRIM_A_STOLEN_VEHICLE";
                                                        }
                                                        else
                                                        {
                                                            if (Common.GetRandomBool(0, 2, 1))
                                                            {
                                                                crime += " CRIM_A_TRAFFIC_FELONY";
                                                            }
                                                            else
                                                            {
                                                                crime += " CRIM_A_TRAFFIC_VIOLATION";
                                                            }
                                                        }

                                                        string phrase = intro + " " + crime + " IN POSITION";
                                                        AudioHelper.PlayActionInScannerUsingPosition(phrase, CPlayer.LocalPlayer.Ped.Position);
                                                    }, 
                                                    Common.GetRandomValue(8000, 12000));
                                            }
                                        }
                                        else
                                        {
                                            this.clipboard = World.CreateObject("AMB_CLIPBOARD", CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0, 0, 5)));
                                            if (this.clipboard != null && this.clipboard.Exists())
                                            {
                                                // Hide player weapon, if any
                                                if (CPlayer.LocalPlayer.Ped.Weapons.Current != Weapon.Unarmed)
                                                {
                                                    CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
                                                }

                                                vehicle.Flags = vehicle.Flags | EVehicleFlags.GotTicket;
                                                CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet("amb@super_idles_b"), "stand_idle_c", 1.0f);
                                                this.clipboard.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.RightHand, Vector3.Zero, Vector3.Zero);
                                                this.isIssuing = true;
                                                DelayedCaller.Call(this.Finish, 5000);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            this.Finish(null);
        }

        /// <summary>
        /// Called when the issuing is finished.
        /// </summary>
        /// <param name="parameter"></param>
        private void Finish(object[] parameter)
        {
            if (this.clipboard != null && this.clipboard.Exists())
            {
                this.clipboard.Delete();
            }

            if (this.isIssuing)
            {
                Stats.UpdateStat(Stats.EStatType.Citations, 1, CPlayer.LocalPlayer.Ped.Position);
            }

            this.isIssuing = false;
        }
    }
}