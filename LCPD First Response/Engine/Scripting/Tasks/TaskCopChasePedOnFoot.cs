namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR;
    using LCPD_First_Response.LCPDFR.GUI;
    using System;
    using System.Collections.Generic;
    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;

    /// <summary>
    /// This task will make officers on foot chase after a suspect
    /// </summary>
    internal class TaskCopChasePedOnFoot : PedTask
    {      
        /// <summary>
        /// The last action the cop was executing
        /// </summary>
        private ETaskChasePedOnFootAction action;

        /// <summary>
        /// The target
        /// </summary>
        private CPed target;

        /// <summary>
        /// Timer to execute the chase logic
        /// </summary>
        private Timer processTimer;

        /// <summary>
        /// The tase task.
        /// </summary>
        private TaskCopTasePed taskCopTasePed;

        private bool firstRun;

        /// <summary>
        /// If we have tried to stop the ped from doing the investigating task
        /// </summary>
        private bool toldToStopInvestigating;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopSearchForPedOnFoot"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="lastKnownPosition">
        /// The last known position of the target
        /// </param>
        public TaskCopChasePedOnFoot(CPed target)
            : base(ETaskID.CopChasePedOnFoot)
        {
            this.target = target;
            this.processTimer = new Timer(100);
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            // If actively chasing, decrement chase count.
            if (this.action == ETaskChasePedOnFootAction.Chase)
            {
                if (this.target.Exists()) this.target.Wanted.OfficersChasingOnFoot--;
            }

            // If ped exists, make sure they can fire and use cover again.
            if (ped.Exists())
            {
                // It is of importance that any remaining combat tasks are cleared so the reset of the shoot rate below will not make the cop
                // actually shoot at the suspect
                if (ped.IsInCombat)
                {
                    Log.Debug("MakeAbortable: Cleared combat tasks before aborting task", this);
                    ped.Task.ClearAll();
                }

                ped.WillUseCover(true);
                ped.SetShootRate(Common.GetRandomValue(50, 70));
            }

            PedDataCop pedDataCop = ped.PedData as PedDataCop;
            pedDataCop.Available = true;
            pedDataCop.CurrentTarget = null;

            // Abort pending calls
            DelayedCaller.ClearAllRunningCalls(false, this);

            this.SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);
            this.firstRun = true;

            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
            {
                ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.BustPed).MakeAbortable(ped);
            }

            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.ArrestPed))
            {
                ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.ArrestPed).MakeAbortable(ped);
            }

            ped.Task.ClearAll();

            // Speech
            if (!ped.IsSayingAmbientSpeech())
            {
                int speakingCopsNearby = 0;

                foreach (CPed cop in Pools.PedPool.GetAll())
                {
                    if (cop != null && cop.Exists())
                    {
                        if (cop.PedGroup == EPedGroup.Cop)
                        {
                            if (cop.Position.DistanceTo(ped.Position) < 10.0f)
                            {
                                if (cop.IsAmbientSpeechPlaying)
                                {
                                    speakingCopsNearby++;
                                }
                            }
                        }
                    }
                }

                if (speakingCopsNearby < 2)
                {
                    // This stops cops from spamming the speech
                    ped.SayAmbientSpeech("SPOT_SUSPECT");
                }
            }

            this.action = ETaskChasePedOnFootAction.CatchUp;
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            // Abort task if target ped does no longer exist

            if (this.target == null || !this.target.Exists())
            {
                this.MakeAbortable(ped);
                return;
            }

            if (!this.target.IsAliveAndWell)
            {
                this.MakeAbortable(ped);
                return;
            }

            if (this.target.Wanted.VisualLost)
            {
                this.MakeAbortable(ped);  
                return;
            }


            if (this.processTimer.CanExecute())
            {
                ped.Debug = Convert.ToString(this.action);

                if (this.action == ETaskChasePedOnFootAction.CatchUp)
                {
                    /*
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexControlMovement) || ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimplePlayRandomAmbients) || ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill) || this.firstRun)
                    {
                        ped.Task.ClearAll();
                        Game.LoadAllPathNodes = true;
                        ped.SetNextDesiredMoveState(Native.EPedMoveState.Sprint);
                        ped.SetPathfinding(true, true, true);
                        ped.Task.GoTo(target, Native.EPedMoveState.Sprint);
                        timeToReapply = 0;
                        firstRun = false;
                    }
                    */

                    /*
                     * Apologies for all the commented code!

                     * This is my new chase task, and it uses some of the game's internal stuff as well as our beloved go to char aiming task.
                     * Basically instead of having all the cops running together using the go to char aiming task, this makes it so that only 4 cops
                     * can be chasing using that task at any time, making the rest use FightAgainst and the advance/flank subtask.
                     * To stop the cops from opening fire (as they seem to do if the ped they are chasing is a mission ped (flags maybe?)), I set their
                     * shoot rate to 0, then set it back to slightly above normal when the task finishes.
                    */

                    // The catch up state means the cop isn't actively chasing using the GoToCharAiming task.
                    // We apply the game's internal stuff here.
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombat))
                    {
                        // If the ped is not in combat, apply the combat tasks and set the shoot rate and stuff.
                        AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();

                        // Tasks/settings for criminal
                        this.target.WantedByPolice = true;
                        this.target.PriorityTargetForEnemies = true;

                        // Tasks/settings for cop
                        ped.SetPathfinding(true, true, true);
                        ped.WillUseCover(false);
                        ped.SetShootRate(0);

                        ped.Task.FightAgainst(this.target);
                    }
                  
                    // The combat tasks make the ped fire at the criminal if they get too close (this doesn't happen because of the shoot rate, so they just aim instead),
                    // but regardless, we don't want this so we'll try to clear their tasks and make them flank or advance or something.
                    
                    if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatFireSubtask))
                    {
                        if (this.target.Wanted.OfficersChasingOnFoot > 4)
                        {
                            // Too many cops actively chasing, lets see if we can make this one flank or something
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatFlankSubtask) || !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatAdvanceSubtask))
                            {
                                ped.Task.ClearAll();
                                ped.SetShootRate(0);
                                ped.APed.TaskCombatFlankSubtask(this.target.APed);
                                return;
                            }
                        }
                        else
                        {
                            // Otherwise if there are less than 4 cops chasing, because they are probably quite close, we'll assign them to chase.
                            this.action = ETaskChasePedOnFootAction.Chase;
                            this.target.Wanted.OfficersChasingOnFoot++;
                            this.firstRun = true;
                            return;
                        }
                    }
                    
                    // The combat task makes them use the investigate task if the suspect gets too far away or they lose visual, we don't really want this, so we'll see if we can stop it
                    if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatInvestigateSubtask))
                    {
                        if (!toldToStopInvestigating)
                        {
                            /*
                            // This was the old code for this block but it didn't seem to work too well.
                            AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();

                            // Cop Tasks/Settings
                            ped.Task.ClearAll();
                            ped.APed.TaskCombatFlankSubtask(this.target.APed);
                            GTA.Native.Function.Call("SET_CHAR_SHOOT_RATE", ped.Handle, 0);
                            toldToStopInvestigating = true;

                            // Debug text
                            TextHelper.PrintText("Ped was investigating and was told to flank.", 2000);
                            return;
                             * */

                            // If we haven't tried to get them to stop investigating, we'll try now
                            AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();

                            // Cop Tasks/Settings
                            ped.Task.ClearAll();
                            ped.APed.TaskCombatFlankSubtask(this.target.APed);
                            ped.SetShootRate(0);
                            toldToStopInvestigating = true;

                            // Debug text
                            // TextHelper.PrintText("Ped was investigating and was told to investigate (lol).", 2000);
                            return;
                        }
                        else
                        {
                            // If we've already told them to stop investigating but they're still doing it, then I guess we should just let them at it.
                        }
                    }

                    if (toldToStopInvestigating)
                    {
                        // If we've told them to stop investigating, check if they are still doing it.
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatInvestigateSubtask))
                        {
                            // If they're not, we can unset the investigating bool.
                            toldToStopInvestigating = false;
                        }
                    }

                    if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWanderCop))
                    {
                        // If for some reason they've gone back to being a normal cop, we'll try to get them back
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombat))
                        {
                            ped.SetShootRate(0);
                            ped.Task.FightAgainst(this.target);
                        }
                    }

                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimplePlayUpperCombatAnim))
                    {
                        // If they are not playing upper body combat anims (NOOSE mainly), make them
                        if (ped.Speed > 1.5f)
                        {
                            if (ped.Weapons.CurrentType == ped.Weapons.AnyHandgun)
                            {
                                if (!ped.Animation.isPlaying(new AnimationSet("gun@cops"), "pistol_partial_a") || ped.Animation.GetCurrentAnimationTime(new AnimationSet("gun@cops"), "pistol_partial_a") > 0.9f)
                                {
                                    ped.Task.PlayAnimSecondaryUpperBody("pistol_partial_a", "gun@cops", 4.0f, false);
                                }
                            }
                            else if (ped.Weapons.CurrentType == ped.Weapons.AnyShotgun || ped.Weapons.CurrentType == ped.Weapons.AnyAssaultRifle)
                            {
                                if (!ped.Animation.isPlaying(new AnimationSet("gun@cops"), "swat_rifle") || ped.Animation.GetCurrentAnimationTime(new AnimationSet("gun@cops"), "swat_rifle") > 0.9f)
                                {
                                    ped.Task.PlayAnimSecondaryUpperBody("swat_rifle", "gun@cops", 4.0f, false);
                                }
                            }
                        }
                    }

                    /*
                    if (ped.Position.DistanceTo(target.Position) < 5.0f)
                    {
                        action = ETaskChasePedOnFootAction.Chase;
                        firstRun = true;
                        TextHelper.PrintText("Cop is now chasing", 3000);
                    }
                    else
                    {
                        timeToReapply ++;
                    }

                    if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHitWall))
                    {
                        TextHelper.PrintText("Hit wall!", 5000);
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatFlankSubtask))
                        {
                            AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                            ped.Task.FightAgainst(target);
                            ped.APed.TaskCombatFlankSubtask(target);
                        }
                        else
                        {
                            if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleFireGun))
                            {
                                if (!target.PedData.CurrentChase.ForceKilling)
                                {
                                    ped.Task.ClearAll();
                                    ped.Task.GoToCharAiming(target, 0.5f, 1.0f);
                                }
                            }
                        }
                    }
                    */
                }
                else if (this.action == ETaskChasePedOnFootAction.Chase)
                {
                    // This is for cops that are actively chasing - they need to be close to the suspect.


                    // Taser logic
                    if (this.taskCopTasePed != null)
                    {
                        if (!this.taskCopTasePed.Active)
                        {
                            Log.Debug("Process: Taser child task is no longer active", this);
                            this.taskCopTasePed = null;
                        }
                        else
                        {


                            // If taser has hit, but task is still active, don't do anything to let it finish clean
                            if (this.taskCopTasePed.HasTaserHit)
                            {
                                return;
                            }
                        }

                        // If cop is fleeing (to get away, don't do anything)
                        if (ped.IsFleeing)
                        {
                            return;
                        }
                    }

                    if (ped.Position.DistanceTo2D(target.Position) > 150.0f)
                    {
                        // The cop is way too far behind the chase, make him regroup.
                        this.firstRun = true;
                        this.action = ETaskChasePedOnFootAction.Regroup;
                        return;
                    }

                    if (ped.Position.DistanceTo(target.Position) > 30.0f && this.target.Wanted.OfficersChasingOnFoot > 1)
                    {
                        // If cop is far away, only make him stop chasing if he's not the last cop chasing.  We alawys try to keep one cop at least chasing.
                        this.target.Wanted.OfficersChasingOnFoot--;
                        this.action = ETaskChasePedOnFootAction.CatchUp;
                        return;
                    }

                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                    {
                        // Otherwise, if the cop is close enough, apply chase task.
                        ped.SetPathfinding(true, true, true);
                        ped.Task.GoToCharAiming(this.target, 4f, 8f);
                    }
                    else
                    {
                        // If the cop has the chase task running

                        /*

                         * OLD CODE *
                        
                        int numberNearCops = 0;

                        foreach (CPed cop in Pools.PedPool)
                        {
                            if (cop != null && cop.Exists())
                            {
                                if (cop.Position.DistanceTo2D(ped.Position) < 6.0f || cop.IsTouching(ped) || ped.IsTouching(cop))
                                {
                                    numberNearCops += 1;
                                }
                            }
                        }

                        if (numberNearCops > 1 && lastRegroup > 50)
                        {
                            TextHelper.PrintText("Number of near cops: " + numberNearCops, 5000);
                            regroupActiveFor = 0;
                            lastRegroup = 0;
                            action = ETaskChasePedOnFootAction.Regroup;
                        }
                        else
                        {
                            if (Common.GetRandomBool(0, 80, 1))
                            {
                                if (!ped.IsSayingAmbientSpeech())
                                {
                                    if (ped.Model == "M_Y_SWAT")
                                    {
                                        ped.SayAmbientSpeech("TARGET");
                                    }
                                    else if (ped.Model == "M_M_FBI")
                                    {
                                        if (Common.GetRandomBool(0, 2, 1)) ped.SayAmbientSpeech("CHASE_IN_GROUP"); else ped.SayAmbientSpeech("SURROUNDED");
                                    }
                                    else
                                    {
                                        if (Common.GetRandomBool(0, 2, 1)) ped.SayAmbientSpeech("CHASE_SOLO"); else ped.SayAmbientSpeech("SURROUNDED");
                                    }
                                }
                            }
                        }
                        
                        */

                        // Speech stuff for chasing.

                        if (!this.target.Wanted.HasBeenAskedToSurrender)
                        {
                            if (ped.HasSpottedCharInFront(this.target) && ped.Position.DistanceTo(this.target.Position) < 10.0f)
                            {
                                // If the suspect hasn't been asked to surrender yet, ask them
                                if (ped.IsAmbientSpeechPlaying)
                                {
                                    ped.CancelAmbientSpeech();
                                }

                                ped.SayAmbientSpeech(ped.VoiceData.StopSpeech);

                                this.target.Wanted.HasBeenAskedToSurrender = true;

                                DelayedCaller.Call(delegate
                                {
                                    if (this.target.PedData.WillStop)
                                    {
                                        ped.Task.ClearAll();
                                        ped.Task.GoToCharAiming(this.target, 4f, 10f);
                                        if (!this.target.IsAmbientSpeechPlaying)
                                        {
                                            this.target.SayAmbientSpeech("GUN_RUN");
                                        }
                                        this.target.Intelligence.TaskManager.ClearTasks();
                                        this.target.Task.ClearAll();
                                        this.target.Task.HandsUp(10000);
                                        this.target.Wanted.Surrendered = true;

                                        if (this.target.IsInVehicle)
                                        {
                                            this.target.Task.LeaveVehicle();
                                        }
                                    }

                                }, this, Common.GetRandomValue(1000, 2500));
                            }
                        }

                        if (!ped.IsSayingAmbientSpeech())
                        {
                            int speakingCopsNearby = 0;

                            foreach (CPed cop in Pools.PedPool.GetAll())
                            {
                                if (cop != null && cop.Exists())
                                {
                                    if (cop.PedGroup == EPedGroup.Cop)
                                    {
                                        if (cop.Position.DistanceTo(ped.Position) < 10.0f)
                                        {
                                            if (cop.IsAmbientSpeechPlaying)
                                            {
                                                speakingCopsNearby++;
                                            }
                                        }
                                    }
                                }
                            }

                            if (speakingCopsNearby < 2)
                            {
                                // This stops cops from spamming the speech
                                if (Common.GetRandomBool(0, 50, 1))
                                {
                                    ped.SayAmbientSpeech(ped.VoiceData.ChaseSpeech);
                                }
                            }
                        }

                        if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun))
                        {
                            if (ped.Position.DistanceTo2D(this.target.Position) > 10f)
                            {
                                // They are aiming and running, can't have this!
                                ped.Task.GoToCharAiming(this.target, 4f, 6f);
                            }
                        }
                    }
                }
                else if (this.action == ETaskChasePedOnFootAction.Regroup)
                {
                    if (ped.Position.DistanceTo2D(this.target.Position) < 75.0f)
                    {
                        this.action = ETaskChasePedOnFootAction.CatchUp;
                        return;
                    }

                    if (this.firstRun)
                    {
                        ped.Task.ClearAll();
                        // ped.Task.GoTo(this.target, Native.EPedMoveState.Sprint);
                        this.firstRun = false;
                    }

                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                    {
                        ped.Task.GoToCharAiming(this.target, 4, 6);
                    }
                }
            }
        }

        /// <summary>
        /// Called when a <see cref="TaskCopTasePed"/> task is ran as subtask of this.
        /// </summary>
        /// <param name="taskCopTasePed">The subtask.</param>
        public void TaseTaskHasRegisteredAsChild(TaskCopTasePed taskCopTasePed)
        {
            this.taskCopTasePed = taskCopTasePed;
            Log.Debug("TaseTaskHasRegisteredAsChild: Entry state: " + this.action, this);
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskCopChasePedOnFoot"; }
        }
    }

    /// <summary>
    /// Describes the different actions.
    /// </summary>
    internal enum ETaskChasePedOnFootAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Catch up to the suspect
        /// </summary>
        CatchUp,

        /// <summary>
        /// Chase the suspect
        /// </summary>
        Chase,

        /// <summary>
        /// If the ped falls too far behind the chase, make them regroup.
        /// </summary>
        Regroup
    }
}