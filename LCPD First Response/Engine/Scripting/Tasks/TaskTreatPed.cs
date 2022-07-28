namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Replica of the medic 911 call logic, ped will go to the target and inject them with something to heal them.
    /// </summary>
    internal class TaskTreatPed : PedTask
    {
        /// <summary>
        /// The ped to be treated
        /// </summary>
        private CPed target;

        /// <summary>
        /// Whether or not we're waiting for the treatment animations to finish
        /// </summary>
        private bool waitingForFinish;

        /// <summary>
        /// If the treatment animation has been reapplied
        /// </summary>
        private bool animationReApplied;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskTreatPed"/> class.
        /// </summary>
        /// <param name="speech">
        /// The ped to treat.
        /// </param>
        public TaskTreatPed(CPed ped)
            : base(ETaskID.TreatPed)
        {
            this.target = ped;
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (ped.Exists())
            {
                if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    ped.Task.ClearAll();
                }

                ped.DontActivateRagdollFromPlayerImpact = false;
                ped.PreventRagdoll = false;
                if (target.Exists()) target.PreventRagdoll = false;
            }
            SetTaskAsDone();
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

            if (waitingForFinish)
            {
                // If waiting for the finish
                if (target.Animation.isPlaying(new GTA.AnimationSet("Missemergencycall"), "player_health_recieve"))
                {
                    // If the target ped is playing the recieve animation
                    if (!ped.Animation.isPlaying(new GTA.AnimationSet("Missemergencycall"), "medic_health_inject"))
                    {
                        // If the medic ped is not playing the medic animation, reapply it.
                        if (!animationReApplied)
                        {
                            ped.Animation.Play(new GTA.AnimationSet("Missemergencycall"), "medic_health_inject", 4.0f);
                            animationReApplied = true;
                        }
                    }
                }
                else
                {
                    // Otherwise, all is finished

                    // Stop bleeding
                    Native.Natives.SetCharBleeding(target, false);

                    // Refill health
                    target.Health = 100;

                    // Ped speech
                    target.SayAmbientSpeech("SAVED");

                    // Flags
                    MakeAbortable(ped);
                    waitingForFinish = false;
                }
            }
            else
            {
                // Otherwise, if the task is still running
                if (ped.IsInVehicle)
                {
                    // If ped is in vehicle, make them leave it and close the door (important to stop them getting stuck)
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle) || ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleCarGetOut))
                    {
                        ped.Task.LeaveVehicle(ped.CurrentVehicle, true);
                    }
                }
                else
                {
                    if (ped.Position.DistanceTo(target.Position) < 2.0f)
                    {
                        // If the ped is close to the target and facing it, start the animations
                        if (Native.Natives.IsCharFacingChar(target, ped) || Native.Natives.IsCharFacingChar(ped, target))
                        {
                            ped.SayAmbientSpeech("EMERG_MEDIC_HEALS_P");
                            target.Animation.Play(new GTA.AnimationSet("Missemergencycall"), "player_health_recieve", 4.0f);
                            ped.Animation.Play(new GTA.AnimationSet("Missemergencycall"), "medic_health_inject", 4.0f);
                            waitingForFinish = true;
                        }
                        else
                        {
                            // Otherwise, make the target turn to face the ped
                            if (!target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord))
                            {
                                ped.DontActivateRagdollFromPlayerImpact = true;
                                ped.PreventRagdoll = true;
                                if (target != CPlayer.LocalPlayer.Ped)
                                {
                                    target.PreventRagdoll = true;
                                    target.Task.ClearAll();
                                    target.Task.TurnTo(ped);
                                }
                                ped.Task.GoTo(target, 0.0f, 1.0f);
                            }
                        }
                    }
                    else
                    {
                        // Go to the ped
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                        {
                            ped.SayAmbientSpeech("EMERG_MEDIC_SEES_P");
                            ped.Weapons.Select(GTA.Weapon.Unarmed);
                            ped.Task.GoToCharAiming(target, 2.0f, 2.5f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskTreatPed"; }
        }
    }
}
