namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;

    /// <summary>
    /// Lifetime task that will be always recreated and assigned to cops by the cop manager. This manages the cops behavior to respond to certain events as well as manages their priorities and communcation
    /// Note: Everything the cop does (in chases and whatsoever) is done here. I decided so to get away from the 'manager' technique, but rather let every single ped manage its own decisions
    /// Note: This task is also assigned to ped that are marked as no longer needed, so exists check are always necessary!
    /// </summary>
    class TaskCop : PedTask
    {
        private const float DistanceToRespondToFleeingCriminal = 30;

        private CPed cop;

        public TaskCop() : base(ETaskID.Cop)
        {
        }

        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();

            ped.GetPedData<PedDataCop>().RequestedPedAction -= new PedDataCop.RequestedPedActionEventHandler(TaskCop_RequestedPedAction);
            ped.Intelligence.TaskManager.NewTask -= new TaskManager.NewTaskEventHandler(TaskManager_NewTask);
            EventFleeingCriminal.EventRaised -= new EventFleeingCriminal.EventRaisedEventHandler(EventFleeingCriminal_EventRaised);
            EventPedBeingArrested.EventRaised -= this.EventPedBeingArrested_EventRaised;
        }

        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            this.cop = ped;

            // Listen to NewTask event to respond to new tasks being assigned
            ped.GetPedData<PedDataCop>().RequestedPedAction += new PedDataCop.RequestedPedActionEventHandler(TaskCop_RequestedPedAction);
            ped.Intelligence.TaskManager.NewTask += new TaskManager.NewTaskEventHandler(TaskManager_NewTask);
            EventFleeingCriminal.EventRaised += new EventFleeingCriminal.EventRaisedEventHandler(EventFleeingCriminal_EventRaised);
            EventPedBeingArrested.EventRaised += this.EventPedBeingArrested_EventRaised;
        }

        private bool TaskCop_RequestedPedAction(object sender, ECopState copState, IPedController controller)
        {
            return HandleNewAction(copState, true);
        }

        private void EventFleeingCriminal_EventRaised(EventFleeingCriminal @event)
        {
            if (!IsAllowedToAct()) return;

            // If close to the criminal
            if (@event.Criminal != null && @event.Criminal.Exists())
            {
                if (this.cop != null && this.cop.Exists())
                {
                    float distance = DistanceToRespondToFleeingCriminal;
                    if (this.cop.IsInVehicle)
                    {
                        distance = distance * 3;
                    }

                    if (@event.Criminal.Position.DistanceTo(this.cop.Position) < distance)
                    {
                        if (HandleNewAction(ECopState.Chase, false))
                        {
                            // Signal all chase instances that this cop is ready to join
                            new EventCopReadyToChase(this.cop, @event.Criminal);
                        }
                    }
                }
            }
        }

        private void EventPedBeingArrested_EventRaised(EventPedBeingArrested @event)
        {
            if (this.cop != null && this.cop.Exists() && this.cop.PedGroup == EPedGroup.Cop && !this.cop.IsInVehicle)
            {
                if (this.cop.Intelligence.IsFreeForAction(EPedActionPriority.AmbientTaskImportant) && @event.Ped.Wanted.IsBeingArrestedByPlayer)
                {
                    if (!this.cop.Intelligence.TaskManager.IsTaskActive(ETaskID.ArrestPed) && this.cop.Position.DistanceTo(@event.Ped.Position) < 15)
                    {
                        TaskArrestPed taskArrestPed = new TaskArrestPed(@event.Ped, 6f, Common.GetRandomValue(5000, 7000));
                        taskArrestPed.AssignTo(this.cop, ETaskPriority.SubTask);
                    }
                }
            }
        }

        private void TaskManager_NewTask(PedTask task)
        {
            if (!IsAllowedToAct()) return;
        }

        /// <summary>
        /// Decides if the cop will do the new requested action depending on the current state.
        /// </summary>
        /// <param name="newCopState">The new state the cop should have</param>
        /// <param name="request">Indicates if the cop was requested by a component or if the cop itself decide to change the state (e.g. due to a fleeing criminal)</param>
        /// <returns></returns>
        private bool HandleNewAction(ECopState newCopState, bool request)
        {
            if (this.cop == null || !this.cop.Exists()) return false;

            ECopState currentCopState = this.cop.GetPedData<PedDataCop>().CopState;

            switch (newCopState)
            {
                case ECopState.Blocker:
                    // Blocker is highest priority
                    if (currentCopState == ECopState.Investigating || currentCopState == ECopState.Chase)
                    {
                        // Reset
                        this.cop.GetPedData<PedDataCop>().ResetPedAction(this.cop.Intelligence.PedController, true);
                        return true;
                    }
                    else if (currentCopState == ECopState.Idle)
                    {
                        return true;
                    }

                    break;
                case ECopState.Chase:
                    // New state should be chase. Allowed if current state is either idle or investigate
                    if (currentCopState == ECopState.Idle || currentCopState == ECopState.Investigating)
                    {
                        // If investigating, abort it
                        if (currentCopState == ECopState.Investigating)
                        {
                            // Reset
                            this.cop.GetPedData<PedDataCop>().ResetPedAction(this.cop.Intelligence.PedController, true);
                        }

                        return true;
                    }

                    break;
                case ECopState.Idle:
                    return true;
                case ECopState.Investigating:
                    // Investigating is only allowed when currently idle
                    if (currentCopState == ECopState.Idle)
                    {
                        return true;
                    }
                    break;
                case ECopState.Roadblock:
                    // Roadblock is allowed when idle or investigating
                    if (currentCopState == ECopState.Idle || currentCopState == ECopState.Investigating)
                    {
                        // If investigating, abort it
                        if (currentCopState == ECopState.Investigating)
                        {
                            // Reset
                            this.cop.GetPedData<PedDataCop>().ResetPedAction(this.cop.Intelligence.PedController, true);
                        }
                        return true;
                    }

                    break;
                case ECopState.SuspectTransporter:
                    if (currentCopState == ECopState.Idle || currentCopState == ECopState.Investigating)
                    {
                        // If investigating, abort it
                        if (currentCopState == ECopState.Investigating)
                        {
                            // Reset
                            this.cop.GetPedData<PedDataCop>().ResetPedAction(this.cop.Intelligence.PedController, true);
                        }

                        return true;
                    }

                    break;
            }

            return false;
        }

        public override void Process(CPed ped)
        {
            if (!IsAllowedToAct()) return;

            if (ped.GetPedData<PedDataCop>().CopState == ECopState.None)
            {
                Log.Debug("STATE IS NONE?!", this);
            }

            if (ped == null || !ped.Exists())
            {
                Log.Info("Process: Task running for non-existant ped", this);
                this.MakeAbortable(ped);
                return;
            }

            if (ped.Intelligence.IsFreeForAction(EPedActionPriority.AmbientTask) && ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatAdvanceSubtask))
            {
                CPed chasingPed = ped.GetEnemyInCombat();
                if (chasingPed != null && chasingPed.Exists())
                {
                    if (chasingPed.Intelligence.IsFreeForAction(EPedActionPriority.AmbientTask))
                    {
                        if (chasingPed.HasOwner)
                        {
                            Log.Warning("Process: Ped free for ambient task, but has an owner", this);
                            Log.Warning("Owner: " + chasingPed.Owner + " Priority: " + chasingPed.Intelligence.CurrentActionPriority, this);
                            return;
                        }

                        if (!chasingPed.PedData.Available)
                        {
                            Log.Warning("Process: Ped free for ambient task, but not available", this);
                            return;
                        }
                        
                        EventAmbientFootChase eventAmbientFootChase = new EventAmbientFootChase(ped, chasingPed);
                    }
                }
            }
        }

        // Helper function
        private PedDataCop GetCopPedData(CPed ped)
        {
            return (PedDataCop) ped.PedData;
        }

        private bool IsAllowedToAct()
        {
            return GetCopPedData(this.cop).AIEnabled;
        }

        public override string ComponentName
        {
            get { return "TaskCop"; }
        }
    }
}
