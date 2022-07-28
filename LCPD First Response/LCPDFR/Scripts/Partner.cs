/*namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.Events;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// Represents a partner cop of the player.
    /// </summary>
    [ScriptInfo("Partner", true)]
    internal class Partner : GameScript, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// The current action (e.g. <see cref="Arrest"/> or <see cref="Pullover"/>).
        /// </summary>
        private object action;

        /// <summary>
        /// The partner ped.
        /// </summary>
        private CPed partnerPed;

        /// <summary>
        /// The state of the partner.
        /// </summary>
        private EPartnerState state;

        /// <summary>
        /// The weapon switch timer.
        /// </summary>
        private NonAutomaticTimer weaponSwitchTimer;

        /// <summary>
        /// Whether weapon switching is blocked.
        /// </summary>
        private bool blockSwitching;

        /// <summary>
        /// Initializes a new instance of the <see cref="Partner"/> class.
        /// </summary>
        public Partner()
        {
            Main.PoliceDepartmentManager.PlayerEnteredLeftPD += new PoliceDepartment.PlayerEnteredLeftPDEventHandler(this.PoliceDepartmentManager_PlayerEnteredLeftPD);
            EventPlayerStartedArrest.EventRaised += new Events.EventPlayerStartedArrest.EventRaisedEventHandler(this.EventPlayerStartedArrest_EventRaised);
            EventPlayerStartedFrisk.EventRaised += new EventPlayerStartedFrisk.EventRaisedEventHandler(this.EventPlayerStartedFrisk_EventRaised);
            EventPlayerStartedPullover.EventRaised += new EventPlayerStartedPullover.EventRaisedEventHandler(this.EventPlayerStartedPullover_EventRaised);

            this.weaponSwitchTimer = new NonAutomaticTimer(500);
        }

        /// <summary>
        /// Describes the state of the partner.
        /// </summary>
        private enum EPartnerState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None,

            /// <summary>
            /// Aiming at the suspect during an arrest.
            /// </summary>
            ArrestAimingAtSuspect,

            /// <summary>
            /// Going to the vehicle of the suspect.
            /// </summary>
            PulloverGoingToCar,

            /// <summary>
            /// Aiming at the suspect.
            /// </summary>
            PulloverAimingAtSuspect,

            /// <summary>
            /// Partner is supposed to keep position.
            /// </summary>
            KeepPosition,
        }

        /// <summary>
        /// Gets a value indicating whether the partner exists at all.
        /// </summary>
        public bool Exists
        {
            get
            {
                return this.partnerPed != null && this.partnerPed.Exists();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the partner is alive.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (this.partnerPed != null && this.partnerPed.Exists())
                {
                    return this.partnerPed.IsAliveAndWell;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the partner ped.
        /// </summary>
        public CPed PartnerPed
        {
            get
            {
                return this.partnerPed;
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            this.FreePartner();
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.Exists)
            {
                if (!this.partnerPed.IsAliveAndWell)
                {
                    this.FreePartner();
                    return;
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.PartnerArrest))
                {
                    if (this.partnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
                    {
                        return;
                    }

                    // Make partner arrest target
                    CPed ped = null;
                    if (CPlayer.LocalPlayer.Ped.IsAiming)
                    {
                        ped = CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed();
                    }
                    else
                    {
                        ped = CPlayer.LocalPlayer.GetTargetedPed();
                    }

                    if (ped != null && ped.Exists() && ped != this.partnerPed && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCop))
                    {
                        if (ped.Position.DistanceTo(this.partnerPed.Position) < 12 && ped.PedData.CanBeArrestedByPlayer)
                        {
                            AudioHelper.PlayActionSound("ARREST_PLAYER");
                            ped.BecomeMissionCharacter();
                            ped.Task.ClearAll();
                            ped.PedData.DontAllowEmptyVehiclesAsTransporter = true;
                            ped.Task.HandsUp(10000);
                            this.ContentManager.AddPed(ped, 50f, EContentManagerOptions.DeleteInsteadOfFree);

                            // Prevent walkie talkie task from fucking up things
                            if (this.partnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie))
                            {
                                this.partnerPed.Intelligence.TaskManager.Abort(this.partnerPed.Intelligence.TaskManager.FindTaskWithID(ETaskID.WalkieTalkie));
                            }

                            this.state = EPartnerState.None;

                            TaskBustPed taskBustPed = new TaskBustPed(ped);
                            taskBustPed.AssignTo(this.partnerPed, ETaskPriority.MainTask);

                            Stats.UpdateStat(Stats.EStatType.PartnerOrderedArrest, 1);
                        }
                    }
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.PartnerRegroup))
                {
                    CPlayer.LocalPlayer.Ped.SayAmbientSpeech("REGROUP");
                    this.Regroup();
                }

                switch (this.state)
                {
                    case EPartnerState.ArrestAimingAtSuspect:
                        // Cancel if cuffed
                        Arrest arrest = (Arrest)this.action;
                        if (arrest.Suspect.Wanted.IsCuffed)
                        {
                            this.partnerPed.Task.ClearAll();
                            this.state = EPartnerState.None;
                        }

                        break;

                    case EPartnerState.PulloverGoingToCar:
                        if (this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHitWall))
                        {
                            Log.Debug("Process: Hit wall, holding position", this);

                            Pullover pullover = (Pullover)this.action;
                            if (pullover.Vehicle.HasDriver)
                            {
                                // Partner hit a wall, so we are going to stop and aim
                                this.partnerPed.Task.ClearAll();
                                this.partnerPed.Task.GoToCharAiming(pullover.Vehicle.Driver, 100f, 100f);
                                this.state = EPartnerState.PulloverAimingAtSuspect;
                                return;
                            }
                        }

                        if (!this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGoToPointAiming))
                        {
                            Pullover pullover = (Pullover)this.action;

                            if (pullover.Vehicle.HasDriver)
                            {
                                // Extend distance when going to passenger window
                                float distance = 3f;
                                if (pullover.Vehicle.GetSideOfStreetVehicleIsAt() == EStreetSide.Right)
                                {
                                    distance = 5f;
                                }

                                this.partnerPed.Task.GoToCharAiming(pullover.Vehicle.Driver, distance, 9f);
                            }

                            this.state = EPartnerState.PulloverAimingAtSuspect;
                        }

                        break;

                    case EPartnerState.KeepPosition:
                        // Play some ambients, such as walkie talkie
                        if (!this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill))
                        {
                            this.partnerPed.Task.StandStill(5000);

                            // Assign stand still task again (only not when using walkie talkie)
                            if (!this.partnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie))
                            {
                                // Random chance to use walkie talkie
                                int randomValue = Common.GetRandomValue(0, 10);
                                if (randomValue < 2)
                                {
                                    TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie("WALKIE_TALKIE");
                                    taskWalkieTalkie.AssignTo(this.partnerPed, ETaskPriority.MainTask);

                                    // Play sound
                                    DelayedCaller.Call(
                                        delegate
                                        {
                                            if (this.partnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie) && !AudioHelper.IsBusy)
                                            {
                                                if (this.partnerPed.Animation.isPlaying(new AnimationSet("missemergencycall"), "idle_answer_radio_a"))
                                                {
                                                    AudioHelper.PlayActionSound("RANDOMCHAT");
                                                }
                                            }
                                        }, 
                                        this, 
                                        800);
                                }
                            }
                        }

                        goto case EPartnerState.None;
                        break;

                    case EPartnerState.None:
                        // Weapon logic
                        if (CPlayer.LocalPlayer.Ped.IsArmed())
                        {
                            if (this.partnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
                            {
                                return;
                            }

                            // Get current weapon
                            Weapon currentWeapon = CPlayer.LocalPlayer.Ped.Weapons.Current;
                            if (this.partnerPed.Weapons.Current != currentWeapon && !this.blockSwitching)
                            {
                                // Player must not be switching at the moment
                                if (this.weaponSwitchTimer.CanExecute(true) && !CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleSwapWeapon))
                                {
                                    this.partnerPed.EnsurePedHasNoWeapon(true);
                                    this.partnerPed.Weapons[currentWeapon].Ammo = 1000;
                                    this.blockSwitching = true;
                                    this.weaponSwitchTimer.Reset();
                                    DelayedCaller.Call(delegate { this.blockSwitching = false; }, this, 2000);
                                }
                            }
                        }
                        break;
                }

                // If player is injured, partner will try revive player.
                
            }
        }

        /// <summary>
        /// Makes the partner keep the current position. This can only be undone by regroup.
        /// </summary>
        public void KeepPosition()
        {
            if (this.Exists)
            {
                this.state = EPartnerState.KeepPosition;
            }
        }

        /// <summary>
        /// Makes the partner move to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public void MoveToPosition(Vector3 position)
        {
            if (this.Exists)
            {
                this.state = EPartnerState.None;

                GTA.TaskSequence sequence = new GTA.TaskSequence();
                sequence.AddTask.RunTo(position, true);
                sequence.AddTask.Wait(5000);
                this.partnerPed.Task.PerformSequence(sequence);
                sequence.Dispose();
            }
        }

        /// <summary>
        /// Cancels all tasks of the partner and makes him regroup.
        /// </summary>
        public void Regroup()
        {
            if (this.Exists)
            {
                this.state = EPartnerState.None;
                this.partnerPed.Task.ClearAll();

                if (!CPlayer.LocalPlayer.Group.isMember(this.partnerPed))
                {
                    CPlayer.LocalPlayer.Group.AddMember(this.partnerPed);
                }
            }
        }

        /// <summary>
        /// Sets <paramref name="ped"/> as the partner.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public void SetPedAsPartner(CPed ped)
        {
            if (this.IsAlive)
            {
                Log.Warning("SetPedAsPartner: Setting new partner while old one still exists! Attempting to free old one properly now", this);
                this.partnerPed.NoLongerNeeded();
            }

            this.partnerPed = ped;
            this.partnerPed.EnsurePedHasWeapon();
            this.partnerPed.RequestOwnership(this);
            this.partnerPed.GetPedData<PedDataCop>().RequestPedAction(ECopState.Blocker, this);

            this.partnerPed.BlockPermanentEvents = false;
            this.partnerPed.WillDoDrivebys = false;
            this.partnerPed.WillUseCarsInCombat = true;
            this.partnerPed.WillFlyThroughWindscreen = true;
            this.partnerPed.SenseRange = 75.0f;
            this.partnerPed.SetPathfinding(true, true, true);
            this.partnerPed.SetWaterFlags(false, false, true);
            this.partnerPed.Accuracy = 100;
            this.partnerPed.Armor = 200;
            this.partnerPed.MaxHealth = 400;
            this.partnerPed.Health = 400;

            CPlayer.LocalPlayer.Group.FollowStatus = 1;
            CPlayer.LocalPlayer.Group.FormationSpacing = 3.5f;
            CPlayer.LocalPlayer.Group.Formation = 5;
            CPlayer.LocalPlayer.Group.SeparationRange = 9999f;
            CPlayer.LocalPlayer.Group.AddMember(this.partnerPed);

            // Kill all running tasks
            this.partnerPed.Task.ClearAll();
            this.partnerPed.Intelligence.TaskManager.ClearTasks();
            this.partnerPed.Task.GoTo(CPlayer.LocalPlayer.Ped);
            this.partnerPed.AttachBlip();
            this.partnerPed.Blip.Scale = 0.5f;
            this.partnerPed.Blip.Friendly = true;
            this.partnerPed.Blip.Display = BlipDisplay.MapOnly;
            this.partnerPed.Blip.Name = CultureHelper.GetText("PARTNER_PARTNER");
        }

        /// <summary>
        /// Teleports the partner to the player.
        /// </summary>
        public void WarpToPlayer()
        {
            if (this.IsAlive)
            {
                if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                {
                    this.partnerPed.WarpIntoVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle, VehicleSeat.AnyPassengerSeat);
                }
                else
                {
                    this.partnerPed.Position = CPlayer.LocalPlayer.Ped.Position;
                }
            }
        }

        /// <summary>
        /// Ped has left the controller entity, this musn't happen since cop state is blocker.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public void PedHasLeft(CPed ped)
        {
        }

        /// <summary>
        /// Called when the player has either entered or left a pd.
        /// </summary>
        /// <param name="policeDepartment">The police department.</param>
        /// <param name="entered">True if entered, false if left.</param>
        private void PoliceDepartmentManager_PlayerEnteredLeftPD(PoliceDepartment policeDepartment, bool entered)
        {
            if (!this.IsAlive)
            {
                return;
            }

            if (!entered)
            {
                // Teleport
                this.partnerPed.Position = policeDepartment.Position;
                this.partnerPed.CurrentRoom = CPlayer.LocalPlayer.Ped.CurrentRoom;
                this.partnerPed.Task.ClearAll();

                // Not sure why, but some PDs fuck up the partner so he won't move after being teleported
                CPed ped = this.partnerPed;
                this.FreePartner();
                this.SetPedAsPartner(ped);

                // If vehicle selectiion is in progress, don't move
                if (Main.GoOnDutyScript.IsInProgress)
                {
                    this.partnerPed.Task.StandStill(-1);
                }
            }
            else
            {
                if (this.partnerPed.IsInVehicle)
                {
                    this.partnerPed.WarpFromCar(CPlayer.LocalPlayer.Ped.Position.Around(1f));
                }

                // Teleport
                this.partnerPed.Position = CPlayer.LocalPlayer.Ped.Position.Around(1f);
                this.partnerPed.CurrentRoom = CPlayer.LocalPlayer.Ped.CurrentRoom;
                this.partnerPed.Task.ClearAll();
            }
        }

        /// <summary>
        /// Called when the player started an arrest.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventPlayerStartedArrest_EventRaised(EventPlayerStartedArrest @event)
        {
            // Make partner aim at suspect
            if (this.IsAlive && !this.partnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
            {
                this.partnerPed.EnsurePedHasWeapon();
                this.partnerPed.Task.ClearAll();
                this.partnerPed.Task.GoToCharAiming(@event.PedBeingArrested, 5f, 10f);
                this.action = @event.Arrest;
                @event.Arrest.OnEnd += new OnEndEventHandler(this.Arrest_OnEnd);
                @event.Arrest.PedResisted += new Action(this.Arrest_PedResisted);
                this.state = EPartnerState.ArrestAimingAtSuspect;
            }
        }

        /// <summary>
        /// Called when the arrest has ended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void Arrest_OnEnd(object sender)
        {
            if (this.IsAlive)
            {
                if (this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    this.partnerPed.Task.ClearAll();
                }
            }

            this.state = EPartnerState.None;
        }

        /// <summary>
        /// Called when the ped resisted during arresting.
        /// </summary>
        private void Arrest_PedResisted()
        {
            if (this.IsAlive)
            {
                if (this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    this.partnerPed.Task.ClearAll();
                }
            }

            this.state = EPartnerState.None;
        }

        /// <summary>
        /// Called when the player started frisking.
        /// </summary>
        /// <param name="event">The event.</param>
        void EventPlayerStartedFrisk_EventRaised(EventPlayerStartedFrisk @event)
        {
            if (this.IsAlive && !this.partnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
            {
                this.partnerPed.EnsurePedHasWeapon();
                this.partnerPed.Task.AimAt(@event.PedBeingFrisked, 10000);
                this.action = @event.Frisk;
            }
        }

        /// <summary>
        /// Called when the player started a pullover.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventPlayerStartedPullover_EventRaised(EventPlayerStartedPullover @event)
        {
            // Go to the car and aim at the suspect
            if (this.IsAlive)
            {
                this.partnerPed.EnsurePedHasWeapon();

                // Always go to the pavement so partner doesn't block the traffic
                Vector3 offset = new Vector3(2, -2, 0);
                if (@event.Vehicle.GetSideOfStreetVehicleIsAt() == EStreetSide.Left)
                {
                    offset = new Vector3(-2, -4, 0);
                }

                this.partnerPed.Task.GoToCoordAiming(@event.Vehicle.Driver.GetOffsetPosition(offset), EPedMoveState.Run, @event.Vehicle.Driver.Position);
                this.action = @event.Pullover;
                @event.Pullover.OnEnd += new OnEndEventHandler(this.Pullover_OnEnd);
                @event.Pullover.PedResisted += new Action(this.Pullover_PedResisted);

                // Set state a little later so there's enough time for the aiming task to be applied
                DelayedCaller.Call(delegate { this.state = EPartnerState.PulloverGoingToCar; }, 2000);
            }
        }

        /// <summary>
        /// Called when the pullover has ended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void Pullover_OnEnd(object sender)
        {
            if (this.IsAlive)
            {
                if (this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    this.partnerPed.Task.ClearAll();
                }
            }

            this.state = EPartnerState.None;
        }

        /// <summary>
        /// Called when the ped resisted during the pullover.
        /// </summary>
        private void Pullover_PedResisted()
        {
            if (this.IsAlive)
            {
                if (this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGoToPointAiming) ||
                    this.partnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    this.partnerPed.Task.ClearAll();
                }
            }

            this.state = EPartnerState.None;
        }

        /// <summary>
        /// Frees the partner.
        /// </summary>
        private void FreePartner()
        {
            if (this.Exists)
            {
                this.partnerPed.ReleaseOwnership(this);
                this.partnerPed.GetPedData<PedDataCop>().ResetPedAction(this);
                this.partnerPed.LeaveGroup();
                this.partnerPed.DeleteBlip();
                this.partnerPed.NoLongerNeeded();
                CPlayer.LocalPlayer.Group.RemoveAllMembers();
            }

            this.state = EPartnerState.None;
            this.partnerPed = null;

            Log.Debug("FreePartner: Partner freed", this);
        }
    }
}*/