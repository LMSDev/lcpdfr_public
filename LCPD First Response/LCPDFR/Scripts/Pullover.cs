namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using AdvancedHookManaged;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.Events;

    using Main = LCPD_First_Response.LCPDFR.Main;
    using TaskSequence = GTA.TaskSequence;

    /// <summary>
    /// Handles a pullover.
    /// </summary>
    [ScriptInfo("Pullover", true)]
    internal class Pullover : GameScript, ICanOwnEntities, IPedController
    {
        /// <summary>
        ///  The actor during the pullover, either the driver or the passenger, depending on the side of the street.
        /// </summary>
        private CPed actor;

        /// <summary>
        /// Whether the cop and the suspect are already in position and they don't need to park anymore.
        /// </summary>
        private bool alreadyInPosition;

        /// <summary>
        /// All peds.
        /// </summary>
        private CPed[] allPeds;

        /// <summary>
        /// The checkpoint to walk into.
        /// </summary>
        private ArrowCheckpoint checkpoint;

        /// <summary>
        /// The checkpoint to alternatively walk into.
        /// </summary>
        private ArrowCheckpoint checkpoint2;

        /// <summary>
        /// The clipboard used when selecting the fine amount.
        /// </summary>
        private GTA.Object clipboard;

        /// <summary>
        /// The cop performing the pullover.
        /// </summary>
        private CPed cop;

        /// <summary>
        /// Used to delay execution when the pullover is done by AI so actions look more realistic.
        /// </summary>
        private NonAutomaticTimer delayTimer;

        /// <summary>
        /// The driver.
        /// </summary>
        private CPed driver;

        /// <summary>
        /// Whether the player went into the checkpoint on the driver side.
        /// </summary>
        private bool inDriverCheckpoint;

        /// <summary>
        /// Whether the pullover is done by player.
        /// </summary>
        private bool isPlayerPullover;

        /// <summary>
        /// Whether the script is waiting for the audio to finish
        /// </summary>
        private bool isWaitingForAudio;

        /// <summary>
        /// The passengers.
        /// </summary>
        private CPed[] passengers;

        /// <summary>
        /// The pursuit instance.
        /// </summary>
        private Pursuit pursuit;

        /// <summary>
        /// The pullover state.
        /// </summary>
        private EPulloverState state;

        /// <summary>
        /// The parking task.
        /// </summary>
        private TaskParkVehicle taskParkVehicle;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pullover"/> class allowing an AI controlled ped to perform the pullover.
        /// </summary>
        /// <param name="cop">
        /// The cop performing the pullover.
        /// </param>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="alreadyInPosition">
        /// Whether the cop and the suspect are already placed.
        /// </param>
        /// <param name="forcePursuit">
        /// Whether a pursuit should be forced.
        /// </param>
        public Pullover(CPed cop, CVehicle vehicle, bool alreadyInPosition, bool forcePursuit = false)
        {
            this.cop = cop;
            this.vehicle = vehicle;
            this.alreadyInPosition = alreadyInPosition;
            this.driver = this.vehicle.Driver;
            if (this.cop.PedGroup == EPedGroup.Player)
            {
                this.delayTimer = new NonAutomaticTimer(0);
                this.isPlayerPullover = true;
            }
            else
            {
                this.delayTimer = new NonAutomaticTimer(2000);
            }

            this.Initialize(forcePursuit, true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pullover"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="forcePursuit">
        /// Whether a pursuit should be forced.
        /// </param>
        /// <param name="noTextIntroduction">
        /// Whether no text should be displayed as introduction.
        /// </param>
        public Pullover(CVehicle vehicle, bool forcePursuit = false, bool noTextIntroduction = false)
        {
            this.cop = CPlayer.LocalPlayer.Ped;
            this.isPlayerPullover = true;
            this.vehicle = vehicle;
            this.driver = this.vehicle.Driver;
            CPlayer.LocalPlayer.LastPedPulledOver = new CPed[] { this.driver };
            this.delayTimer = new NonAutomaticTimer(0);

            this.Initialize(forcePursuit, noTextIntroduction);
        }

        /// <summary>
        /// Gets a value indicating whether the actual pullover has not yet started because the initial audio is still playing.
        /// </summary>
        public bool IsWaitingForStart
        {
            get
            {
                return this.isWaitingForAudio;
            }
        }

        /// <summary>
        /// Fired when the ped resisted during the pullover, that is a chase or a driveby was started.
        /// </summary>
        public event Action PedResisted;

        /// <summary>
        /// Describes the state of the pullover.
        /// </summary>
        private enum EPulloverState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None,

            /// <summary>
            /// Waiting for start.
            /// </summary>
            Waiting,

            /// <summary>
            /// Vehicle tries to park.
            /// </summary>
            Parking,

            /// <summary>
            /// Parking has finished.
            /// </summary>
            ParkingFinished,

            /// <summary>
            /// Player should park behind suspect.
            /// </summary>
            GetBehindCar,

            /// <summary>
            /// Player should exit the vehicle.
            /// </summary>
            GetOutOfCar,

            /// <summary>
            /// Player should go into the checkpoint.
            /// </summary>
            GetIntoCheckpoint,

            /// <summary>
            /// Select a reason for the pullover.
            /// </summary>
            SelectReason,

            /// <summary>
            /// Select whether player should ask for license.
            /// </summary>
            SelectAskForLicense,

            /// <summary>
            /// Select whether a license check should be performed.
            /// </summary>
            SelectDoLicenseCheck,

            /// <summary>
            /// Player should check the license in the police computer.
            /// </summary>
            CheckLicenseInComputer, 
            
            /// <summary>
            /// License check in vehicle is done.
            /// </summary>
            CheckLicenseInComputerChecked,

            /// <summary>
            /// Select what do to with the suspect.
            /// </summary>
            SelectAction,

            /// <summary>
            /// Select the fine amount.
            /// </summary>
            SelectFineAmount,

            /// <summary>
            /// In pursuit.
            /// </summary>
            Pursuit,

            /// <summary>
            /// Driver stepped out.
            /// </summary>
            SteppedOut,
        }

        /// <summary>
        /// Gets a value indicating whether the pullover is a pursuit.
        /// </summary>
        public bool IsPursuit
        {
            get
            {
                return this.state == EPulloverState.Pursuit;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the suspect was forced to leave the vehicle.
        /// </summary>
        public bool SuspectLeftVehicle { get; private set; }

        /// <summary>
        /// Gets the vehicle pulled over.
        /// </summary>
        public CVehicle Vehicle
        {
            get
            {
                return this.vehicle;
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.state == EPulloverState.Waiting)
            {
                return;
            }

            // Check if all peds are dead -> end then
            if (CPed.GetNumberOfDeadPeds(this.allPeds, true) == this.allPeds.Length)
            {
                this.End();
                return;
            }

            if (this.pursuit == null && !this.driver.IsInVehicle && this.state != EPulloverState.SteppedOut)
            {
                Log.Debug("Process: Driver left vehicle during pullove - aborting", this);
                this.End();
                return;
            }

            if (this.cop.Exists() && !this.cop.IsAliveAndWell)
            {
                this.End();
                return;
            }

            if (this.actor != null && this.actor.Exists() && !this.actor.IsAliveAndWell)
            {
                this.End();
                return;
            }

            switch (this.state)
            {
                case EPulloverState.Parking:
                case EPulloverState.ParkingFinished:
                case EPulloverState.GetBehindCar:
                    // If not in vehicle, cancel traffic stop
                    if (!this.cop.IsInVehicle || !this.driver.IsInVehicle)
                    {
                        if (this.isPlayerPullover)
                        {
                            TextHelper.PrintText(CultureHelper.GetText("PULLOVER_ABORTED_OUT_OF_CAR"), 5000);
                        }

                        this.End();
                        return;
                    }

                    break;
            }

            switch (this.state)
            {
                case EPulloverState.Parking:
                    if (!this.driver.Intelligence.TaskManager.IsTaskActive(ETaskID.ParkVehicle))
                    {
                        if (this.taskParkVehicle != null)
                        {
                            if (this.taskParkVehicle.HasBeenParkedSuccessfully)
                            {
                                this.driver.BlockPermanentEvents = true;
                                this.driver.Task.ClearAll();

                                this.state = EPulloverState.ParkingFinished;
                            }
                        }
                    }

                    // If player turns on siren, stop immediately
                    if (CPlayer.LocalPlayer.Ped.IsInVehicle && CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenActive && CPlayer.LocalPlayer.IsPressingHorn)
                    {
                        if (this.driver.Intelligence.TaskManager.IsTaskActive(ETaskID.ParkVehicle))
                        {
                            // Not when siren mode is active
                            Lights lights = Main.ScriptManager.GetRunningScriptInstances("Lights")[0] as Lights;
                            if (this.taskParkVehicle != null && !lights.IsUsingLightsMode)
                            {
                                this.taskParkVehicle.MakeAbortable(this.driver);
                                this.driver.BlockPermanentEvents = true;
                                this.driver.Task.ClearAll();
                                this.driver.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 4000);
                                this.state = EPulloverState.ParkingFinished;
                            }
                        }
                    }

                    break;

                case EPulloverState.ParkingFinished:
                    // Turn off engine
                    this.vehicle.EngineRunning = false;
                    if (this.isPlayerPullover)
                    {
                        TextHelper.PrintText(CultureHelper.GetText("PULLOVER_PARK_BEHIND_VEHICLE"), 5000);
                    }

                    this.state = EPulloverState.GetBehindCar;

                    break;

                case EPulloverState.GetBehindCar:
                    // Player should be close behind the suspect's vehicle and should have almost the same heading
                    // Made the distance based on the vehicle's length so player can also easily pullover buses
                    Vector3 position = this.vehicle.GetOffsetPosition(new Vector3(0, -this.vehicle.Model.GetDimensions().Y, 0));
                    if (this.cop.CurrentVehicle.Position.DistanceTo(position) < 3.0f)
                    {
                        if (Common.IsNumberInRange(this.cop.Heading, this.vehicle.Heading, 15, 15, 360))
                        {
                            if (this.isPlayerPullover)
                            {
                                TextHelper.PrintText(CultureHelper.GetText("PULLOVER_EXIT_VEHICLE"), 5000);
                            }
                            else
                            {
                                this.cop.Task.LeaveVehicle();
                            }

                            this.state = EPulloverState.GetOutOfCar;
                        }
                    }

                    break;

                case EPulloverState.GetOutOfCar:
                    if (!this.cop.IsInVehicle)
                    {
                        this.vehicle.RemoveWindow(VehicleWindow.LeftFront);
                        this.vehicle.RemoveWindow(VehicleWindow.RightFront);

                        this.checkpoint = new ArrowCheckpoint(this.driver.GetOffsetPosition(new Vector3(-1, 0, -0.5f)), this.InDriverCheckpoint);
                        this.checkpoint.DistanceToEnter = 0.3f;
                        this.checkpoint.BlipDisplay = BlipDisplay.ArrowOnly;
                        this.checkpoint2 = new ArrowCheckpoint(this.driver.GetOffsetPosition(new Vector3(1.8f, 0, -0.5f)), this.InCheckpoint);
                        this.checkpoint2.DistanceToEnter = 0.3f;
                        this.checkpoint2.BlipDisplay = BlipDisplay.ArrowOnly;

                        // Player has left the vehicle, fire player started pullover event
                        if (this.isPlayerPullover)
                        {
                            new EventPlayerStartedPullover(this.vehicle, this);
                            TextHelper.PrintText(CultureHelper.GetText("PULLOVER_WALK_UP_TO_SUSPECT"), 5000);
                        }
                        else
                        {
                            // Disable checkpoint
                            this.checkpoint.Enabled = false;
                            this.checkpoint2.Enabled = false;

                            // Always go to the pavement so cop doesn't block the traffic
                            Vector3 checkpointPos = this.driver.GetOffsetPosition(new Vector3(-1, 0, -0.5f));
                            if (this.vehicle.GetSideOfStreetVehicleIsAt() == EStreetSide.Right)
                            {
                                checkpointPos = this.driver.GetOffsetPosition(new Vector3(1.8f, 0, -0.5f));
                            }

                            this.cop.Task.GoToCoordAiming(checkpointPos, EPedMoveState.Walk, this.vehicle.Driver.Position);
                        }

                        // Chance suspect will drive away while player has to get into the checkpoint
                        int chance = Common.GetRandomValue(0, 100);
                        if (chance < 10)
                        {
                            DelayedCaller.Call(
                                delegate
                                    {
                                        if (this.state == EPulloverState.GetIntoCheckpoint)
                                        {
                                            this.checkpoint.DistanceToEnter = -1;
                                            this.checkpoint2.DistanceToEnter = -1;

                                            this.driver.SayAmbientSpeech("generic_fuck_off");
                                            this.cop.SayAmbientSpeech("chase_solo");
                                            this.StartPursuit();

                                            DelayedCaller.Call(
                                                delegate
                                                {
                                                    this.checkpoint.Delete();
                                                    this.checkpoint = null;
                                                    this.checkpoint2.Delete();
                                                    this.checkpoint2 = null;
                                                },
                                                this,
                                                1000);
                                        }
                                    },
                                    this,
                                Common.GetRandomValue(1200, 5000));
                        }

                        this.state = EPulloverState.GetIntoCheckpoint;
                    }
                    else
                    {
                        if (!this.isPlayerPullover &&  !this.cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                        {
                            this.cop.Task.LeaveVehicle();
                        }
                    }

                    break;

                case EPulloverState.GetIntoCheckpoint:
                    if (!this.isPlayerPullover)
                    {
                        if (this.checkpoint != null && this.checkpoint.IsPointInCheckpoint(this.cop.Position))
                        {
                            this.InDriverCheckpoint();
                        }

                        if (this.checkpoint2 != null && this.checkpoint2.IsPointInCheckpoint(this.cop.Position))
                        {
                            this.InCheckpoint();
                        }
                    }

                    break;

                case EPulloverState.SelectReason:
                    string phrase = string.Empty;

                    // Check keys
                    if (this.isPlayerPullover)
                    {
                        if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1))
                        {
                            phrase = "PULLED_OVER_DAMAGED";
                        }
                        else if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2))
                        {
                            phrase = "PULLED_OVER_RECKLESS";
                        }
                        else if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog3))
                        {
                            phrase = "PULLED_OVER_SPEEDING";
                        }
                        else if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog4))
                        {
                            phrase = "INTIMIDATE";
                        }
                    }
                    else
                    {
                        phrase = "PULLED_OVER_RECKLESS";
                    }

                    if (phrase != string.Empty)
                    {
                        // Block from being executed again.
                        this.state = EPulloverState.None;

                        TextHelper.ClearHelpbox();
                        this.cop.SayAmbientSpeech(phrase);
                        DelayedCaller.Call(this.ReasonSelectedCallback, this, 2000, null);
                    }

                    break;

                case EPulloverState.SelectAskForLicense:
                    this.ProcessAskForLicenseSelect();

                    break;

                case EPulloverState.SelectDoLicenseCheck:
                    this.ProcessLicenseSelect();

                    break;

                case EPulloverState.CheckLicenseInComputer:
                    this.ProcessLicenseCheck();
                    break;

                case EPulloverState.CheckLicenseInComputerChecked:
                    this.ProcessLicenseCheckDone();
                break;

                case EPulloverState.SelectAction:
                    this.ProcessActionSelect();

                    break;

                case EPulloverState.SelectFineAmount:
                    this.ProcessFineAmountSelect();

                    break;
            }
        }

        /// <summary>
        /// Initializes the pullover.
        /// </summary>
        /// <param name="forcePursuit">
        /// The force Pursuit.
        /// </param>
        /// <param name="noTextIntroduction">
        /// The no Text Introduction.
        /// </param>
        private void Initialize(bool forcePursuit, bool noTextIntroduction)
        {
            if (this.isPlayerPullover)
            {
                this.driver.Intelligence.RequestForAction(EPedActionPriority.RequiredForUserInteraction, this);
            }
            else
            {
                if (!this.driver.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this))
                {
                    Log.Warning("Initialize: Ped not available", this);
                    return;
                }

                this.cop.RequestOwnership(this);
                this.cop.GetPedData<PedDataCop>().RequestPedAction(ECopState.Blocker, this);
            }

            // Print helpbox here already, so user can't tell whether pursuit started or not
            if (!noTextIntroduction)
            {
                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_START"));
            }

            this.allPeds = new CPed[0];
            this.cop.SayAmbientSpeech("PULL_OVER");
            this.isWaitingForAudio = true;
            this.state = EPulloverState.Waiting;

            // Some time after the audio has been played, make ped react
            DelayedCaller.Call(
                delegate
                    {
                        this.isWaitingForAudio = false;

                        // Ensure ped still exists
                        if (!this.driver.Exists() || !this.vehicle.Exists())
                        {
                            Log.Warning("Initialize: Ped or vehicle disposed while speech was still playing", this);
                            this.End();
                            return;
                        }

                        this.driver.RequestOwnership(this);
                        this.vehicle.RequestOwnership(this);

                        // Setup all peds
                        List<CPed> tempAllPeds = new List<CPed>();
                        tempAllPeds.Add(this.driver);

                        // Check for passengers
                        CPed[] peds = this.vehicle.GetAllPedsInVehicle();
                        if (peds.Length > 0)
                        {
                            foreach (CPed passenger in peds)
                            {
                                if (passenger != this.driver)
                                {
                                    tempAllPeds.Add(passenger);
                                }
                            }
                        }

                        // Set all peds and passengers
                        this.allPeds = tempAllPeds.ToArray();
                        tempAllPeds.Remove(this.driver);
                        this.passengers = tempAllPeds.ToArray();

                        // Increase resist chance when driver is drunk
                        int chance = 10;
                        if (this.driver.Intelligence.TaskManager.IsTaskActive(ETaskID.DriveDrunk))
                        {
                            chance = 5;
                        }

                        // There's a chance the suspect will flee
                        if (!this.driver.PedData.AlwaysSurrender && (Common.GetRandomBool(0, chance, 1) || forcePursuit))
                        {
                            this.StartPursuit();
                        }
                        else
                        {
                            // During pullover, player mustn't be able to ask suspect to step out
                            this.driver.PedData.CanBeArrestedByPlayer = false;

                            if (!this.alreadyInPosition)
                            {
                                this.taskParkVehicle = new TaskParkVehicle(this.vehicle, EVehicleParkingStyle.RightSideOfRoad);
                                this.taskParkVehicle.DontClearIndicatorLightsWhenFinished = true;
                                this.taskParkVehicle.AssignTo(this.driver, ETaskPriority.MainTask);
                                this.state = EPulloverState.Parking;
                            }
                            else
                            {
                                this.driver.BlockPermanentEvents = true;
                                this.driver.Task.ClearAll();
                                this.state = EPulloverState.ParkingFinished;
                            }
                        }
                    },
                    this,
                    1500);
        }

        /// <summary>
        /// Called when the player walked into the driver checkpoint.
        /// </summary>
        private void InDriverCheckpoint()
        {
            this.inDriverCheckpoint = true;
            this.InCheckpoint();
        }

        /// <summary>
        /// Called when the player walked into a checkpoint.
        /// </summary>
        private void InCheckpoint()
        {
            // Driver may randomly speed off on, or try to kill the player
            int rndDecision = Common.GetRandomValue(0, 100);

            // If always surrenders, enforce chance
            if (this.driver.PedData.AlwaysSurrender)
            {
                rndDecision = 0;
            }

            // Speed off
            if (rndDecision > 90)
            {
                this.driver.SayAmbientSpeech("generic_fuck_off");
                this.cop.SayAmbientSpeech("chase_solo");
                this.StartPursuit();
            }
            else if (rndDecision > 85)
            {
                this.SetupPassengers();
                this.driver.Intelligence.ChangeActionPriority(EPedActionPriority.RequiredByScript, this);

                bool isTaxi = this.vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsTaxi);

                // Make peds fight player
                foreach (CPed ped in this.allPeds)
                {
                    if (!ped.Exists() || !ped.Wanted.IsIdle)
                    {
                        continue;
                    }

                    // Skip taxi passengers
                    if (!ped.IsDriver && isTaxi)
                    {
                        continue;
                    }

                    ped.PedData.DefaultWeapon = Weapon.Handgun_Glock;
                    if (Common.GetRandomBool(0, 10, 1))
                    {
                        ped.PedData.DefaultWeapon = Weapon.SMG_Uzi;
                    }

                    ped.EnsurePedHasWeapon();
                    ped.Task.DriveBy(this.cop, 0, 0, 0, 0, 250, 8, 1, 250);
                    ped.AttachBlip();

                    if (this.PedResisted != null)
                    {
                        this.PedResisted.Invoke();
                    }

                    if (!this.isPlayerPullover)
                    {
                        this.cop.Task.FightAgainst(ped);
                    }

                    // Timer to kill driveby task again
                    CPed ped1 = ped;
                    DelayedCaller.Call(delegate
                        {
                            if (ped1.Exists())
                            {
                                ped1.Task.FightAgainst(this.cop);
                            }
                        },
                        this,
                        10000);
                }
            }
            else if (rndDecision >= 0)
            {
                this.actor = this.driver;

                // If on the passenger side and no passenger available, change seat
                if (!this.inDriverCheckpoint)
                {
                    if (this.vehicle.IsSeatFree(VehicleSeat.RightFront))
                    {
                        this.driver.Task.ClearAll();
                        this.driver.Task.ShuffleToNextCarSeat(this.driver.CurrentVehicle);

                        // Go ahead in 3 seconds
                        DelayedCaller.Call(
                        delegate
                        {
                            this.InCheckpointNoAction();
                        },
                        this,
                        3000);
                    }
                    else
                    {
                        this.actor = this.passengers[0];
                        this.InCheckpointNoAction();
                    }
                }
                else
                {
                    this.InCheckpointNoAction();
                }
            }

            this.checkpoint.Delete();
            this.checkpoint = null;
            this.checkpoint2.Delete();
            this.checkpoint2 = null;
        }

        /// <summary>
        /// Called when suspect doesn't flee and no special action is taken.
        /// </summary>
        private void InCheckpointNoAction()
        {
            // Say hello
            this.driver.SayAmbientSpeech("GENERIC_HI");

            // Hide player weapon, if any
            if (this.cop.Weapons.Current != Weapon.Unarmed)
            {
                this.cop.SetWeapon(Weapon.Unarmed);
            }

            if (this.vehicle.IsBig || this.vehicle.Model.IsBike || !this.vehicle.HasRoof)
            {
                Log.Debug("InDriverCheckpoint: Vehicle is big or bike", this);

                TaskSequence taskSequence = new TaskSequence();
                taskSequence.AddTask.TurnTo(this.actor);
                if (!this.vehicle.Model.IsBike)
                {
                    taskSequence.AddTask.PlayAnimation(new AnimationSet("cop"), "copm_licenseintro_truck", 7.0f, AnimationFlags.Unknown01 | AnimationFlags.Unknown09);
                }

                taskSequence.AddTask.PlayAnimation(new AnimationSet("cop"), "copm_licenseloop_truck", 7.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown09);
                taskSequence.Perform(this.cop);
                taskSequence.Dispose();

                if (this.vehicle.IsBig)
                {
                    Log.Debug("InDriverCheckpoint: Vehicle is big", this);
                }
            }
            else
            {
                TaskSequence taskSequence = new TaskSequence();
                taskSequence.AddTask.TurnTo(this.actor);
                taskSequence.AddTask.PlayAnimation(new AnimationSet("cop"), "copm_licenseintro_ncar", 7.0f, AnimationFlags.Unknown01 | AnimationFlags.Unknown09);
                taskSequence.AddTask.PlayAnimation(new AnimationSet("cop"), "copm_licenseloop_ncar", 7.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown09);
                taskSequence.Perform(this.cop);
                taskSequence.Dispose();
            }

            // If in driver checkpoint, play "look to the left" animation
            if (this.inDriverCheckpoint)
            {
                this.actor.Task.PlayAnimSecondaryUpperBody("plyr_licenseloop_ncar", "cop", 8.0f, true, 0, 0, 0, -1);
            }

            // Now select reason for pulling over
            if (this.isPlayerPullover)
            {
                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_SELECT_REASON"));
            }

            this.state = EPulloverState.SelectReason;
        }

        /// <summary>
        /// Called when the player walked into the driver checkpoint after the suspect has been looked up.
        /// </summary>
        private void InDriverCheckpointAfterLookUp()
        {
            // Safety check since some people report crashes inside this function
            if (this.actor == null || !this.actor.Exists())
            {
                Log.Warning("InDriverCheckpointAfterLookUp: Actor disposed while still being in use", this);
                this.End();
                return;
            }

            CPlayer.LocalPlayer.Ped.Heading = this.actor.Heading;
            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_SELECT_ACTION"));
            this.state = EPulloverState.SelectAction;

            if (this.inDriverCheckpoint)
            {
                if (this.checkpoint == null)
                {
                    Log.Warning("InDriverCheckpointAfterLookUp: Checkpoint1 is null", this);
                    return;
                }

                this.checkpoint.Delete();
                this.checkpoint = null;
            }
            else
            {
                if (this.checkpoint2 == null)
                {
                    Log.Warning("InDriverCheckpointAfterLookUp: Checkpoint2 is null", this);
                    return;
                }

                this.checkpoint2.Delete();
                this.checkpoint2 = null;
            }
        }

        /// <summary>
        /// Called a moment after a reason has been selected.
        /// </summary>
        /// <param name="parameter">The reason.</param>
        private void ReasonSelectedCallback(object[] parameter)
        {
            if (this.isPlayerPullover)
            {
                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_ASK_FOR_LICENSE"));
            }

            this.state = EPulloverState.SelectAskForLicense;
            this.delayTimer.Reset();
        }

        /// <summary>
        /// Called while checking whether player should ask for license
        /// </summary>
        private void ProcessAskForLicenseSelect()
        {
            if (!this.delayTimer.CanExecute())
            {
                return;
            }

            bool checkLicense = Common.GetRandomBool(0, 2, 1);
            
             // Ask for license license
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.PulloverLicenseYes) || (!this.isPlayerPullover && checkLicense))
            {
                if (this.isPlayerPullover)
                {
                    TextHelper.ClearHelpbox();
                }

                // Block from being executed again
                this.state = EPulloverState.None;

                this.cop.SayAmbientSpeech("ASK_FOR_ID");

                DelayedCaller.Call(
                delegate(object[] parameter)
                {
                    if (!this.isPlayerPullover)
                    {
                        if (!this.cop.Exists() || !this.actor.Exists())
                        {
                            Log.Warning("ProcessAskForLicenseCheck: Peds disposed in AI pullover while still in use", this);
                            this.End();
                            return;
                        }
                    }

                    this.driver.SayAmbientSpeech("MOBILE_UH_HUH");
                    this.cop.Task.ClearAll();

                    // Does ped have license?
                    if (this.isPlayerPullover && this.driver.PedData.Flags.HasFlag(EPedFlags.WontShowLicense))
                    {
                        this.driver.Intelligence.SayText(CultureHelper.GetText("PULLOVER_DRIVER_FORGOT_LICENSE"), 3500);

                        DelayedCaller.Call(
                            delegate
                            {
                                if (this.isPlayerPullover)
                                {
                                    TextHelper.ClearHelpbox();
                                }

                                this.cop.Task.ClearAll();
                                this.cop.Task.TurnTo(this.actor);
                                this.cop.Task.PlayAnimation(new AnimationSet("FACIALS@M_HI"), "PLYR_MOOD_HAPPY", 1, AnimationFlags.Unknown01);
                                this.cop.Task.ClearAll();

                                if (this.isPlayerPullover)
                                {
                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_SELECT_ACTION"));
                                }

                                this.state = EPulloverState.SelectAction;
                                this.delayTimer.Reset();
                            }, 
                            this, 
                            2500);

                        return;
                    }

                    // Hand over license animation
                    if (this.vehicle.IsBig)
                    {
                        this.cop.Task.PlayAnimation(new AnimationSet("amb@atm"), "m_takecash", 7.0f);
                        this.actor.Task.ClearAll();
                        this.actor.Task.PlayAnimSecondaryUpperBody("plyr_licenseoutro_truck", "cop", 8.0f, false, 0, 0, 0, -1);
                    }
                    else if (this.vehicle.Model.IsBike)
                    {
                        this.actor.Task.ClearAll();
                        this.actor.Task.PlayAnimSecondaryUpperBody("toss_money_scooter", "amb@tollbooth", 7.0f, false, 0, 0, 0, -1);
                        this.cop.Task.PlayAnimation(new AnimationSet("amb@atm"), "m_takecash", 7.0f, AnimationFlags.None);
                    }
                    else
                    {
                        this.cop.Task.PlayAnimation(new AnimationSet("cop"), "copm_licenseoutro_ncar", 7.0f, AnimationFlags.Unknown09);
                        this.actor.Task.ClearAll();
                        this.actor.Task.PlayAnimSecondaryUpperBody("plyr_licenseoutro_truck", "cop", 8.0f, false, 0, 0, 0, -1);
                    }

                    DelayedCaller.Call(
                    delegate
                    {
                        if (!this.isPlayerPullover)
                        {
                            if (!this.cop.Exists() || !this.actor.Exists())
                            {
                                Log.Warning("ProcessAskForLicenseCheck: Peds disposed in AI pullover while still in use #2", this);
                                this.End();
                                return;
                            }
                        }

                        // Say thanks. Use driver ped data here since it was his vehicle and not the actor's one
                        this.cop.SayAmbientSpeech("THANKS");
                        string dataString = string.Empty;

                        if (this.passengers.Length > 0)
                        {
                            if (this.passengers.Length > 1)
                            {
                                Persona personaData = this.driver.PedData.Persona;
                                Persona personaDataPassenger = this.passengers[0].PedData.Persona;
                                Persona personaDataPassenger2 = this.passengers[1].PedData.Persona;
                                CPlayer.LocalPlayer.LastPedPulledOver = new CPed[] { this.driver, this.passengers[0], this.passengers[1] };

                                // Prepare ped data
                                dataString = string.Format(CultureHelper.GetText("PULLOVER_CHECK_SUSPECT_DATA_3"), personaData.FullName, personaData.BirthDay.Day, personaData.BirthDay.Month, personaData.BirthDay.Year, personaDataPassenger.FullName, personaDataPassenger.BirthDay.Day, personaDataPassenger.BirthDay.Month, personaDataPassenger.BirthDay.Year, personaDataPassenger2.FullName, personaDataPassenger2.BirthDay.Day, personaDataPassenger2.BirthDay.Month, personaDataPassenger2.BirthDay.Year);
                            }
                            else
                            {
                                Persona personaData = this.driver.PedData.Persona;
                                Persona personaDataPassenger = this.passengers[0].PedData.Persona;
                                CPlayer.LocalPlayer.LastPedPulledOver = new CPed[] { this.driver, this.passengers[0] };

                                // Prepare ped data
                                dataString = string.Format(CultureHelper.GetText("PULLOVER_CHECK_SUSPECT_DATA_2"), personaData.FullName, personaData.BirthDay.Day, personaData.BirthDay.Month, personaData.BirthDay.Year, personaDataPassenger.FullName, personaDataPassenger.BirthDay.Day, personaDataPassenger.BirthDay.Month, personaDataPassenger.BirthDay.Year);
                            }
                        }
                        else
                        {
                            Persona personaData = this.driver.PedData.Persona;

                            // Prepare ped data
                            dataString = string.Format(CultureHelper.GetText("PULLOVER_CHECK_SUSPECT_DATA"), personaData.FullName, personaData.BirthDay.Day, personaData.BirthDay.Month, personaData.BirthDay.Year);
                        }
                        
                        if (this.isPlayerPullover)
                        {
                            TextHelper.PrintFormattedHelpBox(dataString);
                        }


                        this.state = EPulloverState.SelectDoLicenseCheck;
                        this.delayTimer.Reset();
                    },
                    this,
                    3500);
                },
                this,
                2000);
            }

            // No license check
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.PulloverLicenseNo) || (!this.isPlayerPullover && !checkLicense))
            {
                if (this.isPlayerPullover)
                {
                    TextHelper.ClearHelpbox();
                }

                // Block from being executed again
                this.state = EPulloverState.None;

                this.cop.Task.ClearAll();
                this.cop.Task.TurnTo(this.actor);
                this.cop.Task.PlayAnimation(new AnimationSet("FACIALS@M_HI"), "PLYR_MOOD_HAPPY", 1, AnimationFlags.Unknown01);
                this.cop.Task.ClearAll();

                if (this.isPlayerPullover)
                {
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_SELECT_ACTION"));
                }

                this.state = EPulloverState.SelectAction;
                this.delayTimer.Reset();
            }
        }

        /// <summary>
        /// Called while checking whether license should be checked.
        /// </summary>
        private void ProcessLicenseSelect()
        {
            if (!this.delayTimer.CanExecute())
            {
                return;
            }

            int action = Common.GetRandomValue(0, 2);

            // Check license (AI cops will always do)
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.PulloverLicenseYes) || (!this.isPlayerPullover && action == 0))
            {
                if (this.isPlayerPullover)
                {
                    TextHelper.ClearHelpbox();
                }

                Main.PoliceComputer.PedHasBeenLookedUp += new PoliceComputer.PoliceComputer.PedHasBeenLookedUpEventHandler(this.PoliceComputer_PedHasBeenLookedUp);
                this.state = EPulloverState.CheckLicenseInComputer;
                this.delayTimer.Reset();
            }

            // No license check
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.PulloverLicenseNo) || (!this.isPlayerPullover && action == 1))
            {
                if (this.isPlayerPullover)
                {
                    TextHelper.ClearHelpbox();
                }

                this.cop.Task.ClearAll();
                this.cop.Task.TurnTo(this.actor);
                this.cop.Task.PlayAnimation(new AnimationSet("FACIALS@M_HI"), "PLYR_MOOD_HAPPY", 1, AnimationFlags.Unknown01);
                this.cop.Task.ClearAll();

                if (this.isPlayerPullover)
                {
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_SELECT_ACTION"));
                }

                this.state = EPulloverState.SelectAction;
                this.delayTimer.Reset();
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog4))
            {
                this.state = EPulloverState.None;

                TextHelper.ClearHelpbox();
                TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie(string.Empty);
                taskWalkieTalkie.AssignTo(this.cop, ETaskPriority.MainTask);

                if (this.isPlayerPullover)
                {
                    string nameString = this.allPeds.Aggregate(string.Empty, (current, ped) => current + (ped.PedData.Persona.FullName + ", "));
                    nameString = nameString.Remove(nameString.Length - 2);

                    // We need to check all suspects
                    int numOfSuspects = this.allPeds.Length;
                    DelayedCaller.Call(
                        delegate
                        {
                            string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
                            TextHelper.AddTextToTextWall(string.Format(CultureHelper.GetText("PULLOVER_CALL_IN_ID"), numOfSuspects, nameString), reporter);
                        },
                        this,
                        1500);

                    foreach (CPed ped in this.allPeds)
                    {
                        // Build persona string
                        int citations = ped.PedData.Persona.Citations;
                        ELicenseState license = ped.PedData.Persona.LicenseState;
                        int timesStopped = ped.PedData.Persona.TimesStopped;
                        bool wanted = ped.PedData.Persona.Wanted;
                        string wantedString;
                        if (wanted)
                        {
                            wantedString = Common.GetRandomValue(2, 4) + " active warrants";
                        }
                        else
                        {
                            wantedString = "no active warrant(s)";
                        }


                        string s = CultureHelper.GetText("PULLOVER_CALL_ID_DISPLAY");
                        string data = string.Format(s, ped.PedData.Persona.FullName, citations, license, timesStopped, wantedString);
                        DelayedCaller.Call(delegate { TextHelper.AddTextToTextWall(data, CultureHelper.GetText("POLICE_SCANNER_CONTROL")); }, this, Common.GetRandomValue(11500, 15000));
                    }

                    DelayedCaller.Call(delegate { TextHelper.AddTextToTextWall(CultureHelper.GetText("ARREST_FRISK_CALL_IN_ID_ACK"), CultureHelper.GetText("POLICE_SCANNER_CONTROL")); }, this, Common.GetRandomValue(4000, 6500));
                }

                DelayedCaller.Call(delegate { this.state = EPulloverState.SelectAction; }, this, 15000);

                DelayedCaller.Call(
                    delegate(object[] parameter) 
                {                
                    if (this.isPlayerPullover)
                    {
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_SELECT_ACTION"));
                    }

                    this.delayTimer.Reset(); 
                }, 
                this,
                22000);

            }
        }

        /// <summary>
        /// Called while the license is being checked in the computer.
        /// </summary>
        private void ProcessLicenseCheck()
        {
            if (!this.delayTimer.CanExecute())
            {
                return;
            }

            if (!this.isPlayerPullover)
            {
                if (this.cop.IsSittingInVehicle(this.cop.LastVehicle))
                {
                    this.PoliceComputer_PedHasBeenLookedUp(this.driver.PedData.Persona);
                    this.state = EPulloverState.CheckLicenseInComputerChecked;
                    this.delayTimer.Reset();
                }
                else
                {
                    // Get into vehicle
                    if (!this.cop.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                    {
                        TaskGetInVehicle taskGetInVehicle = new TaskGetInVehicle(this.cop.LastVehicle, VehicleSeat.Driver, VehicleSeat.RightFront, true);
                        taskGetInVehicle.AssignTo(this.cop, ETaskPriority.MainTask);
                    }
                }
            }
            else
            {
                // TODO: Provide option to end?
            }
        }

        /// <summary>
        /// Called when the license check in the vehicle is done.
        /// </summary>
        private void ProcessLicenseCheckDone()
        {
            if (!this.delayTimer.CanExecute())
            {
                return;
            }

            if (!this.isPlayerPullover)
            {
                // If still in vehicle, make leave
                if (this.cop.IsInVehicle && !this.cop.IsGettingOutOfAVehicle)
                {
                    // Walk to the position of the second (pavement side) checkpoint
                    if (this.inDriverCheckpoint)
                    {
                        this.cop.Task.GoToCoordAiming(this.driver.GetOffsetPosition(new Vector3(-1, 0, -0.5f)), EPedMoveState.Walk, this.driver.Position);
                    }
                    else
                    {
                        this.cop.Task.GoToCoordAiming(this.driver.GetOffsetPosition(new Vector3(1.8f, 0, -0.5f)), EPedMoveState.Walk, this.driver.Position);
                    }
                }
                else
                {
                    if (this.checkpoint != null && this.checkpoint.IsPointInCheckpoint(this.cop.Position))
                    {
                        this.state = EPulloverState.SelectAction;
                        this.delayTimer.Reset();
                    }

                    if (this.checkpoint2 != null && this.checkpoint2.IsPointInCheckpoint(this.cop.Position))
                    {
                        this.state = EPulloverState.SelectAction;
                        this.delayTimer.Reset();
                    }
                }
            }
        }

        /// <summary>
        /// Called while the action should be selected.
        /// </summary>
        private void ProcessActionSelect()
        {
            if (!this.delayTimer.CanExecute())
            {
                return;
            }

            int action = Common.GetRandomValue(0, 3);

            // However if ped is wanted, make AI choose step out
            if (!this.isPlayerPullover && (this.driver.PedData.Persona.Wanted || this.driver.PedData.Persona.LicenseState == ELicenseState.Expired
                || this.driver.PedData.Persona.LicenseState == ELicenseState.Revoked))
            {
                Log.Debug("ProcessActionSelect: Driver is wanted or has invalid license", this);
                action = 1;
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1) || (!this.isPlayerPullover && action == 0))
            {
                this.clipboard = World.CreateObject("AMB_CLIPBOARD", this.cop.Position);
                if (this.clipboard != null && this.clipboard.Exists())
                {
                    if (this.isPlayerPullover)
                    {
                        TextHelper.ClearHelpbox();
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_TICKET_AMOUNT_OPTIONS"));
                    }

                    // Hide player weapon, if any
                    if (this.cop.Weapons.Current != Weapon.Unarmed)
                    {
                        this.cop.SetWeapon(Weapon.Unarmed);
                    }

                    this.clipboard.AttachToPed(this.cop, Bone.RightHand, new Vector3(0, 0, 0), new Vector3(0, 0, 0));
                    this.driver.PedData.Persona.TimesStopped++;
                    this.cop.Task.PlayAnimation(new AnimationSet("amb@super_create"), "stand_create", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                    this.state = EPulloverState.SelectFineAmount;
                    this.delayTimer.Reset();
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2) || (!this.isPlayerPullover && action == 1))
            {
                if (this.isPlayerPullover)
                {
                    TextHelper.ClearHelpbox();
                    this.state = EPulloverState.SteppedOut;
                }
                else
                {
                    // Block from being executed again
                    this.state = EPulloverState.SteppedOut;
                }

                this.driver.PedData.Persona.TimesStopped++;

                this.cop.SayAmbientSpeech("GET_OUT_OF_CAR");
                this.cop.Task.ClearAll();
                this.driver.Task.LeaveVehicle(this.driver.CurrentVehicle, true);
                this.SuspectLeftVehicle = true;

                if (this.isPlayerPullover)
                {
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PULLOVER_SUSPECT_LEFT_VEHICLE"));
                }
                else
                {
                    // Make cop go a little away from the door
                    Vector3 position = this.driver.GetOffsetPosition(new Vector3(-3, 0, 0));
                    if (this.vehicle.GetSideOfStreetVehicleIsAt() == EStreetSide.Right)
                    {
                        position = this.driver.GetOffsetPosition(new Vector3(3, 0, 0));
                    }

                    this.cop.Task.ClearAllImmediately();
                    this.cop.EnsurePedHasWeapon();
                    this.cop.Task.GoToCoordAiming(position, EPedMoveState.Walk, this.driver.Position);
                }

                // If there's a passenger, enter driver seat and drive off (passengers in taxis, will simply walk off)
                if (this.passengers.Length > 0)
                {
                    bool isTaxi = this.vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsTaxi);

                    foreach (CPed passenger in this.passengers)
                    {
                        if (!passenger.Wanted.IsIdle)
                        {
                            continue;
                        }

                        if (isTaxi)
                        {
                            passenger.Task.LeaveVehicle();
                            CPed passenger1 = passenger;
                            DelayedCaller.Call(
                                delegate
                                    {
                                        TaskArgue taskArgue = new TaskArgue(this.cop, 6000);
                                        taskArgue.AddLine(CultureHelper.GetText("PULLOVER_TAXI_LATE"));
                                        taskArgue.AssignTo(passenger1, ETaskPriority.MainTask);
                                    },
                                    this,
                                1500);
                        }
                        else
                        {
                            if (passenger.IsInVehicle && passenger.IsAliveAndWell)
                            {
                                passenger.Task.CruiseWithVehicle(this.vehicle, 17f, true);
                                break;
                            }
                        }
                    }
                }

                // End after 3 seconds
                DelayedCaller.Call(
                delegate
                {
                    this.End();
                },
                this,
                3000);

                // After script has ended, make driver stop
                DelayedCaller.Call(
                delegate
                {
                    if (this.driver != null && this.driver.Exists())
                    {
                        this.driver.Task.Wait(-1);
                    }
                },
                this,
                4000,
                true);
            }

            // Issue warning
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog3) || (!this.isPlayerPullover && action == 2))
            {
                if (this.isPlayerPullover)
                {
                    TextHelper.ClearHelpbox();
                }

                // Block from being executed again
                this.state = EPulloverState.None;

                this.cop.SayAmbientSpeech("FOUND_NOTHING");
                this.driver.PedData.Persona.TimesStopped++;

                // End after 3 seconds so speech can be played
                DelayedCaller.Call(
                delegate
                {
                    this.End();
                },
                this,
                3000);
            }
        }

        /// <summary>
        /// Called while the fine amount should be selected.
        /// </summary>
        private void ProcessFineAmountSelect()
        {
            if (!this.delayTimer.CanExecute())
            {
                return;
            }

            int action = Common.GetRandomValue(0, 3);

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog1) || (!this.isPlayerPullover && action == 0))
            {
                this.FineSuspect(100);
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog2) || (!this.isPlayerPullover && action == 1))
            {
                this.FineSuspect(60);
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Dialog3) || (!this.isPlayerPullover && action == 2))
            {
                this.FineSuspect(40);
            }
        }

        /// <summary>
        /// Takes <paramref name="amount"/> as fine from the suspect.
        /// </summary>
        /// <param name="amount">The amount of money.</param>
        private void FineSuspect(int amount)
        {
            if (this.isPlayerPullover)
            {
                TextHelper.ClearHelpbox();
                Stats.UpdateStat(Stats.EStatType.Citations, 1, CPlayer.LocalPlayer.Ped.Position);
            }

            // Block from being executed again
            this.state = EPulloverState.None;

            this.cop.Task.ClearAll();
            this.cop.Task.PlayAnimation(new AnimationSet("amb@super_idles_b"), "stand_idle_b", 4f);

            DelayedCaller.Call(
            delegate
            {
                string fineString = string.Format(CultureHelper.GetText("PULLOVER_CITATION_PAID"), amount);
                if (this.isPlayerPullover)
                {
                    TextHelper.PrintFormattedHelpBox(fineString);
                    CPlayer.LocalPlayer.Money += amount;
                }

                if (!this.cop.Exists())
                {
                    Log.Warning("FineSuspect: Cop disposed while still being in use", this);
                    this.End();
                    return;
                }

                this.cop.SayAmbientSpeech("TOLL_PAID_YES");

                if (!this.driver.Exists())
                {
                    Log.Warning("FineSuspect: Driver disposed while still being in use", this);
                    this.End();
                    return;
                }

                // Whatever may happened, we ensure ped is in vehicle before
                if (this.driver.IsInVehicle)
                {
                    this.driver.Task.CruiseWithVehicle(this.driver.CurrentVehicle, 17f, true);
                }

                this.driver.PedData.Persona.Citations++;

                this.End();
            },
            this,
            11000);
        }

        /// <summary>
        /// Called when a ped has been looked up.
        /// </summary>
        /// <param name="persona">The persona data.</param>
        private void PoliceComputer_PedHasBeenLookedUp(Persona persona)
        {
            // Check if looked up right ped
            if (this.driver.PedData.Persona == persona)
            {
                if (this.isPlayerPullover)
                {
                    TextHelper.PrintText(CultureHelper.GetText("GET_BACK_TO_SUSPECT"), 4000);
                }

                if (this.inDriverCheckpoint)
                {
                    this.checkpoint = new ArrowCheckpoint(this.driver.GetOffsetPosition(new Vector3(-1, 0, -0.5f)), this.InDriverCheckpointAfterLookUp);
                    this.checkpoint.DistanceToEnter = 0.5f;
                    this.checkpoint.BlipDisplay = BlipDisplay.ArrowOnly;
                }
                else
                {
                    this.checkpoint2 = new ArrowCheckpoint(this.driver.GetOffsetPosition(new Vector3(1.8f, 0, -0.5f)), this.InDriverCheckpointAfterLookUp);
                    this.checkpoint2.DistanceToEnter = 0.5f;
                    this.checkpoint2.BlipDisplay = BlipDisplay.ArrowOnly;
                }

                if (!this.isPlayerPullover)
                {
                    if (this.checkpoint != null)
                    {
                        this.checkpoint.Enabled = false;
                    }

                    if (this.checkpoint2 != null)
                    {
                        this.checkpoint2.Enabled = false;
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
            base.End();

            // If driver is not driver, so seats have been changed, shuffle back
            if (!this.inDriverCheckpoint && this.actor == this.driver)
            {
                if (this.driver.Exists() && !this.driver.IsDriver && this.driver.IsInVehicle)
                {
                    if (!this.driver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexShuffleBetweenSeats))
                    {
                        this.driver.Task.ClearAll();
                        this.driver.Task.ShuffleToNextCarSeat(this.driver.CurrentVehicle);
                    }

                    // Call End again in 3000 seconds
                    DelayedCaller.Call(
                        delegate
                            {
                                this.End();
                            },
                            this,
                        3000,
                        true);
                    this.inDriverCheckpoint = true;
                    return;
                }
            }

            if (this.pursuit != null && this.pursuit.IsRunning)
            {
                this.pursuit.EndChase();
            }

            if (this.taskParkVehicle != null)
            {
                if (this.taskParkVehicle.Active)
                {
                    this.taskParkVehicle.DontClearIndicatorLightsWhenFinished = false;
                    this.taskParkVehicle.MakeAbortable(this.driver);
                }
            }

            if (this.vehicle.Exists())
            {
                this.vehicle.AVehicle.IndicatorLightsOn = false;
                this.vehicle.AVehicle.IndicatorLightsMode = VehicleIndicatorLightsMode.Off;
                this.vehicle.ReleaseOwnership(this);
            }

            if (this.checkpoint != null)
            {
                this.checkpoint.Delete();
            }

            if (this.checkpoint2 != null)
            {
                this.checkpoint2.Delete();
            }


            foreach (CPed ped in this.allPeds)
            {
                if (ped.Exists())
                {
                    if (!ped.Wanted.IsBeingArrestedByPlayer)
                    {
                        ped.PedData.CanBeArrestedByPlayer = true;
                        ped.ReleaseOwnership(this);
                        ped.Intelligence.ResetAction(this);
                    }
                }
            }

            if (this.clipboard != null && this.clipboard.Exists())
            {
                this.clipboard.Detach();
                this.clipboard.Delete();
            }

            if (!this.isPlayerPullover)
            {
                this.cop.ReleaseOwnership(this);
                this.cop.GetPedData<PedDataCop>().ResetPedAction(this);
                if (this.cop.Exists())
                {
                    this.cop.Task.ClearAll();
                }
            }

            // If cop is playing animation, cancel
            if (this.cop.Exists() && this.cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleRunNamedAnim))
            {
                this.cop.Task.ClearAllImmediately();
            }

            // Delete event
            EventPlayerStartedArrest.EventRaised -= new EventPlayerStartedArrest.EventRaisedEventHandler(this.EventPlayerStartedArrest_EventRaised);

            if (this.state == EPulloverState.CheckLicenseInComputer)
            {
                Main.PoliceComputer.PedHasBeenLookedUp -= new PoliceComputer.PoliceComputer.PedHasBeenLookedUpEventHandler(this.PoliceComputer_PedHasBeenLookedUp);
            }

            // If suspect left vehicle, we want to keep the helpbox saying how player can deal with him
            if (!this.SuspectLeftVehicle)
            {
                TextHelper.ClearHelpbox();
            }

            Log.Debug("End: Pullover Ended", this);
        }

        /// <summary>
        /// Setups all passengers as mission peds.
        /// </summary>
        private void SetupPassengers()
        {
            foreach (CPed ped in this.passengers)
            {
                if (ped.Exists() && ped.IsSittingInVehicle(this.vehicle) && ped.Wanted.IsIdle)
                {
                    ped.RequestOwnership(this);
                    if (!ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this))
                    {
                        Log.Warning("SetupPassengers: Failed to own passenger, freeing", this);
                        ped.ReleaseOwnership(this);
                    }
                }
            }
        }

        /// <summary>
        /// Starts a new pursuit.
        /// </summary>
        private void StartPursuit()
        {
            // End possible running tasks
            if (this.taskParkVehicle != null)
            {
                if (this.taskParkVehicle.Active)
                {
                    this.taskParkVehicle.DontClearIndicatorLightsWhenFinished = false;
                    this.taskParkVehicle.MakeAbortable(this.driver);
                }
            }

            if (this.vehicle.Exists())
            {
                this.vehicle.AVehicle.IndicatorLightsOn = false;
                this.vehicle.AVehicle.IndicatorLightsMode = VehicleIndicatorLightsMode.Off;
                this.vehicle.ReleaseOwnership(this);
            }

            // Start pursuit, but don't allow cop units (so cops won't start chasing immediately after the car drove away)
            this.SetupPassengers();
            this.driver.Intelligence.ChangeActionPriority(EPedActionPriority.RequiredByScript, this);
            this.pursuit = new Pursuit();
            this.pursuit.CanCopsJoin = false;
            this.pursuit.ChaseEnded += new Chase.ChaseEndedEventHandler(this.pursuit_ChaseEnded);
            this.state = EPulloverState.Pursuit;

            bool isTaxi = this.vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsTaxi);
            foreach (CPed ped in this.allPeds)
            {
                if (!ped.Wanted.IsIdle)
                {
                    continue;
                }

                ped.Intelligence.TaskManager.ClearTasks();

                // Don't add passengers in taxis
                if (isTaxi && !ped.IsDriver)
                {
                    continue;
                }

                this.pursuit.AddTarget(ped);
            }

            if (this.PedResisted != null)
            {
                this.PedResisted.Invoke();
            }

            // Listen to event so we can free the peds from pullover when they are being arrested
            EventPlayerStartedArrest.EventRaised += new EventPlayerStartedArrest.EventRaisedEventHandler(this.EventPlayerStartedArrest_EventRaised);

            // Changed by Sam - using "Press key to call in pursuit" now. We enable the chase after 2.5 for player, but block cops
            if (this.isPlayerPullover)
            {
                DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALL_IN_PURSUIT")); }, this, 5000);
                this.pursuit.MakeActiveChase(2500, -1);
            }
            else
            {
                // Allow player to join the chase
                this.pursuit.MakeActiveChase(-1, 2500);
                this.pursuit.CanPlayerJoin = true;

                // Free cop again so he can join the pursuit
                this.cop.ReleaseOwnership(this);
                this.cop.GetPedData<PedDataCop>().ResetPedAction(this);
            }

            // Chance ped will leave vehicle soon
            int leaveChance = Common.GetRandomValue(0, 100);
            if (leaveChance < 10)
            {
                DelayedCaller.Call(
                    delegate
                    {
                        if (this.pursuit.IsRunning)
                        {
                            if (this.driver.Exists())
                            {
                                this.driver.Intelligence.AddVehicleToBlacklist(this.vehicle, 15000);
                                this.driver.LeaveVehicle();
                            }
                        }
                    },
                    this,
                    Common.GetRandomValue(3000, 8000));
            }
        }

        /// <summary>
        /// Called when player starts to arrest.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventPlayerStartedArrest_EventRaised(EventPlayerStartedArrest @event)
        {
            // Free our peds
            foreach (CPed ped in this.allPeds)
            {
                if (ped == @event.PedBeingArrested)
                {
                    ped.ReleaseOwnership(this, false);
                }
            }
        }

        /// <summary>
        /// Called when the chase has ended.
        /// </summary>
        private void pursuit_ChaseEnded()
        {
            this.End();
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
            ped.ReleaseOwnership(this);
            ped.Intelligence.ResetAction(this);
        }
    }
}