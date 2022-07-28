namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class TaskMarkAsNoLongerNeeded : PedTask
    {
        public TaskMarkAsNoLongerNeeded() : base(ETaskID.MarkAsNoLongerNeeded)
        {
        }

        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            ped.NoLongerNeeded();
            MakeAbortable(ped);
        }

        public override string ComponentName
        {
            get { return "TaskMarkAsNoLongerNeeded"; }
        }
    }
}
