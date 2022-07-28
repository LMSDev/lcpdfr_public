namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR;
    using LCPD_First_Response.LCPDFR.GUI;
    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;

    /// <summary>
    /// This task will make the ped pull out the taser and fires the taser at the target.
    /// </summary>
    internal class TaskCopTasePed : PedTask
    {      
        /// <summary>
        /// The last action the cop was executing
        /// </summary>
        private ETaskTasePedAction action;

        /// <summary>
        /// The target
        /// </summary>
        private CPed target;

        /// <summary>
        /// The ammo of the handgun.
        /// </summary>
        private int handgunAmmo;

        /// <summary>
        /// The handgun weapon.
        /// </summary>
        private Weapon handgunWeapon;

        /// <summary>
        /// Timer to execute the chase logic
        /// </summary>
        private Timer processTimer;

        /// <summary>
        /// How many 100ms have passed since Taser was fired
        /// </summary>
        private int timeSinceDeployment;

        /// <summary>
        /// Whether or not the target can be Tased more than once by multiple cops
        /// </summary>
        private bool allowMultipleTasers;

        /// <summary>
        /// Whether or not the holster animation has actually started
        /// </summary>
        private bool holsterAnimStarted;

        /// <summary>
        /// Whether this task should run as a subtask of the chase on foot task and so not control any movement.
        /// </summary>
        private bool runAsSubtaskOfChaseOnFoot;

        /// <summary>
        /// How long the task has been running
        /// </summary>
        private int runTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopTasePed"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public TaskCopTasePed(CPed target) : base(ETaskID.CopTasePed)
        {
            this.target = target;
            this.processTimer = new Timer(100);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopTasePed"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="allowMultipleTasers">
        /// Whether or not the target can be Tased more than once by multiple cops
        /// </param>
        public TaskCopTasePed(CPed target, bool allowMultipleTasers)
            : base(ETaskID.CopTasePed)
        {
            this.target = target;
            this.processTimer = new Timer(100);
            this.allowMultipleTasers = allowMultipleTasers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopTasePed"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="allowMultipleTasers">
        /// Whether or not the target can be Tased more than once by multiple cops
        /// </param>
        public TaskCopTasePed(CPed target, bool allowMultipleTasers, bool runAsSubtaskOfChaseOnFoot) : base(ETaskID.CopTasePed)
        {
            this.target = target;
            this.processTimer = new Timer(100);
            this.allowMultipleTasers = allowMultipleTasers;
            this.runAsSubtaskOfChaseOnFoot = runAsSubtaskOfChaseOnFoot;
        }

        /// <summary>
        /// Gets a value indicating whether the taser has hit.
        /// </summary>
        public bool HasTaserHit { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether a surrendering suspect should be tasered anyway.
        /// </summary>
        public bool IgnoreSurrender { get; set; }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (this.target.Exists()) this.target.Wanted.OfficersTasing--;

            if (ped.Exists())
            {
                ped.BlockWeaponSwitching = false;

                if (ped.Weapons.Current == Weapon.Misc_Unused0)
                {
                    ped.Weapons.inSlot(WeaponSlot.Handgun).Remove();
                    ped.Weapons.FromType(this.handgunWeapon).Ammo = this.handgunAmmo;
                }

                DelayedCaller.Call(delegate { if (ped.Exists()) ped.SetTaserLight(false, true, true); }, Common.GetRandomValue(1, 1000));

                // If seek entity aiming task is still active, clear
                if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    ped.Task.ClearAll();
                }
            }

            this.SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            // Save weapons and clear flags
            this.target.Wanted.OfficersTasing++;
            this.handgunWeapon = ped.Weapons.inSlot(WeaponSlot.Handgun);
            this.handgunAmmo = ped.Weapons.inSlot(WeaponSlot.Handgun).Ammo;
            this.target.ClearLastDamageEntity();

            // Obviously if it picks up the Taser as the last known weapon, we need to change this
            if (this.handgunWeapon == Weapon.Misc_Unused0 || this.handgunAmmo < 17)
            {
                this.handgunWeapon = Weapon.Handgun_Glock;
                this.handgunAmmo = 170;
            }

            ped.SetTaserLight(true, true, true);

            // Speech and goto task
            if (!ped.IsSayingAmbientSpeech())
            {
                ped.SayAmbientSpeech("DRAW_GUN");
            }

            this.action = ETaskTasePedAction.DrawBegin;

            if (!this.runAsSubtaskOfChaseOnFoot && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
            {
                ped.Task.GoToCharAiming(this.target, 2f, 8f);
            }

            if (this.runAsSubtaskOfChaseOnFoot)
            {
                TaskCopChasePedOnFoot taskCopChasePedOnFoot = ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopChasePedOnFoot) as TaskCopChasePedOnFoot;
                if (taskCopChasePedOnFoot != null)
                {
                    taskCopChasePedOnFoot.TaseTaskHasRegisteredAsChild(this);
                }
                else
                {
                    Log.Warning("Initialize: Failed to find hosting task", this);
                }
            }

            this.runTime = 0;
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            // GTA.Game.DisplayText(this.action.ToString());

            // Abort task if target ped does no longer exist
            if (this.target == null || !this.target.Exists() || !this.target.IsAliveAndWell || this.target.Wanted.VisualLost || this.target.IsGettingIntoAVehicle
                || this.target.Position.DistanceTo(ped.Position) > 15f || this.target.IsInVehicle || this.target.IsInWater)
            {
                this.MakeAbortable(ped);
                return;
            }

            // If target has been tased already and multiple tasers are not allowed, end if compliance chance is not below 100 (so another taser shot is required)
            if (this.target.PedData.HasBeenTased && !this.allowMultipleTasers && this.target.PedData.ComplianceChance >= 100)
            {
                // TextHelper.PrintText("Cancelled", 3000);
                this.MakeAbortable(ped);
                return;
            }

            if (this.runAsSubtaskOfChaseOnFoot && !ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
            {
                if (this.action == ETaskTasePedAction.DrawFinish || this.action == ETaskTasePedAction.DrawDrawing || this.action == ETaskTasePedAction.DrawBegin)
                {
                    this.MakeAbortable(ped);
                    return;
                }
            }

            // Execute taser logic
            if (this.processTimer.CanExecute())
            {
                switch (this.action)
                {
                        case ETaskTasePedAction.DrawBegin:
                        if (!ped.Animation.isPlaying(new AnimationSet("gun@deagle"), "unholster"))
                        {
                            ped.Weapons.RemoveAll();
                            ped.Weapons.FromType(Weapon.Misc_Unused0).Ammo = 2;
                            ped.Task.PlayAnimSecondaryUpperBody("unholster", "gun@deagle", 4.0f, false);
                            this.action = ETaskTasePedAction.DrawDrawing;
                        }

                        break;

                        case ETaskTasePedAction.DrawDrawing:
                        // The draw animation should be in progress here.
                        if (ped.Animation.isPlaying(new AnimationSet("gun@deagle"), "unholster"))
                        {
                            // If it is, check the time
                            if (ped.Animation.GetCurrentAnimationTime(new AnimationSet("gun@deagle"), "unholster") > 0.25f)
                            {
                                // If the animation has been running for a little bit, force the Taser weapon on the ped
                                ped.SetWeapon(Weapon.Misc_Unused0);
                                ped.BlockWeaponSwitching = true;
                            }
                        }
                        else
                        {
                            // Otherwise if the animation isn't running, lets check why
                            if (ped.Weapons.Current != Weapon.Misc_Unused0)
                            {
                                // If the ped doesn't have the Taser weapon out, it must have failed to play otherwise
                                // The check time code above would have set it, so we go back a stage.
                                this.action = ETaskTasePedAction.DrawBegin;
                            }
                            else
                            {
                                // Otherwise, if they have the Taser weapon, it must be that the animation has finished.
                                // If so, move on.
                                this.action = ETaskTasePedAction.DrawFinish;
                            }
                        }

                        break;

                        case ETaskTasePedAction.DrawFinish:
                        // Obviously, if for some reason they don't have the Taser anymore, add it back.
                        if (ped.Weapons.Current != Weapon.Misc_Unused0)
                        {
                            ped.Weapons.FromType(Weapon.Misc_Unused0).Ammo = 2;
                            ped.SetWeapon(Weapon.Misc_Unused0);
                        }

                        // Taser is equipped, we need to get close (not too close) to the suspect now
                        float distance = ped.Position.DistanceTo(this.target.Position);

                        // If close
                        if (distance < 6)
                        {
                            // If too close, make flee
                            if (distance < 2)
                            {
                                if (!ped.IsFleeing)
                                {
                                    ped.Task.FleeFromChar(this.target);
                                    ped.Weapons.FromType(Weapon.Misc_Unused0).Ammo = 2;
                                    ped.SetWeapon(Weapon.Misc_Unused0);
                                }
                            }
                            else
                            {
                                this.action = ETaskTasePedAction.Fire;
                            }
                        }
                        else
                        {
                            if (!this.runAsSubtaskOfChaseOnFoot && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                            {
                                ped.Task.GoToCharAiming(this.target, 3f, 25f);
                            }

                            if (distance > 10)
                            {
                                // Also, we can do some anim stuff here.
                                if (ped.Speed > 1)
                                {
                                    if (!ped.IsAiming)
                                    {
                                        if (!ped.Animation.isPlaying(new AnimationSet("gun@cops"), "pistol_partial_a"))
                                        {
                                            ped.Task.PlayAnimSecondaryUpperBody("pistol_partial_a", "gun@cops", 4.0f, false);
                                        }
                                    }
                                }
                            }

                            // Some random speech
                            if (!ped.IsSayingAmbientSpeech())
                            {
                                if (Common.GetRandomBool(0, 25, 1))
                                {
                                    ped.SayAmbientSpeech(ped.VoiceData.ChaseSpeech);
                                }
                            }
                        }

                        break;

                    case ETaskTasePedAction.Fire:
                        if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun))
                        {
                            // If ped has clear LOS
                            if (ped.HasSpottedCharInFront(this.target))
                            {
                                // If target has surrendered, don't shoot.
                                if (this.target.Wanted.Surrendered && !this.IgnoreSurrender)
                                {
                                    ped.SetWeapon(Weapon.Misc_Unused0);
                                    ped.Task.GoToCharAiming(this.target, Common.GetRandomValue(5, 9), 10f);
                                    this.timeSinceDeployment = 0;
                                    this.action = ETaskTasePedAction.Surrendered;
                                    return;
                                }


                                // If the taget hasn't been given a chance to surrender, we'll give them one now
                                if (!this.target.Wanted.HasBeenAskedToSurrenderBeforeTaser && !this.IgnoreSurrender)
                                {
                                    if (!ped.IsAmbientSpeechPlaying)
                                    {
                                        ped.SayAmbientSpeech(ped.VoiceData.StopSpeech);
                                    }

                                    if (this.target.PedData.WillStop)
                                    {
                                        ped.SetWeapon(Weapon.Misc_Unused0);
                                        ped.Task.GoToCharAiming(this.target, 3f, 10f);
                                        if (!this.target.IsAmbientSpeechPlaying)
                                        {
                                            this.target.SayAmbientSpeech("GUN_RUN");
                                        }
                                        this.target.Intelligence.TaskManager.ClearTasks();
                                        this.target.Task.ClearAll();
                                        this.target.Task.HandsUp(10000);
                                        this.target.Wanted.Surrendered = true;
                                        this.target.Wanted.HasBeenAskedToSurrenderBeforeTaser = true;
                                        this.target.Wanted.HasBeenAskedToSurrender = true;
                                        this.timeSinceDeployment = 0;
                                        this.action = ETaskTasePedAction.Surrendered;
                                        // LCPD_First_Response.LCPDFR.GUI.TextHelper.PrintText("Surrendering", 2000);
                                        return;
                                    }
                                }

                                // Deploy the Taser at the target by applying shoot task.
                                ped.Task.ShootAt(this.target, ShootMode.SingleShotKeepAim, 5000);
                                this.timeSinceDeployment = 0;
                                this.action = ETaskTasePedAction.Fired;
                            }
                        }
                        else
                        {
                            if (!this.runAsSubtaskOfChaseOnFoot && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                            {
                                ped.Task.GoToCharAiming(this.target, 2f, 8f);
                            }
                        }

                        // Make sure Taser is current weapon
                        ped.Weapons.FromType(Weapon.Misc_Unused0).Ammo = 2;
                        ped.SetWeapon(Weapon.Misc_Unused0);
                        break;

                    case ETaskTasePedAction.Fired:
                        // Taser deployed
                        if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(ped.Position) < 10)
                        {
                            AudioHelper.PlayActionSound("TASER_DEPLOY");
                        }

                        this.action = ETaskTasePedAction.DamageDetect;

                        break;

                    case ETaskTasePedAction.DamageDetect:
                        // This is to detect if the Taser hit or not
                        // We increase the timeSinceDeployment by one each tick and only check it if its greater than 3
                        this.timeSinceDeployment++;

                        // In case ped is drunk, we check for CTaskSimpleBlendFromNM and CTaskSimpleNMScriptControl
                        if (this.timeSinceDeployment == 4 && (this.target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleNMOnFire) ||
                            this.target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleBlendFromNM) 
                            || this.target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleNMScriptControl)))
                        {
                            // This // should // make it so the suspects always fall down
                            this.target.ApplyForceRelative(new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0));

                            // Other stuff
                            this.target.DropCurrentWeapon();
                            this.target.PedData.ComplianceChance += 50;
                            this.target.PedData.HasBeenTased = true;
                            this.target.HandleAudioAnimEvent("PAIN_HIGH");
                            this.HasTaserHit = true;
                        }

                        if (this.timeSinceDeployment > 10)
                        {
                            if (this.target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleNMOnFire) || this.HasTaserHit)
                            {
                                // It hit him - go to next stage
                                ped.Task.ClearAll();
                                ped.Task.PlayAnimSecondaryUpperBody("holster", "gun@deagle", 4.0f, false);
                                this.action = ETaskTasePedAction.HitHolster;
                            }
                            else
                            {
                                // Missed, try again
                                this.action = ETaskTasePedAction.DrawFinish;
                            }
                        }

                        break;

                    case ETaskTasePedAction.Surrendered:
                        // Suspect surrendered when they saw the Taser
                        this.timeSinceDeployment++;

                        if (this.timeSinceDeployment > 20)
                        {
                            // Holster and abort so they can arrest.
                            this.timeSinceDeployment = 0;
                            ped.Weapons.Glock.Ammo = 170;
                            ped.SetWeapon(Weapon.Misc_Unused0);
                            ped.Task.ClearAll();
                            this.action = ETaskTasePedAction.HitHolster;
                        }

                        break;

                    case ETaskTasePedAction.HitHolster:
                        this.timeSinceDeployment++;

                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleSwapWeapon))
                        {
                            ped.Task.SwapWeapon(Weapon.Handgun_Glock);
                        }
                        else
                        {
                            if (this.timeSinceDeployment > 10)
                            {
                                this.MakeAbortable(ped);
                            }
                        }

                        if (this.timeSinceDeployment > 40)
                        {
                            this.MakeAbortable(ped);
                        }

                        break;

                        /*
                    case ETaskTasePedAction.HitHolster:
                        TextHelper.PrintText("HitHolster", 3000);
                        // If not reloading, holster the Taser.
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleReloadGun))
                        {
                            if (!ped.Animation.isPlaying(new AnimationSet("gun@deagle"), "holster"))
                            {
                                ped.Task.PlayAnimSecondaryUpperBody("holster", "gun@deagle", 4.0f, false);
                            }
                            else
                            {
                                this.action = ETaskTasePedAction.Holster;
                            }
                        }

                        break;

                    case ETaskTasePedAction.Holster:
                        TextHelper.PrintText("Holster", 3000);
                        // If not reloading, holster the Taser.
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleReloadGun))
                        {
                            if (!ped.Animation.isPlaying(new AnimationSet("gun@deagle"), "holster"))
                            {
                                // If the animation is nearly done, move on
                                this.MakeAbortable(ped);
                            }
                            else
                            {
                                if (ped.Animation.GetCurrentAnimationTime(new AnimationSet("gun@deagle"), "holster") > 0.75f)
                                {
                                    // If the animation is nearly done, remove weapon
                                    ped.SetWeapon(Weapon.Unarmed);
                                    ped.Weapons.inSlot(WeaponSlot.Handgun).Remove();
                                    ped.Weapons.FromType(this.handgunWeapon).Ammo = this.handgunAmmo;
                                }
                            }
                        }

                        break;
                         * */
                }
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskCopTasePed"; }
        }
    }

    /// <summary>
    /// Describes the different actions.
    /// </summary>
    internal enum ETaskTasePedAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Begin to draw the taser.
        /// </summary>
        DrawBegin,

        /// <summary>
        /// Wait for the taser to be completely visible.
        /// </summary>
        DrawDrawing,
        
        /// <summary>
        /// Catch up with the suspect.
        /// </summary>
        DrawFinish,

        /// <summary>
        /// Fire the taser.
        /// </summary>
        Fire,

        /// <summary>
        /// Shot has been fired.
        /// </summary>
        Fired,

        /// <summary>
        /// Shot hit.
        /// </summary>
        DamageDetect,

        /// <summary>
        /// Suspect surrendered.
        /// </summary>
        Surrendered,

        /// <summary>
        /// The shot hit, holster slowly.
        /// </summary>
        HitHolster,

        /// <summary>
        /// Weapon is being holstered again.
        /// </summary>
        Holster
    }
}