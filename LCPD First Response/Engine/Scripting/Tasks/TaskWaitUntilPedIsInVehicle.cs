namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;

    internal class TaskWaitUntilPedIsInVehicle : PedTask
    {
        private CPed pedToWaitFor;
        private CVehicle vehicle;

        public TaskWaitUntilPedIsInVehicle(CPed pedToWaitFor, CVehicle vehicle) : base(ETaskID.WaitUntilPedIsInVehicle)
        {
            this.pedToWaitFor = pedToWaitFor;
            this.vehicle = vehicle;
        }

        public TaskWaitUntilPedIsInVehicle(CPed pedToWaitFor, CVehicle vehicle, int timeOut) : base(ETaskID.WaitUntilPedIsInVehicle, timeOut)
        {
            this.pedToWaitFor = pedToWaitFor;
            this.vehicle = vehicle;
        }

        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            if (!this.pedToWaitFor.Exists() || !this.vehicle.Exists())
            {
                MakeAbortable(ped);
                return;
            }

            // If in vehicle and vehicle is vehicle isn't stopped, stop
            if (ped.IsInVehicle())
            {
                if (!ped.CurrentVehicle.IsStopped)
                {
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                    {
                        ped.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 1000);
                    }
                }
            }

            if (pedToWaitFor.IsInVehicle(vehicle))
            {
                SetTaskAsDone();
            }
        }

        public override string ComponentName
        {
            get { return "TaskWaitUntilPedIsInVehicle"; }
        }
    }
}
