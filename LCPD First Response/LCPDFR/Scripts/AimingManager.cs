namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.Events;

    using Main = LCPD_First_Response.LCPDFR.Main;
    using Timer = LCPD_First_Response.Engine.Timers.Timer;
    using LCPD_First_Response.Engine.IO;

    /// <summary>
    /// Responsible for everything requiring aiming at peds, e.g. starting and managing arrests and frisking or partner selection in the street.
    /// </summary>
    [ScriptInfo("AimingManager", true)]
    internal class AimingManager : GameScript, IPedController
    {
        /// <summary>
        /// The maximum distance a ped can be arrested.
        /// </summary>
        private const float MaximumDistanceToArrest = 10f;

        /// <summary>
        /// Arrest scripts.
        /// </summary>
        private List<Arrest> arrests;

        /// <summary>
        /// Whether the targeted ped can be used as partner.
        /// </summary>
        private bool canBeUsedAsPartner;

        /// <summary>
        /// Whether the indicator to signal the arrest ability should be drawn.
        /// </summary>
        private bool drawIndicator;

        /// <summary>
        /// Whether the indicator is blue.
        /// </summary>
        private bool drawBlue;

        /// <summary>
        /// Timer used to change the drawing color.
        /// </summary>
        private NonAutomaticTimer drawTimer;

        /// <summary>
        /// The frisk scripts.
        /// </summary>
        private List<Frisk> frisks;

        /// <summary>
        /// The timer used to measure the timer the arrest key is hold down.
        /// </summary>
        private Timer holdDownArrestKeyTimer;

        /// <summary>
        /// Whether the help box should be redrawn the next time, even though the target didn't change.
        /// </summary>
        private bool menuRedrawNextTime;

        /// <summary>
        /// Whether the user wants to have more options displayed.
        /// </summary>
        public bool menuShowMoreOptions;

        /// <summary>
        /// Whether the user has chosen the fining option.
        /// </summary>
        private bool menuOptionFine;

        /// <summary>
        /// Whether the user has chosen the hold option.
        /// </summary>
        private bool menuOptionHold;

        /// <summary>
        /// Whether the user has chosen the investigate option.
        /// </summary>
        private bool menuOptionInvestigate;

        /// <summary>
        /// Whether the user has chosen the leave option.
        /// </summary>
        private bool menuOptionLeave;

        /// <summary>
        /// The targeted ped;
        /// </summary>
        private CPed target;

        /// <summary>
        /// The old target;
        /// </summary>
        private CPed oldTarget;

        /// <summary>
        /// Sounds for calling in IDs
        /// </summary>
        private readonly string[] ambientSounds = new string[] { "MOBILE_TWO_WAY_GARBLED_SEQ", "MOBILE_TWO_WAY_GARBLED_GLITCH" };

        /// <summary>
        /// Initializes a new instance of the <see cref="AimingManager"/> class.
        /// </summary>
        public AimingManager()
        {
            this.arrests = new List<Arrest>();
            this.frisks = new List<Frisk>();
            this.drawTimer = new NonAutomaticTimer(500);

            EventArrestedPedSittingInPlayerVehicle.EventRaised += new EventArrestedPedSittingInPlayerVehicle.EventRaisedEventHandler(this.EventArrestedPedSittingInPlayerVehicle_EventRaised);
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            // Kill taxi hail script
            Natives.TerminateAllScriptsWithThisName("taxi_trigger");

            this.target = null;
            this.canBeUsedAsPartner = false;
            this.drawIndicator = false;

            // Player has to be on foot
            if (!CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                if (CPlayer.LocalPlayer.Ped.IsAiming || CPlayer.LocalPlayer.IsTargettingAnything)
                {
                    // Get targeted ped
                    CPed targetedPed = null;
                    if (CPlayer.LocalPlayer.Ped.IsAiming)
                    {
                        targetedPed = CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed();
                    }
                    else
                    {
                        targetedPed = CPlayer.LocalPlayer.GetTargetedPed();
                    }

                    if (targetedPed != null && targetedPed.Exists() && targetedPed.IsAliveAndWell &&  !targetedPed.IsGettingUp && !targetedPed.IsOnFire)
                    {
                        if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(targetedPed.Position) < MaximumDistanceToArrest)
                        {
                            // No cops
                            if (!targetedPed.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCop) && targetedPed.PedData.CanBeArrestedByPlayer
                                && !targetedPed.Wanted.ResistedArrest && !targetedPed.Wanted.IsDeciding)
                            {
                                // We want to show the helpboxes only once for every ped (don't repeat when target hasn't switched)
                                if (this.oldTarget != targetedPed || this.menuRedrawNextTime)
                                {
                                    // New target, so reset, but only if redraw wasn't forced
                                    if (!this.menuRedrawNextTime)
                                    {
                                        this.menuShowMoreOptions = false;
                                        this.menuOptionFine = false;
                                        this.menuOptionHold = false;
                                        this.menuOptionInvestigate = false;
                                        this.menuOptionLeave = false;
                                    }

                                    // If ped is being arrested by AI and already cuffed, skip
                                    if (targetedPed.Wanted.IsBeingArrested && !targetedPed.Wanted.IsBeingArrestedByPlayer && targetedPed.Wanted.IsCuffed)
                                    {
                                        return;
                                    }

                                    // Reset redrawing
                                    this.menuRedrawNextTime = false;

                                    // If neither being arrested or being frisked, show options to leave vehicle or to stop
                                    if (!targetedPed.Wanted.IsBeingArrestedByPlayer && !targetedPed.Wanted.IsBeingFrisked)
                                    {
                                        // If in vehicle and doing less than 5, show option to leave vehicle
                                        if (targetedPed.IsInVehicle && targetedPed.CurrentVehicle.Speed < 5)
                                        {
                                            if (targetedPed.PedData.Persona.Gender == Gender.Male)
                                            {
                                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_LEAVE_VEHICLE_M"));
                                            }
                                            else
                                            {
                                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_LEAVE_VEHICLE_F"));
                                            }
                                        }
                                        else if (!targetedPed.IsInVehicle && !targetedPed.Wanted.IsStopped)
                                        {
                                            // If not in vehicle and not yet stopped, display stop option
                                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_CAN_STOP_NOW"));
                                        }
                                        else if (targetedPed.Wanted.IsStopped)
                                        {
                                            // If target is stopped, show either more options or simple basic options
                                            if (this.menuShowMoreOptions)
                                            {
                                                if (this.menuOptionHold)
                                                {
                                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_OPTIONS_HOLD"));
                                                }
                                                else if (this.menuOptionInvestigate)
                                                {
                                                    if (targetedPed.PedData.Flags.HasFlag(EPedFlags.IdHasBeenChecked))
                                                    {
                                                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_OPTIONS_INVESTIGATE_ID_CHECKED"));
                                                    }
                                                    else
                                                    {
                                                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_OPTIONS_INVESTIGATE"));
                                                    }
                                                }
                                                else if (this.menuOptionLeave)
                                                {
                                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_OPTIONS_LEAVE"));
                                                }
                                                else if (this.menuOptionFine)
                                                {
                                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_OPTIONS_FINE"));
                                                }
                                                else
                                                {
                                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_OPTIONS_MORE"));
                                                }
                                            }
                                            else
                                            {
                                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_CAN_ARREST_NOW"));
                                            }
                                        }
                                    }
                                    else if (targetedPed.Wanted.IsCuffed)
                                    {
                                        // If cuffed and in vehicle, tell user he can get the suspect out
                                        if (targetedPed.IsInVehicle)
                                        {
                                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_SUSPECT_IN_VEHICLE"));
                                        }
                                        else
                                        {
                                            // If not in vehicle but arrested by player and cuffed, show additional options such as releasing the suspect
                                            if (targetedPed.Wanted.IsBeingArrestedByPlayer)
                                            {
                                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_ALREADY_ARRESTED_OPTIONS"));
                                            }
                                        }
                                    }
                                }

                                this.target = targetedPed;
                                this.oldTarget = this.target;

                                // No indicator when being arrested but hands still up, to avoid the indicator being drawn instantly when the suspect surrendered, but not
                                // when he resisted, so user could tell how the suspect will behave
                                if (targetedPed.Wanted.IsBeingArrestedByPlayer && targetedPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHandsUp))
                                {
                                    return;
                                }

                                this.drawIndicator = true;
                            }
                            else
                            {
                                // Indicator for partner
                                if (Main.PartnerManager.CanPartnerBeAdded)
                                {
                                    this.target = targetedPed;
                                    this.canBeUsedAsPartner = true;
                                    this.drawIndicator = true;
                                }
                            }
                        }
                    }
                }
            }


            if (this.target != null)
            {
                this.ProcessKeys();
            }
            else
            {
                this.oldTarget = null;
            }

            if (this.drawIndicator)
            {
                if (this.drawTimer.CanExecute())
                {
                    this.drawBlue = !this.drawBlue;
                }

                if (this.canBeUsedAsPartner)
                {
                    this.drawBlue = true;
                }

                float distance = this.target.Position.DistanceTo2D(CPlayer.LocalPlayer.Ped.Position);
                if (this.drawBlue)
                {
                    Engine.GUI.Gui.DrawCorona(this.target.Position, distance, System.Drawing.Color.DarkGray);
                    Engine.GUI.Gui.DrawCorona(this.target.Position, distance * 1.75f, System.Drawing.Color.Blue);
                }
                else
                {
                    Engine.GUI.Gui.DrawCorona(this.target.Position, distance, System.Drawing.Color.DarkGray);
                    Engine.GUI.Gui.DrawCorona(this.target.Position, distance * 1.75f, System.Drawing.Color.Red);
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

            // End all scripts
            foreach (Arrest arrest in this.arrests.ToArray())
            {
                arrest.End();
            }
        }

        /// <summary>
        /// Adds <paramref name="arrest"/> to the internal list.
        /// </summary>
        /// <param name="arrest">The arrest instance.</param>
        private void AddArrest(Arrest arrest)
        {
            Main.ScriptManager.RegisterScriptInstance(arrest);
            arrest.OnEnd += new OnEndEventHandler(this.arrest_OnEnd);
            this.arrests.Add(arrest);
        }

        /// <summary>
        /// Invoked when a ped has been arrested (by AI) and was placed in a vehicle where the player is the driver.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventArrestedPedSittingInPlayerVehicle_EventRaised(EventArrestedPedSittingInPlayerVehicle @event)
        {
            Arrest arrest = new Arrest(@event.Ped, true);
            this.AddArrest(arrest);
        }

        /// <summary>
        /// Processes the keys.
        /// </summary>
        private void ProcessKeys()
        {
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Arrest) && !this.canBeUsedAsPartner)
            {
                // If modifier has been used, start chase
                if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.StartChaseOnPed))
                {

                    this.target.BecomeMissionCharacter();
                    this.target.AlwaysFreeOnDeath = true;
                    this.target.AttachBlip();

                    // Try to determine if they've actually done anything wrong
                    if (this.target.IsInCombat || this.target.IsInMeleeCombat || CPlayer.LocalPlayer.Ped.HasBeenDamagedBy(this.target))
                    {
                        // They've been naughty
                        this.target.PedData.ComplianceChance = Common.GetRandomValue(0, 25);
                    }
                    else if (this.target.IsNotInCombatOrOnlyFleeing)
                    {
                        // They've possibly been naughty
                        this.target.PedData.ComplianceChance = Common.GetRandomValue(25, 50);
                    }
                    else
                    {
                        // They've probably not been naughty
                        this.target.PedData.ComplianceChance = Common.GetRandomValue(75, 100);
                    }

                    this.target.Task.WanderAround();
                    Chase chase = Pursuit.RegisterSuspect(this.target, true);
                    CPed ped1 = this.target;

                    if (this.target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill))
                    {
                        // Seems scenario peds get their scenario cancelled and stand still after being made a mission char, so this kind of fixes that.
                        this.target.Task.WanderAround();
                    }

                    Stats.UpdateStat(Stats.EStatType.PedsSetAsWanted, 1);

                    // Free ped on end
                    chase.ChaseEnded +=
                        delegate
                        {
                            if (ped1.Exists())
                            {
                                ped1.NoLongerNeeded();
                            }
                        };

                    return;
                }

                // If in vehicle, order to leave (small chance of pursuit). This doesn't apply for suspects already in custody and in player's vehicle though, of course.
                if (this.target.IsInVehicle && !this.target.Wanted.IsBeingArrestedByPlayer)
                {
                    CPlayer.LocalPlayer.Ped.SayAmbientSpeech("GET_OUT_OF_CAR_SHOUT");

                    if (this.target.CurrentVehicle.Speed > 5)
                    {
                        return;
                    }

                    int randomChance = Common.GetRandomValue(0, 100);

                    // Only if ped is driver and not in a chase already
                    if (randomChance < 20 && this.target.IsDriver && this.target.PedData.CurrentChase == null && this.target.Intelligence.IsFreeForAction(EPedActionPriority.RequiredForUserInteraction))
                    {
                        // Start pursuit
                        Pullover pullover = new Pullover(this.target.CurrentVehicle, true, true);
                        Main.ScriptManager.RegisterScriptInstance(pullover);
                    }
                    else
                    {
                        // TODO: Currently disabled for mission peds, because it doesn't work properly. We need to put surrender and resistance logic right here, because it bugs the arresting 
                        // task of cops and the flee task somehow
                        if (this.target.PedData.CurrentChase != null)
                        {
                            return;
                        }

                        // Door must be opened for stopped peds, since we put them into the back
                        bool isDoorOpen = (this.target.GetSeatInVehicle() == VehicleSeat.LeftRear && this.target.CurrentVehicle.Door(VehicleDoor.LeftRear).isOpen)
                            || (this.target.GetSeatInVehicle() == VehicleSeat.RightRear && this.target.CurrentVehicle.Door(VehicleDoor.RightRear).isOpen);

                        if (!isDoorOpen && this.target.Wanted.IsStopped)
                        {
                            TextHelper.PrintText(CultureHelper.GetText("ARREST_OPEN_DOOR_LEAVE"), 5000);
                            return;
                        }

                        this.target.PedData.Available = false;
                        this.target.BecomeMissionCharacter();
                        this.target.AlwaysFreeOnDeath = true;
                        this.target.BlockPermanentEvents = true;

                        // Might be already added since we put him into the vehicle
                        if (this.target.ContentManager != this.ContentManager)
                        {
                            this.ContentManager.AddPed(this.target, 100f);
                        }

                        this.target.Task.LeaveVehicle(this.target.CurrentVehicle, true);

                        // If ped hasn't been stopped, get back into vehicle soon. Ped would be stopped when we placed it inside the vehicle earlier
                        if (!this.target.Wanted.IsStopped)
                        {
                            this.target.PedData.AskedToLeaveVehicle = true;
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_PED_LEFT_VEHICLE"));

                            // Re-enter vehicle soon
                            DelayedCaller.Call(this.LeftVehicleCallback, 15000, this.target);
                        }
                    }

                    return;
                }

                // If target hasn't stopped yet, order it to stop
                if (!this.target.Wanted.IsStopped && !this.target.Wanted.IsBeingArrestedByPlayer && !this.target.Wanted.IsBeingFrisked)
                {
                    CPed ped = this.target;

                    // The initial arresting process
                    Action action = delegate
                    {
                        // Restore flag
                        ped.PedData.CanBeArrestedByPlayer = true;

                        // If suspect is not in combat or only fleeing, stop
                        if (ped.IsNotInCombatOrOnlyFleeing && !ped.PedData.AlwaysResistArrest)
                        {
                            // Check if ped will stop
                            if (ped.PedData.WillStop)
                            {
                                // Make mission char and add to content manager
                                bool success = ped.Intelligence.RequestForAction(EPedActionPriority.RequiredForUserInteraction, this);
                                if (!success)
                                {
                                    Log.Warning("ProcessKeys: Failed to request ownership of ped for stopping.", this);
                                    return;
                                }

                                ped.RequestOwnership(this);
                                ped.AlwaysFreeOnDeath = true;
                                this.ContentManager.AddPed(ped, 100f);

                                // Make friends with cops and look at player to wait for further instructions (player can now arrest or frisk)
                                ped.MakeFriendsWithCops(true);
                                ped.BlockPermanentEvents = true;
                                ped.Task.ClearAll();
                                ped.Intelligence.TaskManager.ClearTasks();
                                ped.Task.StandStill(1000);
                                ped.DontActivateRagdollFromPlayerImpact = true;
                                ped.Wanted.IsStopped = true;

                                // Make peds around notice
                                new EventPedBeingArrested(ped);

                                // Redraw help box to shore new options
                                this.menuRedrawNextTime = true;

                                Stats.UpdateStat(Stats.EStatType.PedsStopped, 1);

                                DelayedCaller.Call(
                                    delegate
                                    {
                                        // The retreat task likes to fuck up things, so we use the heavy task clearer here
                                        if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask))
                                        {
                                            ped.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexShockingEventFlee);
                                            ped.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexCombat);
                                            ped.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexCombatRetreatSubtask);
                                            ped.Task.ClearAll();
                                        }

                                        ped.Task.TurnTo(CPlayer.LocalPlayer.Ped);

                                        ped.AttachBlip();
                                        if (ped.HasBlip)
                                        {
                                            ped.Blip.Display = BlipDisplay.MapOnly;
                                            ped.Blip.Scale = 0.5f;
                                            ped.Blip.Name = "Suspect";
                                        }
                                    },
                                    500);
                            }
                            else
                            {
                                // If not fleeing already, run
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask) 
                                    && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                                {
                                    ped.Task.FleeFromChar(CPlayer.LocalPlayer.Ped);
                                }

                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_TASE"));
                            }
                        }
                        else
                        {
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_TASE"));
                            // Suspect is fighting, don't stop
                        }
                    };

                    // Block from being stopped again
                    this.target.PedData.CanBeArrestedByPlayer = false;

                    // Play audio and proceed after a short delay, so audio was already played and it looks more realistic
                    CPlayer.LocalPlayer.Ped.SayAmbientSpeech(CPlayer.LocalPlayer.Ped.VoiceData.StopSpeech);
                    DelayedCaller.Call(delegate { action(); }, 500);
                }
                else
                {
                    // If already being arrested, let the script know
                    if (this.target.Wanted.IsBeingArrestedByPlayer)
                    {
                        // But only when ped is already cuffed
                        if (this.target.Wanted.IsCuffed)
                        {
                            foreach (Arrest arrest in this.arrests)
                            {
                                if (arrest.IsArrestingPed(this.target))
                                {
                                    arrest.PlayerTriggeredArrestAgain();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // If already stopped, process keys
                        this.holdDownArrestKeyTimer = new Timer(10, this.ProcessKeysStillDownTimer, DateTime.Now, this.target);
                        this.holdDownArrestKeyTimer.Start();
                    }
                }
            }


            if (KeyHandler.IsKeyDown(ELCPDFRKeys.RecruitPartner) && this.canBeUsedAsPartner)
            {
                // Recruit
                if (CPlayer.LocalPlayer.Ped.IsAiming || CPlayer.LocalPlayer.IsTargettingOrAimingAtPed)
                {
                    if (Main.PartnerManager.AddPartner(this.target))
                    {
                        Stats.UpdateStat(Stats.EStatType.PartnerRecruited, 1);
                    }
                }
            }

            this.ProcessKeysDialog();
        }

        /// <summary>
        /// Processes the keys for the dialogs.
        /// </summary>
        private void ProcessKeysDialog()
        {
            if (this.menuShowMoreOptions)
            {
                if (this.menuOptionHold)
                {
                    // Ask to go into player's vehicle
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1))
                    {
                        if (CPlayer.LocalPlayer.LastVehicle != null && CPlayer.LocalPlayer.LastVehicle.Exists())
                        {
                            TaskGetInVehicle taskGetInVehicle = new TaskGetInVehicle(CPlayer.LocalPlayer.LastVehicle, VehicleSeat.LeftRear, VehicleSeat.RightRear, false);
                            taskGetInVehicle.AssignTo(this.target, ETaskPriority.MainTask);
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_IN_VEHICLE"));
                            this.menuOptionHold = false;
                            this.menuShowMoreOptions = false;

                        }
                        else
                        {
                            TextHelper.PrintText(CultureHelper.GetText("ARREST_NO_VEHICLE"), 5000);
                        }
                    }

                    // Hold suspect
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2))
                    {
                        string[] randomAnimations = new string[] { "I_SAID_NO", "IM_TALKING_2_YOU", "ITS_MINE", "THAT_WAY" };
                        CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody(Common.GetRandomCollectionValue<string>(randomAnimations), "gestures@male", 4.0f, true, 0, 0, 0, 3000);
                        TextHelper.ClearHelpbox();
                        this.menuOptionHold = false;
                        this.menuShowMoreOptions = false;
                    }
                }
                else if (this.menuOptionInvestigate)
                {
                    // Start frisking
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1))
                    {
                        // Get target and free from this instance
                        CPed ped = this.target;
                        if (ped.Wanted.IsBeingArrestedByPlayer || ped.Wanted.IsBeingFrisked)
                        {
                            return;
                        }

                        this.ContentManager.RemovePed(ped);
                        ped.ReleaseOwnership(this);
                        ped.MakeFriendsWithCops(false);
                        ped.Wanted.IsStopped = false;
                        TextHelper.ClearHelpbox();
                        this.menuOptionInvestigate = false;
                        this.menuShowMoreOptions = false;

                        // In case not owned by aiming manager (because PedIntelligence.Surrender directly set IsStopped), request action again here
                        if (ped.Intelligence.PedController != this)
                        {
                            ped.Intelligence.RequestForAction(EPedActionPriority.RequiredForUserInteraction, this);
                        }

                        // Start frisk script
                        Frisk frisk = new Frisk(ped);
                        Main.ScriptManager.RegisterScriptInstance(frisk);
                        frisk.OnEnd += new OnEndEventHandler(this.frisk_OnEnd);
                        this.frisks.Add(frisk);
                    }

                    // Ask for ID
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2))
                    {
                        if (this.target.PedData.Flags.HasFlag(EPedFlags.OnlyAllowFrisking))
                        {
                            TextHelper.PrintText("~r~Action not allowed", 5000);
                            return;
                        }

                        CPlayer.LocalPlayer.Ped.SayAmbientSpeech("ASK_FOR_ID");
                        TextHelper.ClearHelpbox();
                        this.menuOptionInvestigate = false;
                        this.menuShowMoreOptions = false;
                        CPed ped = this.target;

                        Action handOverIDAction = delegate
                        {
                            if (ped.PedData.Flags.HasFlag(EPedFlags.WontShowLicense))
                            {
                                // Play speech from ped too.  We try one first and if it doesn't work we resort to the other.
                                ped.SayAmbientSpeech("GENERIC_DEJECTED");
                                DelayedCaller.Call(delegate
                                {
                                    if (ped != null && ped.Exists())
                                    {
                                        if (!ped.IsSayingAmbientSpeech())
                                        {
                                            ped.SayAmbientSpeech("SHIT");
                                        }
                                    }
                                }
                                    , 250);
                                ped.Intelligence.SayText(CultureHelper.GetText("ARREST_FRISK_FORGOT_ID"), 3500);
                                return;
                            }

                            GTA.TaskSequence targetTask = new GTA.TaskSequence();
                            targetTask.AddTask.TurnTo(CPlayer.LocalPlayer.Ped);
                            targetTask.AddTask.PlayAnimation(new AnimationSet("amb@nightclub_ext"), "clubber_id_check", 4f, AnimationFlags.None);
                            targetTask.Perform(ped);
                            targetTask.Dispose();

                            GTA.TaskSequence copTask = new GTA.TaskSequence();
                            copTask.AddTask.TurnTo(ped);
                            copTask.AddTask.PlayAnimation(new AnimationSet("amb@nightclub_ext"), "bouncer_a_checkid", 4f, AnimationFlags.None);
                            copTask.Perform(CPlayer.LocalPlayer.Ped);
                            copTask.Dispose();

                            // Remember we checked the ped
                            ped.PedData.Flags |= EPedFlags.IdHasBeenChecked;
                            CPlayer.LocalPlayer.LastPedPulledOver = CPlayer.LocalPlayer.LastPedPulledOver = new CPed[] { ped };

                            // Present ID to user
                            string name = ped.PedData.Persona.Forename + " " + ped.PedData.Persona.Surname;
                            DateTime birthDay = ped.PedData.Persona.BirthDay;
                            string data = string.Format(CultureHelper.GetText("FRISK_ID"), name, birthDay.ToLongDateString());
                            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(data); }, 4000);
                        };

                        DelayedCaller.Call(delegate { handOverIDAction(); }, 2500);
                    }

                    // Fine suspect
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog3))
                    {
                        if (this.target.PedData.Flags.HasFlag(EPedFlags.OnlyAllowFrisking))
                        {
                            TextHelper.PrintText("~r~Action not allowed", 5000);
                            return;
                        }

                        this.menuOptionInvestigate = false;
                        this.menuOptionFine = true;
                        this.menuRedrawNextTime = true;
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog4) && this.target.PedData.Flags.HasFlag(EPedFlags.IdHasBeenChecked))
                    {
                        TextHelper.ClearHelpbox();
                        this.menuOptionInvestigate = false;
                        this.menuShowMoreOptions = false;

                        TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie(string.Empty);
                        taskWalkieTalkie.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.MainTask);
                        CPed ped = this.target;

                        DelayedCaller.Call(
                            delegate
                                {
                                    string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
                                    TextHelper.AddTextToTextWall(string.Format(CultureHelper.GetText("ARREST_FRISK_CALL_IN_ID"), ped.PedData.Persona.FullName, ped.PedData.Persona.BirthDay.ToShortDateString()), reporter);
                                },
                            this,
                            1500);

                        // Build persona string
                        int citations = ped.PedData.Persona.Citations;
                        ELicenseState license = ped.PedData.Persona.LicenseState;
                        int timesStopped = ped.PedData.Persona.TimesStopped;
                        bool wanted = ped.PedData.Persona.Wanted;
                        string wantedString;
                        if (wanted)
                        {
                            wantedString = Common.GetRandomValue(2, 4) + " active warrants.";
                        }
                        else
                        {
                            wantedString = "no active warrants.";
                        }

                        string data = string.Format(CultureHelper.GetText("ARREST_FRISK_CALL_IN_ID_DISPLAY_M"), citations, license.ToString().ToLower(), timesStopped, wantedString);

                        if (ped.PedData.Persona.Gender == Gender.Female)
                        {
                            data = string.Format(CultureHelper.GetText("ARREST_FRISK_CALL_IN_ID_DISPLAY_F"), citations, license.ToString().ToLower(), timesStopped, wantedString);
                        }
                        
                        DelayedCaller.Call(delegate { TextHelper.AddTextToTextWall(CultureHelper.GetText("ARREST_FRISK_CALL_IN_ID_ACK"), CultureHelper.GetText("POLICE_SCANNER_CONTROL")); }, this, Common.GetRandomValue(4000, 6500));
                        DelayedCaller.Call(delegate { TextHelper.AddTextToTextWall(data, CultureHelper.GetText("POLICE_SCANNER_CONTROL")); SoundEngine.PlaySound("POLICE_SCANNER_SCANNER_RESIDENT_IN_NOISE_01"); }, this, Common.GetRandomValue(11500, 15000));
                    }
                }
                else if (this.menuOptionLeave)
                {
                    // Move on
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1))
                    {
                        TextHelper.ClearHelpbox();
                        CPlayer.LocalPlayer.Ped.SayAmbientSpeech("MOVE_ON");
                        this.menuOptionLeave = false;
                        this.menuShowMoreOptions = false;
                        this.target.Task.WanderAround();
                        this.ReleasePed();
                    }

                    // Evacuate area
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2))
                    {
                        TextHelper.ClearHelpbox();
                        CPlayer.LocalPlayer.Ped.SayAmbientSpeech("EVACUATE_AREA");
                        this.menuOptionLeave = false;
                        this.menuShowMoreOptions = false;
                        this.ReleasePed();
                        this.target.Task.FleeFromChar(CPlayer.LocalPlayer.Ped);
                    }

                    // Leave area
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog3))
                    {
                        TextHelper.ClearHelpbox();
                        this.menuShowMoreOptions = false;
                        this.menuOptionLeave = false;
                        this.ReleasePed();
                        TaskLeaveScene taskLeaveScene = new TaskLeaveScene();
                        taskLeaveScene.AssignTo(this.target, ETaskPriority.MainTask);
                    }
                }
                else if (this.menuOptionFine)
                {
                    int amount = 0;

                    // 100
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1))
                    {
                        amount = 100;
                    }

                    // 60
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2))
                    {
                        amount = 60;
                    }

                    // 40
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog3))
                    {
                        amount = 40;
                    }

                    if (amount != 0)
                    {
                        this.menuOptionFine = false;
                        this.menuShowMoreOptions = false;
                        GTA.TaskSequence targetTask = new GTA.TaskSequence();
                        targetTask.AddTask.TurnTo(CPlayer.LocalPlayer.Ped);
                        targetTask.AddTask.PlayAnimation(new AnimationSet("amb@nightclub_ext"), "clubber_id_check", 4f, AnimationFlags.None);
                        targetTask.Perform(this.target);
                        targetTask.Dispose();

                        TextHelper.ClearHelpbox();
                        DelayedCaller.Call(
                            delegate
                            {
                                CPlayer.LocalPlayer.Money += amount;
                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("TOLL_PAID_YES");
                                Stats.UpdateStat(Stats.EStatType.Citations, 1, CPlayer.LocalPlayer.Ped.Position);
                            }, 
                            2500);
                    }
                }
                else
                {
                    // Hold options
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1))
                    {
                        if (this.target.PedData.Flags.HasFlag(EPedFlags.OnlyAllowFrisking))
                        {
                            TextHelper.PrintText("~r~Action not allowed", 5000);
                        }
                        else
                        {
                            this.menuOptionHold = true;
                            this.menuRedrawNextTime = true;
                        }
                    }

                    // Investigate options
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2))
                    {
                        this.menuOptionInvestigate = true;
                        this.menuRedrawNextTime = true;
                    }

                    // Leave options
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog3))
                    {
                        if (this.target.PedData.Flags.HasFlag(EPedFlags.OnlyAllowFrisking))
                        {
                            TextHelper.PrintText("~r~Action not allowed", 5000);
                        }
                        else
                        {
                            this.menuOptionLeave = true;
                            this.menuRedrawNextTime = true;
                        }
                    }

                    // Release suspect
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog4))
                    {
                        CPlayer.LocalPlayer.Ped.SayAmbientSpeech("FOUND_NOTHING");
                        TextHelper.ClearHelpbox();
                        this.ReleasePed();
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the key is still down.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void ProcessKeysStillDownTimer(params object[] parameter)
        {
            TimeSpan timeElasped = DateTime.Now - (DateTime)parameter[0];

            // If key is still down
            if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.Arrest))
            {
                if (timeElasped.TotalMilliseconds < 350)
                {
                    return;
                }

                // Key has been held down for more than 350ms, so we toggle additional options or go back in the menus
                this.holdDownArrestKeyTimer.Stop();

                if (this.menuShowMoreOptions)
                {
                    if (this.menuOptionHold || this.menuOptionInvestigate || this.menuOptionLeave || this.menuOptionFine)
                    {
                        // Go back to more options menu
                        this.menuOptionFine = false;
                        this.menuOptionHold = false;
                        this.menuOptionInvestigate = false;
                        this.menuOptionLeave = false;
                    }
                    else
                    {
                        // Not in any menu, reset to basic arrest menu
                        this.menuShowMoreOptions = false;
                    }
                }
                else
                {
                    this.menuShowMoreOptions = true;
                }

                // Redraw menu
                this.menuRedrawNextTime = true;
            }
            else
            {
                // Key has been released almost immediately, so we arrest or frisk, depending on menu
                this.holdDownArrestKeyTimer.Stop();

                if (!this.menuShowMoreOptions)
                {
                    // Get target
                    CPed ped = parameter[1] as CPed;

                    if (ped.Wanted.IsBeingFrisked)
                    {
                        return;
                    }

                    // Free target from this instance
                    this.ContentManager.RemovePed(ped);
                    ped.ReleaseOwnership(this);
                    ped.MakeFriendsWithCops(false);
                    ped.Wanted.IsStopped = false;

                    // In case not owned by aiming manager (because PedIntelligence.Surrender directly set IsStopped), request action again here
                    if (ped.Intelligence.PedController != this)
                    {
                        ped.Intelligence.RequestForAction(EPedActionPriority.RequiredForUserInteraction, this);
                    }

                    // Start arrest script
                    Arrest arrest = new Arrest(ped);
                    this.AddArrest(arrest);
                }
            }
        }

        /// <summary>
        /// Called when the arrest script has ended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void arrest_OnEnd(object sender)
        {
            this.arrests.Remove((Arrest)sender);
            (((Arrest)sender).Suspect).Intelligence.ResetAction(this);
        }

        /// <summary>
        /// Called when the frisk script has ended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void frisk_OnEnd(object sender)
        {
            // Don't make ped move away after frisk
            Frisk frisk = (Frisk)sender;
            frisk.Suspect.Wanted.IsStopped = true;
            frisk.Suspect.PedData.Available = false;
            frisk.Suspect.BecomeMissionCharacter();
            this.ContentManager.AddPed(frisk.Suspect, 100f);
            frisk.Suspect.Task.TurnTo(CPlayer.LocalPlayer.Ped);
            frisk.Suspect.RequestOwnership(this);

            this.frisks.Remove((Frisk)sender);
        }

        /// <summary>
        /// The left vehicle callback to make the ped enter again.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void LeftVehicleCallback(object[] parameter)
        {
            CPed ped = (CPed)parameter[0];
            if (ped.Exists())
            {
                if (ped.PedData.AskedToLeaveVehicle)
                {
                    if (!ped.Wanted.IsStopped && !ped.Wanted.IsBeingArrestedByPlayer && !ped.Wanted.IsBeingFrisked && ped.Wanted.IsIdle)
                    {
                        ped.PedData.Available = true;
                        ped.BlockPermanentEvents = false;
                        ped.NoLongerNeeded();
                        ped.PedData.AskedToLeaveVehicle = false;
                        this.ContentManager.RemovePed(ped);

                        CVehicle vehicle = ped.LastVehicle;
                        if (vehicle != null && vehicle.Exists())
                        {
                            if (!vehicle.HasDriver || !vehicle.Driver.IsAliveAndWell)
                            {
                                ped.Task.CruiseWithVehicle(vehicle, 15f, true);
                            }
                            else
                            {
                                ped.Task.EnterVehicle(vehicle, VehicleSeat.AnyPassengerSeat);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Releases the stopped ped.
        /// </summary>
        private void ReleasePed()
        {
            // Free target from this instance
            this.target.Intelligence.ResetAction(this);
            this.ContentManager.RemovePed(this.target);
            this.target.MakeFriendsWithCops(false);
            this.target.BlockPermanentEvents = false;
            this.target.Wanted.IsStopped = false;
            this.target.ReleaseOwnership(this);
            this.target.DeleteBlip();
            this.target.NoLongerNeeded();

            Stats.UpdateStat(Stats.EStatType.PedsReleased, 1);
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
        }
    }
}