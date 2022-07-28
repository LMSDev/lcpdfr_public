namespace LCPD_First_Response.Engine.Timers
{
    using System.Collections.Generic;

    /// <summary>
    /// Has nothing to do with ped tasks, but is used to manage system tasks.
    /// </summary>
    internal static class TaskManager
    {
        /// <summary>
        /// List of all tasks.
        /// </summary>
        private static List<SystemTask> tasks;

        /// <summary>
        /// Initializes static members of the <see cref="TaskManager"/> class.
        /// </summary>
        static TaskManager()
        {
            tasks = new List<SystemTask>();
        }

        /// <summary>
        /// Adds <paramref name="systemTask"/> to the task list.
        /// </summary>
        /// <param name="systemTask">The task.</param>
        public static void AddTask(SystemTask systemTask)
        {
            tasks.Add(systemTask);
        }

        /// <summary>
        /// Processes all tasks.
        /// </summary>
        public static void Process()
        {
            List<SystemTask> tasksToRemove = new List<SystemTask>();

            for (int i = 0; i < tasks.Count; i++)
            {
                SystemTask systemTask = tasks[i];
                if (!systemTask.IsActive)
                {
                    tasksToRemove.Add(systemTask);
                    continue;
                }

                ISystemTask systemTaskInterface = (ISystemTask)systemTask;
                systemTaskInterface.InternalProcess();
                if (!systemTask.IsActive)
                {
                    tasksToRemove.Add(systemTask);
                }
            }

            foreach (SystemTask systemTask in tasksToRemove)
            {
                tasks.Remove(systemTask);
            }
        }
    }
}
