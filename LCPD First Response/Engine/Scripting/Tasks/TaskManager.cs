namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// The task manager.
    /// </summary>
    internal class TaskManager : ICoreTickable
    {
        /// <summary>
        /// The main tasks.
        /// </summary>
        private List<PedTask> mainTasks;

        /// <summary>
        /// The subtasks.
        /// </summary>
        private List<PedTask> subTasks;

        /// <summary>
        /// The permanent tasks, only used for reference. They are stored in subTasks.
        /// </summary>
        private List<PedTask> permanentTasks;

        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager"/> class. Anonymous instance without a ped reference. Will be added to CoreTickCalls
        /// </summary>
        public TaskManager()
        {
            this.mainTasks = new List<PedTask>();
            this.subTasks = new List<PedTask>();
            this.permanentTasks = new List<PedTask>();

            Pools.CoreTicks.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public TaskManager(CPed ped)
        {
            this.ped = ped;

            this.mainTasks = new List<PedTask>();
            this.subTasks = new List<PedTask>();
            this.permanentTasks = new List<PedTask>();
        }

        /// <summary>
        /// Event handler for new tasks.
        /// </summary>
        /// <param name="task">The task.</param>
        public delegate void NewTaskEventHandler(PedTask task);

        /// <summary>
        /// The event for new tasks.
        /// </summary>
        public event NewTaskEventHandler NewTask;

        /// <summary>
        /// Aborts <paramref name="task"/>.
        /// </summary>
        /// <param name="task">The ped.</param>
        public void Abort(PedTask task)
        {
            foreach (PedTask mainTask in this.mainTasks)
            {
                if (task.TaskID == mainTask.TaskID)
                {
                    task.MakeAbortable(this.ped);
                }
            }

            foreach (PedTask subTask in this.subTasks)
            {
                if (task.TaskID == subTask.TaskID)
                {
                    task.MakeAbortable(this.ped);
                }
            }
        }

        /// <summary>
        /// Aborts the first task with <paramref name="taskID"/>. Returns true on success.
        /// </summary>
        /// <param name="taskID">The task ID.</param>
        /// <returns>True on success.</returns>
        public bool AbortInternalTask(EInternalTaskID taskID)
        {
            return this.ped.APed.AbortTask((int)taskID, 2);
        }

        /// <summary>
        /// Assigns <paramref name="task"/> to the ped using <paramref name="taskPriority"/>.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="taskPriority">The priority.</param>
        public void Assign(PedTask task, ETaskPriority taskPriority)
        {
            if (taskPriority == ETaskPriority.MainTask)
            {
                this.mainTasks.Add(task);
                task.Initialize(this.ped);
            }

            if (taskPriority == ETaskPriority.SubTask)
            {
                this.subTasks.Add(task);
                task.Initialize(this.ped);
            }

            if (taskPriority == ETaskPriority.Permanent)
            {
                this.subTasks.Add(task);
                this.permanentTasks.Add(task);
                task.Initialize(this.ped);
            }

            NewTaskEventHandler handler = this.NewTask;
            if (handler != null)
            {
                handler(task);
            }
        }

        // TODO: Abort priority (so tasks such as TaskCop could stay because they have a high priority)
        /// <summary>
        /// Clears all main tasks.
        /// </summary>
        public void ClearMainTask()
        {
            // Use for loop so tasks can assign new tasks in MakeAbortable
            for (int i = 0; i < this.mainTasks.Count; i++)
            {
                PedTask mainTask = this.mainTasks[i];
                if (mainTask.Active)
                {
                    mainTask.MakeAbortable(this.ped);
                }
            }

            this.RemoveNonActiveTasks(true, false);
        }

        /// <summary>
        /// Clears all subtasks.
        /// </summary>
        public void ClearSubTasks()
        {
            // Use for loop so tasks can assign new tasks in MakeAbortable
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                PedTask subTask = this.subTasks[i];
                if (subTask.Active && !this.permanentTasks.Contains(subTask))
                {
                    subTask.MakeAbortable(this.ped);
                }
            }

            this.RemoveNonActiveTasks(false, true);
        }

        /// <summary>
        /// Clears all custom tasks except those flagged as <see cref="ETaskPriority.Permanent"/>.
        /// </summary>
        public void ClearTasks()
        {
            this.ClearMainTask();
            this.ClearSubTasks();
        }

        public PedTask FindTaskWithID(ETaskID taskID)
        {
            foreach (PedTask mainTask in this.mainTasks)
            {
                if (taskID == mainTask.TaskID) return mainTask;
            }
            foreach (PedTask subTask in this.subTasks)
            {
                if (taskID == subTask.TaskID) return subTask;
            }
            return null;
        }

        public EInternalTaskID[] GetActiveInternalTasks()
        {
            List<EInternalTaskID> activeTasks = new List<EInternalTaskID>();
            foreach (EInternalTaskID task in Enum.GetValues(typeof(EInternalTaskID)))
            {
                if (this.ped.APed.IsTaskActive((int)task))
                {
                    activeTasks.Add(task);
                }
            }
            return activeTasks.ToArray();
        }

        public ETaskID[] GetActiveTaskIDs()
        {
            List<ETaskID> activeTasks =new List<ETaskID>();

            foreach (PedTask task in this.mainTasks.Concat(this.subTasks))
            {
                if (task.Active)
                {
                    activeTasks.Add(task.TaskID);
                }
            }
            return activeTasks.ToArray();
        }

        /// <summary>
        /// Gets the active tasks.
        /// </summary>
        /// <returns>The tasks.</returns>
        public PedTask[] GetActiveTasks()
        {
            return this.mainTasks.Concat(this.subTasks).ToArray();
        }

        /// <summary>
        /// Checks the internal tasks of a ped and returns true if task is found
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool IsInternalTaskActive(EInternalTaskID task)
        {
            // Check ped's internal tasks
            return ped.APed.IsTaskActive((int) task);
        }

        /// <summary>
        /// Checks the tasks of a ped and returns true if task is found
        /// </summary>
        /// <param name="taskID"></param>
        /// <returns></returns>
        public bool IsTaskActive(ETaskID taskID)
        {
            foreach (PedTask task in this.mainTasks.Concat(this.subTasks))
            {
                if (task.TaskID == taskID && task.Active) return true;
            }
            return false;
        }

        public void Process()
        {
            List<PedTask> mainTasksToRemove = new List<PedTask>();
            List<PedTask> subTasksToRemove = new List<PedTask>();

            // Process main tasks
            for (int i = 0; i < this.mainTasks.Count; i++)
            {
                PedTask mainTask = this.mainTasks[i];
                if (!mainTask.Active)
                {
                    mainTasksToRemove.Add(mainTask);
                    continue;
                }
                mainTask.InternalProcess(this.ped);
                if (!mainTask.Active) mainTasksToRemove.Add(mainTask);
            }

            // Process subtasks
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                PedTask subTask = this.subTasks[i];
                if (!subTask.Active)
                {
                    subTasksToRemove.Add(subTask);
                    continue;
                }
                subTask.InternalProcess(this.ped);
                if (!subTask.Active) subTasksToRemove.Add(subTask);
            }

            // Remove deleted tasks
            foreach (PedTask task in mainTasksToRemove)
            {
                this.mainTasks.Remove(task);
            }
            foreach (PedTask task in subTasksToRemove)
            {
                this.subTasks.Remove(task);
            }
        }


        /// <summary>
        /// Shutdowns this task manager instance.
        /// </summary>
        public void Shutdown()
        {
            this.ClearTasks();

            // Shutdown permanent tasks as well
            for (int i = 0; i < this.permanentTasks.Count; i++)
            {
                PedTask task = this.permanentTasks[i];
                if (task.Active)
                {
                    task.MakeAbortable(this.ped);
                }
            }

            this.mainTasks.Clear();
            this.subTasks.Clear();
            this.permanentTasks.Clear();
        }

        /// <summary>
        /// Removes all non active tasks.
        /// </summary>
        /// <param name="removeMainTasks">
        /// Whether main tasks should be removed.
        /// </param>
        /// <param name="removeSubTasks">
        /// Whether sub tasks should be removed.
        /// </param>
        private void RemoveNonActiveTasks(bool removeMainTasks, bool removeSubTasks)
        {
            List<PedTask> mainTasksToRemove = new List<PedTask>();
            List<PedTask> subTasksToRemove = new List<PedTask>();

            // Process main tasks
            if (removeMainTasks)
            {
                for (int i = 0; i < this.mainTasks.Count; i++)
                {
                    PedTask mainTask = this.mainTasks[i];
                    if (!mainTask.Active)
                    {
                        mainTasksToRemove.Add(mainTask);
                    }
                }
            }

            // Process subtasks
            if (removeSubTasks)
            {
                for (int i = 0; i < this.subTasks.Count; i++)
                {
                    PedTask subTask = this.subTasks[i];
                    if (!subTask.Active)
                    {
                        subTasksToRemove.Add(subTask);
                    }
                }
            }

            // Remove deleted tasks
            foreach (PedTask task in mainTasksToRemove)
            {
                this.mainTasks.Remove(task);
            }
            foreach (PedTask task in subTasksToRemove)
            {
                this.subTasks.Remove(task);
            }
        }
    }
}
