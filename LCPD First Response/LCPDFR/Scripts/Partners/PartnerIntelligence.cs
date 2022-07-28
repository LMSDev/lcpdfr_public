namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using System;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Scripts.Events;

    using Main = LCPD_First_Response.LCPDFR.Main;

    enum EPartnerState
    {
        None,
        Idle,
        Follow,
        StandStill,
        SupportPlayerAction,
        Arrest,
    }

    class PartnerIntelligence : IExtendedIntelligence
    {
        private Arrest arrestScript;

        /// <summary>
        /// The weapon switch timer.
        /// </summary>
        private NonAutomaticTimer weaponSwitchTimer;

        /// <summary>
        /// Whether weapon switching is blocked.
        /// </summary>
        private bool blockSwitching;

        /// <summary>
        /// The current behavior.
        /// </summary>
        private Behavior currentBehavior;

        /// <summary>
        /// The current state.
        /// </summary>
        private EPartnerState currentState;

        /// <summary>
        /// The current task.
        /// </summary>
        private PedTask currentTask;

        /// <summary>
        /// The ped.
        /// </summary>
        private readonly Partner partner;

        /// <summary>
        /// The task to enter a vehicle.
        /// </summary>
        private TaskGetInVehicle getInVehicleTask;

        public event Action PartnerDied;

        public PartnerIntelligence(Partner partner)
        {
            this.currentState = EPartnerState.Idle;
            this.partner = partner;
            this.weaponSwitchTimer = new NonAutomaticTimer(500);
        }

        public bool IsFreeForTask
        {
            get
            {
                return this.currentBehavior == null;
            }
        }

        public void HasBeenKilled()
        {
            Log.Caller();

            this.AbortCurrentBehavior();

            if (this.PartnerDied != null)
            {
                this.PartnerDied();
            }

            this.Shutdown();
        }

        public void Initialize()
        {
            Log.Caller();

            Main.GoOnDutyScript.PlayerWentOnDuty += this.GoOnDutyScript_PlayerWentOnDuty;
            EventPlayerWarped.EventRaised += this.EventPlayerWarped_EventRaised;
            EventPlayerStartedArrest.EventRaised += EventPlayerStartedArrest_EventRaised;
            EventPlayerStartedFrisk.EventRaised += EventPlayerStartedFrisk_EventRaised;
            EventPlayerStartedPullover.EventRaised += EventPlayerStartedPullover_EventRaised;

            EventPartnerWantsToEnterVehicle.EventRaised += EventPartnerWantsToEnterVehicle_EventRaised;
            EventPartnerWantsToSupportArresting.EventRaised += EventPartnerWantsToSupportArresting_EventRaised;
            Main.PoliceDepartmentManager.PlayerEnteredLeftPD += PoliceDepartmentManager_PlayerEnteredLeftPD;
        }

        public void Process()
        {
            // Execute behavior as long as its necessary.
            if (this.currentBehavior != null)
            {
                // Downcasting to access interface members.
                IInternalBehavior internalBehavior = this.currentBehavior;
                if (this.currentBehavior.BehaviorState != EBehaviorState.Failed && internalBehavior.InternalRun() == EBehaviorState.Running)
                {
                    return;
                }

                Log.Debug("Process: Behavior has finished", "PartnerIntelligence");
                this.currentBehavior = null;
            }

            // Run behavior trees.
            Func<Behavior> onFootBehavior = 
                // Player is on foot, assist if necessary.
                Selector(LCPDFRPlayer.LocalPlayer.IsArresting,
                this.AssistWithArrest,

                // Player is on foot, check if he enters a vehicle so we can do as well. Simply follow if not.
                Selector(CPlayer.LocalPlayer.Ped.IsEnteringVehicle, this.EnterVehicle, this.FollowPlayer));

            Func<Behavior> behaviorTree =
                // If player is not yet on duty.
                Selector(Inverter(Globals.IsOnDuty), 
                
                    this.PlayerNotYetOnDuty, 

                    // If player is in vehicle.
                    Selector(CPlayer.LocalPlayer.Ped.IsInVehicle,
 
                        // Check if we are too.
                        Selector(this.partner.PartnerPed.IsInVehicle, 
                
                            // Both in vehicle, is he leaving?
                            Selector(CPlayer.LocalPlayer.Ped.IsLeavingVehicle, 
                            
                                // If we are in the same vehicle, leave too.
                                Decorator(this.AreWeInPlayersVehicle, this.LeaveVehicle),

                            this.ProcessDriving),

                        // We aren't in vehicle, so enter if player is not currently leaving it.
                        Decorator(Inverter(CPlayer.LocalPlayer.Ped.IsLeavingVehicle), this.EnterVehicle)),

                    onFootBehavior));

            // Run logic!
            this.currentBehavior = behaviorTree.Invoke();
        }

        public void Shutdown()
        {
            Log.Caller();

            this.AbortCurrentBehavior();

            Main.GoOnDutyScript.PlayerWentOnDuty -= this.GoOnDutyScript_PlayerWentOnDuty;
            EventPlayerWarped.EventRaised -= this.EventPlayerWarped_EventRaised;
            EventPlayerStartedArrest.EventRaised -= this.EventPlayerStartedArrest_EventRaised;
            EventPlayerStartedFrisk.EventRaised -= EventPlayerStartedFrisk_EventRaised;
            EventPlayerStartedPullover.EventRaised -= EventPlayerStartedPullover_EventRaised;

            EventPartnerWantsToEnterVehicle.EventRaised -= this.EventPartnerWantsToEnterVehicle_EventRaised;
            EventPartnerWantsToSupportArresting.EventRaised -= EventPartnerWantsToSupportArresting_EventRaised;
            Main.PoliceDepartmentManager.PlayerEnteredLeftPD -= PoliceDepartmentManager_PlayerEnteredLeftPD;
        }

        public void ArrestPed(CPed ped)
        {
            this.AbortCurrentBehavior();
            this.currentBehavior = new BehaviorHoldPedAtGunpoint(this.partner, ped, true);
        }

        public void FollowInVehicle(CVehicle vehicle)
        {
            // TODO: Priorities.
            this.AbortCurrentBehavior();

            this.currentBehavior = new BehaviorFollowPlayerInVehicle(this.partner, vehicle);
        }

        public void CoverTarget(CPed target)
        {
            this.AbortCurrentBehavior();
            this.currentBehavior = new BehaviorCoverTarget(this.partner, target);
        }

        public void ForceIdle()
        {
            this.AbortCurrentBehavior();
            this.partner.PartnerPed.Task.ClearAll();
        }

        public void HoldPedAtGunpoint(CPed ped)
        {
            this.AbortCurrentBehavior();

            this.currentBehavior = new BehaviorHoldPedAtGunpoint(this.partner, ped, false);
        }

        public void HoldPosition()
        {
            this.AbortCurrentBehavior();
            this.SetIsInPlayerGroup(false);

            // Stand still and play ambient animations, such as walkie talkie.
            this.currentBehavior = new AnonymousBehavior(
                delegate
                {
                    if (!this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill))
                    {
                        this.partner.PartnerPed.Task.AlwaysKeepTask = true;
                        this.partner.PartnerPed.Task.StandStill(Int32.MaxValue);

                        // Assign stand still task again (only not when using walkie talkie)
                        if (!this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie))
                        {
                            // Random chance to use walkie talkie
                            int randomValue = Common.GetRandomValue(0, 10);
                            if (randomValue < 2)
                            {
                                TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie("WALKIE_TALKIE");
                                taskWalkieTalkie.AssignTo(this.partner.PartnerPed, ETaskPriority.MainTask);

                                // Play sound
                                DelayedCaller.Call(
                                    delegate
                                    {
                                        if (this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie) && !AudioHelper.IsBusy)
                                        {
                                            if (this.partner.PartnerPed.Animation.isPlaying(new AnimationSet("missemergencycall"), "idle_answer_radio_a"))
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

                    return EBehaviorState.Running;
                });
        }

        public void MoveToPosition(Vector3 position)
        {
            this.ForceIdle();

            this.currentBehavior = new AnonymousBehavior(
                delegate
                {
                    if (this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimplePauseSystemTimer))
                    {
                        return EBehaviorState.Success;
                    }

                    if (!this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexUseSequence))
                    {
                        GTA.TaskSequence sequence = new GTA.TaskSequence();
                        sequence.AddTask.RunTo(position, true);
                        sequence.AddTask.Wait(5000);
                        this.partner.PartnerPed.Task.PerformSequence(sequence);
                        sequence.Dispose();
                    }

                    return EBehaviorState.Running;
                },
                () => this.partner.PartnerPed.Task.ClearAll());
        }

        public void SetIsInPlayerGroup(bool isInGroup)
        {
            if (isInGroup)
            {
                CPlayer.LocalPlayer.Group.AddMember(this.partner.PartnerPed, true);
            }
            else
            {
                CPlayer.LocalPlayer.Group.RemoveMember(this.partner.PartnerPed);
            }
        }

        public VehicleSeat FindSeatForVehicle(CVehicle vehicle)
        {
            return  this.FindSeatForVehicle(vehicle, false);
        }

        public VehicleSeat FindSeatForVehicle(CVehicle vehicle, bool allowDriverSeat)
        {
            VehicleSeat[] freeSeats = new VehicleSeat[] { VehicleSeat.RightFront, VehicleSeat.LeftRear, VehicleSeat.RightRear };
            if (allowDriverSeat)
            {
                freeSeats = new VehicleSeat[] { VehicleSeat.Driver, VehicleSeat.RightFront, VehicleSeat.LeftRear, VehicleSeat.RightRear };
            }

            foreach (VehicleSeat vehicleSeat in freeSeats)
            {
                if (!vehicle.IsSeatFree(vehicleSeat))
                {
                    CPed pedOnSeat = vehicle.GetPedOnSeat(vehicleSeat);
                    if (pedOnSeat != null && pedOnSeat.Exists() && pedOnSeat.IsAliveAndWell) continue;
                }

                EventPartnerWantsToEnterVehicle eventPartnerWantsToEnterVehicle = new EventPartnerWantsToEnterVehicle(this.partner, vehicle, vehicleSeat);
                if (eventPartnerWantsToEnterVehicle.Result)
                {
                    Log.Debug("Got seat: " + vehicleSeat, "PartnerIntelligence");
                    return vehicleSeat;
                }
            }

            return VehicleSeat.None;
        }

        private void GoOnDutyScript_PlayerWentOnDuty()
        {
            Log.Caller();

            // Enter the vehicle now.
            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                this.partner.PartnerPed.Task.ClearAll();
                this.partner.PartnerPed.Task.EnterVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle, VehicleSeat.RightFront);
            }
        }

        private void EventPlayerWarped_EventRaised(EventPlayerWarped @event)
        {
            Log.Caller();

            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                this.partner.PartnerPed.WarpIntoVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle, VehicleSeat.AnyPassengerSeat);
            }
            else
            {
                this.partner.PartnerPed.Position = CPlayer.LocalPlayer.Ped.Position;
            }
        }

        private void EventPlayerStartedArrest_EventRaised(EventPlayerStartedArrest @event)
        {
            Log.Caller();

            // Are we arresting at the moment?
            if (this.currentBehavior is BehaviorSupportArrest)
            {
                // Only leave current suspect if at least one partner takes care of him.
                EventPartnerWantsToSupportArresting eventPartnerWantsToSupportArresting =
                    new EventPartnerWantsToSupportArresting(this.partner, (this.currentBehavior as BehaviorSupportArrest).Arrest);

                if (eventPartnerWantsToSupportArresting.Result)
                {
                    Log.Debug("No partner is assisting with current suspect", "PartnerIntelligence");
                    return;
                }
            }

            this.arrestScript = @event.Arrest;

            // TODO: Priorities.
            this.AbortCurrentBehavior();
        }

        void EventPlayerStartedFrisk_EventRaised(EventPlayerStartedFrisk @event)
        {
            if (!(this.currentBehavior is BehaviorSupportArrest))
            {
                this.AbortCurrentBehavior();

                this.partner.PartnerPed.EnsurePedHasWeapon();
                this.partner.PartnerPed.Task.AimAt(@event.PedBeingFrisked, Int32.MaxValue);
                this.partner.PartnerPed.SetFlashlight(true, true, false);
                this.currentBehavior = new AnonymousBehavior(
                    delegate
                    {
                        if (!@event.Frisk.Suspect.Exists() || !@event.Frisk.Suspect.Wanted.IsBeingFrisked)
                        {
                            this.partner.PartnerPed.Task.ClearAll();
                            return EBehaviorState.Success;
                        }

                        return EBehaviorState.Running;
                    });
            }
        }

        void EventPlayerStartedPullover_EventRaised(EventPlayerStartedPullover @event)
        {
            this.AbortCurrentBehavior();
            this.currentBehavior = new BehaviorSupportPullover(this.partner, @event.Pullover);
        }

        void PoliceDepartmentManager_PlayerEnteredLeftPD(PoliceDepartment policeDepartment, bool entered)
        {
            if (!entered)
            {
                // Teleport.
                this.partner.PartnerPed.Position = policeDepartment.Position;
                this.partner.PartnerPed.CurrentRoom = CPlayer.LocalPlayer.Ped.CurrentRoom;
                this.partner.PartnerPed.Task.ClearAll();

                // Not sure why, but some PDs fuck up the partner so he won't move after being teleported.
                this.partner.Reset();

                // If vehicle selection is in progress, don't move.
                if (Main.GoOnDutyScript.IsInProgress)
                {
                    this.partner.PartnerPed.Task.StandStill(-1);
                }
            }
            else
            {
                if (this.partner.PartnerPed.IsInVehicle)
                {
                    this.partner.PartnerPed.WarpFromCar(CPlayer.LocalPlayer.Ped.Position.Around(1f));
                }

                // Teleport.
                this.partner.PartnerPed.Position = CPlayer.LocalPlayer.Ped.Position.Around(1f);
                this.partner.PartnerPed.CurrentRoom = CPlayer.LocalPlayer.Ped.CurrentRoom;
                this.partner.PartnerPed.Task.ClearAll();
            }
        }

        private bool EventPartnerWantsToEnterVehicle_EventRaised(EventPartnerWantsToEnterVehicle @event)
        {
            Log.Caller();

            // Ignore events sent by us.
            if (@event.Partner == this.partner) return true;

            // Check whether we occupy this seat already.
            if (this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
            {
                TaskGetInVehicle getInVehicle = (TaskGetInVehicle)this.partner.PartnerPed.Intelligence.TaskManager.FindTaskWithID(ETaskID.GetInVehicle);
                if (@event.Vehicle == getInVehicle.Vehicle)
                {
                    if (@event.Seat == getInVehicle.Seat)
                    {
                        // If partner has priority over us.
                        if (@event.Partner.PartnerGroup < this.partner.PartnerGroup)
                        {
                            Log.Debug("Other partner has priority over us", "PartnerIntelligence");

                            // Abort our own task and let other partner know he can enter.
                            this.partner.PartnerPed.Intelligence.TaskManager.Abort(getInVehicle);
                            this.getInVehicleTask = null;
                            return true;
                        }

                        return false;
                    }
                }
            }

            return true;
        }

        bool EventPartnerWantsToSupportArresting_EventRaised(EventPartnerWantsToSupportArresting @event)
        {
            Log.Caller();

            // Ignore events sent by us.
            if (@event.Partner == this.partner) return true;

            if (this.currentBehavior is BehaviorSupportArrest)
            {
                // If we are supporting the same arrest, return false.
                if ((this.currentBehavior as BehaviorSupportArrest).Arrest == @event.Arrest)
                {
                    Log.Debug("We're arresting the same suspect", "PartnerIntelligence");
                    return false;
                }
            }

            return true;
        }

        static Func<Behavior> Selector(Func<bool> cond, Func<Behavior> ifTrue, Func<Behavior> ifFalse)
        {
            return cond() ? ifTrue : ifFalse;
        }

        static Func<Behavior> Selector(bool cond, Func<Behavior> ifTrue, Func<Behavior> ifFalse)
        {
            return cond ? ifTrue : ifFalse;
        }

        static Action Sequencer(Action a, Action b)
        {
            return () =>
            {
                a();
                b();
            };
        }

        static Func<Behavior> Decorator(Func<bool> cond, Func<Behavior> ifTrue)
        {
            return cond() ? ifTrue : (() => null);
        }

        static Func<Behavior> Decorator(bool cond, Func<Behavior> ifTrue)
        {
            return cond ? ifTrue : (() => null );
        }

        static bool Inverter(bool result)
        {
            return !result;
        }

        private void AbortCurrentBehavior()
        {
            if (this.currentBehavior != null)
            {
                this.currentBehavior.Abort();
                this.currentBehavior = null;
                Log.Debug("AbortCurrentBehavior: Aborted", "PartnerIntelligence");
            }
        }

        private bool AreWeInPlayersVehicle()
        {
            if (CPlayer.LocalPlayer.Ped.CurrentVehicle != null && CPlayer.LocalPlayer.Ped.CurrentVehicle.Exists())
            {
                return this.partner.PartnerPed.IsSittingInVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle);
            }

            return false;
        }

        private Behavior PlayerNotYetOnDuty()
        {
            if (Main.GoOnDutyScript.IsInProgress)
            {
                this.partner.PartnerPed.Task.StandStill(-1);
            }

            return null;
        }

        private Behavior ProcessDriving()
        {
            return null;
        }

        private Behavior EnterVehicle()
        {
            AnonymousBehavior behavior = new AnonymousBehavior(
                delegate
                {
                    // We want to enter the vehicle as well.
                    if (!this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                    {
                        CVehicle vehicle = CPlayer.LocalPlayer.GetVehiclePlayerWouldEnter();
                        if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                        {
                            vehicle = CPlayer.LocalPlayer.Ped.CurrentVehicle;
                        }

                        if (vehicle != null && vehicle.Exists())
                        {
                            if (this.partner.PartnerPed.IsInVehicle(vehicle))
                            {
                                return EBehaviorState.Success;
                            }

                            if (this.partner.PartnerPed.Intelligence.IsVehicleBlacklisted(vehicle))
                            {
                                Log.Debug("Vehicle is blacklisted for partner", "PartnerIntelligence");
                                return EBehaviorState.Failed;
                            }

                            // Find out where we can enter.
                            VehicleSeat seat = this.FindSeatForVehicle(vehicle);
                            if (seat == VehicleSeat.None)
                            {
                                this.partner.PartnerPed.Intelligence.AddVehicleToBlacklist(vehicle, 5000);
                                Log.Debug("Process: Failed to find seat", "PartnerIntelligence");
                                return EBehaviorState.Failed;
                            }

                            this.SetIsInPlayerGroup(false);
                            Log.Debug("Removed partner from player group", "PartnerIntelligence");

                            this.partner.PartnerPed.Task.ClearAll();
                            this.getInVehicleTask = new TaskGetInVehicle(vehicle, seat, seat, false, true);
                            this.getInVehicleTask.EnterStyle = EPedMoveState.Run;
                            this.getInVehicleTask.AssignTo(this.partner.PartnerPed, ETaskPriority.MainTask);
                            Log.Debug("Process: Made partner enter vehicle", "PartnerIntelligence");
                        }
                        else
                        {
                            this.partner.PartnerPed.Debug = "No player vehicle found";
                            this.partner.PartnerPed.Task.StandStill(-1);
                            return EBehaviorState.Failed;
                        }
                    }

                    return EBehaviorState.Running;
                });

            return behavior;
        }

        private Behavior FollowPlayer()
        {
            if (!CPlayer.LocalPlayer.Ped.IsInVehicle && !this.partner.PartnerPed.IsInVehicle && !this.partner.PartnerPed.IsEnteringVehicle)
            {
                if (!CPlayer.LocalPlayer.Group.isMember(this.partner.PartnerPed))
                {
                    Log.Debug("Readded partner to player group", "PartnerIntelligence");
                    this.SetIsInPlayerGroup(true);
                }
            }

            // Weapon logic
            if (CPlayer.LocalPlayer.Ped.IsArmed())
            {
                // Get current weapon
                Weapon currentWeapon = CPlayer.LocalPlayer.Ped.Weapons.Current;
                if (this.partner.PartnerPed.Weapons.Current != currentWeapon && !this.blockSwitching)
                {
                    // Player must not be switching at the moment
                    if (this.weaponSwitchTimer.CanExecute(true) && !CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleSwapWeapon))
                    {
                        this.partner.PartnerPed.EnsurePedHasNoWeapon(true);
                        this.partner.PartnerPed.Weapons[currentWeapon].Ammo = 1000;
                        this.partner.PartnerPed.PedData.DefaultWeapon = currentWeapon;
                        this.blockSwitching = true;
                        this.weaponSwitchTimer.Reset();
                        DelayedCaller.Call(delegate { this.blockSwitching = false; }, this, 2000);
                    }
                }
            }

            return null;
        }

        private Behavior LeaveVehicle()
        {
            AnonymousBehavior anonymousBehavior = new AnonymousBehavior(
                delegate
                {
                    if (this.partner.PartnerPed.IsInVehicle && !this.partner.PartnerPed.IsGettingOutOfAVehicle)
                    {
                        this.partner.PartnerPed.Task.LeaveVehicle(this.partner.PartnerPed.CurrentVehicle, true);
                    }

                    return !this.partner.PartnerPed.IsInVehicle ? EBehaviorState.Success : EBehaviorState.Running;
                });

            return anonymousBehavior;
        }

        private Behavior AssistWithArrest()
        {
            // If no script is set, find one without a partner assigned (this can happen if partner is recruited while arrest is already running).
            if (this.arrestScript == null)
            {
                // Get all arrest scripts.
                BaseScript[] baseScripts = Main.ScriptManager.GetRunningScriptInstances("Arrest");
                if (baseScripts.Length > 0)
                {
                    // Skip the cuffed suspects.
                    Arrest[] scripts = (from script in baseScripts where script is Arrest && !((Arrest)script).Suspect.Wanted.IsCuffed select (Arrest)script).ToArray();
                    foreach (Arrest arrest in scripts)
                    {
                        EventPartnerWantsToSupportArresting eventPartnerWantsToSupportArresting = new EventPartnerWantsToSupportArresting(this.partner, arrest);
                        if (eventPartnerWantsToSupportArresting.Result)
                        {
                            // If we found a script, this function will work next tick.
                            Log.Debug("Found arrest", "PartnerIntelligence");
                            this.arrestScript = arrest;
                        }
                    }

                    // If still null (because all instances have at least one partner assigned), pick the first one. TODO: Use the one with the least partners.
                    if (scripts.Length > 0 && this.arrestScript == null)
                    {
                        this.arrestScript = scripts.First();
                    }
                }

                // Still no script, abort.
                if (this.arrestScript == null)
                {
                    return null;
                }
            }

            // Set current arrest script to null, so we have to dynamically find one next time.
            BehaviorSupportArrest supportArrest = new BehaviorSupportArrest(this.partner, this.arrestScript);
            this.arrestScript = null;
            return supportArrest;
        }
    }
}