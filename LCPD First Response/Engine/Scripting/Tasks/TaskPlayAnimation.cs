namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Plays an animation. Useful when an animation should be reapplied. Not ready yet.
    /// </summary>
    internal class TaskPlayAnimation : PedTask
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
        /// The animation speed.
        /// </summary>
        private float speed;

        /// <summary>
        /// Whether the animation should be applied as secondary upper.
        /// </summary>
        private bool secondaryUpper;

        /// <summary>
        /// The animation flags.
        /// </summary>
        private GTA.AnimationFlags animationFlags;

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
        /// Whether the animation should keep running.
        /// </summary>
        private bool keepRunning;

        /// <summary>
        /// Whether the animation has been applied.
        /// </summary>
        private bool hasBeenApplied;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPlayAnimation"/> class.
        /// </summary>
        public TaskPlayAnimation() : base(ETaskID.PlayAnimation)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPlayAnimation"/> class.
        /// </summary>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskPlayAnimation(int timeOut) : base(ETaskID.PlayAnimation, timeOut)
        {
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskPlayAnimation";
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
            throw new NotImplementedException();

            if (this.hasBeenApplied && !this.keepRunning)
            {
                this.MakeAbortable(ped);
                return;
            }

            if (this.secondaryUpper)
            {
                ped.Task.PlayAnimSecondaryUpperBody(this.animationName, this.animationSetName, this.unknown2, this.unknown3, this.unknown4, this.unknown5, this.unknown6, this.duration);
                this.hasBeenApplied = true;
            }
            else
            {
                ped.Task.PlayAnimation(new AnimationSet(this.animationSetName), this.animationName, this.speed, this.animationFlags);
            }
        }

        /// <summary>
        /// Sets the animation parameter.
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
        public void SetAnimation(string animationName, string animationSetName, float speed)
        {
            this.animationName = animationName;
            this.animationSetName = animationSetName;
            this.speed = speed;
            this.animationFlags = AnimationFlags.None;
            this.secondaryUpper = false;
        }

        /// <summary>
        /// Sets the animation parameter.
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
        public void SetAnimation(string animationName, string animationSetName, float speed, GTA.AnimationFlags animationFlags)
        {
            this.animationName = animationName;
            this.animationSetName = animationSetName;
            this.speed = speed;
            this.animationFlags = animationFlags;
            this.secondaryUpper = false;
        }

        /// <summary>
        /// Sets the animation parameter.
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
        /// <param name="keepRunning">
        /// Whether the animation should keep running.
        /// </param>
        public void SetAnimationSecondaryUpperBody(string animationName, string animationSetName, float unknown2, bool unknown3, int unknown4, int unknown5, int unknown6, int duration, bool keepRunning)
        {
            this.animationName = animationName;
            this.animationSetName = animationSetName;
            this.unknown2 = unknown2;
            this.unknown3 = unknown3;
            this.unknown4 = unknown4;
            this.unknown5 = unknown5;
            this.unknown6 = unknown6;
            this.duration = duration;
            this.keepRunning = keepRunning;
            this.secondaryUpper = true;
        }
    }
}