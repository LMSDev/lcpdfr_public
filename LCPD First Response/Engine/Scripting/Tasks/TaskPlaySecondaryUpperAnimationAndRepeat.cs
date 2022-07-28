namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Plays secondary upper animation and repeats it all the time.
    /// </summary>
    internal class TaskPlaySecondaryUpperAnimationAndRepeat : PedTask
    {
        /// <summary>
        /// The animation name.
        /// </summary>
        private string animationName;

        /// <summary>
        /// The animation set name.
        /// </summary>
        private string animationSetName;

        /// <summary>
        /// Unknown 2.
        /// </summary>
        private float unknown2;

        /// <summary>
        /// Unknown 3.
        /// </summary>
        private bool unknown3;

        /// <summary>
        /// Unknown 4.
        /// </summary>
        private int unknown4;

        /// <summary>
        /// Unknown 5.
        /// </summary>
        private int unknown5;

        /// <summary>
        /// Unknown 6.
        /// </summary>
        private int unknown6;

        /// <summary>
        /// The duration.
        /// </summary>
        private int duration;

        /// <summary>
        /// The timer used to play the animation.
        /// </summary>
        private Timers.NonAutomaticTimer animationTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPlaySecondaryUpperAnimationAndRepeat"/> class.
        /// <param name="animationName">
        /// The animation name.
        /// </param>
        /// <param name="animationSetName">
        /// The animation set name.
        /// </param>
        /// <param name="unknown2">
        /// The unknown 2.
        /// </param>
        /// <param name="unknown3">
        /// The unknown 3.
        /// </param>
        /// <param name="unknown4">
        /// The unknown 4.
        /// </param>
        /// <param name="unknown5">
        /// The unknown 5.
        /// </param>
        /// <param name="unknown6">
        /// The unknown 6.
        /// </param>
        /// <param name="duration">
        /// The duration.
        /// </param>
        /// </summary>
        public TaskPlaySecondaryUpperAnimationAndRepeat(string animationName, string animationSetName, float unknown2, bool unknown3, int unknown4, int unknown5, int unknown6, int duration) : base(ETaskID.PlaySecondaryUpperAnimationAndRepeat)
        {
            this.animationName = animationName;
            this.animationSetName = animationSetName;
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.unknown4 = unknown4;
            this.unknown5 = unknown5;
            this.unknown6 = unknown6;
            this.duration = duration;
            this.animationTimer = new NonAutomaticTimer(1000);
            this.animationTimer.Trigger();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPlaySecondaryUpperAnimationAndRepeat"/> class.
        /// </summary>
        /// <param name="animationName">
        /// The animation name.
        /// </param>
        /// <param name="animationSetName">
        /// The animation set name.
        /// </param>
        /// <param name="unknown2">
        /// The unknown 2.
        /// </param>
        /// <param name="unknown3">
        /// The unknown 3.
        /// </param>
        /// <param name="unknown4">
        /// The unknown 4.
        /// </param>
        /// <param name="unknown5">
        /// The unknown 5.
        /// </param>
        /// <param name="unknown6">
        /// The unknown 6.
        /// </param>
        /// <param name="duration">
        /// The duration.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskPlaySecondaryUpperAnimationAndRepeat(string animationName, string animationSetName, float unknown2, bool unknown3, int unknown4, int unknown5, int unknown6, int duration, int timeOut) : base(ETaskID.PlaySecondaryUpperAnimationAndRepeat, timeOut)
        {
            this.animationName = animationName;
            this.animationSetName = animationSetName;
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.unknown4 = unknown4;
            this.unknown5 = unknown5;
            this.unknown6 = unknown6;
            this.duration = duration;
            this.animationTimer = new NonAutomaticTimer(500);
            this.animationTimer.Trigger();
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "PlaySecondaryUpperAnimationAndRepeat";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (this.animationTimer.CanExecute())
            {
                if (!ped.Animation.isPlaying(new AnimationSet(this.animationSetName), this.animationName) && !ped.IsGettingIntoAVehicle && !ped.IsGettingUp 
                    && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                {
                    ped.Task.PlayAnimSecondaryUpperBody(this.animationName, this.animationSetName, this.unknown2, this.unknown3, this.unknown4, this.unknown5, this.unknown6, this.duration);
                }
            }
        }
    }
}
