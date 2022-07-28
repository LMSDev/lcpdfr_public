namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Placeholder task that does nothing, but stores the vehicle for cross-task communication rather then getting the vehicle from the scenario which is not available for a task
    /// </summary>
    class TaskArrestedPedAndDriveAway : PedTask
    {
        public CVehicle Vehicle { get; private set; }

        private CPed target;


        public TaskArrestedPedAndDriveAway(CPed ped, CVehicle vehicle) : base(ETaskID.ArrestedPedAndDriveAway)
        {
            this.target = ped;
            this.Vehicle = vehicle;
        }

        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            if (this.target != null && this.target.Exists())
            {
                if (this.target.Wanted.HasBeenArrested)
                {
                    MakeAbortable(ped);
                }
            }
            else
            {
                MakeAbortable(ped);
            }
        }

        public override string ComponentName
        {
            get { return "TaskArrestedPedAndDriveAway"; }
        }
    }
}
