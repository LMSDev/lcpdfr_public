namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Timers;

    using Timer = GTA.Timer;

    class TaskFleeEvadeCops : PedTask
    {
        private const float VehicleScanRange = 15;

        private bool allowTakingHostages;
        private bool allowVehicles;
        private bool allowWeapons;
        private NonAutomaticTimer fleeingCriminalEventTimer;
        private bool visualLostSaved;
        private EVehicleSearchCriteria vehicleSearchCriteria;

        private TaskFleeEvadeCopsInVehicle taskFleeInVehicle;
        private TaskFleeEvadeCopsOnFoot taskFleeOnFoot;

        private bool suspended;

        /// <summary>
        /// Gets or sets a value indicating whether vehicles are allowed.
        /// </summary>
        public bool AllowVehicles
        {
            get
            {
                return this.allowVehicles;
            }

            set
            {
                this.allowVehicles = value;

                if (this.taskFleeOnFoot != null)
                {
                    this.taskFleeOnFoot.AllowVehicles = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether weapons are allowed.
        /// </summary>
        public bool AllowWeapons
        {
            get
            {
                return this.allowWeapons;
            }

            set
            {
                this.allowWeapons = value;

                if (this.taskFleeOnFoot != null)
                {
                    this.taskFleeOnFoot.AllowWeapons = value;
                }

                if (this.taskFleeInVehicle != null)
                {
                    this.taskFleeInVehicle.AllowWeapons = value;
                }
            }
        }

        /// <summary>
        /// Do not directly use this, but rather create an instance of chase
        /// </summary>
        /// <param name="allowTakingHostages"></param>
        /// <param name="allowVehicles"></param>
        /// <param name="vehicleSearchCriteria"></param>
        /// <param name="allowWeapons"></param>
        public TaskFleeEvadeCops(bool allowTakingHostages, bool allowVehicles, EVehicleSearchCriteria vehicleSearchCriteria, bool allowWeapons) : base(ETaskID.FleeEvadeCops)
        {
            this.allowTakingHostages = allowTakingHostages;
            this.allowVehicles = allowVehicles;
            this.vehicleSearchCriteria = vehicleSearchCriteria;
            this.allowWeapons = allowWeapons;

            this.fleeingCriminalEventTimer = new NonAutomaticTimer(3000);
        }

        public override void MakeAbortable(CPed ped)
        {
            // Abort subtasks
            if (this.taskFleeInVehicle != null)
            {
                this.taskFleeInVehicle.MakeAbortable(ped);
            }
            if (this.taskFleeOnFoot != null)
            {
                this.taskFleeOnFoot.MakeAbortable(ped);
            }
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            if (ped.Wanted.HasBeenArrested)
            {
                MakeAbortable(ped);
                return;
            }
            if (this.suspended) return;

            if (!ped.Wanted.VisualLost)
            {
                // Fire event, but not all the time, the performance impact is too big
                if (this.fleeingCriminalEventTimer.CanExecute())
                {
                    new EventFleeingCriminal(ped);
                }
            }

            // Process visual lost
            if (ped.Wanted.OfficersVisual == 0 && !CPlayer.LocalPlayer.HasVisualOnSuspect(ped))
            {
                ped.Wanted.VisualLostSince++;
                //Game.DisplayText("Visual Lost Since: " + ped.Wanted.VisualLostSince + "   Visual Lost: " + ped.Wanted.VisualLost);

                // If visual is lost for more than 20seconds, set state and save data
                if (ped.Wanted.VisualLostSince > 200)
                {

                    // Save 'last known' data
                    if (!this.visualLostSaved)
                    {
                        Log.Debug("Visual lost", this);

                        ped.Wanted.LastKnownInVehicle = ped.IsInVehicle;
                        ped.Wanted.LastKnownOnFoot = !ped.Wanted.LastKnownInVehicle;
                        ped.Wanted.LastKnownPosition = ped.Position;
                        if (ped.Wanted.LastKnownInVehicle)
                        {
                            ped.Wanted.LastKnownVehicle = ped.CurrentVehicle;
                        }

                        this.visualLostSaved = true;

                        
                    }

                    if (ped.Position.DistanceTo(ped.Wanted.LastKnownPosition) > 20.0f)
                    {
                        if (!ped.Wanted.VisualLost)
                        {
                            new EventVisualLost(ped);
                            ped.Wanted.VisualLost = true;

                            // Reset flags, so ped actually gets a new chance when found again (so drop weapon, arrest etc. can be performed again)
                            ped.Wanted.ResetArrestFlags();
                        }
                    }
                }
            }
            else
            {
                //Game.DisplayText("Officers visual: " + ped.Wanted.OfficersVisual.ToString());

                // Reset everything
                ped.Wanted.LastKnownInVehicle = false;
                ped.Wanted.LastKnownOnFoot = false;
                ped.Wanted.LastKnownPosition = Vector3.Zero;
                ped.Wanted.LastKnownVehicle = null;
                ped.Wanted.VisualLost = false;
                ped.Wanted.VisualLostSince = 0;
                this.visualLostSaved = false;
            }

            // Process in vehicle escape
            if (ped.IsSittingInVehicle())
            {
                // Cancel on foot task if running
                if (this.taskFleeOnFoot != null)
                {
                    this.taskFleeOnFoot.MakeAbortable(ped);
                    this.taskFleeOnFoot = null;
                }

                if (this.taskFleeInVehicle == null)
                {
                    this.taskFleeInVehicle = new TaskFleeEvadeCopsInVehicle();
                    this.taskFleeInVehicle.AllowWeapons = this.allowWeapons;
                    ped.Intelligence.TaskManager.Assign(this.taskFleeInVehicle, ETaskPriority.SubTask);
                }

                // If task no longer active, reset
                if (!this.taskFleeInVehicle.Active)
                {
                    this.taskFleeInVehicle = null;
                }
            }

            // Process on foot escape
           if (!ped.IsInVehicle())
           {
               // Create new flee on foot task if not already created
               if (this.taskFleeOnFoot == null)
               {
                   this.taskFleeOnFoot = new TaskFleeEvadeCopsOnFoot(this.allowVehicles, this.allowTakingHostages, this.allowWeapons, this.vehicleSearchCriteria);
                   ped.Intelligence.TaskManager.Assign(this.taskFleeOnFoot, ETaskPriority.SubTask);
               }
           }
        }

        public bool IsSleeping()
        {
            return this.suspended;
        }

        public void ResumeTask(CPed ped)
        {
            this.suspended = false;
            // Suspect was probably in combat, cancel
            ped.Task.ClearAllImmediately();
            ped.BlockPermanentEvents = true;
        }

        public void SuspendTask(CPed ped)
        {
            this.suspended = true;
        }

        public override string ComponentName
        {
            get { return "TaskFleeEvadeCops"; }
        }
    }
}
