namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Makes a ped chat with other peds.
    /// </summary>
    internal class TaskChat : PedTask
    {
        /// <summary>
        /// The chat animations.
        /// </summary>
        private SequencedList<Animation> chatAnimations;

        /// <summary>
        /// The currently playing animation.
        /// </summary>
        private Animation currentAnimation;

        /// <summary>
        /// Whether the ped has been already slided to the position.
        /// </summary>
        private bool hasSlided;

        /// <summary>
        /// Whether the ped shouldn't move and directly start talking.
        /// </summary>
        private bool keepPosition;

        /// <summary>
        /// Whether the ped is going to the target.
        /// </summary>
        private bool isGoing;

        /// <summary>
        /// The targeted ped.
        /// </summary>
        private CPed target;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskChat"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public TaskChat(CPed target) : base(ETaskID.Chat)
        {
            this.target = target;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskChat"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="keepPosition">
        /// Don't move when starting.
        /// </param>
        public TaskChat(CPed target, bool keepPosition) : base(ETaskID.Chat)
        {
            this.target = target;
            this.keepPosition = keepPosition;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskChat";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the peds are in place and conversation has started.
        /// </summary>
        public bool InConversation { get; private set; }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            this.SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            // Check because it might be already set via SetAnimations
            if (this.chatAnimations == null)
            {
                this.chatAnimations = new SequencedList<Animation>();
                this.chatAnimations.Add(new Animation("amb@pimps_pros", "street_argue_f_a"));
                this.chatAnimations.Add(new Animation("amb@pimps_pros", "street_argue_f_b"));
                this.chatAnimations.Add(new Animation("amb@pimps_pros", "argue_a"));
                this.chatAnimations.Add(new Animation("amb@pimps_pros", "argue_b"));
                this.chatAnimations.Shuffle();
                this.currentAnimation = this.chatAnimations.Next();
            }

            if (!this.keepPosition)
            {
                // Go infront of ped
                ped.Task.GoTo(this.target.GetOffsetPosition(new Vector3(0, 1.5f, 0)));

                this.isGoing = true;
            }
            else
            {
                // Turn to ped if not allowed to move
                TaskLookAtPed taskLookAtPed = new TaskLookAtPed(this.target, false, false, true, 2000);
                taskLookAtPed.AssignTo(ped, ETaskPriority.MainTask);
            }
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!this.target.Exists())
            {
                this.MakeAbortable(ped);
                return;
            }

            // If ped is going but task is no longer running, turn
            if (this.isGoing && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexControlMovement))
            {
                // If close, turn to ped
                if (ped.Position.DistanceTo(this.target.Position) < 5)
                {
                    if (!this.hasSlided)
                    {
                        ped.Task.SlideToCoord(this.target.GetOffsetPosition(new Vector3(0, 1f, 0)), 0, 0);
                        this.hasSlided = true;
                    }
                    else
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexUseSequence) && !ped.Intelligence.TaskManager.IsTaskActive(ETaskID.LookAtPed))
                        {
                            TaskLookAtPed taskLookAtPed = new TaskLookAtPed(this.target, false, false, true, 2000);
                            taskLookAtPed.AssignTo(ped, ETaskPriority.MainTask);
                            this.isGoing = false;
                        }
                    }
                }
                else
                {
                    // Go infront of ped
                    ped.Task.GoTo(this.target.GetOffsetPosition(new Vector3(0, 1f, 0)));
                }
            }
            
            // When ped has stopped, play animation
            if (!this.isGoing)
            {
                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.LookAtPed))
                {
                    AnimationSet animationSet = new AnimationSet(this.currentAnimation.AnimationSetName);

                    // Start animation
                    if (!ped.Animation.isPlaying(animationSet, this.currentAnimation.AnimationName))
                    {
                        // If this is not the first animation to be played, chose new one
                        if (this.InConversation)
                        {
                            this.currentAnimation = this.chatAnimations.Next();
                            animationSet = new AnimationSet(this.currentAnimation.AnimationSetName);
                        }

                        ped.Task.PlayAnimation(animationSet, this.currentAnimation.AnimationName, 8.0f);
                    }

                    // Set flag
                    this.InConversation = true;
                }
            }
        }

        /// <summary>
        /// Sets the animations for the chatting.
        /// </summary>
        /// <param name="animations">The animations.</param>
        public void SetAnimations(Animation[] animations)
        {
            this.chatAnimations = new SequencedList<Animation>();
            foreach (Animation animation in animations)
            {
                this.chatAnimations.Add(animation);
            }

            this.currentAnimation = this.chatAnimations.Next();
        }
    }
}