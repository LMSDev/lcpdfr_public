namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Abstract base class for all ped tasks.
    /// </summary>
    internal abstract class PedTask : BaseComponent, ICanOwnEntities
    {
        /// <summary>
        /// Timer used for time out.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PedTask"/> class.
        /// </summary>
        /// <param name="taskID">
        /// The task id.
        /// </param>
        protected PedTask(ETaskID taskID) : this(taskID, int.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PedTask"/> class.
        /// </summary>
        /// <param name="taskID">
        /// The task id.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        protected PedTask(ETaskID taskID, int timeOut)
        {
            this.TaskID = taskID;
            this.TimeOut = timeOut;

            this.Active = true;
            this.timer = new NonAutomaticTimer(timeOut);
        }

        /// <summary>
        /// Gets a value indicating whether the task is still active (or being deleted).
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Gets the task id.
        /// </summary>
        public ETaskID TaskID { get; private set; }

        /// <summary>
        /// Gets the time out for the task.
        /// </summary>
        public float TimeOut { get; private set; }

        /// <summary>
        /// Marks the task as done and thus the task will be deleted next tick by the task manager.
        /// </summary>
        protected void SetTaskAsDone()
        {
            this.Active = false;
        }

        /// <summary>
        /// Assigns the task to <paramref name="ped"/> using <paramref name="taskPriority"/>. TODO: Extend priorities.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="taskPriority"></param>
        public void AssignTo(CPed ped, ETaskPriority taskPriority)
        {
            ped.Intelligence.TaskManager.Assign(this, taskPriority);
        }

        public void InternalProcess(CPed ped)
        {
            // Check if task is still valid
            this.Process(ped);

            if (this.timer.CanExecute())
            {
                MakeAbortable(ped);
                return;
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public abstract void MakeAbortable(CPed ped);

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public virtual void Initialize(CPed ped)
        {
            // Reset timer
            this.timer.Reset();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public abstract void Process(CPed ped);
    }

    internal enum EInternalTaskID : uint
    {
        CTaskSimpleMovePlayer = 2,
        CTaskComplexPlayerOnFoot = 4,
        CTaskComplexPlayerGun = 6,
        CTaskComplexPlayerPlaceCarBomb = 7,
        CTaskComplexPlayerIdles = 8,
        CTaskComplexMedicTreatInjuredPed = 0x64,
        CTaskComplexTreatAccident = 0x65,
        CTaskComplexDriveFireTruck = 0x6B,
        CTaskComplexUseWaterCannon = 0x6D,
        CTaskSimpleAssessInjuredPed = 0x6F,
        CTaskComplexMedicDriver = 0x70,
        CTaskComplexMedicPassenger = 0x71,
        CTaskComplexMedicWandering = 0x71,
        CTaskComplexPlayerSettingsTask = 0x73,
        CTaskSimpleNone = 0xC8,
        CTaskSimpleSetCharCombatDecisionMaker = 0xC8,
        CTaskSimpleLeaveGroup = 0xC8,
        CTaskSimplePause = 0xCA,
        CTaskSimplePauseSystemTimer = 0xCA,
        CTaskSimpleStandStill = 0xCB,
        CTaskSimpleGetUp = 0xCD,
        CTaskComplexGetUpAndStandStill = 0xCE,
        CTaskSimpleFall = 0xCF,
        CTaskComplexFallAndGetUp = 0xD0,
        CTaskSimpleJumpLaunch = 0xD2,
        CTaskComplexJump = 0xD3,
        CTaskSimpleDie = 0xD4,
        CTaskComplexDie = 0xD9,
        CTaskSimpleDead = 0xDA,
        CTaskSimpleTired = 0xDB,
        CTaskSimpleSitDown = 0xDC,
        CTaskComplexSitIdle = 0xDD,
        CTaskSimpleStandUp = 0xDE,
        CTaskComplexSitDownThenIdleThenStandUp = 0xDF,
        CTaskComplexHitResponse = 0xE6,
        CTaskComplexUseEffect = 0xE9,
        CTaskComplexWaitAtAttractor = 0xEA,
        CTaskComplexUseAttractor = 0xEB,
        CTaskComplexWaitForDryWeather = 0xEC,
        CTaskComplexWaitForBus = 0xED,
        CTaskSimpleWaitForBus = 0xEE,
        CTaskSimpleJumpInAir = 0xF1,
        CTaskSimpleJumpLand = 0xF2,
        CTaskComplexSequence = 0xF4,
        CTaskComplexOnFire = 0xFA,
        CTaskSimpleClimb = 0xFE,
        CTaskComplexInWater = 0x10C,
        CTaskSimpleTriggerLookAt = 0x10D,
        CTaskSimpleClearLookAt = 0x10E,
        CTaskSimpleSetCharDecisionMaker = 0x10F,
        CTaskComplexUseSequence = 0x111,
        CTaskComplexInjuredOnGround = 0x116,
        CTaskSimplePathfindProblem = 0x118,
        CTaskSimpleDoNothing = 0x119,
        CTaskSimpleMoveStandStill = 0x11A,
        CTaskSimpleMoveDoNothing = 0x11B,
        CTaskSimpleMoveNone = 0x11C,
        CTaskComplexControlMovement = 0x11D,
        CTaskComplexMoveSequence = 0x11E,
        CTaskComplexClimbLadder = 0x11F,
        CTaskSimpleClimbLadder = 0x120,
        CTaskComplexClimbLadderFully = 0x121,
        CTaskComplexMoveAroundCoverPoints = 0x122,
        CTaskSimplePlayRandomAmbients = 0x123,
        CTaskSimpleMovePathfindProblem = 0x124,
        CTaskSimpleMoveInAir = 0x125,
        CTaskSimpleNetworkClone = 0x126,
        CTaskSimpleInjuredOnGroundTransition = 0x127,
        CTaskComplexUseClimbOnRoute = 0x128,
        CTaskComplexUseDropDownOnRoute = 0x129,
        CTaskComplexUseLadderOnRoute = 0x12A,
        CTaskSimpleSay = 0x12D,
        CTaskSimpleShakeFist = 0x12E,
        CTaskSimpleAffectSecondaryBehaviour = 0x132,
        CTaskSimplePickUpObject = 0x138,
        CTaskSimplePutDownObject = 0x139,
        CTaskComplexPickUpObject = 0x13A,
        CTaskComplexPickUpAndCarryObject = 0x13B,
        CTaskSimpleOpenDoor = 0x13D,
        CTaskSimpleShovePed = 0x13E,
        CTaskSimpleSwapWeapon = 0x13F,
        CTaskComplexShockingEventWatch = 0x141,
        CTaskComplexShockingEventFlee = 0x142,
        CTaskComplexShockingEventGoto = 0x143,
        CTaskComplexShockingEventHurryAway = 0x144,
        CTaskSimplePutOnHelmet = 0x145,
        CTaskSimpleTakeOffHelmet = 0x146,
        CTaskComplexCarReactToVehicleCollision = 0x147,
        CTaskComplexReactToRanPedOver = 0x148,
        CTaskComplexCarReactToVehicleCollisionGetOut = 0x149,
        CTaskComplexStationaryScenario = 0x15E,
        CTaskComplexSeatedScenario = 0x15F,
        CTaskComplexDrivingScenario = 0x161,
        CTaskComplexMoveBetweenPointsScenario = 0x162,
        CTaskComplexChatScenario = 0x163,
        CTaskComplexWaitForSeatToBeFree = 0x164,
        CTaskComplexWaitForDoorToBeOpen = 0x166,
        CTaskComplexDriveWanderForTime = 0x167,
        CTaskComplexWaitForTime = 0x168,
        CTaskComplexWaitTillItsOkToStop = 0x168,
        CTaskComplexGetInCarScenario = 0x169,
        CTaskComplexWalkWithPedScenario = 0x16A,
        CTaskComplexMobileChatScenario = 0x16B,
        CTaskSimpleSayAudio = 0x16C,
        CTaskComplexPoliceSniperScenario = 0x16D,
        CTaskComplexMobileMakeCall = 0x16E,
        CTaskComplexWaitForSteppingOut = 0x16F,
        CTaskSimpleRunDictAnim = 0x190,
        CTaskSimpleRunNamedAnim = 0x191,
        CTaskSimpleHitFromBack = 0x193,
        CTaskSimpleHitFromFront = 0x194,
        CTaskSimpleHitFromLeft = 0x195,
        CTaskSimpleHitFromRight = 0x196,
        CTaskSimpleHitWall = 0x19B,
        CTaskSimpleCower = 0x19C,
        CTaskSimpleHandsUp = 0x19D,
        CTaskSimpleDuck = 0x19F,
        CTaskComplexMelee = 0x1AF,
        CTaskSimpleMoveMeleeMovement = 0x1B0,
        CTaskSimpleMeleeActionResult = 0x1B1,
        CTaskSimpleHitHead = 0x1F4,
        CTaskComplexEvasiveStep = 0x1F6,
        CTaskComplexWalkRoundCarWhileWandering = 0x1FC,
        CTaskComplexWalkRoundFire = 0x202,
        CTaskComplexStuckInAir = 0x203,
        CTaskComplexMove_StepAwayFromCollisionObjects = 0x204,
        CTaskComplexWalkRoundEntity = 0x205,
        CTaskSimpleSidewaysDive = 0x207,
        CTaskComplexInvestigateDeadPed = 0x258,
        CTaskComplexReactToGunAimedAt = 0x259,
        CTaskComplexExtinguishFires = 0x25C,
        CTaskComplexAvoidPlayerTargetting = 0x25D,
        CTaskComplexStealCar = 0x28E,
        CTaskComplexLeaveCarAndFlee = 0x2C2,
        CTaskComplexLeaveCarAndWander = 0x2C3,
        CTaskComplexScreamInCarThenLeave = 0x2C4,
        CTaskComplexCarDriveBasic = 0x2C5,
        CTaskComplexDriveToPoint = 0x2C6,
        CTaskComplexCarDriveWander = 0x2C7,
        CTaskComplexLeaveAnyCar = 0x2CA,
        CTaskComplexGetOffBoat = 0x2CC,
        CTaskComplexEnterAnyCarAsDriver = 0x2CD,
        CTaskComplexCarDriveTimed = 0x2CF,
        CTaskComplexDrivePointRoute = 0x2D1,
        CTaskComplexCarSetTempAction = 0x2D3,
        CTaskComplexCarDriveMission = 0x2D4,
        CTaskComplexCarDrive = 0x2D5,
        CTaskComplexCarDriveMissionFleeScene = 0x2D6,
        CTaskComplexCarDriveMissionKillPed = 0x2D9,
        CTaskComplexPlayerDrive = 0x2DC,
        CTaskComplexNewGetInVehicle = 0x2DE,
        CTaskComplexOpenVehicleDoor = 0x2DF,
        CTaskComplexClimbIntoVehicle = 0x2E0,
        CTaskComplexClearVehicleSeat = 0x2E1,
        CTaskComplexNewExitVehicle = 0x2E2,
        CTaskComplexShuffleBetweenSeats = 0x2E3,
        CTaskComplexGangDriveby = 0x2E4,
        CTaskComplexCloseVehicleDoor = 0x2E5,
        CTaskComplexBackOff = 0x2E6,
        CTaskComplexBeArrestedAndDrivenAway = 0x2E7,
        CTaskComplexArrestedAIPedAndDriveAway = 0x2E8,
        CTaskComplexGoToCarDoorAndStandStill = 0x320,
        CTaskSimpleCarAlign = 0x321,
        CTaskSimpleCarOpenDoorFromOutside = 0x322,
        CTaskSimpleCarOpenLockedDoorFromOutside = 0x323,
        CTaskSimpleCarCloseDoorFromInside = 0x325,
        CTaskSimpleCarCloseDoorFromOutside = 0x326,
        CTaskSimpleCarGetIn = 0x327,
        CTaskSimpleCarShuffle = 0x328,
        CTaskSimpleCarSetPedInVehicle = 0x32B,
        CTaskSimpleCarGetOut = 0x32D,
        CTaskSimpleCarJumpOut = 0x32E,
        CTaskSimpleCarSetPedOut = 0x330,
        CTaskSimpleCarSlowDragPedOut = 0x334,
        CTaskSimpleSetPedAsAutoDriver = 0x33B,
        CTaskSimpleWaitUntilPedIsOutCar = 0x33D,
        CTaskSimpleCreateCarAndGetIn = 0x340,
        CTaskSimpleStartCar = 0x343,
        CTaskSimpleShunt = 0x344,
        CTaskSimpleSmashCarWindow = 0x346,
        CTaskSimpleThrowGrenadeFromVehicle = 0x347,
        CTaskSimpleCarSlowBeDraggedOut = 0x355,
        CTaskSimpleMoveGoToPoint = 0x384,
        CTaskComplexGoToPointShooting = 0x385,
        CTaskSimpleMoveAchieveHeading = 0x386,
        CTaskComplexMoveGoToPointAndStandStill = 0x387,
        CTaskComplexGoToPointAndStandStillTimed = 0x387,
        CTaskComplexMoveFollowPointRoute = 0x389,
        CTaskComplexMoveSeekEntityT = 0x38B,
        CTaskComplexSmartFleePoint = 0x38E,
        CTaskComplexSmartFleeEntity = 0x38F,
        CTaskComplexWander = 0x390,
        CTaskComplexWanderStandard = 0x390,
        CTaskComplexWanderFlee = 0x390,
        CTaskComplexWanderMedic = 0x390,
        CTaskComplexWanderCriminal = 0x390,
        CTaskComplexWanderCop = 0x390,
        CTaskComplexFollowLeaderInFormation = 0x391,
        CTaskComplexGoToAttractor = 0x393,
        CTaskComplexMoveAvoidOtherPedWhileWandering = 0x395,
        CTaskComplexGoToPointAnyMeans = 0x396,
        CTaskComplexTurnToFaceEntityOrCoord = 0x398,
        CTaskComplexFollowLeaderAnyMeans = 0x39B,
        CTaskComplexGoToPointAiming = 0x39C,
        CTaskComplexTrackEntity = 0x39D,
        CTaskComplexFleeAnyMeans = 0x39F,
        CTaskComplexFleeShooting = 0x3A0,
        CTaskComplexFollowPatrolRoute = 0x3A3,
        CTaskComplexSeekEntityAiming = 0x3A5,
        CTaskSimpleSlideToCoord = 0x3A6,
        CTaskComplexFollowPedFootsteps = 0x3A8,
        CTaskSimpleMoveTrackingEntity = 0x3AD,
        CTaskComplexMoveFollowNavMeshRoute = 0x3AE,
        CTaskSimpleMoveGoToPointOnRoute = 0x3AF,
        CTaskComplexEscapeBlast = 0x3B0,
        CTaskComplexMoveGetToPointContinuous = 0x3B1,
        CTaskComplexCop = 0x3B2,
        CTaskComplexSearchForPedOnFoot = 0x3B3,
        CTaskComplexSearchForPedInCar = 0x3B4,
        CTaskComplexMoveWander = 0x3B5,
        CTaskComplexMoveBeInFormation = 0x3B6,
        CTaskComplexMoveCrowdAroundLocation = 0x3B7,
        CTaskComplexMoveCrossRoadAtTrafficLights = 0x3B8,
        CTaskComplexMoveWaitForTraffic = 0x3B9,
        CTaskComplexMoveGoToPointStandStillAchieveHeading = 0x3BB,
        CTaskSimpleMoveWaitForNavMeshSpecialActionEvent = 0x3BC,
        CTaskComplexMoveReturnToRoute = 0x3BE,
        CTaskComplexMoveGoToShelterAndWait = 0x3BF,
        CTaskComplexMoveGetOntoMainNavMesh = 0x3C0,
        CTaskSimpleMoveSlideToCoord = 0x3C1,
        CTaskComplexMoveGoToPointRelativeToEntityAndStandStill = 0x3C2,
        CTaskComplexCopHelicopter = 0x3C3,
        CTaskComplexHelicopterStrafe = 0x3C4,
        CTaskComplexUseMobilePhoneAndMovement = 0x3C5,
        CTaskComplexFleeAndDive = 0x3C6,
        CTaskComplexGetOutOfWater = 0x3C7,
        CTaskComplexDestroyCar = 0x3EB,
        CTaskComplexDestroyCarArmed = 0x3ED,
        CTaskSimpleBeHit = 0x3F0,
        CTaskSimpleThrowProjectile = 0x3FA,
        CTaskSimpleSetCharIgnoreWeaponRangeFlag = 0x409,
        CTaskComplexSeekCover = 0x40C,
        CTaskComplexSeekCoverShooting = 0x40C,
        CTaskComplexAimAndThrowProjectile = 0x40E,
        CTaskSimplePlayerAimProjectile = 0x40F,
        CTaskComplexGun = 0x410,
        CTaskSimpleAimGun = 0x411,
        CTaskSimpleFireGun = 0x412,
        CTaskSimpleReloadGun = 0x413,
        CTaskComplexSlideIntoCover = 0x414,
        CTaskComplexPlayerInCover = 0x416,
        CTaskComplexGoIntoCover = 0x417,
        CTaskComplexCombatClosestTargetInArea = 0x418,
        CTaskSimpleNewGangDriveBy = 0x419,
        CTaskComplexCombatAdditionalTask = 0x41A,
        CTaskSimplePlayUpperCombatAnim = 0x41B,
        CTaskSimplePlaySwatSignalAnim = 0x41C,
        CTaskSimpleCombatRoll = 0x41D,
        CTaskComplexNewUseCover = 0x41E,
        CTaskSimplePlayAnimAndSlideIntoCover = 0x41F,
        CTaskSimpleLoadAnimPlayAnim = 0x420,
        CTaskSimplePlayAnimAndSlideOutOfCover = 0x421,
        CTaskComplexThrowProjectile = 0x422,
        CTaskSimpleArrestPed = 0x44C,
        CTaskComplexArrestPed = 0x44D,
        CTaskSimplePlayerBeArrested = 0x454,
        CTaskComplexGangHasslePed = 0x4BC,
        CTaskSimpleTogglePedThreatScanner = 0x515,
        CTaskSimpleMoveSwim = 0x518,
        CTaskSimpleDuckToggle = 0x51A,
        CTaskSimpleWaitUntilAreaCodesMatch = 0x51B,
        CTaskComplexMoveAboutInjured = 0x51E,
        CTaskComplexRevive = 0x51F,
        CTaskComplexReact = 0x520,
        CTaskComplexUseMobilePhone = 0x640,
        CTaskComplexCombat = 0x76C,
        CTaskComplexCombatFireSubtask = 0x76D,
        CTaskComplexCombatAdvanceSubtask = 0x76E,
        CTaskComplexCombatSeekCoverSubtask = 0x76F,
        CTaskComplexCombatRetreatSubtask = 0x770,
        CTaskComplexCombatChargeSubtask = 0x771,
        CTaskComplexCombatInvestigateSubtask = 0x772,
        CTaskComplexCombatPullFromCarSubtask = 0x773,
        CTaskComplexCombatPersueInCarSubtask = 0x774,
        CTaskComplexCombatBustPed = 0x776,
        CTaskComplexCombatExecutePedSubtask = 0x777,
        CTaskComplexCombatFlankSubtask = 0x779,
        CTaskComplexSetAndGuardArea = 0x78C,
        CTaskComplexStandGuard = 0x78D,
        CTaskComplexSeperate = 0x78E,
        CTaskSimpleNMRollUpAndRelax = 0x838,
        CTaskSimpleNMPose = 0x839,
        CTaskSimpleNMBrace = 0x83A,
        CTaskSimpleNMShot = 0x83B,
        CTaskSimpleNMHighFall = 0x83C,
        CTaskSimpleNMBalance = 0x83D,
        CTaskSimpleNMExplosion = 0x83E,
        CTaskSimpleNMOnFire = 0x83F,
        CTaskSimpleNMScriptControl = 0x840,
        CTaskSimpleNMJumpRollFromRoadVehicle = 0x841,
        CTaskSimpleNMFlinch = 0x842,
        CTaskSimpleNMSit = 0x843,
        CTaskSimpleNMFallDown = 0x844,
        CTaskSimpleBlendFromNM = 0x845,
    }

    internal enum ETaskID
    {
        AdvancedDrivingAI,
        Argue,
        ArrestedPedAndDriveAway,
        ArrestPed,
        BeingBusted,
        BustPed,
        ChasePed,
        ChasePedInVehicle,
        ChasePedOnFoot,
        Chat,
        Cop,
        CopChasePedOnFoot,
        CopHelicopter,
        CopSearchForPedOnFoot,
        CopSearchForPedInVehicle,
        CopTasePed,
        CopUpdateVisualForTarget,
        CuffPed,
        DriveDrunk,
        FightToPoint,
        Flashlight,
        FleeEvadeCops,
        FleeEvadeCopsInVehicle,
        FleeEvadeCopsOnFoot,
        GetInVehicle,
        HeliFlyOff,
        HeliFollowRoute,
        Investigate,
        LeaveScene,
        LookAtPed,
        LookAtPosition,
        MarkAsNoLongerNeeded,
        ParkVehicle,
        PlayAnimation,
        PlayAnimationAndRepeat,
        PlaySecondaryUpperAnimationAndRepeat,
        Scenario,
        Sequence,
        Test,
        TreatPed,
        Timed,
        WaitUntilPedIsInVehicle,
        WalkieTalkie,
        WalkDrunk,
        Wander,
    }

    /// <summary>
    /// The different task priorities.
    /// </summary>
    internal enum ETaskPriority
    {
        /// <summary>
        /// A normal task that affects movement.
        /// </summary>
        MainTask = 0,

        /// <summary>
        /// A secondary task that most of the time does some background logic and doesn't affect movement.
        /// </summary>
        SubTask = 1,

        /// <summary>
        /// A task that should run all the time.
        /// </summary>
        Permanent = 2,
    }
}