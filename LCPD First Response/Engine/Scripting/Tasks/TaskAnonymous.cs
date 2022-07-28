using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    /// <summary>
    /// Anonymous task that doesn't require a ped instance
    /// </summary>
    abstract class TaskAnonymous : PedTask
    {
        protected TaskAnonymous(ETaskID taskID) : base(taskID)
        {
        }

        protected TaskAnonymous(ETaskID taskID, bool autoAssign) : base(taskID)
        {
            if (autoAssign) AssignTo();
        }

        protected TaskAnonymous(ETaskID taskID, int timeOut) : base(taskID, timeOut)
        {}

        protected TaskAnonymous(ETaskID taskID, int timeOut, bool autoAssign) : base(taskID, timeOut)
        {
            if (autoAssign) AssignTo();
        }

        /// <summary>
        /// Anonymous assign with no ped target
        /// </summary>
        public void AssignTo()
        {
            // Register with invalid ped
            Main.TaskManager.Assign(this, ETaskPriority.MainTask);
        }
    }
}
