namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// Represents a police department.
    /// </summary>
    [ScriptInfo("PoliceDepartment", true)]
    internal class PoliceDepartment : GameScript
    {
        /// <summary>
        /// The name of the lobby and office rooms.
        /// </summary>
        private const string LobbyRoomName = "R_F6227E42_0000FA09";

        /// <summary>
        /// The name of the chief's room.
        /// </summary>
        private const string ChiefRoomName = "R_8045126D_0000270A";

        /// <summary>
        /// The name of the duty/locker room.
        /// </summary>
        private const string LockerRoomName = "R_8F0EB000_0000F504";

        /// <summary>
        /// The name of the office room.
        /// </summary>
        private const string OfficeRoomName = "R_B99E853B_00002A0D";

        /// <summary>
        /// The position of the payphone.
        /// </summary>
        private static readonly Vector3 PayphonePosition = new Vector3(103.29f, -686.18f, 14.77f);

        /// <summary>
        /// The position of the locker cop's window he smokes at.
        /// </summary>
        private static readonly SpawnPoint LockerCopWindowPosition = new SpawnPoint(242, new Vector3(126.60f, -686.70f, 14.77f));

        /// <summary>
        /// The position of the soda/vending machines.
        /// </summary>
        private static readonly Vector3 SodaPosition = new Vector3(115.32f, -676.63f, 14.77f);

        /// <summary>
        /// The position of the bulletproof vest.
        /// </summary>
        private static readonly Vector3 BulletProofVestPosition = new Vector3(111.75f, -687.68f, 14.74f);

        /// <summary>
        /// The position of the first aid box.
        /// </summary>
        private static readonly Vector3 FirstAidBoxPosition = new Vector3(109.73f, -687.34f, 14.77f);

        /// <summary>
        /// The position where to go on duty.
        /// </summary>
        private static readonly Vector3 GoOnDutyPosition = new Vector3(121.75f, -688.09f, 13.47f);

        /// <summary>
        /// The position where the pd can be left.
        /// </summary>
        private static readonly Vector3 LeavePosition = new Vector3(96.25f, -683.35f, 13.47f);

        /// <summary>
        /// The position where the partner is spawned.
        /// </summary>
        private static readonly Vector3 PartnerPosition = new Vector3(114.09f, -687.75f, 14.77f);

        /// <summary>
        /// The position where the player starts in the pd.
        /// </summary>
        private static readonly Vector3 SpawnPosition = new Vector3(98.44f, -683.27f, 14.77f);

        /// <summary>
        /// The position where the player can start the tutorial.
        /// </summary>
        private static readonly Vector3 TutorialPosition = new Vector3(106.40f, -695.29f, 13.47f);

        // Introduction

        /// <summary>
        /// Position of the instruction cam in position 1.
        /// </summary>
        private static readonly Vector3 InstructionCam1Position = new Vector3(117.85f, -682.02f, 16.37f);

        /// <summary>
        /// Position of the instruction cam in position 2.
        /// </summary>
        private static readonly Vector3 InstructionCam2Position = new Vector3(108.32f, -681.72f, 16.37f);

        /// <summary>
        /// Position of the instruction cam in position 3.
        /// </summary>
        private static readonly Vector3 InstructionCam3Position = new Vector3(103.81f, -683.19f, 16.37f);

        /// <summary>
        /// Position of the instruction cam in position 4.
        /// </summary>
        private static readonly Vector3 InstructionCam4Position = new Vector3(115.27f, -681.75f, 16.37f);

        /// <summary>
        /// Position of the chief in the bribery scenario in his office
        /// </summary>
        private static readonly Vector3 ChiefCopBriberyPosition = new Vector3(124.37f, -671.47f, 14.81f);

        /// <summary>
        /// Position of the prostitute in the bribery scenario in the chief's office
        /// </summary>
        private static readonly Vector3 CriminalPedBriberyPosition = new Vector3(123.30f, -672.39f, 14.81f);

        /// <summary>
        /// Position of the prostitute in the bribery scenario in the chief's office
        /// </summary>
        private static readonly Vector3 FriskPosition = new Vector3(103.97f, -680.50f, 14.77f);

        /// <summary>
        /// Position of the prostitute in the bribery scenario in the chief's office
        /// </summary>
        private static readonly Vector3 CopPosition = new Vector3(103.97f, -681.25f, 14.77f);

        /// <summary>
        /// Positions of the cops which get inspected in the inspection scenario.
        /// </summary>
        private static readonly Vector3[] InspectedCopPositions = new Vector3[2] { new Vector3(122.0f, -674.9f, 14.90f), new Vector3(123.50f, -674.10f, 14.90f) };

        /// <summary>
        /// Positions of the locker objects
        /// </summary>
        private static readonly Vector3[] LockerPositions = new Vector3[6] 
        { 
            new Vector3(121.42f, -689.951f, 13.7727f), 
            new Vector3(121.223f, -689.501f, 13.7727f), 
            new Vector3(121.025f, -689.051f, 13.7727f), 
            new Vector3(120.828f, -688.601f, 13.7727f),
            new Vector3(120.6f, -688.146f, 13.7727f),
            new Vector3(120.136f, -687.391f, 14.4609f), 
        };

        /// <summary>
        /// Models of the locker objects
        /// </summary>
        private static readonly Model[] LockerModels = new Model[6] 
        { 
            -1278808911,
            -1278808911,
            -1278808911,
            -1278808911,
            -1278808911,
            1887654458
        };

        /// <summary>
        /// Quaternion of the locker objects
        /// </summary>
        private static readonly Quaternion LockerQuaternion = new Quaternion(0f, 0f, 0.85f, 0.53f);

        /// <summary>
        /// The instruction cam.
        /// </summary>
        private static Camera introductionCam;

        /// <summary>
        /// Whether introduction is running.
        /// </summary>
        private static bool introductionRunning;

        /// <summary>
        /// Whether the chief is spawned.
        /// </summary>
        private static bool isChiefSpawned;

        /// <summary>
        /// Whether the inspection scenario is to happen.
        /// </summary>
        private static bool isInspectionReady;

        /// <summary>
        /// Whether the inspection scenario is happening.
        /// </summary>
        private static bool isInspectionPlaying;

        /// <summary>
        /// Whether the bribery scenario is happening.
        /// </summary>
        private static bool isBriberyPlaying;

        /// <summary>
        /// Whether the bribery scenario is to happen.
        /// </summary>
        private static bool isBriberyReady;

        /// <summary>
        /// Whether the arrest scenario is happening.
        /// </summary>
        private static bool isArrestPlaying;

        /// <summary>
        /// Whether the cop at the locker has done his animation.
        /// </summary>
        private static bool isLockerCopActive;

        /// <summary>
        /// Whether the PD has been populated
        /// </summary>
        private static bool hasBeenPopulated;

        /// <summary>
        /// The state of the introduction.
        /// </summary>
        private static EPoliceDepartmentIntroductionState introductionState;

        /// <summary>
        /// The state of the bribery scenario.
        /// </summary>
        private static EBriberyScenarioState briberyState;

        /// <summary>
        /// The state of the bribery scenario.
        /// </summary>
        private static EArrestScenarioState arrestState;

        /// <summary>
        /// The checkpoint to leave the pd.
        /// </summary>
        private static ArrowCheckpoint leavePDCheckpoint;

        /// <summary>
        /// The task for the criminal ped handcuffs
        /// </summary>
        private static TaskPlaySecondaryUpperAnimationAndRepeat taskPlaySecondaryUpperAnimationAndRepeat;

        // Ambient peds

        /// <summary>
        /// Whether peds have been spawned already, e.g. by other multiplayer players.
        /// </summary>
        private static bool havePedsBeenSpawned;

        /// <summary>
        /// The cop leaning against the wall.
        /// </summary>
        private static CPed leanCop;

        /// <summary>
        /// The cop arresting the criminalPed.
        /// </summary>
        private static CPed arrestingCop;

        /// <summary>
        /// The criminal being arrested by the arrestingCop.
        /// </summary>
        private static CPed criminalPed;

        /// <summary>
        /// The cop walking.
        /// </summary>
        private static CPed walkingCop;

        /// <summary>
        /// The working cop 1
        /// </summary>
        private static CPed workingCop;

        /// <summary>
        /// The working cop 2
        /// </summary>
        private static CPed workingCop2;

        /// <summary>
        /// The cop talking.
        /// </summary>
        private static CPed talkCop1;

        /// <summary>
        /// The cop talking 2.
        /// </summary>
        private static CPed talkCop2;

        /// <summary>
        /// The cop guarding.
        /// </summary>
        private static CPed guardCop;

        /// <summary>
        /// The cop in the locker room.
        /// </summary>
        private static CPed lockerCop;

        /// <summary>
        /// The chief.
        /// </summary>
        private static CPed chiefCop;

        /// <summary>
        /// The cops being inspected in the chief's office
        /// </summary>
        private static List<CPed> inspectedCops = new List<CPed>();

        /// <summary>
        /// The locker objects
        /// </summary>
        private static List<GTA.Object> lockerObjects = new List<GTA.Object>();

        /// <summary>
        /// The radio object where sounds come from
        /// </summary>
        private static GTA.Object radioObject;
        private static GTA.Object chiefRadioObject;

        /// <summary>
        /// The position of the radio object
        /// </summary>
        private static readonly Vector3 radioObjectPosition = new Vector3(115.709f, -687.453f, 14.65f);
        private static readonly Vector3 chiefRadioObjectPosition = new Vector3(122.114f, -674.897f, 14.7471f);

        /// <summary>
        /// The position of the payphone.
        /// </summary>
        private static readonly String[] CriminalModels = new String[10] { "f_y_street_05", "f_y_street_02", "f_y_hooker_03", "f_y_hooker_03", "f_o_peasteuro_02", "m_y_dealer", "f_y_hooker_03", "m_y_gmaf_lo_01", "m_y_gbik_lo_02", "m_y_genstreet_16" };

        /// <summary>
        /// The checkpoint to go on duty.
        /// </summary>
        private static ArrowCheckpoint goOnDutyCheckpoint;

        /// <summary>
        /// The checkpoint to get a partner.
        /// </summary>
        private static ArrowCheckpoint partnerCheckpoint;

        /// <summary>
        /// The checkpoint to start the tutorial.
        /// </summary>
        private static ArrowCheckpoint tutorialCheckpoint;

        /// <summary>
        /// The partner ped.
        /// </summary>
        private static CPed partnerPed;

        /// <summary>
        /// The component selector for the partner.
        /// </summary>
        private static PedComponentSelector partnerComponentSelector;

        /// <summary>
        /// The first aid pickup.
        /// </summary>
        private static CPickup pickupFirstAid;

        /// <summary>
        /// The bulletproof vest pickup.
        /// </summary>
        private static CPickup pickupVest;

        /// <summary>
        /// The checkpoint to enter the pd.
        /// </summary>
        private ArrowCheckpoint arrowCheckpoint;

        /// <summary>
        /// The blip of the police department.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The position of the blip.
        /// </summary>
        private Vector3 blipPosition;

        /// <summary>
        /// The spawnpoint of the vehicle.
        /// </summary>
        private SpawnPoint vehiclePosition;

        /// <summary>
        /// Whether the police department blip and checkpoint are visible.
        /// </summary>
        private bool visible;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoliceDepartment"/> class.
        /// </summary>
        /// <param name="blipPosition">
        /// The blip position.
        /// </param>
        /// <param name="vehiclePosition">
        /// The vehicle position.
        /// </param>
        /// <param name="vehicleHeading">
        /// The vehicle heading.
        /// </param>
        public PoliceDepartment(Vector3 blipPosition, Vector3 vehiclePosition, float vehicleHeading)
        {
            this.blipPosition = blipPosition;
            this.vehiclePosition = new SpawnPoint(vehicleHeading, vehiclePosition);
        }

        /// <summary>
        /// Event when player has entered/left a pd.
        /// </summary>
        /// <param name="policeDepartment">The pd.</param>
        /// <param name="entered">True if entered, false if left.</param>
        public delegate void PlayerEnteredLeftPDEventHandler(PoliceDepartment policeDepartment, bool entered);

        /// <summary>
        /// Delegate used when player is close to a pd.
        /// </summary>
        /// <param name="policeDepartment">The police department.</param>
        /// <returns>True if player can enter the pd, false if not.</returns>
        public delegate bool PlayerCloseToPDEventHandler(PoliceDepartment policeDepartment);

        /// <summary>
        /// Fired when the player has entered or left the police department.
        /// </summary>
        public event PlayerEnteredLeftPDEventHandler PlayerEnteredLeft;

        /// <summary>
        /// Fired when the player is close to a pd.
        /// </summary>
        public event PlayerCloseToPDEventHandler PlayerCloseToPD;

        /// <summary>
        /// The state of the introduction in the police department.
        /// </summary>
        private enum EPoliceDepartmentIntroductionState
        {
            /// <summary>
            ///  Not running.
            /// </summary>
            Off,

            /// <summary>
            /// Introduction has started.
            /// </summary>
            Start,

            /// <summary>
            /// Introduction shows the partner.
            /// </summary>
            Partner,

            /// <summary>
            /// Introduction shows where to go on duty.
            /// </summary>
            Duty,

            /// <summary>
            /// Introduction shows where to leave.
            /// </summary>
            Leave,

            /// <summary>
            /// Introduction has finished.
            /// </summary>
            End,
        }

        /// <summary>
        /// The state of the bribery scenario in the police department.
        /// </summary>
        private enum EBriberyScenarioState
        {
            /// <summary>
            ///  Not running.
            /// </summary>
            Off,

            /// <summary>
            /// The chief goes to meet the prostitute.
            /// </summary>
            Start,

            /// <summary>
            /// The chief has met the prostitute
            /// </summary>
            Meet,

            /// <summary>
            /// The chief and the prostitute walk to the office
            /// </summary>
            Office,

            /// <summary>
            /// The prostitute starts dancing for the chief
            /// </summary>
            Dance,

            /// <summary>
            /// The prostitute leaves the chief's office
            /// </summary>
            End,
        }

        /// <summary>
        /// The state of the arrest scenario in the police department.
        /// </summary>
        private enum EArrestScenarioState
        {
            /// <summary>
            ///  Not running.
            /// </summary>
            Off,

            /// <summary>
            /// The cop and criminal walk over to the wall
            /// </summary>
            Start,

            /// <summary>
            /// The cop frisks the criminal
            /// </summary>
            Frisk,

            /// <summary>
            /// The cop and criminal sit down
            /// </summary>
            End,
        }

        /// <summary>
        /// Gets the position.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return this.blipPosition;
            }
        }

        /// <summary>
        /// Gets the vehicle position.
        /// </summary>
        public Vector3 VehiclePosition
        {
            get
            {
                return this.vehiclePosition.Position;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the police department blip and checkpoint are visible.
        /// </summary>
        public bool Visible
        {
            get
            {
                return this.visible;
            }

            set
            {
                this.visible = value;

                if (this.visible)
                {
                    // If blip doesn't exist, create
                    if (this.blip == null)
                    {
                        this.blip = Blip.AddBlip(this.blipPosition);
                        this.blip.Icon = BlipIcon.Building_PoliceStation;
                    }

                    // If checkpoint doesn't exist, create
                    if (this.arrowCheckpoint == null)
                    {
                        this.arrowCheckpoint = new ArrowCheckpoint(this.blipPosition, this.PlayerIsCloseToPDCallback);
                        if (!this.arrowCheckpoint.HasInitializedProperly)
                        {
                            this.arrowCheckpoint = null;
                        }
                        else
                        {
                            this.arrowCheckpoint.BlipDisplay = BlipDisplay.ArrowOnly;
                            this.arrowCheckpoint.BlipColor = BlipColor.Cyan;
                            this.arrowCheckpoint.DistanceToEnter = 1.0f;
                        }
                    }
                }
                else
                {
                    if (this.blip != null && this.blip.Exists())
                    {
                        this.blip.Delete();
                        this.blip = null;
                    }

                    if (this.arrowCheckpoint != null)
                    {
                        this.arrowCheckpoint.Delete();
                        this.arrowCheckpoint = null;
                    }
                }
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!hasBeenPopulated)
            {
                return;
            }

            this.ProcessIntroduction();

            if (chiefCop != null && chiefCop.Exists())
            {
                this.ProcessInspection();
            }

            this.ProcessLockerCop();

            if (criminalPed != null && arrestingCop != null)
            {
                this.ProccessArrest();
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            // Delete blip
            this.Visible = false;
        }

        /// <summary>
        /// Basic stuff for the cop at the locker
        /// </summary>
        private void ProcessLockerCop()
        {
            if (!isLockerCopActive)
            {
                if (lockerCop != null)
                {
                    if (lockerCop.Exists())
                    {
                        if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(lockerCop.Position) < 5.0f)
                        {
                            GTA.TaskSequence lockerTask = new GTA.TaskSequence();
                            lockerTask.AddTask.PlayAnimation(new AnimationSet("clothing"), "brushoff_suit_stand", 4.0f);
                            lockerTask.AddTask.GoTo(LockerCopWindowPosition.Position);
                            lockerTask.AddTask.ped.Task.AchieveHeading(LockerCopWindowPosition.Heading);
                            lockerTask.AddTask.PlayAnimation(new AnimationSet("amb@bouncer_idles_b"), "lookaround_a", 4.0f, AnimationFlags.Unknown05);
                            lockerTask.Perform(lockerCop);
                            isLockerCopActive = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Starts off the Inspection Scenario where the cops are lined up in the chief's office and fired
        /// </summary>
        private void StartInspectionScenario()
        {
            GTA.TaskSequence chiefSequence = new GTA.TaskSequence();

            foreach (CPed inspectedPed in inspectedCops)
            {
                if (inspectedPed.Exists())
                {
                    chiefSequence.AddTask.GoTo(inspectedPed.GetOffsetPosition(new Vector3(0, 2, 0)));
                    chiefSequence.AddTask.TurnTo(inspectedPed);
                    chiefSequence.AddTask.Wait(1500);
                }
            }

            chiefSequence.AddTask.Wait(4000);
            chiefSequence.AddTask.GoTo(PayphonePosition);
            chiefSequence.AddTask.ped.Task.AchieveHeading(180f);
            chiefSequence.AddTask.StartScenario("Scenario_PayPhone", PayphonePosition);

            chiefCop.Task.ClearAll();
            chiefCop.Task.PerformSequence(chiefSequence);

            isInspectionPlaying = true;
        }

        /// <summary>
        /// Processes all inspection logic.
        /// </summary>
        private void ProcessInspection()
        {
            if (isInspectionReady && !isInspectionPlaying)
            {
                if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(chiefCop.Position) < 8.0f)
                {
                    if (chiefCop.Exists())
                    {
                        this.StartInspectionScenario();
                        return;
                    }
                }
            }

            if (isInspectionPlaying)
            {
                foreach (CPed inspectedPed in inspectedCops)
                {
                    if (inspectedPed.Exists())
                    {
                        if (inspectedPed.Money != 1)
                        {
                            if (chiefCop.Position.DistanceTo(inspectedPed.GetOffsetPosition(new Vector3(0, 2, 0))) < 0.7f)
                            {
                                if (!chiefCop.IsAmbientSpeechPlaying)
                                {
                                    chiefCop.SayAmbientSpeech("JACKING_GENERIC_BACK");
                                }

                                CPed ped = inspectedPed;
                                DelayedCaller.Call(delegate { ped.SayAmbientSpeech("INTIMIDATE"); }, this, 1500, null);
                                DelayedCaller.Call(delegate { ped.Task.GoTo(LeavePosition); }, this, Common.GetRandomValue(2000, 4000), null);
                                inspectedPed.Money = 1;
                            }
                            else if (chiefCop.Position.DistanceTo(inspectedPed.Position) > 10.0f && inspectedPed.Money != 1)
                            {
                                inspectedPed.Task.GoTo(LeavePosition);
                                inspectedPed.Money = 1;
                            }
                        }
                        else if (inspectedPed.Money == 1)
                        {
                            if (inspectedPed.Position.DistanceTo(LeavePosition) < 1.75f)
                            {
                                inspectedPed.Delete();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes all arrest logic.
        /// </summary>
        private void ProccessArrest()
        {
            if (isArrestPlaying)
            {
                if (arrestState == EArrestScenarioState.Frisk)
                {
                    if (!arrestingCop.Animation.isPlaying(new AnimationSet("cop"), "cop_search"))
                    {
                        GTA.TaskSequence criminalTask = new GTA.TaskSequence();
                        criminalTask.AddTask.GoTo(new Vector3(118.6f, -685.50f, 14.77f));
                        criminalTask.AddTask.ped.Task.AchieveHeading(0f);
                        if (Settings.UsingPoliceStationMod)
                        {
                            criminalTask.AddTask.PlayAnimation(new AnimationSet("sit"), "sit_down", 4.0f, AnimationFlags.None);
                            GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 117.81f, -685.93f, 14.77f, -814569204, 0.00f, -2, 1);
                        }
                        criminalPed.Task.ClearAll();
                        criminalPed.Task.PerformSequence(criminalTask);

                        GTA.TaskSequence copTask = new GTA.TaskSequence();
                        copTask.AddTask.GoTo(new Vector3(117.81f, -685.93f, 14.77f));
                        copTask.AddTask.ped.Task.AchieveHeading(0f);
                        if (Settings.UsingPoliceStationMod)
                        {
                            copTask.AddTask.PlayAnimation(new AnimationSet("sit"), "sit_down", 4.0f, AnimationFlags.None);
                            GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 116.81f, -685.93f, 14.77f, -814569204, 0.00, -2, 1);
                        }
                        arrestingCop.Task.ClearAll();
                        arrestingCop.Task.PerformSequence(copTask);

                        arrestState = EArrestScenarioState.End;
                    }
                }

                if (arrestState == EArrestScenarioState.End)
                {
                    if (isBriberyReady && chiefCop != null && chiefCop.Exists())
                    {
                        chiefCop.Task.ClearAll();
                        chiefCop.Task.GoTo(criminalPed,EPedMoveState.Walk);
                        chiefCop.Task.AlwaysKeepTask = true;
                        isBriberyPlaying = true;
                        briberyState = EBriberyScenarioState.Start;
                    }

                    arrestState = EArrestScenarioState.Off;
                    isArrestPlaying = false;
                }
            }

            if (isBriberyPlaying)
            {
                if (briberyState == EBriberyScenarioState.Start)
                {
                    if (chiefCop.Position.DistanceTo(criminalPed.Position) < 3.0f)
                    {
                        chiefCop.Task.ClearAll();
                        GTA.TaskSequence chiefTask = new GTA.TaskSequence();
                        chiefTask.AddTask.TurnTo(criminalPed);
                        chiefTask.AddTask.Wait(2000);
                        chiefTask.Perform(chiefCop);
                        criminalPed.SayAmbientSpeech("SOLICIT");
                        briberyState = EBriberyScenarioState.Meet;
                    }
                }
                else if (briberyState == EBriberyScenarioState.Meet)
                {
                    if (!criminalPed.IsAmbientSpeechPlaying)
                    {
                        chiefCop.SayAmbientSpeech("GET_IN_CAR");

                        GTA.TaskSequence chiefTask = new GTA.TaskSequence();
                        chiefTask.AddTask.GoTo(ChiefCopBriberyPosition);
                        chiefTask.AddTask.Wait(1000);
                        chiefTask.AddTask.ped.Task.AchieveHeading(135f);
                        chiefTask.Perform(chiefCop);

                        GTA.TaskSequence criminalTask = new GTA.TaskSequence();
                        criminalTask.AddTask.Wait(1500);
                        criminalTask.AddTask.GoTo(CriminalPedBriberyPosition);
                        criminalTask.Perform(criminalPed);

                        briberyState = EBriberyScenarioState.Office;
                    }  
                }
                else if (briberyState == EBriberyScenarioState.Office)
                {
                    if (chiefCop.Position.DistanceTo(ChiefCopBriberyPosition) < 1.0f && criminalPed.Position.DistanceTo(CriminalPedBriberyPosition) < 1.0f)
                    {
                        criminalPed.Task.PlayAnimation(new AnimationSet("amb@pimps_pros"), "car_proposition", 0.5f); 
                        
                        chiefCop.Task.LookAtChar(criminalPed, -1, EPedLookType.MoveHeadAsMuchAsPossible);

                        briberyState = EBriberyScenarioState.Dance;
                    }
                }
                else if (briberyState == EBriberyScenarioState.Dance)
                {
                    if (!criminalPed.Animation.isPlaying(new AnimationSet("amb@pimps_pros"), "car_proposition"))
                    {
                        chiefCop.SayAmbientSpeech("GENERIC_YES_PLEASE");
                        GTA.TaskSequence criminalTask = new GTA.TaskSequence();
                        criminalTask.AddTask.GoTo(new Vector3(117.6f, -685.50f, 14.77f));
                        if (Settings.UsingPoliceStationMod)
                        {
                            criminalTask.AddTask.ped.Task.AchieveHeading(0f);
                            GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 117.81f, -685.93f, 14.77f, -814569204, 0.00f, -2, 1);
                        }
                        criminalTask.Perform(criminalPed);

                        GTA.TaskSequence chiefTask = new GTA.TaskSequence();
                        chiefTask.AddTask.Wait(2000);
                        chiefTask.AddTask.UseMobilePhone();
                        chiefTask.Perform(chiefCop);

                        DelayedCaller.Call(delegate { chiefCop.SayAmbientSpeech("GENERIC_HI"); }, this, 2000, null);

                        briberyState = EBriberyScenarioState.Off;
                        isBriberyPlaying = false;
                    }
                }
            }
        }

        /// <summary>
        /// Processes all introduction logic.
        /// </summary>
        private void ProcessIntroduction()
        {
            // Don't allow introduction while model selection is in progress
            if (Main.GoOnDutyScript.HasFocus)
            {
                return;
            }

            // If key is down and introduction is not running, allow partner selection if either model selection is still ongoing (because we are not on duty at this point, but already have
            // selected our cop model) or when we are already on duty
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.AcceptPartner) && !introductionRunning && (Main.GoOnDutyScript.IsInProgress || Globals.IsOnDuty))
            {
                // Check if player is in checkpoint
                if (partnerCheckpoint.IsPointInCheckpoint(CPlayer.LocalPlayer.Ped.Position) && partnerCheckpoint.Enabled)
                {
                    partnerComponentSelector = new PedComponentSelector(partnerPed);
                    partnerComponentSelector.SelectionAborted += this.pedComponentSelector_SelectionAborted;
                    partnerComponentSelector.SelectionFinished += this.pedComponentSelector_SelectionFinished;
                    Main.ScriptManager.RegisterScriptInstance(partnerComponentSelector);
                    CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = false;
                    CPlayer.LocalPlayer.CanControlCharacter = false;
                    CPlayer.LocalPlayer.Ped.Visible = false;

                    TextHelper.ClearHelpbox();
                    
                    // Remove checkpoint
                    partnerCheckpoint.Visible = false;
                    partnerCheckpoint.Enabled = false;
                    return;
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.TutorialStartPoliceDepartment) && !introductionRunning && Globals.IsOnDuty)
            {
                if (tutorialCheckpoint.IsPointInCheckpoint(CPlayer.LocalPlayer.Ped.Position) && tutorialCheckpoint.Enabled)
                {
                    tutorialCheckpoint.Visible = false;
                    tutorialCheckpoint.Enabled = false;

                    Gui.DisplayHUD = true;

                    // Fire event
                    if (this.PlayerEnteredLeft != null)
                    {
                        this.PlayerEnteredLeft(this, false);
                    }

                    // Start tutorial
                    Tutorial tutorial = Main.ScriptManager.StartScript<Tutorial>("Tutorial");
                    tutorial.OnEnd += new OnEndEventHandler(this.tutorial_OnEnd);
                    return;   
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.ViewIntroduction))
            {
                if (!introductionRunning)
                {
                    // Don't want to activate the tutorial if the player is trying to buy a soda.
                    if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(SodaPosition) > 2.5f)
                    {
                        // Start introduction
                        CPlayer.LocalPlayer.CanControlCharacter = false;
                        introductionRunning = true;
                        introductionState = EPoliceDepartmentIntroductionState.Start;
                    }
                }
                else
                {
                    this.EndIntroduction();
                }
            }

            // Introduction
            if (introductionRunning)
            {
                switch (introductionState)
                {
                    case EPoliceDepartmentIntroductionState.Start:

                        // Create camera
                        if (introductionCam == null || !introductionCam.Exists())
                        {
                            introductionCam = new Camera();
                        }

                        // Setup camera
                        introductionCam.Position = InstructionCam1Position;
                        introductionCam.Heading = 140;
                        introductionCam.Rotation = new Vector3(introductionCam.Rotation.X - 6, introductionCam.Rotation.Y, introductionCam.Rotation.Z);
                        introductionCam.Activate();

                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_INTRODUCTION_START"));
                        introductionState = EPoliceDepartmentIntroductionState.Partner;
                        break;

                    case EPoliceDepartmentIntroductionState.Partner:
                        if (!TextHelper.IsHelpboxBeingDisplayed)
                        {
                            introductionCam.Position = InstructionCam2Position;
                            introductionCam.Heading = 190;
                            introductionCam.Rotation = new Vector3(introductionCam.Rotation.X - 6, introductionCam.Rotation.Y, introductionCam.Rotation.Z);

                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_INTRODUCTION_PARTNER"));
                            introductionState = EPoliceDepartmentIntroductionState.Duty;
                        }

                        break;

                    case EPoliceDepartmentIntroductionState.Duty:
                        if (!TextHelper.IsHelpboxBeingDisplayed)
                        {
                            introductionCam.Position = InstructionCam4Position;
                            introductionCam.Heading = 250;
                            introductionCam.Rotation = new Vector3(introductionCam.Rotation.X - 6, introductionCam.Rotation.Y, introductionCam.Rotation.Z);

                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_INTRODUCTION_ONDUTY"));
                            introductionState = EPoliceDepartmentIntroductionState.Leave;
                        }

                        break;

                    case EPoliceDepartmentIntroductionState.Leave:
                        if (!TextHelper.IsHelpboxBeingDisplayed)
                        {
                            introductionCam.Position = InstructionCam3Position;
                            introductionCam.Heading = 90;
                            introductionCam.Rotation = new Vector3(introductionCam.Rotation.X - 6, introductionCam.Rotation.Y, introductionCam.Rotation.Z);

                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_INTRODUCTION_LEAVE"));
                            introductionState = EPoliceDepartmentIntroductionState.End;
                        }

                        break;

                    case EPoliceDepartmentIntroductionState.End:
                        if (!TextHelper.IsHelpboxBeingDisplayed)
                        {
                            this.EndIntroduction();
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Ends the introduction.
        /// </summary>
        private void EndIntroduction()
        {
            // End introduction
            if (introductionCam != null && introductionCam.Exists())
            {
                introductionCam.Deactivate();
            }

            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();
            introductionRunning = false;
            introductionState = EPoliceDepartmentIntroductionState.Off;
        }

        /// <summary>
        /// Called when the tutorial ended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void tutorial_OnEnd(object sender)
        {
            // Force no weapon
            CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
            CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = true;
            Gui.DisplayHUD = false;
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();

            // Render checkpoint outside
            this.Visible = true;

            // Fire event
            if (this.PlayerEnteredLeft != null)
            {
                this.PlayerEnteredLeft(this, true);
            }
        }

        /// <summary>
        /// Called when the selection has been aborted.
        /// </summary>
        private void pedComponentSelector_SelectionAborted()
        {
            partnerComponentSelector.SelectionAborted -= this.pedComponentSelector_SelectionAborted;
            partnerComponentSelector.SelectionFinished -= this.pedComponentSelector_SelectionFinished;

            partnerCheckpoint.Visible = true;
            partnerCheckpoint.Enabled = true;
            partnerCheckpoint.BlipDisplay = BlipDisplay.ArrowOnly;
            TextHelper.ClearHelpbox();

            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = true;
            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.Ped.Visible = true;
            CPlayer.LocalPlayer.Ped.Alpha = 0;
        }

        /// <summary>
        /// Called when the selection has finished.
        /// </summary>
        private void pedComponentSelector_SelectionFinished()
        {
            partnerComponentSelector.SelectionAborted -= this.pedComponentSelector_SelectionAborted;
            partnerComponentSelector.SelectionFinished -= this.pedComponentSelector_SelectionFinished;

            Main.PartnerManager.AddPartner(partnerPed);
            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_GOT_PARTNER"));

            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = true;
            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.Ped.Visible = true;
            CPlayer.LocalPlayer.Ped.Alpha = 0;

            Stats.UpdateStat(Stats.EStatType.PartnerRecruitedPD, 1);
        }

        /// <summary>
        /// Starts the going on duty process.
        /// </summary>
        private void GoOnDuty()
        {
            if (!Main.GoOnDutyScript.IsInProgress)
            {
                if (!Globals.IsOnDuty)
                {
                    Main.GoOnDutyScript.Start(this.vehiclePosition, GoOnDutyPosition);
                    Main.GoOnDutyScript.PedModelSelectionFinished += new Action(this.GoOnDutyScript_PedModelSelectionFinished);
                }
                else
                {
                    Main.GoOnDutyScript.GoOffDuty();

                    // No longer on duty, hide checkpoint
                    partnerCheckpoint.Visible = false;
                    partnerCheckpoint.Enabled = false;
                    tutorialCheckpoint.Visible = false;
                    tutorialCheckpoint.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Called when the ped model selection has been finished.
        /// </summary>
        private void GoOnDutyScript_PedModelSelectionFinished()
        {
            // Disable player control and fade out screen
            CPlayer.LocalPlayer.CanControlCharacter = false;
            Game.FadeScreenOut(3000, true);

            // Force no weapon
            CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = false;
            CPlayer.LocalPlayer.Ped.Task.SwapWeapon(Weapon.Unarmed);
            CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = true;

            // Unlock all islands so the player doesn't get a wanted level when ported
            World.UnlockAllIslands();

            // Preload scene
            World.LoadEnvironmentNow(GoOnDutyPosition);
            CPlayer.LocalPlayer.TeleportTo(GoOnDutyPosition);

            // Set heading and place cam behind ped
            CPlayer.LocalPlayer.Ped.Heading = 115;
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();

            // Create checkpoints, peds etc. and fade in
            // this.CleanupPD();
            // this.PopulatePD();
            this.SpawnPartner();

            // Note: not necessary any more as model selection takes place in the room now.
            Game.FadeScreenIn(3000);

            // Prevent player from being able to enter the checkpoint
            goOnDutyCheckpoint.TriggerPlayerIsInArrow(false);

            // Hide hud and make player able to move again
            Gui.DisplayHUD = false;
            CPlayer.LocalPlayer.CanControlCharacter = true;
        }
        
        /// <summary>
        /// Called when the player is close to the pd.
        /// </summary>
        private void PlayerIsCloseToPDCallback()
        {
            if (this.PlayerCloseToPD != null)
            {
                // If at least one event listener returns false, do not enter.
                foreach (PlayerCloseToPDEventHandler playerCloseToPDEventHandler in this.PlayerCloseToPD.GetInvocationList())
                {
                    if (!playerCloseToPDEventHandler.Invoke(this))
                    {
                        return;
                    }
                }

                // If player is in a vehicle, do not enter.
                if (CPlayer.LocalPlayer.Ped.IsInVehicle())
                {
                    return;
                }
            }

            this.EnteredPD();
        }

        /// <summary>
        /// Setups some things when player enters pd, such as blocking all kind of weapons.
        /// </summary>
        private void EnteredPD()
        {
            // Disable player control and fade out screen
            CPlayer.LocalPlayer.CanControlCharacter = false;
            Game.FadeScreenOut(3000, true);

            // Force no weapon
            CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
            CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = true;

            // Unlock all islands so the player doesn't get a wanted level when ported
            World.UnlockAllIslands();

            // Preload scene
            World.LoadEnvironmentNow(SpawnPosition);

            // Teleport player
            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                // Warp out of vehicle and free it
                CVehicle vehicle = CPlayer.LocalPlayer.Ped.CurrentVehicle;
                CPlayer.LocalPlayer.Ped.WarpFromCar(SpawnPosition);
                if (vehicle.Exists())
                {
                    vehicle.NoLongerNeeded();
                }
            }

            CPlayer.LocalPlayer.TeleportTo(SpawnPosition);

            // Set heading and place cam behind ped
            CPlayer.LocalPlayer.Ped.Heading = 270;

            // Create checkpoints, peds etc. and fade in
            this.PopulatePD();
            Game.FadeScreenIn(3000);

            // Hide hud and make player able to move again
            Gui.DisplayHUD = false;
            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();

            // Print helpbox after some delay
            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_IN_POLICE_DEPARTMENT")); }, this, 3500, null);

            // Fire event
            if (this.PlayerEnteredLeft != null)
            {
                this.PlayerEnteredLeft(this, true);
            }

            Stats.UpdateStat(Stats.EStatType.PDEntered, 1);

            // Print helpbox
            //if (!LCPDMain.Global.HBDisplayedPoliceDepartmentExplanation)
            //{
            //    Functions.PrintHelp(Language.POLICEDEPARTMENT_IN_POLICE_DEPARTMENT);
            //    LCPDMain.Global.HBDisplayedPoliceDepartmentExplanation = true;
            //}
        }

        /// <summary>
        /// Disposes all entities in the pd and ports the player back.
        /// </summary>
        private void LeavePD()
        {
            // Deactivate player control and fade out
            CPlayer.LocalPlayer.CanControlCharacter = false;
            Game.FadeScreenOut(3000, true);

            // Preload scene
            World.LoadEnvironmentNow(this.blipPosition);

            // Port player infront of pd
            CPlayer.LocalPlayer.TeleportTo(this.blipPosition);
            
            // Disable checkpoint, cleanup pd and fade in
            if (this.arrowCheckpoint != null)
            {
                this.arrowCheckpoint.TriggerPlayerIsInArrow(false);
            }

            // Clear pending calls
            DelayedCaller.ClearAllRunningCalls(false, this);

            this.CleanupPD();
            Game.FadeScreenIn(3000);

            // Show hud
            Gui.DisplayHUD = true;

            // Active player control
            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = false;

            // Fire event
            if (this.PlayerEnteredLeft != null)
            {
                this.PlayerEnteredLeft(this, false);
            }
        }     

        /// <summary>
        /// Spawns all entities inside the police department.
        /// </summary>
        private void PopulatePD()
        {
            Room room = Room.FromString(LobbyRoomName);
            Room chiefRoom = Room.FromString(ChiefRoomName);
            Room lockerRoom = Room.FromString(LockerRoomName);
            Room officeRoom = Room.FromString(OfficeRoomName);

            // Create custom objects (lockers inside the duty room and the radio)
            for (int i = 0; i < LockerPositions.Length; i++)
            {
                GTA.Object locker = World.CreateObject(LockerModels[i], LockerPositions[i]);
                if (locker != null && locker.Exists())
                {
                    locker.RotationQuaternion = LockerQuaternion;
                    locker.CurrentRoom = lockerRoom;
                    locker.FreezePosition = true;
                    lockerObjects.Add(locker);
                }
            }

            radioObject = World.CreateObject("cj_radio_2", radioObjectPosition);
            if (radioObject != null && radioObject.Exists())
            {
                radioObject.CurrentRoom = officeRoom;
                radioObject.FreezePosition = true;
                GTA.Native.Function.Call("PRELOAD_STREAM", "INTERIOR_STREAMS_IRISH_BARS");
                GTA.Native.Function.Call("PLAY_STREAM_FROM_OBJECT", radioObject);
            }

            chiefRadioObject = World.CreateObject("cj_radio_2", chiefRadioObjectPosition);
            if (chiefRadioObject != null && chiefRadioObject.Exists())
            {
                chiefRadioObject.CurrentRoom = chiefRoom;
                chiefRadioObject.FreezePosition = true;
                GTA.Native.Function.Call("PRELOAD_STREAM", "INTERIOR_STREAMS_IRISH_BARS");
                GTA.Native.Function.Call("PLAY_STREAM_FROM_OBJECT", chiefRadioObject);
            }

            // Create checkpoint to leave pd
            leavePDCheckpoint = new ArrowCheckpoint(LeavePosition, this.LeavePD);
            leavePDCheckpoint.BlipDisplay = BlipDisplay.ArrowOnly;
            goOnDutyCheckpoint = new ArrowCheckpoint(GoOnDutyPosition, this.GoOnDuty);
            goOnDutyCheckpoint.BlipDisplay = BlipDisplay.ArrowOnly;

            string mainCopPedModelToUse = "M_Y_COP";
            string secondaryCopPedModelToUse = "M_M_FATCOP_01";
            string alternateCopPedModelToUse = "M_Y_COP_TRAFFIC";

            // Determine which cop ped models to use
            if (CModel.IsCurrentCopModelAlderneyModel)
            {
                mainCopPedModelToUse = "M_Y_STROOPER";
                secondaryCopPedModelToUse = "M_Y_STROOPER";
            }

            // Ensure models are loaded
            new CModel("M_M_FATCOP_01").LoadIntoMemory(false);

            bool spawnChief = Common.GetRandomBool(0, 2, 1);
            bool spawnCriminal = Common.GetRandomBool(0, 3, 1);

            if (spawnChief)
            {
                chiefCop = new CPed("IG_FRANCIS_MC", new Vector3(121.16f, -666.62f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
                if (chiefCop.Exists())
                {
                    chiefCop.Voice = "DERRICK_MCREARY";
                    chiefCop.Heading = 340;
                    chiefCop.CurrentRoom = chiefRoom;
                    chiefCop.BlockPermanentEvents = true;
                    chiefCop.Task.AlwaysKeepTask = true;
                    chiefCop.Weapons.RemoveAll();
                    chiefCop.Task.StartScenario("Scenario_SmokingOutsideOffice", chiefCop.Position);
                    isChiefSpawned = true;
                }
            }

            if (isChiefSpawned)
            {
                bool inspectCops = Common.GetRandomBool(0, 3, 1);

                if (inspectCops)
                {
                    for (int i = 0; i < InspectedCopPositions.Length; i++)
                    {
                        CPed inspectedCop = new CPed(mainCopPedModelToUse, InspectedCopPositions[i], EPedGroup.Cop, havePedsBeenSpawned);

                        if (inspectedCop.Exists())
                        {
                            inspectedCop.Weapons.RemoveAll();
                            inspectedCop.Heading = 0;
                            inspectedCop.CurrentRoom = chiefRoom;
                            inspectedCop.BlockPermanentEvents = true;
                            inspectedCop.FixCopClothing();
                            inspectedCop.Task.PlayAnimation(new AnimationSet("move_m@bness_a"), "idle", 8.0f, AnimationFlags.Unknown05);
                            inspectedCop.Task.AlwaysKeepTask = true;
                            inspectedCops.Add(inspectedCop);
                        }
                    }

                    isInspectionReady = true;
                }
            }

            if (spawnCriminal)
            {
                criminalPed = new CPed(CriminalModels[Common.GetRandomValue(0, CriminalModels.Length)].ToUpper(), FriskPosition, EPedGroup.MissionPed, havePedsBeenSpawned);
                
                if (criminalPed.Exists())
                {
                    arrestingCop = new CPed(mainCopPedModelToUse, CopPosition, EPedGroup.Cop, havePedsBeenSpawned);

                    if (arrestingCop.Exists())
                    {
                        arrestingCop.CurrentRoom = room;
                        arrestingCop.BlockPermanentEvents = true;
                        arrestingCop.FixCopClothing();
                        arrestingCop.Weapons.RemoveAll();
                        arrestingCop.Skin.SetPropIndex(0, 0);

                        criminalPed.CurrentRoom = room;
                        criminalPed.BlockPermanentEvents = true;

                        bool searchPed = Common.GetRandomBool(0, 2, 1);

                        if (searchPed)
                        {
                            arrestingCop.Animation.Play(new AnimationSet("cop"), "cop_search", 1.0f);
                            criminalPed.Animation.Play(new AnimationSet("cop"), "crim_searched", 1.0f);

                            arrestingCop.SayAmbientSpeech("INTIMIDATE");
                            DelayedCaller.Call(delegate { criminalPed.SayAmbientSpeech("INTIMIDATE_RESP"); }, this, 2500, null);

                            arrestState = EArrestScenarioState.Frisk;
                        }
                        else
                        {
                            GTA.TaskSequence criminalTask = new GTA.TaskSequence();
                            criminalTask.AddTask.GoTo(new Vector3(118.6f, -685.50f, 14.77f));
                            criminalTask.AddTask.ped.Task.AchieveHeading(0f);
                            if (Settings.UsingPoliceStationMod)
                            {
                                criminalTask.AddTask.PlayAnimation(new AnimationSet("sit"), "sit_down", 4.0f, AnimationFlags.None);
                                GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 117.81f, -685.93f, 14.77f, -814569204, 0.00f, -2, 1);
                            }
                            criminalPed.Task.ClearAll();
                            criminalPed.Task.PerformSequence(criminalTask);

                            GTA.TaskSequence copTask = new GTA.TaskSequence();
                            copTask.AddTask.GoTo(new Vector3(117.81f, -685.93f, 14.77f));
                            copTask.AddTask.ped.Task.AchieveHeading(0f);
                            if (Settings.UsingPoliceStationMod)
                            {
                                copTask.AddTask.PlayAnimation(new AnimationSet("sit"), "sit_down", 4.0f, AnimationFlags.None);
                                GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 116.81f, -685.93f, 14.77f, -814569204, 0.00, -2, 1);
                            }
                            arrestingCop.Task.ClearAll();
                            arrestingCop.Task.PerformSequence(copTask);

                            arrestState = EArrestScenarioState.End;
                        }

                        isArrestPlaying = true;

                        if (!isInspectionReady)
                        {
                            // if the inspection scenario isn't playing, the chief is free so we can set up the bribery scenario
                            if (criminalPed.Model == "F_Y_HOOKER_03")
                            {
                                isBriberyReady = true;
                                isBriberyPlaying = false;
                            }
                        }
                    }
                    else
                    {
                        criminalPed.Delete();
                        isArrestPlaying = false;
                        arrestState = EArrestScenarioState.Off;
                    }
                }
            }

            // Create ambient peds

            walkingCop = new CPed(mainCopPedModelToUse, new Vector3(115.57f, -675.79f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
            if (walkingCop.Exists())
            {
                if (Common.GetRandomBool(0, 2, 1))
                {
                    walkingCop.Position = GoOnDutyPosition;
                    walkingCop.CurrentRoom = lockerRoom;
                }
                else
                {
                    walkingCop.Heading = 250;
                    walkingCop.CurrentRoom = room;
                }

                walkingCop.BlockPermanentEvents = true;
                GTA.TaskSequence walkTask = new GTA.TaskSequence();
                walkTask.AddTask.GoTo(new Vector3(106.0f, -698.60f, 14.77f));
                GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 106.0f, -698.60, 14.77f, -16212709, 0.00, -2, 1);
                walkingCop.Task.PerformSequence(walkTask);
                walkingCop.Task.AlwaysKeepTask = true;
                walkingCop.Weapons.RemoveAll();
                walkingCop.FixCopClothing();
                walkingCop.Skin.SetPropIndex(0, 0);
            }

            workingCop = new CPed(mainCopPedModelToUse, new Vector3(117.4f, -686.01f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
            if (workingCop.Exists())
            {
                workingCop.CurrentRoom = room;
                workingCop.BlockPermanentEvents = true;
                GTA.TaskSequence workTask = new GTA.TaskSequence();
                GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 113.58f, -690.78f, 14.77f, -16212709, 0.00, -2, 1);
                workingCop.Task.PerformSequence(workTask);
                workingCop.Task.AlwaysKeepTask = true;
                workingCop.Weapons.RemoveAll();
                workingCop.FixCopClothing();
            }

            workingCop2 = new CPed(secondaryCopPedModelToUse, new Vector3(117.87f, -684.36f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
            if (workingCop2.Exists())
            {
                workingCop2.CurrentRoom = room;
                workingCop2.BlockPermanentEvents = true;
                GTA.TaskSequence workTask = new GTA.TaskSequence();
                GTA.Native.Function.Call("TASK_SIT_DOWN_ON_NEAREST_OBJECT", 0, 0, 2, 111.35f, -694.60f, 14.77f, -16212709, 0.00, -2, 1);
                workingCop2.Task.PerformSequence(workTask);
                workingCop2.Task.AlwaysKeepTask = true;
                workingCop2.Weapons.RemoveAll();
                workingCop2.FixCopClothing();
            }

            talkCop1 = new CPed(mainCopPedModelToUse, new Vector3(115.57f, -671.79f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
            if (talkCop1.Exists())
            {
                talkCop1.Heading = 250;
                talkCop1.CurrentRoom = room;
                talkCop1.BlockPermanentEvents = true;
                talkCop1.Task.PlayAnimation(new AnimationSet("amb@nightclub_ext"), "street_chat_b", 8.0f, AnimationFlags.Unknown05);
                talkCop1.Task.AlwaysKeepTask = true;
                talkCop1.Weapons.RemoveAll();
                talkCop1.FixCopClothing();
            }

            talkCop2 = new CPed(secondaryCopPedModelToUse, new Vector3(117.24f, -671.63f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
            if (talkCop2.Exists())
            {
                talkCop2.Heading = 145;
                talkCop2.CurrentRoom = room;
                talkCop2.BlockPermanentEvents = true;
                talkCop2.Task.PlayAnimation(new AnimationSet("amb@nightclub_ext"), "street_chat_a", 8.0f, AnimationFlags.Unknown05);
                talkCop2.Task.AlwaysKeepTask = true;
                talkCop2.Weapons.RemoveAll();
                talkCop2.FixCopClothing();
            }

            leanCop = new CPed(alternateCopPedModelToUse, new Vector3(112.76f, -680.09f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
            if (leanCop.Exists())
            {
                leanCop.Heading = 185;
                leanCop.CurrentRoom = room;
                leanCop.BlockPermanentEvents = true;
                leanCop.Task.PlayAnimation(new AnimationSet("amb@lean_idles"), "lean_idle_a", 8.0f, AnimationFlags.Unknown05);
                leanCop.Task.AlwaysKeepTask = true;
                leanCop.Weapons.RemoveAll();
            }

            guardCop = new CPed(mainCopPedModelToUse, new Vector3(119.28f, -686.55f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
            if (guardCop.Exists())
            {
                guardCop.Heading = 50;
                guardCop.CurrentRoom = room;
                guardCop.BlockPermanentEvents = true;
                guardCop.Skin.SetPropIndex(0, 0);
                guardCop.Weapons.AssaultRifle_M4.Ammo = 1;
                guardCop.Weapons.Select(Weapon.Rifle_M4);
                guardCop.Task.StartScenario("Scenario_HeavilyArmedPolice", guardCop.Position);
                guardCop.Task.AlwaysKeepTask = true;
                guardCop.FixCopClothing();
            }

            bool spawnLockerCop = Common.GetRandomBool(0, 2, 1);
            if (spawnLockerCop)
            {
                lockerCop = new CPed(mainCopPedModelToUse, new Vector3(122.00f, -689.47f, 14.77f), EPedGroup.Cop, havePedsBeenSpawned);
                if (lockerCop != null && lockerCop.Exists())
                {
                    lockerCop.Heading = 115;
                    lockerCop.CurrentRoom = lockerRoom;
                    lockerCop.BlockPermanentEvents = true;
                    lockerCop.Skin.SetPropIndex(0, 0);
                    lockerCop.FixCopClothing();
                    lockerCop.Weapons.RemoveAll();
                }
            }

            this.SpawnPartner();

            // If on duty, offer tutorial
            if (Globals.IsOnDuty)
            {
                tutorialCheckpoint = new ArrowCheckpoint(TutorialPosition, () => TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_IN_TUTORIAL_CHECKPOINT")));
                tutorialCheckpoint.BlipDisplay = BlipDisplay.ArrowOnly;
            }

            // Create first aid pickup
            pickupFirstAid = new CPickup(EPickupType.FirstAidBox, FirstAidBoxPosition, new Vector3(90, 0, 0));
            if (pickupFirstAid.Exists())
            {
                pickupFirstAid.CurrentRoom = room;
            }

            // Create bulletproof vest pickup
            pickupVest = new CPickup(EPickupType.BulletProofVest, BulletProofVestPosition, new Vector3(270, 0, 0));
            if (pickupVest.Exists())
            {
                pickupVest.CurrentRoom = room;
            }

            hasBeenPopulated = true;
        }

        /// <summary>
        /// Spawns the partner ped.
        /// </summary>
        private void SpawnPartner()
        {
            Room room = Room.FromString(LobbyRoomName);

            // Partner selection (either if on duty or if selection is in progress (so cop model has been selected already))
            if (Globals.IsOnDuty || Main.GoOnDutyScript.IsInProgress)
            {
                // Partner ped will only spawn when there is no current partner
                if (Main.PartnerManager.CanPartnerBeAdded)
                {
                    partnerPed = new CPed(CPlayer.LocalPlayer.Model, PartnerPosition, EPedGroup.Cop, false);
                    if (partnerPed.Exists())
                    {
                        partnerPed.CurrentRoom = room;
                        partnerPed.Heading = 180;
                        partnerPed.BlockPermanentEvents = true;
                        partnerPed.FixCopClothing();

                        Vector3 checkpointPos = partnerPed.GetOffsetPosition(new Vector3(0, 1, -2.2f));

                        // When in checkpoint, display helptext
                        partnerCheckpoint = new ArrowCheckpoint(checkpointPos, () => TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("POLICEDEPARTMENT_IN_PARTNER_CHECKPOINT")));
                        partnerCheckpoint.BlipDisplay = BlipDisplay.ArrowOnly;
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up the police department and deletes all entities spawned.
        /// </summary>
        private void CleanupPD()
        {
            hasBeenPopulated = false;

            if (leavePDCheckpoint != null)
            {
                leavePDCheckpoint.Delete();
                leavePDCheckpoint = null;
            }

            if (goOnDutyCheckpoint != null)
            {
                goOnDutyCheckpoint.Delete();
                goOnDutyCheckpoint = null;
            }

            if (tutorialCheckpoint != null)
            {
                tutorialCheckpoint.Delete();
                tutorialCheckpoint = null;
            }

            if (talkCop1.Exists())
            {
                talkCop1.Delete();
            }

            if (talkCop2.Exists())
            {
                talkCop2.Delete();
            }

            if (walkingCop.Exists())
            {
                walkingCop.Delete();
            }

            if (workingCop.Exists())
            {
                workingCop.Delete();
            }

            if (workingCop2.Exists())
            {
                workingCop2.Delete();
            }

            if (leanCop.Exists())
            {
                leanCop.Delete();
            }

            if (guardCop.Exists())
            {
                guardCop.Delete();
            }

            if (lockerCop != null && lockerCop.Exists())
            {
                lockerCop.Delete();
            }

            if (chiefCop != null)
            {
                if (chiefCop.Exists())
                {
                    chiefCop.Delete();
                }
            }

            if (criminalPed != null)
            {
                if (criminalPed.Exists())
                {
                    criminalPed.Delete();
                }
            }

            if (arrestingCop != null)
            {
                if (arrestingCop.Exists())
                {
                    arrestingCop.Delete();
                }
            }

            if (radioObject != null && radioObject.Exists())
            {
                radioObject.Delete();
            }

            if (chiefRadioObject != null && chiefRadioObject.Exists())
            {
                chiefRadioObject.Delete();
            }

            foreach (CPed inspectedPed in inspectedCops)
            {
                if (inspectedPed != null)
                {
                    if (inspectedPed.Exists())
                    {
                        inspectedPed.Delete();
                    }
                }
            }

            isInspectionPlaying = false;
            isInspectionReady = false;
            isArrestPlaying = false;
            isBriberyPlaying = false;
            isBriberyReady = false;
            briberyState = EBriberyScenarioState.Off;
            arrestState = EArrestScenarioState.Off;

            inspectedCops.Clear();


            foreach (GTA.Object locker in lockerObjects)
            {
                if (locker != null)
                {
                    if (locker.Exists())
                    {
                        locker.Delete();
                    }
                }
            }

            lockerObjects.Clear();

            if (partnerPed != null && partnerPed.Exists())
            {
                // Only delete if not partner ped
                if (!Main.PartnerManager.IsPartner(partnerPed))
                {
                    partnerPed.Delete();
                    partnerPed = null;
                }
            }

            if (partnerCheckpoint != null)
            {
                partnerCheckpoint.Delete();
                partnerCheckpoint = null;
            }

            if (pickupFirstAid != null)
            {
                pickupFirstAid.Delete();
                pickupFirstAid = null;
            }

            if (pickupVest != null)
            {
                pickupVest.Delete();
                pickupVest = null;
            }
        }
    }
}
