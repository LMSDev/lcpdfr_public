namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.Events;

    using Main = LCPD_First_Response.LCPDFR.Main;
    using TaskSequence = LCPD_First_Response.Engine.Scripting.Tasks.TaskSequence;
    using Timer = LCPD_First_Response.Engine.Timers.Timer;
    using LCPD_First_Response.Engine.Scripting;

    /// <summary>
    /// Handles the process of arresting a ped.
    /// </summary>
    [ScriptInfo("Arrest", true)]
    internal class Arrest : GameScript
    {
        /// <summary>
        /// Whether backup has been requested.
        /// </summary>
        private bool backupRequested;

        /// <summary>
        /// Whether the script already got user input how to deal with the suspect.
        /// </summary>
        private bool gotInput;

        /// <summary>
        /// Whether the ped has resisted and is fighting.
        /// </summary>
        private bool hasResisted;

        /// <summary>
        /// Whether the ped made a new decision after being unarmed.
        /// </summary>
        private bool madeNewDecision;

        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The instance to manage taking a suspect to a pd.
        /// </summary>
        private PlayerSuspectTransport playerSuspectTransport;

        /// <summary>
        /// Whether the player takes care of the suspect.
        /// </summary>
        private bool playerTakesCare;

        /// <summary>
        /// The scenario task to arrest the ped via AI.
        /// </summary>
        private TaskScenario taskScenario;

        /// <summary>
        /// Whether the ped has turned away from player already.
        /// </summary>
        private bool turnedFromPlayer;

        /// <summary>
        /// The scenario.
        /// </summary>
        private ScenarioSuspectTransport scenarioSuspectTransport;

        /// <summary>
        /// The timer used to measure the timer the arrest key is hold down.
        /// </summary>
        private Timer holdDownArrestKeyTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Arrest"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public Arrest(CPed ped)
        {
            this.ped = ped;

            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Arrest"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        /// <param name="alreadyArrested">
        /// Whether the ped has been already arrested and cuffing etc. can be skipped.
        /// </param>
        public Arrest(CPed ped, bool alreadyArrested)
        {
            this.ped = ped;

            if (!alreadyArrested)
            {
                this.Initialize();
            }
            else
            {
                // If in player's vehicle, take to PD
                if (CPlayer.LocalPlayer.Ped.IsInVehicle && this.ped.IsInVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle))
                {
                    // Listen to event when not receiving it
                    if (!this.playerTakesCare)
                    {
                        // Create new player suspect transport instance
                        this.playerSuspectTransport = new PlayerSuspectTransport(this.ped);
                        this.playerSuspectTransport.SuspectInCustody += this.playerSuspectTransport_SuspectInCustody;
                        Main.ScriptManager.RegisterScriptInstance(this.playerSuspectTransport);
                        this.playerTakesCare = true;
                    }
                }
            }
        }

        /// <summary>
        /// Fired when the ped resisted during the arrest.
        /// </summary>
        public event Action PedResisted;

        /// <summary>
        /// Gets a value indicating whether the arrest still requires player attention, so a key input or bringing the suspect to a PD.
        /// </summary>
        public bool RequiresPlayerAttention
        {
            get
            {
                // No input yet, still needs player
                if (!this.gotInput)
                {
                    return true;
                }
                else
                {
                    // Got input but no backup, so suspect should be brought to PD
                    if (!this.backupRequested)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the suspect.
        /// </summary>
        public CPed Suspect
        {
            get
            {
                return this.ped;
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.ped.Exists() || !this.ped.IsAliveAndWell)
            {
                this.End();
                return;
            }

            // If ped has resisted, wait for death or check if ped has dropped weapon
            if (this.hasResisted)
            {
                // If decision is made, add blip
                if (!this.ped.Wanted.IsDeciding && !this.ped.HasBlip)
                {
                    this.ped.AttachBlip();

                    if (this.PedResisted != null)
                    {
                        this.PedResisted();
                    }
                }

                // If decision whether to fight or surrender is made, but ped has been unarmed, make a new decision
                if (!this.madeNewDecision && !this.ped.Wanted.IsDeciding && !this.ped.IsArmed())
                {
                    Log.Debug("Process: Weapon dropped", this);

                    // Force surrender
                    this.ped.PedData.AlwaysSurrender = true;
                    this.ped.PedData.AlwaysResistArrest = false;
                    this.ped.Intelligence.OnBeingArrested(CPlayer.LocalPlayer.Ped);

                    // Set flags
                    this.ped.Wanted.IsBeingArrestedByPlayer = true;
                    this.ped.MakeFriendsWithCops(true);
                    this.ped.Task.ClearAll();
                    this.ped.Intelligence.TaskManager.ClearTasks();
                    this.ped.Task.HandsUp(10000);
                    this.ped.DontActivateRagdollFromPlayerImpact = true;

                    // Start arresting
                    this.StartArresting();
                    this.hasResisted = false;

                    Log.Debug("Process: Surrender", this);

                    this.madeNewDecision = true;
                }

                return;
            }

            if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.BeingBusted))
            {
                new EventPedBeingArrested(this.ped);

                if (!this.ped.Wanted.IsCuffed)
                {
                    if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(this.ped.Position) < 0.8f)
                    {
                        if (!CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CuffPed))
                        {
                            TaskCuffPed taskCuffPed = new TaskCuffPed(this.ped);
                            taskCuffPed.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.MainTask);
                        }
                    }
                    else
                    {
                        if (!this.turnedFromPlayer)
                        {
                            this.ped.Task.AchieveHeading(CPlayer.LocalPlayer.Ped.Heading);
                            this.turnedFromPlayer = true;
                        }
                    }
                }
                else
                {
                    // Check if there's another arrest script running that requires input
                    bool multipleSuspects = false;
                    if (Main.ScriptManager.GetRunningScriptInstances("Arrest").Length > 1)
                    {
                        foreach (BaseScript scriptInstance in Main.ScriptManager.GetRunningScriptInstances("Arrest"))
                        {
                            if (scriptInstance != this)
                            {
                                if (!(scriptInstance as Arrest).gotInput)
                                {
                                    multipleSuspects = true;
                                    break;
                                }
                            }
                        }
                    }

                    bool isTargetting = CPlayer.LocalPlayer.IsTargetting(this.ped);
                    if (!isTargetting)
                    {
                        isTargetting = CPlayer.LocalPlayer.IsTargettingChar(this.ped);
                    }


                    // Ped is cuffed, ask player what to do next
                    if (!TextHelper.IsHelpboxBeingDisplayed)
                    {
                        if (multipleSuspects)
                        {
                            if (!isTargetting)
                            {
                                // Prevent from overriden the help text of the other arrest instance
                                if (!CPlayer.LocalPlayer.IsTargettingOrAimingAtPed)
                                {
                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_MULTIPLE_SUSPECTS"));
                                }
                            }
                            else
                            {
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_SUSPECT_IS_CUFFED"));
                            }
                        }
                        else
                        {
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_SUSPECT_IS_CUFFED"));
                        }
                    }

                    // When more than one arrest is running, player has to aim at the suspect in order for the keychecks to be processed
                    if (multipleSuspects)
                    {
                        if (!isTargetting)
                        {
                            return;
                        }
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ArrestDriveToPD))
                    {
                        // Ped will follow player and wait for vehicle
                        this.gotInput = true;
                        this.ped.Wanted.HasBeenArrested = true;
                        this.ped.Intelligence.TaskManager.ClearTasks();
                        this.playerTakesCare = true;
                        Stats.UpdateStat(Stats.EStatType.Arrests, 1, this.ped.Position);

                        // Create new player suspect transport instance
                        TextHelper.ClearHelpbox();
                        this.playerSuspectTransport = new PlayerSuspectTransport(this.ped);
                        this.playerSuspectTransport.SuspectInCustody += this.playerSuspectTransport_SuspectInCustody;
                        Main.ScriptManager.RegisterScriptInstance(this.playerSuspectTransport);

                        Stats.UpdateStat(Stats.EStatType.ArrestTakenToStation, 1);
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ArrestCallTransporter))
                    {
                        this.gotInput = true;
                        this.ped.Intelligence.TaskManager.ClearTasks();
                        TaskPlayAnimationAndRepeat taskPlayAnimationAndRepeat = new TaskPlayAnimationAndRepeat("idle", "move_m@h_cuffed", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                        taskPlayAnimationAndRepeat.AssignTo(this.ped, ETaskPriority.MainTask);

                        Main.BackupManager.RequestBackupUnit(this.ped.Position, true, EModelFlags.IsPolice, EModelFlags.IsSuspectTransporter, 2, true, ECopState.SuspectTransporter, this.BackupDispatchedCallback);
                        this.backupRequested = true;
                        Stats.UpdateStat(Stats.EStatType.Arrests, 1, this.ped.Position);
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_SUSPECT_WILL_BE_DRIVEN_AWAY"));

                        Stats.UpdateStat(Stats.EStatType.ArrestCalledTransporter, 1);
                    }
                }
            }
            else
            {
                // Being busted task not running and target not cuffed, something went terribly wrong here, so we reapply the task
                if (!this.ped.Wanted.IsCuffed && this.ped.Animation.isPlaying(new AnimationSet("busted"), "idle_2_hands_up"))
                {
                    Log.Warning("Process: Being busted not active but target not yet cuffed and still in animation", this);
                    TaskBeingBusted taskBeingBusted = new TaskBeingBusted(CPlayer.LocalPlayer.Ped);
                    taskBeingBusted.AssignTo(this.ped, ETaskPriority.MainTask);
                }

                if (this.backupRequested)
                {
                    // If suspect is in custody, play sound
                    if (this.scenarioSuspectTransport != null)
                    {
                        if (this.scenarioSuspectTransport.IsSuspectInCustody)
                        {
                            DelayedCaller.Call(delegate { AudioHelper.PlaySuspectInCustody(true); }, 1000);
                            this.scenarioSuspectTransport = null;
                            this.End();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            if (this.playerSuspectTransport != null)
            {
                this.playerSuspectTransport.SuspectInCustody -= this.playerSuspectTransport_SuspectInCustody;
                if (!this.playerSuspectTransport.Finished)
                {
                    this.playerSuspectTransport.End();
                }
            }

            this.ped.Wanted.IsBeingArrestedByPlayer = false;
            this.ped.ReleaseOwnership(this);

            base.End();
        }

        /// <summary>
        /// Checks whether <paramref name="ped"/> is being arrested in this instance.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>True if being arrested, false otherwise.</returns>
        public bool IsArrestingPed(CPed ped)
        {
            return this.ped == ped;
        }

        /// <summary>
        /// Called when the player has triggered arrest for this ped again, although it's already arrested, e.g. to release it.
        /// </summary>
        public void PlayerTriggeredArrestAgain()
        {
            // Start a timer checking how long the key is still pressed
            this.holdDownArrestKeyTimer = new Timer(10, this.PlayerTriggeredArrestAgainTimer, DateTime.Now);
            this.holdDownArrestKeyTimer.Start();
        }

        /// <summary>
        /// Called when the suspect is in custody.
        /// </summary>
        private void playerSuspectTransport_SuspectInCustody()
        {
            this.End();
        }

        /// <summary>
        /// Called a short while after the player triggered arrest again, to distinguish between a keypress and holding down the key.
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void PlayerTriggeredArrestAgainTimer(params object[] parameter)
        {
            TimeSpan timeElasped = DateTime.Now - (DateTime)parameter[0];

            // If key is still down
            if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.Arrest))
            {
                if (timeElasped.TotalMilliseconds < 350)
                {
                    return;
                }

                this.holdDownArrestKeyTimer.Stop();

                // If down for longer than 350ms, request new unit
                if (this.backupRequested)
                {
                    if (this.taskScenario != null)
                    {
                        // No longer works when already in custody
                        if (this.scenarioSuspectTransport.IsSuspectInCustody)
                        {
                            return;
                        }

                        this.taskScenario.MakeAbortable(this.ped);
                        this.taskScenario = null;
                        this.scenarioSuspectTransport = null;

                        // Reclaim control until backup is dispatched
                        this.ped.RequestOwnership(this);
                        this.ContentManager.AddPed(this.ped);
                    }

                    Main.BackupManager.RequestPoliceBackup(this.ped.Position, null, true, EModelFlags.None, EModelFlags.IsSuspectTransporter, false, ECopState.SuspectTransporter, this.BackupDispatchedCallback);
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_SUSPECT_WILL_BE_DRIVEN_AWAY"));
                }
            }
            else
            {
                this.holdDownArrestKeyTimer.Stop();

                // If ped has already been arrested, give options to call for additional backup or to release the suspect
                if (this.ped.Wanted.IsBeingArrestedByPlayer)
                {
                    if (this.ped.IsInVehicle)
                    {
                        if (this.playerSuspectTransport != null)
                        {
                            this.playerSuspectTransport.ForceLeaveVehicle();
                        }
                    }
                    else
                    {
                        if (this.playerSuspectTransport != null)
                        {
                            this.playerSuspectTransport.End();
                        }

                        // Release suspect
                        this.ped.Task.ClearAll();
                        this.ped.Wanted.IsCuffed = false;
                        this.ped.Wanted.HasBeenArrested = false;
                        this.ped.Intelligence.TaskManager.ClearTasks();
                        this.End();
                        this.ped.Wanted.ResetArrestFlags();

                        if (this.backupRequested && this.taskScenario != null)
                        {
                            this.taskScenario.MakeAbortable(this.ped);
                            this.taskScenario = null;
                            this.scenarioSuspectTransport = null;
                        }
                    }

                    TextHelper.ClearHelpbox();
                }
            }
        }

        /// <summary>
        /// Called when backup has been dispatched.
        /// </summary>
        /// <param name="cops">The cops.</param>
        private void BackupDispatchedCallback(CPed[] cops)
        {
            // If freed in the meantime, abort and free cops
            if (!this.ped.Wanted.IsBeingArrestedByPlayer)
            {
                foreach (CPed cop in cops)
                {
                    if (cop.Exists())
                    {
                        cop.NoLongerNeeded();
                    }
                }

                return;
            }

            // Start new scenario and give ped control to it
            this.ContentManager.RemovePed(this.ped);
            if (this.ped.Owner.GetType() != typeof(Arrest))
            {
                Log.Warning("BackupDispatchedCallback: Current owner is not Arrest script. Tell LMS.", this);

                foreach (CPed cop in cops)
                {
                    if (cop.Exists())
                    {
                        cop.NoLongerNeeded();
                    }
                }

                return;
            }

            this.ped.ReleaseOwnership(this);

            this.scenarioSuspectTransport = new ScenarioSuspectTransport(this.ped, cops);
            this.taskScenario = new TaskScenario(this.scenarioSuspectTransport);
        }

        /// <summary>
        /// Initializes the arresting process.
        /// </summary>
        private void Initialize()
        {
            // Fire event
            new EventPlayerStartedArrest(this.ped, this);

            // The initial arresting process
            Action action = delegate
            {
                // Restore flag
                this.ped.PedData.CanBeArrestedByPlayer = true;

                // Ensure ped was not killed
                if (!this.ped.IsAliveAndWell)
                {
                    Log.Debug("Initialize: Ped died while asking to arrest", this);
                    this.End();
                    return;
                }

                // Check if arrest has already resisted
                if (!this.ped.Wanted.ResistedArrest)
                {
                    // Make mission char and add to content manager
                    this.ped.RequestOwnership(this);
                    this.ped.AlwaysFreeOnDeath = true;
                    this.ContentManager.AddPed(this.ped);

                    // Prevent fleeing
                    this.ped.BlockPermanentEvents = true;
                    this.ped.Task.ClearAll();

                    // Invoke arresting
                    this.ped.Intelligence.OnBeingArrested(CPlayer.LocalPlayer.Ped);
                    if (this.ped.Wanted.Surrendered)
                    {
                        // Start the actual arresting process
                        this.ped.Wanted.IsBeingArrestedByPlayer = true;
                        this.ped.MakeFriendsWithCops(true);
                        this.ped.Task.ClearAll();
                        this.ped.Intelligence.TaskManager.ClearTasks();
                        this.ped.Task.HandsUp(10000);
                        this.ped.DontActivateRagdollFromPlayerImpact = true;

                        DelayedCaller.Call(this.StartArresting, 2000);
                    }
                    else
                    {
                        // Ped has resisted and is going to fight
                        this.hasResisted = true;
                        this.ped.Wanted.IsBeingArrestedByPlayer = false;
                    }
                }
                else
                {
                    if (this.PedResisted != null)
                    {
                        this.PedResisted();
                    }

                    this.ped.Wanted.IsBeingArrestedByPlayer = false;

                    // Shutdown
                    this.End();
                }
            };

            // Play audio and proceed after a short delay, so audio was already played and it looks more realistic
            CPlayer.LocalPlayer.Ped.SayAmbientSpeech(LCPDFRPlayer.LocalPlayer.Ped.VoiceData.ArrestSpeech);
            DelayedCaller.Call(delegate { action();  }, 1000);

            // Make ped stand still though (since when it has been stopped it will stand still)
            // If not, there would be 1 second where the ped would walk after player wanted to arrest
            this.ped.Task.StandStill(1000);

            // Block ped so it can't be arrested anymore
            this.ped.PedData.CanBeArrestedByPlayer = false;
            this.ped.Wanted.IsBeingArrestedByPlayer = true;
        }

        /// <summary>
        /// Starts the arresting process.
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void StartArresting(params object[] parameter)
        {
            TextHelper.PrintText(CultureHelper.GetText("ARREST_GET_BEHIND"), 5000);

            TaskBeingBusted taskBeingBusted = new TaskBeingBusted(CPlayer.LocalPlayer.Ped);
            taskBeingBusted.AssignTo(this.ped, ETaskPriority.MainTask);
        }
    }
}