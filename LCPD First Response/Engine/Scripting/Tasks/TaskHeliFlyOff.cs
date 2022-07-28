namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Makes a helicopter properly leave the scene.
    /// </summary>
    internal class TaskHeliFlyOff : PedTask
    {
        /// <summary>
        /// The position where the ped is spawned (LCPD helipad at Francis International).
        /// </summary>
        private readonly GTA.Vector3 pedPosition = new GTA.Vector3(2132.44f, 440.55f, 23.68f);

        /// <summary>
        /// The temp ped.
        /// </summary>
        private CPed tempPed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHeliFlyOff"/> class.
        /// </summary>
        public TaskHeliFlyOff() : base(ETaskID.HeliFlyOff)
        {
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskHeliFlyOff";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (this.tempPed != null && this.tempPed.Exists())
            {
                this.tempPed.Delete();
            }

            this.SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (this.tempPed == null || !this.tempPed.Exists())
            {
                // Create ped out at the LCPD helipad at Francis International. Delete ped after 60 seconds
                this.tempPed = new CPed(CModel.BasicCopModel, this.pedPosition, EPedGroup.MissionPed);
                if (this.tempPed.Exists())
                {
                    // ped.APed.TaskCombatPersueInCarSubtask(this.tempPed.APed);
                    ped.Task.HeliMission(ped.CurrentVehicle, 0, 0, this.tempPed.Position, 4, 20.0f, 0, -1, 200, 150);
                    this.tempPed.Visible = false;
                    DelayedCaller.Call(this.DeletePed, 60000);
                }
            }
        }

        /// <summary>
        /// Deletes the ped.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void DeletePed(object[] parameter)
        {
            this.tempPed.Delete();
            this.MakeAbortable(null);
        }
    }
}
