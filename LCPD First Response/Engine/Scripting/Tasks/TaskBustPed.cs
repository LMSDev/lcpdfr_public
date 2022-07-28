namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Timers;

    internal class TaskBustPed : PedTask
    {
        private const float DistanceToStartCuff = 6;

        private PedTask runningTask;
        private CPed target;
        private NonAutomaticTimer timerKeepAimingBeforeCuffing;

        /// <summary>
        /// The timer for expensive actions, such as firing arrest event to nearby peds and checking for combat in area.
        /// </summary>
        private NonAutomaticTimer timerExpensiveActions;

        public TaskBustPed(CPed ped) : base(ETaskID.BustPed)
        {
            this.target = ped;
            // Before cuffing, aim for at least 2,5 seconds
            this.timerKeepAimingBeforeCuffing = new NonAutomaticTimer(2500);
            this.timerExpensiveActions = new NonAutomaticTimer(500);
        }

        /// <summary>
        /// Gets or sets a value indicating whether cuffing is blocked and the cop can only aim and the ped and not proceed any further. Setting this to false will
        /// immediately allow cuffing again.
        /// </summary>
        public bool BlockCuffing { get; set; }

        public override void MakeAbortable(CPed ped)
        {
            if (this.runningTask != null)
            {
                this.runningTask.MakeAbortable(this.target);
            }

            if (ped.Exists())
            {
                if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    ped.Task.ClearAll();
                }
            }

            SetTaskAsDone();
        }

        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

        //http://www.gtamodding.com/index.php?title=TASK_GO_STRAIGHT_TO_COORD
        //http://www.gtamodding.com/index.php?title=TASK_GO_STRAIGHT_TO_COORD_AIMING

            // Apply aim task at first
            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGun))
            {
                ped.EnsurePedHasWeapon();
                this.runningTask = new TaskArrestPed(this.target, 8f, 5000);
                this.runningTask.AssignTo(ped, ETaskPriority.MainTask);
            }

            // Notify close peds
            new EventPedBeingArrested(this.target);
        }

        public override void Process(CPed ped)
        {
            if (!this.target.Exists() || !this.target.IsAliveAndWell)
            {
                MakeAbortable(ped);
                return;
            }

            // If target has been arrested and scenario task is not active (so our cop is not the suspect transporter driver)
            if (this.target.Wanted.HasBeenArrested)
            {
                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.Scenario))
                {
                    MakeAbortable(ped);
                }
                return;
            }

            bool doExpensiveStuffThisTick = this.timerExpensiveActions.CanExecute();

            // To reduce spamming of this event, only the arresting ped will fire it
            if (doExpensiveStuffThisTick && this.target.Wanted.ArrestedBy == ped)
            {
                new EventPedBeingArrested(this.target);
            }
            else
            {
                // If arresting by player, make sure this ped is not cuffing atm
                if ((this.target.Wanted.IsBeingArrestedByPlayer || this.target.Wanted.IsBeingFrisked) && ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CuffPed))
                {
                    TaskCuffPed taskCuffped = ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CuffPed) as TaskCuffPed;
                    taskCuffped.MakeAbortable(ped);
                }
            }

            // If the target doesn't have the being busted task running, apply
            if (!this.target.Intelligence.TaskManager.IsTaskActive(ETaskID.BeingBusted))
            {
                if (!this.target.Wanted.IsBeingArrestedByPlayer || ped.PedGroup == EPedGroup.Player)
                {
                    // Check if the target will surrender (if not already resisted)
                    if (!this.target.Wanted.ResistedArrest && !this.target.Wanted.IsDeciding)
                    {
                        this.target.Intelligence.OnBeingArrested(ped);
                    }

                    if (this.target.Wanted.Surrendered && !this.target.Wanted.IsDeciding)
                    {
                        if (!this.target.IsRagdoll && !this.target.IsGettingUp)
                        {
                            this.runningTask = new TaskBeingBusted(ped);
                            this.runningTask.AssignTo(this.target, ETaskPriority.MainTask);
                            //this.target.Wanted.ArrestedBy.AttachBlip().Color = BlipColor.White;
                        }
                    }
                    else
                    {
                        // Run this task until the criminal decided
                        if (!this.target.Wanted.IsDeciding)
                        {
                            MakeAbortable(ped);
                        }
                    }
                }

                return;
            }

            // If area in combat, cancel busting
            if (doExpensiveStuffThisTick && CPed.IsPedInCombatInArea(ped.Position, 25f))
            {
                Log.Debug("Process: Combat while busting", this);
                this.MakeAbortable(ped);
                return;
            }


            // If ped is cuffed, abort
            if (this.target.Wanted.IsCuffed)
            {
                // Ped is cuffed, abort busting task
                this.MakeAbortable(ped);
                return;
            }

            // If close to the target (using 2D here because height doesn't matter), cuff. If not, arrest first
            if (ped.Position.DistanceTo2D(this.target.Position) < DistanceToStartCuff)
            {
                ped.Debug = "Close enough to cuff";
                // Check if the initial aiming task has finished
                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.ArrestPed))
                {
                    // We started the arrest, so we are allowed to cuff
                    if (this.target.Wanted.ArrestedBy == ped && !this.BlockCuffing)
                    {
                        ped.Debug = "Will cuff";
                        //this.target.Wanted.ArrestedBy.AttachBlip().Color = BlipColor.Orange;
                        // If cuff task isn't running
                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CuffPed))
                        {
                            // Keep aiming for at least 2,5seconds
                            if (this.timerKeepAimingBeforeCuffing.CanExecute(true))
                            {
                                ped.Task.ClearAll();
                                this.runningTask = new TaskCuffPed(this.target);
                                this.runningTask.AssignTo(ped, ETaskPriority.MainTask);
                            }
                            else
                            {
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun))
                                {
                                    // If not already aiming
                                    ped.Task.AimAt(this.target, 5000);
                                }
                            }
                        }
                    }
                    else
                    {
                        ped.Debug = "Not allowed to cuff";
                        // If really close
                        if (ped.Position.DistanceTo2D(this.target.Position) <= DistanceToStartCuff / 2)
                        {
                            ped.Debug = "Too close to target, fleeing";
                            // Don't get too close to prevent blocking the cuff task
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                            {
                                ped.EnsurePedHasWeapon();
                                // Retreat task would be quite too fast, so we use flee here
                                ped.Task.FleeFromChar(this.target, false, 1500);
                            }
                        }
                        else
                        {
                            // If not, simply aim
                            ped.Debug = "In range to cuff, but not allowed = aim";
                            this.runningTask = new TaskArrestPed(this.target, 8, 20000);
                            this.runningTask.AssignTo(ped, ETaskPriority.MainTask);
                        }
                    }
                }
            }
            else
            {
                ped.Debug = "Not in range to cuff, get closer while aiming";
                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.ArrestPed))
                {
                    // Check if the initial aiming task has finished
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun))
                    {
                        // Cops that only help busting should be around 8 metres away to prevent them from blocking the bust process. The busting cop can go really close
                        float distanceToStop = 8;
                        if (this.target.Wanted.ArrestedBy == ped) distanceToStop = 4;

                        this.runningTask = new TaskArrestPed(this.target, distanceToStop, 5000);
                        this.runningTask.AssignTo(ped, ETaskPriority.MainTask);
                    }
                }
            }
        }

        public override string ComponentName
        {
            get { return "TaskBustPed"; }
        }
    }
}
