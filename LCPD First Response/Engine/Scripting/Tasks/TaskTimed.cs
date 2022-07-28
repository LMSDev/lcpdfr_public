/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCPD_First_Response.Engine.Entities;

namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Flexible TimerManager replacement, calls callback once or regurlary after a predefined amount of time.
    /// Use SystemTaskTimed for system tasks.
    /// </summary>
    [Obsolete("Will soon be replaced by SystemTaskTimed/DelayedCaller")]
    internal class TaskTimed : TaskAnonymous
    {
        // TODO: Params (see old TimerManager)
        public delegate void TaskTimedCallback(object data);

        private TaskTimedCallback callback;
        private object data;
        private bool once;
        private int time;
        private Timer timer;

        public TaskTimed(int time, TaskTimedCallback callback, object data, bool autoAssign = true) : base(ETaskID.Timed)
        {
            this.time = time;
            this.callback = callback;
            this.data = data;

            // Don't forward auto assign to base constructor but call here to ensure this.time is set when Initialize is called
            if (autoAssign)
            {
                AssignTo();
            }
        }

        public TaskTimed(int time, bool once, TaskTimedCallback callback, object data, bool autoAssign = true) : base(ETaskID.Timed)
        {
            this.time = time;
            this.callback = callback;
            this.data = data;
            this.once = once;

            // Don't forward auto assign to base constructor but call here to ensure this.time is set when Initialize is called
            if (autoAssign)
            {
                AssignTo();
            }
        }

        public TaskTimed(int time, int timeOut, TaskTimedCallback callback, object data, bool autoAssign = true)  : base(ETaskID.Timed, timeOut)
        {
            this.time = time;
            this.callback = callback;
            this.data = data;

            // Don't forward auto assign to base constructor but call here to ensure this.time is set when Initialize is called
            if (autoAssign)
            {
                AssignTo();
            }
        }

        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            this.timer = new Timer(this.time);
        }

        public override void Process(CPed ped)
        {
            if (this.timer.CanExecute())
            {
                this.callback.Invoke(this.data);

                if (this.once)
                {
                    this.MakeAbortable(ped);
                }
            }
        }

        public override string ComponentName
        {
            get { return "TaskTimed"; }
        }
    }
}*/