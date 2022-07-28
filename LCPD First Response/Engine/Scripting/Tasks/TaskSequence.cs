namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System.Collections.Generic;

    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Class to store a list of tasks that can be executed one after the other
    /// </summary>
    internal class TaskSequence
    {
        /// <summary>
        /// List of stored tasks.
        /// </summary>
        private SequencedList<PedTask> tasks;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskSequence"/> class.
        /// </summary>
        public TaskSequence()
        {
            this.tasks = new SequencedList<PedTask>();
        }

        /// <summary>
        /// Adds <paramref name="task"/> to the list of tasks.
        /// </summary>
        /// <param name="task">The task to add.</param>
        public void AddTask(PedTask task)
        {
            this.tasks.Add(task);
        }

        /// <summary>
        /// Assign the task to <paramref name="ped"/>
        /// </summary>
        /// <param name="ped">The ped that the task will be assigned to.</param>
        public void AssignTo(CPed ped)
        {
            TaskUseSequence taskUseSequence = new TaskUseSequence(this);
            taskUseSequence.AssignTo(ped, ETaskPriority.MainTask);
        }

        /// <summary>
        /// Returns the next task.
        /// </summary>
        /// <returns>Next task.</returns>
        public PedTask GetNextTask()
        {
            if (this.IsTaskAvailable())
            {
                return this.tasks.Next();
            }

            return null;
        }

        /// <summary>
        /// Returns if there is a task available that can be retrieved using GetNextTask.
        /// </summary>
        /// <returns>True if there a task available, false if not.</returns>
        public bool IsTaskAvailable()
        {
            return this.tasks.IsNextItemAvailable();
        }
    }
}
