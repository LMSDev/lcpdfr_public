namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Plays an animation and repeats.
    /// </summary>
    internal class TaskPlayAnimationAndRepeat : PedTask
    {
        /// <summary>
        /// The animation name.
        /// </summary>
        private string animationName;

        /// <summary>
        /// The animation set name.
        /// </summary>
        private AnimationSet animationSet;

        /// <summary>
        /// The speed.
        /// </summary>
        private float speed;

        /// <summary>
        /// The animation flags.
        /// </summary>
        private AnimationFlags animationFlags;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPlayAnimationAndRepeat"/> class.
        /// </summary>
        /// <param name="animationName">
        /// The animation name.
        /// </param>
        /// <param name="animationSetName">
        /// The animation set name.
        /// </param>
        /// <param name="speed">
        /// The speed.
        /// </param>
        public TaskPlayAnimationAndRepeat(string animationName, string animationSetName, float speed) : base(ETaskID.PlayAnimationAndRepeat)
        {
            this.animationName = animationName;
            this.animationSet = new AnimationSet(animationSetName);
            this.speed = speed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPlayAnimationAndRepeat"/> class.
        /// </summary>
        /// <param name="animationName">
        /// The animation name.
        /// </param>
        /// <param name="animationSetName">
        /// The animation set name.
        /// </param>
        /// <param name="speed">
        /// The speed.
        /// </param>
        /// <param name="animationFlags">
        /// The animation flags.
        /// </param>
        public TaskPlayAnimationAndRepeat(string animationName, string animationSetName, float speed, AnimationFlags animationFlags)
            : base(ETaskID.PlayAnimationAndRepeat)
        {
            this.animationName = animationName;
            this.animationSet = new AnimationSet(animationSetName);
            this.speed = speed;
            this.animationFlags = animationFlags;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskPlayAnimationAndRepeat";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            this.SetTaskAsDone();
        }

        /// <summary> 
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!ped.Animation.isPlaying(this.animationSet, this.animationName))
            {
                ped.Task.PlayAnimation(this.animationSet, this.animationName, this.speed, this.animationFlags);
            }
        }
    }
}