
namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Timers;
    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;

    internal class TaskBeingBusted : PedTask
    {
        public bool HasSurrendered { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the task has a vehicle assigned.
        /// </summary>
        public bool HasVehicle { get; private set; }

        private bool appliedMoveTask;
        private CPed bustedBy;
        private bool cuffAnimPlayed;

        /// <summary>
        /// Whether the suspect has changed their heading already.
        /// </summary>
        private bool hasChangedHeading;

        /// <summary>
        /// The vehicle to use;
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Used to prevent an instant check for the cuff anim just after it has been applied (because the animation sometimes hasn't already been started by the game)
        /// </summary>
        private Timer timerAnimationCheck;

        /// <summary>
        /// The timer to measure the timeout when getting into the car.
        /// </summary>
        private NonAutomaticTimer timerGetIntoCar;

        /// <summary>
        /// The timer to prevent stopping nearby vehicles occuring every tick.
        /// </summary>
        private NonAutomaticTimer timerStopNearbyVehicles;

        public TaskBeingBusted(CPed ped) : base(ETaskID.BeingBusted)
        {
            this.bustedBy = ped;
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            ped.Wanted.ArrestedBy = null;
            ped.Wanted.IsBeingArrested = false;
            ped.Wanted.IsStopped = false;
            this.SetTaskAsDone();

            // Abort pending calls
            DelayedCaller.ClearAllRunningCalls(false, this);
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            // Only do so if ped is not cuffed
            if (!ped.Wanted.IsCuffed)
            {
                this.timerAnimationCheck = new Timer(1000, ETimerOptions.OneTimeReturnTrue);
                this.timerStopNearbyVehicles = new NonAutomaticTimer(500);

                // Kill possible flee task
                if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.FleeEvadeCops))
                {
                    PedTask task = ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.FleeEvadeCops);
                    task.MakeAbortable(ped);
                }

                // Ped will surrender, so apply some flags to prevent the internal bust task to make the ped flee again
                ped.Wanted.IsBeingArrested = true;
                ped.Wanted.IsStopped = true;
                ped.Wanted.ArrestedBy = this.bustedBy;

                ped.Wanted.Surrendered = true;
                ped.BlockPermanentEvents = true;
                ped.Task.AlwaysKeepTask = true;

                // If ped has been asked to surrender and hands are in the air, don't cancel tasks to prevent fucked up animation
                if (!ped.Wanted.IsBeingArrestedByPlayer)
                {
                    ped.Task.ClearAllImmediately();
                }

                if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask))
                {
                    ped.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexShockingEventFlee);
                    ped.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexCombat);
                    ped.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexCombatRetreatSubtask);
                    ped.Task.ClearAll();
                }

                // Just incase the ped still carries a weapon, drop
                ped.DropCurrentWeapon();

                AnimationSet animationSet = new AnimationSet("busted");
                // Flag documentation: (Keep in mind that flags can behave different on different animations)
                // Unknown12 = Animation can be aborted (e.g. to aim), Unknown09 = balance upper body (so animation as well as euphoria). No lower body animation
                // Unknown11 = Allow movement while animation is running
                // Unknown02 = Freeze position
                // Unknown05 = Loop (restart)
                // Unknown06 = animation doesn't end
                ped.Task.PlayAnimation(animationSet, "idle_2_hands_up", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
            }
        }

        public override void Process(CPed ped)
        {
            // If the arresting ped no longer exists or is no longer busting, cancel
            if (ped.Wanted.ArrestedBy == null || !ped.Wanted.ArrestedBy.Exists())
            {
                // Only important when not yet cuffed
                if (!ped.Wanted.IsCuffed)
                {
                    Log.Debug("Process: ArrestedBy is invalid", this);
                    this.MakeAbortable(ped);
                    return;
                }
            }
            else
            {
                // If this ped is not yet cuffed, it requires the bust task to be executed by the busting ped in order to be cuffed in the first place. So if this task is not running
                // and we are not yet cuffed, cancel so other cops can start busting (and cuffing) us.
                if (!this.cuffAnimPlayed)
                {
                    if (!ped.Wanted.ArrestedBy.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed) && !ped.Wanted.IsBeingArrestedByPlayer)
                    {
                        Log.Debug("Process: ArrestedBy is no longer busting but target is not yet cuffed", this);
                        this.MakeAbortable(ped);
                        return;
                    }
                }
            }

            // If cuffed ped should get into a cop vehicle
            if (ped.Wanted.IsCuffed)
            {
                // Get vehicle
                if (this.HasVehicle)
                {
                    if (this.vehicle != null && this.vehicle.Exists())
                    {
                        // If the bust task is not active and our ped is in a police vehicle, set as arrested
                        if (ped.IsInVehicle(this.vehicle))
                        {
                            ped.CurrentVehicle.IsSuspectTransporter = true;
                            ped.Wanted.HasBeenArrested = true;
                            ped.BlockPermanentEvents = true;

                            // If flag is set, remove blip
                            if (!ped.DontRemoveBlipWhenBusted)
                            {
                                ped.DeleteBlip();
                            }

                            this.MakeAbortable(ped);
                            return;
                        }

                        // Enter vehicle
                        if (ped.Position.DistanceTo(this.vehicle.Position) < 10)
                        {
                            if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                            {
                                TaskGetInVehicle taskGetInVehicle = new TaskGetInVehicle(this.vehicle, VehicleSeat.RightRear, VehicleSeat.LeftRear, true, true);
                                taskGetInVehicle.AssignTo(ped, ETaskPriority.MainTask);
                            }
                        }
                        else
                        {
                            if (this.timerGetIntoCar == null)
                            {
                                this.timerGetIntoCar = new NonAutomaticTimer(20000);
                            }

                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexControlMovement) || !this.appliedMoveTask)
                            {
                                ped.Task.GoTo(this.vehicle.Position);
                                this.appliedMoveTask = true;
                            }

                            if (this.timerGetIntoCar.CanExecute())
                            {
                                // If ped doesn't move, teleport
                                float distance = ped.Position.DistanceTo(this.vehicle.Position);

                                DelayedCaller.Call(
                                    delegate
                                        {
                                            if (!this.vehicle.Exists())
                                            {
                                                Log.Warning("Process: Vehicle disposed while attempting to teleport", this);
                                                return;
                                            }

                                            // If distance is still the same, teleport to vehicle
                                            if (ped.Position.DistanceTo(this.vehicle.Position) == distance)
                                            {
                                                ped.Position = this.vehicle.Position.Around(3f);
                                                this.appliedMoveTask = false;
                                            }
                                        },
                                        this,
                                        1000);
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("Process: HasVehicle is true but vehicle doesn't exist", this);
                    }
                }
                else
                {
                    // Wander around (only if not arrested by player)
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
                    {
                        if (!ped.Wanted.IsBeingArrestedByPlayer)
                        {
                            ped.Task.WanderAround();
                        }
                    }
                }

                return;
            }
            else
            {
                if (this.timerStopNearbyVehicles.CanExecute())
                {
                    // Suspect is not yet cuffed, to prevent problems with nearby cars, we stop the whole traffic
                    CVehicle[] vehicles = ped.Intelligence.GetVehiclesAround(25f, EVehicleSearchCriteria.NoCarsWithCopDriver | EVehicleSearchCriteria.NoPlayersLastVehicle | EVehicleSearchCriteria.DriverOnly);
                    foreach (CVehicle vehicle in vehicles)
                    {
                        if (vehicle.Exists() && vehicle.Driver.PedGroup != EPedGroup.Player && vehicle.Driver.PedGroup != EPedGroup.Cop)
                        {
                            if (!vehicle.Driver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                            {
                                vehicle.Driver.Task.CarTempAction(ECarTempActionType.SlowDownSoftly, 4000);
                            }
                        }
                    }
                }
            }

            // If being cuffed task is active and cop plays anim, ensure our anim is played
            if (ped.Wanted.ArrestedBy.Intelligence.TaskManager.IsTaskActive(ETaskID.CuffPed))
            {
                // If this ped is the current target of the cuffing task and the anim has been played.
                TaskCuffPed taskCuffPed = (TaskCuffPed)ped.Wanted.ArrestedBy.Intelligence.TaskManager.FindTaskWithID(ETaskID.CuffPed);
                if (taskCuffPed.IsTarget(ped) && taskCuffPed.AnimPlayed)
                {
                    AnimationSet animationSet = new AnimationSet("cop");
                    bool isPlaying = ped.Animation.isPlaying(animationSet, "crim_cuffed");
                    bool isLastFrame = ped.Animation.GetCurrentAnimationTime(animationSet, "crim_cuffed") == 1.0;

                    // If anim has been played, set as cuffed
                    if (this.timerAnimationCheck.CanExecute(true) && this.cuffAnimPlayed && isLastFrame)
                    {
                        ped.BlockGestures = true;
                        ped.Wanted.IsCuffed = true;

                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.PlayAnimationAndRepeat))
                        {
                            TaskPlayAnimationAndRepeat taskPlayAnimationAndRepeat = new TaskPlayAnimationAndRepeat("idle", "move_m@h_cuffed", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                            taskPlayAnimationAndRepeat.AssignTo(ped, ETaskPriority.MainTask);
                        }

                        if (!ped.Wanted.IsBeingArrestedByPlayer)
                        {
                            // Start scenario to be driven away
                            Log.Debug("Process: Creating ScenarioArrestedPedAndDriveAway for ped: " + ped.Handle, this);
                            var scenarioArrestedPedAndDriveAway = new ScenarioArrestedPedAndDriveAway(ped.Wanted.ArrestedBy, ped);
                            TaskScenario taskScenario = new TaskScenario(scenarioArrestedPedAndDriveAway, false);
                            taskScenario.AssignTo();
                        }

                        return;
                    }
                    // If anim is not yet playing, play
                    if (!isPlaying)
                    {
                        ped.Animation.Play(animationSet, "crim_cuffed", 1.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09 | AnimationFlags.Unknown06);
                        this.cuffAnimPlayed = true;
                    }
                }

                if (!this.hasChangedHeading)
                {
                    // Achieve heading of busting cop
                    ped.Task.AchieveHeading(this.bustedBy.Heading);
                    this.hasChangedHeading = true;
                }
            }

            // HACK: Don't allow fleeing
            //if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask))
            //{
            //    ped.Task.ClearAll();
            //}
        }

        /// <summary>
        /// Sets the vehicle to use to <paramref name="vehicle"/>.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        public void SetVehicleToUse(CVehicle vehicle)
        {
            this.vehicle = vehicle;
            this.HasVehicle = true;
        }

        public override string ComponentName
        {
            get { return "TaskBeingBusted"; }
        }
    }
}
