namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    internal class TaskWander : PedTask
    {
        public TaskWander() : base(ETaskID.Wander)
        {
        }

        public TaskWander(int timeOut) : base(ETaskID.Wander, timeOut)
        {
        }

        public override void MakeAbortable(CPed ped)
        {
            // If wander task is still active, clear
            if (ped.Exists() && ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
            {
                ped.Task.ClearAll();
            }
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            // If not already wandering, wander
            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
            {
                ped.Task.WanderAround();
            }
        }

        public override string ComponentName
        {
            get { return "TaskWander"; }
        }
    }
}
