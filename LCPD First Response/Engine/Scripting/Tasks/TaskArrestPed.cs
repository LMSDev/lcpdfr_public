namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    internal class TaskArrestPed : PedTask
    {
        private float distance;
        private CPed target;

        public TaskArrestPed(CPed ped, float stopDistance, int timeOut) : base(ETaskID.ArrestPed, timeOut)
        {
            this.distance = stopDistance;
            this.target = ped;
        }

        public override void MakeAbortable(CPed ped)
        {
            if (ped.Exists())
            {
                if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    ped.Task.ClearAll();
                }
            }
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            if (!this.target.Exists() || !this.target.IsAliveAndWell)
            {
                MakeAbortable(ped);
                return;
            }

            // Go right behind the ped while aiming
            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
            {
                ped.EnsurePedHasWeapon();
                ped.Task.GoToCharAiming(this.target, this.distance, 15);
            }
        }

        public override string ComponentName
        {
            get { return "TaskArrestPed"; }
        }
    }
}
