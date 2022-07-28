namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;
    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR;
    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;

    /// <summary>
    /// The cuffing task.
    /// </summary>
    internal class TaskCuffPed : PedTask
    {
        /// <summary>
        /// The heading to achieve.
        /// </summary>
        private float heading;

        /// <summary>
        /// Timer to check how long cuffing is taking and to teleport the ped if necessary.
        /// </summary>
        private Timer maximumDurationTimer;

        /// <summary>
        /// Timer to reapply sliding task.
        /// </summary>
        private Timer slideTimer;

        /// <summary>
        /// The target.
        /// </summary>
        private CPed target;

        private readonly string[] cuffsIntroSounds = new string[] { "AMBIENCE_PEDS_CUFFS_SLAP_01", "AMBIENCE_PEDS_CUFFS_SLAP_02", "AMBIENCE_PEDS_CUFFS_SLAP_03" };
        private readonly string[] cuffsSounds = new string[] { "AMBIENCE_PEDS_CUFFS_TIGHTEN_01", "AMBIENCE_PEDS_CUFFS_TIGHTEN_02", "AMBIENCE_PEDS_CUFFS_TIGHTEN_04", "AMBIENCE_PEDS_CUFFS_TIGHTEN_05" };

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCuffPed"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public TaskCuffPed(CPed ped) : base(ETaskID.CuffPed)
        {
            this.target = ped;
            this.slideTimer = new Timer(500);
            this.maximumDurationTimer = new Timer(20000, ETimerOptions.OneTimeReturnTrue);
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskCuffPed"; }
        }

        /// <summary>
        /// Gets a value indicating whether the cuffing animation has been played.
        /// </summary>
        public bool AnimPlayed { get; private set; }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            this.heading = ped.Heading;

            if (!ped.IsAmbientSpeechPlaying && ped != CPlayer.LocalPlayer.Ped)
            {
                ped.SayAmbientSpeech(ped.VoiceData.ArrestSpeech);
            }
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!this.target.Exists() || !this.target.IsAliveAndWell)
            {
                MakeAbortable(ped);
                return;
            }

            // If cuffing is taking too long, teleport ped
            if (this.maximumDurationTimer.CanExecute() && !this.AnimPlayed)
            {
                Log.Debug("Process: Failed to reach ped", this);
                ped.Heading = this.target.Heading;
                ped.SetPositionDontWarpGang(this.target.GetOffsetPosition(new Vector3(0, -0.50f, -1)));
                //ped.Position = this.target.GetOffsetPosition(new Vector3(0, -0.50f, -1));
            }

            // If really close to char, start cuffing
            Vector3 pos = this.target.GetOffsetPosition(new Vector3(0, -0.40f, 0));
            if (ped.Position.DistanceTo(pos) < 0.35f || this.maximumDurationTimer.CanExecute())
            {
                if (!this.AnimPlayed)
                {
                    // Make sure we have a good heading and the suspect isn't currently changing its heading
                    //if (!Common.IsNumberInRange(ped.Heading, this.target.Heading, 1.5f, 1.5f) && !this.target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleMoveAchieveHeading))
                    //{
                    //    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleMoveAchieveHeading))
                    //    {
                    //        ped.Task.AchieveHeading(this.target.Heading);
                    //    }
                    //}
                    // If heading is ok
                    if (Common.IsNumberInRange(ped.Heading, this.target.Heading, 10, 10, 360) || this.maximumDurationTimer.CanExecute())
                    {
                        // Place correctly and abort current ask to prevent a heading change
                        ped.Heading = this.target.Heading;
                        //ped.Position = this.target.GetOffsetPosition(new Vector3(0, -0.50f, -1));
                        ped.SetPositionDontWarpGang(this.target.GetOffsetPosition(new Vector3(0, -0.50f, -1)));
                        ped.Task.ClearAll();
                    }
                    else
                    {
                        return;
                    }

                    // Heading is ok, play anim
                    ped.SayAmbientSpeech("INTIMIDATE_RESP");
                    AnimationSet animationSet = new AnimationSet("cop");
                    if (!ped.Animation.isPlaying(animationSet, "cop_cuff"))
                    {
                        // Ensure ped has no weapon
                        if (ped.Weapons.Current != Weapon.Unarmed)
                        {
                            ped.SetWeapon(Weapon.Unarmed);
                        }

                        if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(ped.Position) < 7.5f)
                        {
                            DelayedCaller.Call(delegate { SoundEngine.PlaySoundFromPed(ped, Common.GetRandomCollectionValue<string>(cuffsIntroSounds)); }, 3000);
                            DelayedCaller.Call(delegate { SoundEngine.PlaySoundFromPed(ped, Common.GetRandomCollectionValue<string>(cuffsSounds)); }, 3500);
                        }
                        ped.Animation.Play(animationSet, "cop_cuff", 1.0f, AnimationFlags.Unknown01);
                        this.AnimPlayed = true;
                    }
                }
            }
            else if (!this.AnimPlayed)
            {
                // Don't do this everytime
                if (this.slideTimer.CanExecute(true))
                {
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexUseSequence))
                    {
                        ped.Task.SlideToCoord(pos, this.heading, 0);
                    }
                }
            }
            if (this.AnimPlayed && this.target.Wanted.IsCuffed)
            {
                SetTaskAsDone();
            }
        }

        /// <summary>
        /// Checks if <paramref name="ped"/> is the target of the cuffing task.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>If ped is the target.</returns>
        public bool IsTarget(CPed ped)
        {
            return this.target == ped;
        }
    }
}
