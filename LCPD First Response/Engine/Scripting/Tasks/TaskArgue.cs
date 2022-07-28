namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    /// <summary>
    /// Task that makes a ped argue with another, so playing some "pissed off" animations.
    /// </summary>
    internal class TaskArgue : PedTask
    {
        /// <summary>
        /// The argue animations.
        /// </summary>
        private string[] animations = new string[] { "crazy_rant_01", "crazy_rant_02", "crazy_rant_03" };

        /// <summary>
        /// The animation to play.
        /// </summary>
        private string animation;

        /// <summary>
        /// The target to argue with.
        /// </summary>
        private CPed target;

        /// <summary>
        /// The text to display.
        /// </summary>
        private SequencedList<string> text;

        /// <summary>
        /// The text timer.
        /// </summary>
        private NonAutomaticTimer textTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskArgue"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public TaskArgue(CPed target) : base(ETaskID.Argue)
        {
            this.target = target;
            this.text = new SequencedList<string>();
            this.textTimer = new NonAutomaticTimer(5000);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskArgue"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskArgue(CPed target, int timeOut) : base(ETaskID.Argue, timeOut)
        {
            this.target = target;
            this.text = new SequencedList<string>();
            this.textTimer = new NonAutomaticTimer(5000);
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskArgue";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            ped.Intelligence.SetDrawTextAbovePedsHeadEnabled(false);
            this.SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            TaskLookAtPed taskLookAtPed = new TaskLookAtPed(this.target, false, false, true, 3000);
            taskLookAtPed.AssignTo(ped, ETaskPriority.MainTask);

            this.animation = Common.GetRandomCollectionValue<string>(this.animations);
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (this.target != null && this.target.Exists())
            {
                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.LookAtPed))
                {
                    if (!ped.Animation.isPlaying(new AnimationSet("amb@argue"), this.animation))
                    {
                        ped.Task.PlayAnimation(new AnimationSet("amb@argue"), this.animation, 8.0f);
                        this.textTimer.Trigger();
                    }
                }
            }

            if (this.textTimer.CanExecute())
            {
                if (this.text.IsNextItemAvailable())
                {
                    ped.Intelligence.SetDrawTextAbovePedsHead(this.text.Next());
                    ped.Intelligence.SetDrawTextAbovePedsHeadEnabled(true);
                }
                else
                {
                    ped.Intelligence.SetDrawTextAbovePedsHeadEnabled(false);
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the conversation.
        /// </summary>
        /// <param name="text">The text.</param>
        public void AddLine(string text)
        {
            this.text.Add(text);
        }
    }
}