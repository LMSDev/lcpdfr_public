namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Timers;

    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.Engine.Scripting.Events;

    internal class TaskCopChasePed : PedTask
    {
        private const float DistanceToLeaveVehicle = 25;
        private const float DistanceToStartBusting = 5.0f;
        private const float DistanceToStartTasing = 12.0f;
        private const float DistanceToStartDragging = 25;
        private const float DistanceToStartLookingForAVehicle = 25;
        private const float DistanceToStartShooting = 10;
        private const float SpeedToStartLookingForAVehicle = 20;
        private const float VehicleScanDistance = 15;

        private int timesincelast = 0;

        private bool askedToDropWeapon;
        private bool canSeeSuspect;
        private bool fightSuspect;
        private bool firstAction;
        private bool toldtoFightSuspect;
        private EInternalTaskID internalTask; // Which internal task is active at the moment

        /// <summary>
        /// The last action the cop was executing
        /// </summary>
        private ETaskChasePedAction lastAction;
        private bool noVehicleAround;
        private CPed target;

        /// <summary>
        /// Timer to prevent the drag out of vehicle to be issued to often and thus bug.
        /// </summary>
        private Timer dragOutOfVehicleTimer;

        /// <summary>
        /// Timer to execute the chase logic
        /// </summary>
        private Timer processTimer;

        /// <summary>
        /// Timer to delay check for nearby combats for performance reasons.
        /// </summary>
        private NonAutomaticTimer timerNearbyCombats;

        /// <summary>
        /// Do not directly use this, but rather create an instance of chase
        /// </summary>
        /// <param name="target"></param>
        /// <param name="allowKilling"></param>
        /// <param name="allowTasing"></param>
        /// <param name="allowVehicles"></param>
        public TaskCopChasePed(CPed target, bool allowKilling, bool allowTasing, bool allowVehicles) : base(ETaskID.ChasePed)
        {
            this.target = target;
            this.target.Wanted.OfficersChasing++;
            this.firstAction = true;
            this.processTimer = new Timer(250);
            this.dragOutOfVehicleTimer = new Timer(500);
            this.timerNearbyCombats = new NonAutomaticTimer(250);
        }

        public override void MakeAbortable(CPed ped)
        {
            this.target.Wanted.OfficersChasing--;

            PedDataCop pedDataCop = ped.PedData as PedDataCop;
            pedDataCop.Available = true;
            pedDataCop.CurrentTarget = null;

            if (ped.Exists())
            {
                if (ped.Model == "M_Y_SWAT")
                {
                    ped.SetAnimGroup("move_m@swat");
                }
            }

            // If seek entity aiming task is still active, clear
            if (ped.Exists() && ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
            {
                ped.Task.ClearAll();
            }

            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopHelicopter))
            {
                // TODO: Verify whether this is necessary
                TaskCopHelicopter taskCopHelicopter = ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopHelicopter) as TaskCopHelicopter;
                taskCopHelicopter.MakeAbortable(ped);
            }

            SetTaskAsDone();
        }

        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            PedDataCop pedDataCop = ped.PedData as PedDataCop;
            pedDataCop.Available = false;
            pedDataCop.CurrentTarget = this.target;

            // TODO: Verify if this call is necessary
            ped.Task.ClearAll();
            AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();

            // Enhance movement
            ped.SetWaterFlags(false, false, true);
            ped.SetPathfinding(true, true, true);

            if (ped.Model == "M_Y_SWAT")
            {
                ped.SetAnimGroup("move_cop");

                if (!ped.Weapons.Current.isPresent)
                {
                    // Make them equip their gun
                    ped.Weapons.inSlot(WeaponSlot.Rifle).Select();
                }
            }

            if (this.target == CPlayer.LocalPlayer.Ped)
            {
                ped.SetPedWontAttackPlayerWithoutWantedLevel(false);
            }

            this.lastAction = ETaskChasePedAction.Unknown;
        }

        public override void Process(CPed ped)
        {
            // Abort task if target ped does no longer exist
            if (!this.target.Exists() || !this.target.Exists())
            {
                MakeAbortable(ped);
                return;
            }

            if (!this.target.IsAliveAndWell)
            {
                MakeAbortable(ped);
                return;
            }

            // Check if we can see the ped
            this.canSeeSuspect = ped.GetPedData<PedDataCop>().CanSeeSuspect;

            // Check if suspect has damaged us
            if (ped.HasBeenDamagedBy(this.target))
            {
                this.target.Wanted.CopsDamaged++;

                // Make close cops say speech
                if (this.target.PedData.CurrentChase != null)
                {
                    foreach (CPed cop in this.target.PedData.CurrentChase.Cops)
                    {
                        if (cop.Exists())
                        {
                            if (cop.Position.DistanceTo2D(ped.Position) < 40.0f)
                            {
                                cop.CancelAmbientSpeech();

                                CPed cop1 = cop;
                                DelayedCaller.Call(
                                    delegate
                                    {
                                        if (cop1.Exists())
                                        {
                                            if (this.target.Wanted.CopsDamaged >= 3)
                                            {
                                                cop1.SayAmbientSpeech("WANTED_LEVEL_INC_TO_2");
                                            }
                                            else
                                            {
                                                cop1.SayAmbientSpeech("FIGHT");
                                            }
                                        }
                                    }, 
                                    Common.GetRandomValue(250, 500));
                            }
                        }
                    }
                }

                if (this.target.Wanted.CopsDamaged >= 3)
                {
                    // If number of cops damaged is above 3 or above, make cops attack.
                    if (!this.toldtoFightSuspect)
                    {
                        // If not already ordered to fight
                        DelayedCaller.Call(delegate
                        {
                            if (this.target.PedData.CurrentChase != null)
                            {
                                this.target.PedData.CurrentChase.ForceKilling = true;
                            }
                            this.fightSuspect = true;
                        }, Common.GetRandomValue(1500, 3500));

                        toldtoFightSuspect = true;

                        new EventOfficerAttacked(ped, this.target, true);
                        // Damage entity is cleared here
                    }
                }
                else
                {
                    ped.ClearLastDamageEntity();
                }
            }

            if (this.processTimer.CanExecute())
            {
                // Get next action to do
                ETaskChasePedAction action = this.GetNextAction(ped);

                // Check if task is running
                bool running = ped.Intelligence.TaskManager.IsInternalTaskActive(this.internalTask);

                // If last action is not equal to new action, set internal running flag to false, so new task will be applied
                // Now I know this is kind of done twice because the internal task is set to the next action, but this will help when the internal task is the same
                // for two different actions
                if (action != this.lastAction)
                {
                    running = false;
                }
                this.lastAction = action;

                // Assign task if the task is not running or if it's the first time
                if (!running || this.firstAction)
                {
                    // GTA.Game.DisplayText(" -- " + action.ToString() + " -- ");
                    //ped.Debug = action.ToString();

                    if (action == ETaskChasePedAction.AskToDropWeapon)
                    {
                        ped.SayAmbientSpeech("SPOT_GUN");
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGun))
                        {
                            ped.EnsurePedHasWeapon();
                            ped.Task.ShootAt(this.target, GTA.ShootMode.AimOnly, 10000);
                        }
                        this.target.Intelligence.OnCopAsksToDropWeapon(ped);
                        this.target.Wanted.WeaponSpotted = true;
                        this.askedToDropWeapon = true;
                    }
                    if (action == ETaskChasePedAction.Bust)
                    {
                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
                        {
                            // Let the tase task finish before, if any
                            if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopTasePed))
                            {
                                if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                                {
                                    ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopChasePedOnFoot).MakeAbortable(ped);
                                }

                                TaskBustPed taskBustPed = new TaskBustPed(this.target);
                                taskBustPed.AssignTo(ped, ETaskPriority.MainTask);
                            }
                        }
                    }

                    if (action == ETaskChasePedAction.Tase)
                    {
                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                        {
                            TaskCopChasePedOnFoot chaseTask = new TaskCopChasePedOnFoot(this.target);
                            chaseTask.AssignTo(ped, ETaskPriority.MainTask);
                        }

                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopTasePed))
                        {
                            TaskCopTasePed taskTasePed = new TaskCopTasePed(this.target, false, true);
                            taskTasePed.AssignTo(ped, ETaskPriority.MainTask);
                        }
                    }

                    if (action == ETaskChasePedAction.DriveToLastKnownPosition)
                    {
                        // If passenger, prevent drive by
                        if (ped.IsInVehicle)
                        {
                            if (!ped.IsDriver)
                            {
                                ped.WillDoDrivebys = false;
                            }

                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveMission))
                            {
                                ped.Task.DriveSlowlyTo(ped.CurrentVehicle, World.GetNextPositionOnStreet(this.target.Wanted.LastKnownPosition));
                            }

                            if (!ped.CurrentVehicle.SirenActive)
                            {
                                ped.CurrentVehicle.SirenActive = true;
                            }
                        }
                    }

                    if (action == ETaskChasePedAction.ChaseInHelicopter)
                    {

                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopHelicopter))
                        {
                            TaskCopHelicopter taskCopHelicopter = new TaskCopHelicopter(this.target);
                            taskCopHelicopter.AssignTo(ped, ETaskPriority.MainTask);
                        }
                    }

                    if (action == ETaskChasePedAction.ChaseInVehicle)
                    {
                        // If passenger, prevent drive by
                        if (ped.IsInVehicle && !ped.IsDriver)
                        {
                            ped.WillDoDrivebys = false;
                        }

                        AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                        
                        if (ped.PedData.CurrentChase != null && this.target.IsInVehicle)
                        {
                            // If mode is active
                            if (ped.PedData.CurrentChase.ChaseTactic == EChaseTactic.Active)
                            {
                                // Random chance to play audio when starting to chase
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatPersueInCarSubtask))
                                {
                                    if (lastAction != ETaskChasePedAction.ChaseInVehicle)
                                    {
                                        if (Common.GetRandomBool(0, 2, 1))
                                        {
                                            if (!ped.IsAmbientSpeechPlaying)
                                            {
                                                ped.SayAmbientSpeech("MOVE_IN");
                                            }
                                        }
                                    }
                                }

                                ped.APed.TaskCombatPersueInCarSubtask(this.target);
                            }
                            else
                            {
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveMission))
                                {
                                    // ped.Task.ClearAll();
                                    GTA.Native.Function.Call("TASK_CAR_MISSION", (GTA.Ped)ped, (GTA.Vehicle)ped.CurrentVehicle, (GTA.Vehicle)this.target.CurrentVehicle, 12, 65.0f, (int)3, 4, 20);
                                }
                            }
                        }
                        else
                        {
                            ped.APed.TaskCombatPersueInCarSubtask(this.target);
                        }

                        // Based on the distance to the target, lower maximum possible speed
                        float distance = ped.Position.DistanceTo(this.target.Position);

                        if (distance < 100)
                        {
                            if (this.target.IsInVehicle)
                            {
                                // TODO: Find stopping distance and lower speed of too fast
                                //GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)ped, this.target.CurrentVehicle.Speed);
                            }
                        }

                        // Ensure siren is on
                        if (ped.IsInVehicle())
                        {
                            if (!ped.CurrentVehicle.SirenActive)
                            {
                                ped.CurrentVehicle.SirenActive = true;
                            }
                        }
                    }

                    if (action == ETaskChasePedAction.ChaseOnFoot)
                    {
                        if (this.target.Wanted.IsCuffed)
                        {
                            ped.EnsurePedHasWeapon();

                            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                            {
                                ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopChasePedOnFoot).MakeAbortable(ped);
                            }

                            // Prevent cop from being too close
                            float distance = ped.Position.DistanceTo(this.target.Position);
                            if (distance < 3)
                            {
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                                {
                                    // Retreat task would be quite too fast, so we use flee here
                                    ped.Task.FleeFromChar(this.target, false, 1500);
                                }
                            }
                            else
                            {
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                                {
                                    ped.Task.GoToCharAiming(this.target, 6, 10);
                                }
                            }
                        }
                        else
                        {
                            float distance = ped.Position.DistanceTo(this.target.Position);
                            if (distance < DistanceToStartBusting && (this.target.Wanted.IsStopped || this.target.Wanted.IsBeingArrestedByPlayer))
                            {
                                ped.EnsurePedHasWeapon();

                                // Prevent cop from being too close
                                if (distance < 3)
                                {
                                    if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                                    {
                                        ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopChasePedOnFoot).MakeAbortable(ped);
                                    }

                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                                    {
                                        // Retreat task would be quite too fast, so we use flee here
                                        ped.Task.FleeFromChar(this.target, false, 1500);
                                    }
                                }
                                else
                                {
                                    if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                                    {
                                        TaskCopChasePedOnFoot chaseTask = new TaskCopChasePedOnFoot(this.target);
                                        chaseTask.AssignTo(ped, ETaskPriority.MainTask);
                                    }
                                }
                            }
                            else
                            {
                                /*
                                int nearCops = 0;

                                // Check how many cops are really close to this one
                                if (this.target.PedData.CurrentChase != null)
                                {
                                    foreach (CPed cop in this.target.PedData.CurrentChase.Cops)
                                    {
                                        if (cop != null && cop.Exists())
                                        {
                                            if (cop.Position.DistanceTo2D(ped.Position) < 4)
                                            {
                                                // He's pretty nearby I guess
                                                nearCops++;
                                            }
                                        }
                                    }
                                }

                                //LCPD_First_Response.LCPDFR.GUI.TextHelper.PrintText("near cops: " + nearCops.ToString(), 5000);

                                if (nearCops > 1)
                                {
                                    //ped.SetNextDesiredMoveState(EPedMoveState.Run);
                                    //ped.Task.RunTo(this.target.Position.Around(5.0f), true);
                                    Natives.SetNextDesiredMoveState(EPedMoveState.Sprint);
                                    //ped.Task.GoTo(this.target, Common.GetRandomValue(2, 4), Common.GetRandomValue(2, 4));
                                    if (World.GetNextPositionOnPavement(this.target.Position).DistanceTo(ped.Position) < World.GetNextPositionOnStreet(this.target.Position).DistanceTo(ped.Position))
                                    {
                                        ped.SetNextDesiredMoveState(EPedMoveState.Sprint);
                                        ped.Task.RunTo(World.GetNextPositionOnPavement(this.target.Position));
                                    }
                                    else
                                    {
                                        ped.SetNextDesiredMoveState(EPedMoveState.Sprint);
                                        ped.Task.RunTo(World.GetNextPositionOnStreet(this.target.Position));
                                    }
                                    //GTA.Native.Function.Call("TASK_GO_TO_COORD_ANY_MEANS", ped.Handle, this.target.Position.X, this.target.Position.Y, this.target.Position.Z, 3, 0);
                                    //Game.Console.Print("using modified");
                                }
                                else
                                {
                                    Natives.SetNextDesiredMoveState(EPedMoveState.Sprint);
                                    ped.Task.GoTo(this.target, EPedMoveState.Sprint);
                                    //GTA.Native.Function.Call("TASK_GO_TO_COORD_ANY_MEANS", ped.Handle, this.target.Position.X, this.target.Position.Y, this.target.Position.Z, 3, 0);
                                }

                                //ped.Task.GoToCharAiming(this.target, 6, 10);

                                if (!ped.IsSayingAmbientSpeech())
                                {
                                    if (ped.Model == "M_Y_SWAT")
                                    {
                                        if (Common.GetRandomBool(0, 2, 1)) ped.SayAmbientSpeech("TARGET");
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

                                // Also, we can do some anim stuff here.
                                float velX = ped.Velocity.X;
                                float velY = ped.Velocity.Y;
                                if (velX > 2.5f || velX < -2.5f || velY > 2.5f || velY < -2.5f)
                                {
                                    if (ped.Weapons.CurrentType == ped.Weapons.AnyHandgun)
                                    {
                                        if (!ped.Animation.isPlaying(new AnimationSet("gun@cops"), "pistol_partial_a"))
                                        {
                                            ped.Task.PlayAnimSecondaryUpperBody("pistol_partial_a", "gun@cops", 4.0f, false);
                                        }
                                    }
                                    else if (ped.Weapons.CurrentType == ped.Weapons.AnyShotgun || ped.Weapons.CurrentType == ped.Weapons.AnyAssaultRifle)
                                    {
                                        if (!ped.Animation.isPlaying(new AnimationSet("gun@cops"), "swat_rifle"))
                                        {
                                            ped.Task.PlayAnimSecondaryUpperBody("swat_rifle", "gun@cops", 4.0f, false);
                                        }
                                    }
                                }
                                 * */

                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                                {
                                    TaskCopChasePedOnFoot chaseTask = new TaskCopChasePedOnFoot(this.target);
                                    chaseTask.AssignTo(ped, ETaskPriority.MainTask);
                                }
                            }
                        }
                    }
                    else if (action != ETaskChasePedAction.Tase)
                    {
                        foreach (PedTask task in ped.Intelligence.TaskManager.GetActiveTasks())
                        {
                            if (task != null && task.TaskID == ETaskID.CopChasePedOnFoot)
                            {
                                task.MakeAbortable(ped);
                            }
                        }
                    }


                    if (action == ETaskChasePedAction.DragOutOfVehicle)
                    {
                        // The timer should only run when the drag out of vehicle action is chosen, so we want it to reset when on our first call
                        if (this.dragOutOfVehicleTimer.CanExecute(true))
                        {
                            bool active0 = ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatPullFromCarSubtask);
                            bool active1 = ped.APed.IsTaskActive(0x773);
                            Log.Debug(active0.ToString() + active1.ToString(), this);

                            // Note: For some reason the pull from car subtask is "hidden" before the ped reaches the vehicle
                            // and so we also check for the movement task
                            if ((!ped.APed.IsTaskActive(0x773) && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexControlMovement)) || this.firstAction)
                            {
                                ped.APed.TaskCombatPullFromCarSubtask(this.target);
                            }
                        }
                    }
                    if (action == ETaskChasePedAction.GetIntoNewVehicle)
                    {
                        if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                        {
                            action = ETaskChasePedAction.LookForCriminal;
                            return;
                        }

                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                        {
                            // Check if there is a cop car in range without a driver
                            CVehicle closestVehicle = ped.Intelligence.GetClosestVehicle(EVehicleSearchCriteria.CopOnly | EVehicleSearchCriteria.NoDriverOnly | EVehicleSearchCriteria.NoPlayersLastVehicle, VehicleScanDistance);
                            if (closestVehicle == null || closestVehicle.HasDriver)
                            {
                                // Look for a civil vehicle
                                closestVehicle = ped.Intelligence.GetClosestVehicle(EVehicleSearchCriteria.NoDriverOnly | EVehicleSearchCriteria.StoppedOnly | EVehicleSearchCriteria.NoPlayersLastVehicle, VehicleScanDistance);
                            }
                            if (closestVehicle != null && !closestVehicle.HasDriver)
                            {
                                this.noVehicleAround = false;
                                TaskGetInVehicle taskGetInVehicle = new TaskGetInVehicle(closestVehicle, false, VehicleScanDistance);
                                taskGetInVehicle.EnterStyle = EPedMoveState.Sprint;
                                taskGetInVehicle.AssignTo(ped, ETaskPriority.MainTask);
                            }
                            else
                            {
                                this.noVehicleAround = true;
                            }
                        }
                    }
                    if (action == ETaskChasePedAction.Kill)
                    {
                        // Cops won't report being attacked if this stage is active
                        if (ped.PedData.ReportBeingAttacked) ped.PedData.ReportBeingAttacked = false;
                        
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombat))
                        {
                            if (ped.IsInVehicle && ped.IsDriver)
                            {
                                ped.CurrentVehicle.SirenActive = true;
                            }

                            // If NOOSE boat or NOOSE rear passenger in Stockade, try drive by
                            if (ped.IsInVehicle && !ped.IsDriver)
                            {
                                if (ped.PedSubGroup == EPedSubGroup.Noose)
                                {
                                    if (ped.GetSeatInVehicle() == VehicleSeat.LeftRear || ped.GetSeatInVehicle() == VehicleSeat.RightRear)
                                    {
                                        if (ped.CurrentVehicle.Model == "DINGHY" || ped.CurrentVehicle.Model == "NSTOCKADE")
                                        {
                                            ped.WillDoDrivebys = true;
                                        }
                                    }
                                }
                            }

                            if (ped.IsInVehicle && ped.CurrentVehicle.Model.IsHelicopter)
                            {
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleNewGangDriveBy))
                                {
                                    // Equip with right weapon (m4 or advanced mg if TBOGT)
                                    if (Main.IsTbogt)
                                    {
                                        ped.PedData.DefaultWeapon = Weapon.TBOGT_AdvancedMG;
                                    }
                                    else
                                    {
                                        ped.PedData.DefaultWeapon = Weapon.Rifle_M4;
                                    }

                                    ped.EnsurePedHasWeapon();
                                    ped.SetWeapon(ped.PedData.DefaultWeapon);

                                    bool fromRightHandSeat = true;
                                    if (ped.GetSeatInVehicle() == VehicleSeat.LeftRear)
                                    {
                                        fromRightHandSeat = false;
                                    }

                                    ped.Task.DriveBy(this.target, 0, 0.0f, 0.0f, 0.0f, 500.0f, 8, Convert.ToInt32(fromRightHandSeat), 250);
                                }
                            }
                            else
                            {
                                // If ped is roadblock ped use shooting task instead for longer range
                                if (ped.PedData.Flags.HasFlag(EPedFlags.IsRoadblockPed))
                                {
                                    if (ped.HasSpottedChar(this.target))
                                    {
                                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGun))
                                        {
                                            ped.SenseRange = 400f;
                                            ped.Task.ShootAt(this.target, ShootMode.Continuous, 5000);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombat) || !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGun))
                                    {
                                        ped.Task.ClearAll();
                                        ped.SenseRange = 400.0f;
                                        ped.Task.FightAgainst(this.target, 10000);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Else, if the state is not kill, make it so they can report being attacked.
                        if (!ped.PedData.ReportBeingAttacked) ped.PedData.ReportBeingAttacked = true;
                    }


                    if (action == ETaskChasePedAction.LeaveVehicle)
                    {
                        ped.LeaveVehicle();
                    }
                    if (action == ETaskChasePedAction.LookForCriminal)
                    {
                        // TODO: Suspect lost speech?
                        // If on foot, wander around last known position
                        if (ped.IsInVehicle)
                        {
                            // If in heli
                            if (ped.CurrentVehicle.Model.IsHelicopter)
                            {
                                internalTask = EInternalTaskID.CTaskSimpleNone;
                                action = ETaskChasePedAction.ChaseInHelicopter;
                            }
                            else
                            {
                                if (ped.Position.DistanceTo(this.target.Wanted.LastKnownPosition) < 40.0f)
                                {
                                    if (this.target.Wanted.LastKnownInVehicle)
                                    {
                                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle) && ped.CurrentVehicle.HasDriver)
                                        {
                                            ped.Task.ClearAll();
                                            TaskCopSearchForPedInVehicle searchTask = new TaskCopSearchForPedInVehicle(this.target, this.target.Wanted.LastKnownPosition);
                                            searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                            ped.Debug = "Assigned vehicle task.";
                                        }
                                    }
                                    else
                                    {
                                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle) && !ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                                        {
                                            // TextHelper.PrintText("On Foot: " + this.target.Wanted.OfficersSearchingOnFoot + " | In Vehicle: " + this.target.Wanted.OfficersSearchingInAVehicle + " | Total: " + this.target.Wanted.OfficersChasing, 3000);
                                            if (this.target.Wanted.OfficersSearchingOnFoot < this.target.Wanted.OfficersChasing / 1.75)
                                            {
                                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                                                {
                                                    TaskCopSearchForPedOnFoot searchTask = new TaskCopSearchForPedOnFoot(this.target, this.target.Wanted.LastKnownPosition);
                                                    searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                                    ped.Debug = "Assigned foot task.";
                                                }
                                            }
                                            else
                                            {
                                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle) && ped.CurrentVehicle.HasDriver)
                                                {
                                                    ped.Task.ClearAll();
                                                    TaskCopSearchForPedInVehicle searchTask = new TaskCopSearchForPedInVehicle(this.target, this.target.Wanted.LastKnownPosition);
                                                    searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                                    ped.Debug = "Assigned vehicle task because too many.";
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (ped.IsInVehicle && ped.CurrentVehicle.HasDriver)
                                    {
                                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle))
                                        {
                                            TaskCopSearchForPedInVehicle searchTask = new TaskCopSearchForPedInVehicle(this.target, this.target.Wanted.LastKnownPosition);
                                            searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                            ped.Debug = "Assigned vehicle task because far away and in vehicle.";
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle))
                            {
                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                                {
                                    if (this.target.Wanted.OfficersSearchingOnFoot < this.target.Wanted.OfficersChasing / 1.75)
                                    {
                                        TaskCopSearchForPedOnFoot searchTask = new TaskCopSearchForPedOnFoot(this.target, this.target.Wanted.LastKnownPosition);
                                        searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                        ped.Debug = "Assigned foot task because on foot";
                                    }
                                }
                            }
                        }
                        
                        /*
                        // If the suspect is lost, we need to set up the search process appropriately.
                        // If last seen in a vehicle, cops will mostly search with vehicles.
                        // If last seen on foot, cops will search with both vehicles and officers on foot.

                        if (this.target.Wanted.LastKnownOnFoot)
                        {
                            if (this.target.Wanted.OfficersSearchingOnFoot < this.target.Wanted.OfficersChasing / 1.5)
                            {
                                // Too few cops searching on foot, so assign this one if possible 
                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                                {
                                    TaskCopSearchForPedOnFoot searchTask = new TaskCopSearchForPedOnFoot(this.target, this.target.Wanted.LastKnownPosition);
                                    searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                    ped.Debug = "Assigned foot task because too few";
                                }
                            }
                            else
                            {
                                if (ped.IsInVehicle && ped.CurrentVehicle.IsDriveable)
                                {
                                    if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle))
                                    {
                                        TaskCopSearchForPedInVehicle searchTask = new TaskCopSearchForPedInVehicle(this.target, this.target.Wanted.LastKnownPosition);
                                        searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                        ped.Debug = "Assigned vehicle task because too many";
                                    }
                                }
                            }
                        }
                        else if (this.target.Wanted.LastKnownInVehicle)
                        {
                            if (this.target.Wanted.OfficersSearchingInAVehicle < this.target.Wanted.OfficersChasing / 1.25)
                            {
                                // Too few cops searching in a vehicle, so assign this one
                                if (ped.IsInVehicle && ped.CurrentVehicle.IsDriveable)
                                {
                                    if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle))
                                    {
                                        TaskCopSearchForPedInVehicle searchTask = new TaskCopSearchForPedInVehicle(this.target, this.target.Wanted.LastKnownPosition);
                                        searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                        ped.Debug = "Assigned vehicle task because too few";
                                    }
                                }
                            }
                            else
                            {
                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                                {
                                    TaskCopSearchForPedOnFoot searchTask = new TaskCopSearchForPedOnFoot(this.target, this.target.Wanted.LastKnownPosition);
                                    searchTask.AssignTo(ped, ETaskPriority.MainTask);
                                    ped.Debug = "Assigned foot task because too many";
                                }
                            }
                        }

                           */                    
                        /*
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatInvestigateSubtask))
                        {
                            ped.Task.ClearAll();
                            ped.Task.FightAgainst(this.target);
                            ped.APed.TaskCombatInvestigateSubtask(this.target.APed);
                            if (!ped.IsSayingAmbientSpeech()) ped.SayAmbientSpeech("SPLIT_UP_AND_SEARCH");
                        }
                        */

                        /*
                        timesincelast += 1;

                       
                        if (true)
                        {
                            if (timesincelast > 10)
                            {
                                if (ped.IsInVehicle())
                                {
                                    Game.Console.Print("applied vehicle tasks");
                                    Vector3 lastPosition = this.target.Wanted.LastKnownPosition;
                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSearchForPedOnFoot) || !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimplePlayUpperCombatAnim))
                                    {
                                        //ped.Task.FightAgainst(this.target);
                                        //AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                                        ped.APed.TaskCombatInvestigateSubtask(this.target.APed);
                                        ped.APed.TaskSearchForPedOnFoot(this.target.APed);
                                        //ped.APed.TaskCombatInvestigateSubtask(CPlayer.LocalPlayer.Ped.APed);
                                        //ped.APed.TaskSearchForPedOnFoot(CPlayer.LocalPlayer.Ped.APed);
                                        if (!ped.IsSayingAmbientSpeech()) ped.SayAmbientSpeech("SPLIT_UP_AND_SEARCH");
                                        timesincelast = 0;
                                    }
                                }
                                else
                                {
                                    Game.Console.Print("applied foot tasks");
                                    Vector3 lastPosition = this.target.Wanted.LastKnownPosition;
                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSearchForPedOnFoot) || !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatAdvanceSubtask))
                                    {
                                        //ped.Task.FightAgainst(this.target);
                                        //AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                                        ped.APed.TaskCombatInvestigateSubtask(this.target.APed);
                                        ped.APed.TaskSearchForPedOnFoot(this.target.APed);
                                        //ped.APed.TaskCombatInvestigateSubtask(CPlayer.LocalPlayer.Ped.APed);
                                        //ped.APed.TaskSearchForPedOnFoot(CPlayer.LocalPlayer.Ped.APed);
                                        if (!ped.IsSayingAmbientSpeech()) ped.SayAmbientSpeech("SPLIT_UP_AND_SEARCH");
                                        timesincelast = 0;
                                    }
                                }
                            }
                          

                            /*
                            if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.Wander))
                            {
                                TaskWander taskWander = new TaskWander(30000);
                                taskWander.AssignTo(ped, ETaskPriority.MainTask);
                            }
                             
                        }
                        else
                        {
                            // Skip this for heli units
                            if (ped.CurrentVehicle.Model.IsHelicopter)
                            {
                                return;
                            }

                            // If driver, cruise
                            if (ped.IsDriver)
                            {
                                if (ped.Position.DistanceTo2D(this.target.Wanted.LastKnownPosition) > 30)
                                {
                                    // Get close to last position and then wander
                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexDriveToPoint))
                                    {
                                        ped.Task.DriveTo(this.target.Wanted.LastKnownPosition, 50f, false, true);
                                    }
                                }
                                // Close to last known position, wander around
                                else if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveWander))
                                {
                                    ped.Task.CruiseWithVehicle(ped.CurrentVehicle, 10, true);

                                    // Disable siren
                                    ped.CurrentVehicle.SirenActive = false;
                                }
                            }
                        }
                         * */
                    }
                    else
                    {
                        foreach (PedTask task in ped.Intelligence.TaskManager.GetActiveTasks())
                        {
                            if (task != null && (task.TaskID == ETaskID.CopSearchForPedOnFoot || task.TaskID == ETaskID.CopSearchForPedInVehicle))
                            {
                                task.MakeAbortable(ped);
                            }
                        }
                    }

                    if (action == ETaskChasePedAction.NegotiateToDropWeapon)
                    {
                        /* Removed for now
                        // Small chance cop will tase
                        if (!this.target.PedData.HasBeenTased)
                        {
                            if (Common.GetRandomBool(0, 15, 1))
                            {
                                this.target.DropCurrentWeapon();
                                this.target.ForceRagdoll(5000, false);
                                this.target.ApplyForceRelative(new Vector3(0, -5, 0));
                                this.target.PedData.HasBeenTased = true;
                            }
                        }
                         * */

                        // Keep aiming
                        if (!ped.IsAiming)
                        {
                            ped.Task.GoToCharAiming(this.target, 4, 10);
                        }

                    }
                    if (action == ETaskChasePedAction.NoLongerNeeded)
                    {
                        MakeAbortable(ped);
                    }
                    if (action == ETaskChasePedAction.RequestVehicle)
                    {
                        PedDataCop pedDataCop = ped.PedData as PedDataCop;
                        pedDataCop.NeedsVehicleForChase = true;
                        
                        // TODO: Create task that also changes the ChaseState here

                        // Not sure if this wouldn't be overkill for chases due to all the officers and vehicles, so for now we make the cop wander
                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.Wander))
                        {
                            TaskWander taskWander = new TaskWander(5000);
                            taskWander.AssignTo(ped, ETaskPriority.MainTask);
                        }
                    }
                    if (action == ETaskChasePedAction.WaitAtRoadblock)
                    {
                        // Wait and aim at suspect if getting closer
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun) 
                            && ped.Position.DistanceTo2D(this.target.Position) < 100)
                        {
                            // Compute direction to aim

                            Vector3 direction = this.target.Position - ped.Position;
                            direction.Normalize();

                            Vector3 aimPos = ped.Position + direction;
                            ped.Task.AimAt(aimPos, 10000);
                        }
                    }

                    this.firstAction = false;
                }
            }
        }

        private ETaskChasePedAction GetNextAction(CPed ped)
        {
            float distance = ped.Position.DistanceTo(target.Position);
            bool suspectInVehicle = target.IsInVehicle();
            bool weInVehicle = ped.IsInVehicle();

            if (this.target.Wanted.HasBeenArrested)
            {
                // Only free cop if bust task is already done
                //if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
                //{
                    return ETaskChasePedAction.NoLongerNeeded;
                //}
            }

            // If suspect is being arrested, only continue assisting if there is no other suspect
            if (this.target.Wanted.IsBeingArrested && this.target.Wanted.IsBeingArrestedByPlayer)
            {
                // If there are other criminals that can be chased, abort chase for this one and chase another
                if (ped.PedData.CurrentChase != null)
                {
                    if (!ped.PedData.CurrentChase.IsOnlyAvailableSuspect(this.target))
                    {
                        return ETaskChasePedAction.NoLongerNeeded;
                    }
                }
            }

            // If visual on suspect is lost, only search for the suspect if there is no other suspect
            if (this.target.Wanted.VisualLost)
            {
                // If there are other criminals that can be chased, abort chase for this one and chase another
                if (ped.PedData.CurrentChase != null)
                {
                    if (!ped.PedData.CurrentChase.IsOnlyAvailableSuspect(this.target))
                    {
                        return ETaskChasePedAction.NoLongerNeeded;
                    }
                }

                // If in a heli, chase in heli
                if (weInVehicle)
                {
                    // If in heli
                    if (ped.CurrentVehicle.Model.IsHelicopter)
                    {
                        internalTask = EInternalTaskID.CTaskSimpleNone;
                        return ETaskChasePedAction.ChaseInHelicopter;
                    }
                    else
                    {
                        if (ped.Position.DistanceTo(this.target.Wanted.LastKnownPosition) > 40.0f && !ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle))
                        {
                            internalTask = EInternalTaskID.CTaskComplexCarDriveMission;
                            return ETaskChasePedAction.DriveToLastKnownPosition;
                        }
                        else
                        {
                            return ETaskChasePedAction.LookForCriminal;
                        }
                    }
                }
                else
                {
                    /*
                    if (!ped.IsGettingIntoAVehicle)
                    {
                        if (this.target.Wanted.LastKnownInVehicle && this.target.Wanted.OfficersSearchingInAVehicle < this.target.Wanted.OfficersChasing / 1.25)
                        {
                            if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                            {
                                ped.Debug = "told to get in vehicle because too few";
                                internalTask = EInternalTaskID.CTaskComplexNewGetInVehicle;
                                return ETaskChasePedAction.GetIntoNewVehicle;
                            }
                        }
                        else if (this.target.Wanted.LastKnownOnFoot && this.target.Wanted.OfficersSearchingOnFoot >= this.target.Wanted.OfficersChasing / 1.5)
                        {
                            if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
                            {
                                ped.Debug = "told to get in vehicle because too many";
                                internalTask = EInternalTaskID.CTaskComplexNewGetInVehicle;
                                return ETaskChasePedAction.GetIntoNewVehicle;
                            }
                        }
                     

                        return ETaskChasePedAction.LookForCriminal;
                    }
                    */

                    return ETaskChasePedAction.LookForCriminal;
                }
            }

            if (this.target.Wanted.WeaponUsed || this.fightSuspect || (ped.PedData as PedDataCop).ForceKilling)
            {
                // Only continue when not in a helicopter as driver
                if (!weInVehicle || (weInVehicle && !ped.CurrentVehicle.Model.IsHelicopter) || (weInVehicle && ped.CurrentVehicle.Model.IsHelicopter && !ped.IsDriver))
                {
                    // If being arrested by player or deciding at the moment, don't kill
                    if (this.target.Wanted.IsBeingArrestedByPlayer  || this.target.Wanted.IsStopped || this.target.Wanted.IsDeciding)
                    {
                    }
                    else
                    {
                        if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                        {
                            ped.Intelligence.TaskManager.Abort(ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopChasePedOnFoot));
                        }
                        return ETaskChasePedAction.Kill;
                    }
                }
            }
       
            // If ped is assigned to roadblock, no advanced behavior until a suspect is close
            if (ped.PedData.Flags.HasFlag(EPedFlags.IsRoadblockPed))
            {
                if (distance > 20)
                {
                    return ETaskChasePedAction.WaitAtRoadblock;
                }
                else
                {
                    // Unset flag
                    ped.PedData.Flags &= ~EPedFlags.IsRoadblockPed;
                }
            }

            // If our suspect is in a vehicle and we are not
            if (suspectInVehicle && !weInVehicle)
            {
                // If suspect is moving too fast or is too far away, try to get a vehicle, prefer cop vehicles
                if (target.CurrentVehicle.Speed > SpeedToStartLookingForAVehicle || distance > DistanceToStartLookingForAVehicle)
                {
                    // If there's a vehicle around
                    if (!this.noVehicleAround)
                    {
                        internalTask = EInternalTaskID.CTaskComplexNewGetInVehicle;
                        return ETaskChasePedAction.GetIntoNewVehicle;
                    }
                    else
                    {
                        // TODO: Request a unit to pick the cop up
                        return ETaskChasePedAction.RequestVehicle;
                    }
                }
                // Neither moving fast, nor too far away, so either run to the suspect is further away or drag him out of the vehicle
                else if (distance < DistanceToStartDragging && this.target.CurrentVehicle.Speed < 3)
                {
                    internalTask = EInternalTaskID.CTaskComplexCombatPullFromCarSubtask;
                    return ETaskChasePedAction.DragOutOfVehicle;
                }
                else
                {
                    internalTask = EInternalTaskID.CTaskComplexControlMovement;
                    return ETaskChasePedAction.ChaseOnFoot;
                }
            }

            // If we are in a vehicle
            if (weInVehicle)
            {
                // If in heli
                if (ped.CurrentVehicle.Model.IsHelicopter)
                {
                    internalTask = EInternalTaskID.CTaskSimpleNone;
                    return ETaskChasePedAction.ChaseInHelicopter;
                }

                // If suspect is not in a vehicle
                if (!suspectInVehicle)
                {
                    // Drive close to him if too far away for exiting the vehicle
                    if (distance > DistanceToLeaveVehicle) 
                    {
                        internalTask = EInternalTaskID.CTaskComplexCombatPersueInCarSubtask;
                        return ETaskChasePedAction.ChaseInVehicle;
                    }
                    // Close to suspect, exit vehicle
                    else
                    {
                        internalTask = EInternalTaskID.CTaskComplexNewExitVehicle;
                        return ETaskChasePedAction.LeaveVehicle;
                    }
                }
                // If suspect is in a vehicle
                if (suspectInVehicle)
                {
                    // Chase in vehicle
                    // If enemy target is stopped, exit
                    if (this.target.CurrentVehicle.IsStuck(2000) && ped.Position.DistanceTo(this.target.Position) < DistanceToLeaveVehicle && ped.PedData.CurrentChase != null &&
                        ped.PedData.CurrentChase.ChaseTactic == EChaseTactic.Passive)
                    {
                        internalTask = EInternalTaskID.CTaskComplexNewExitVehicle;
                        return ETaskChasePedAction.LeaveVehicle;
                    }
                    else
                    {
                        internalTask = EInternalTaskID.CTaskComplexCombatPersueInCarSubtask;
                        return ETaskChasePedAction.ChaseInVehicle;
                    }
                }

            }

            // If both are on foot
            if (!suspectInVehicle && !weInVehicle)
            {
                bool armed = this.target.IsArmed();
                // If suspect has pulled out a gun, ask him to drop
                if (this.target.IsArmed() && distance < DistanceToStartShooting)
                {
                    // If not already done, ask to drop weapon
                    if (!this.askedToDropWeapon)
                    {
                        // Only do so if we have visual
                        if (this.canSeeSuspect)
                        {
                            this.internalTask = EInternalTaskID.CTaskComplexGun;
                            return ETaskChasePedAction.AskToDropWeapon;
                        }
                    }
                    else if (this.target.Wanted.IsDeciding && this.target.Weapons.Current.Slot == WeaponSlot.Melee)
                    {
                        // While ped is deciding, process negotiate task, so some speeches and small chance of tasing
                        this.internalTask = EInternalTaskID.CTaskSimpleDoNothing;
                        return ETaskChasePedAction.NegotiateToDropWeapon;
                    }

                    // If drop weapon resisted, shoot. TODO: Only make aggresive cops shoot, passive cops should wait for the suspect to start shooting
                    if (this.target.Wanted.ResistedDropWeapon && !this.target.Wanted.IsDeciding)
                    {
                        ped.Task.ClearAll();
                        return ETaskChasePedAction.Kill;
                    }

                    // Rather wait until suspect start's shooting
                    return ETaskChasePedAction.ChaseOnFoot;
                }

                // If resisted arrest
                if (this.target.Wanted.ResistedArrest)
                {
                    // Suspected decided to fight, so we won't ask him to drop the weapon
                    this.askedToDropWeapon = true;
                    return ETaskChasePedAction.Unknown;
                }

                // Only bust if neither the target nor the cop are getting into a vehicle, the bust task could fuck it up
                if ((distance < DistanceToStartBusting || distance < DistanceToStartTasing) && !this.target.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle)
                && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle) && this.canSeeSuspect && 
                !this.target.Wanted.IsCuffed)
                {
                    // When the suspect has surrendered already, only allow continue arresting if there is no combat in the area
                    if (this.target.Wanted.Surrendered)
                    {
                        if (distance < DistanceToStartBusting)
                        {
                            if (this.timerNearbyCombats.CanExecute())
                            {
                                if (CPed.IsPedInCombatInArea(this.target.Position, 25f))
                                {
                                    // There is a combat, so free this cop from chasing the current suspect
                                    return ETaskChasePedAction.NoLongerNeeded;
                                }
                            }

                            this.internalTask = EInternalTaskID.CTaskComplexCombatBustPed;
                            return ETaskChasePedAction.Bust;
                        }
                        else
                        {
                            this.internalTask = EInternalTaskID.CTaskSimpleNone;
                            return ETaskChasePedAction.ChaseOnFoot;
                        }
                    }
                    else
                    {
                        if (this.target.PedData.HasBeenTased)
                        {
                            // If the target has been tased, allow busting.
                            if (distance < DistanceToStartBusting)
                            {
                                this.internalTask = EInternalTaskID.CTaskComplexCombatBustPed;
                                return ETaskChasePedAction.Bust;
                            }
                            else
                            {
                                this.internalTask = EInternalTaskID.CTaskSimpleNone;
                                return ETaskChasePedAction.ChaseOnFoot;
                            }
                        }
                        else
                        {
                            if (distance < DistanceToStartTasing)
                            {
                                // If the target hasn't been tased, manage the cops and get them to do it

                                CPed[] copsInArea = CPed.SortByDistanceToPosition(Pools.PedPool.GetAll(), this.target.Position);

                                if (this.target.Wanted.OfficersTasing > 2)
                                {
                                    foreach (CPed cop in copsInArea)
                                    {
                                        if (cop.Intelligence.TaskManager.IsTaskActive(ETaskID.CopTasePed) && cop != ped)
                                        {
                                            // If cop is tasing then check if this task ped is closer
                                            if (ped.Position.DistanceTo(this.target.Position) - 2 < cop.Position.DistanceTo(this.target.Position))
                                            {
                                                Log.Debug("Process: Killed tase task", this);

                                                // Cancel the taser task for this cop and apply it to our task ped instead as he's closer.
                                                cop.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopTasePed).MakeAbortable(cop);

                                                this.internalTask = EInternalTaskID.CTaskComplexSeekEntityAiming;
                                                return ETaskChasePedAction.Tase;
                                            }
                                        }
                                        else
                                        {
                                            // The other cop is closer, so don't apply the task for this ped.
                                            this.internalTask = EInternalTaskID.CTaskSimpleNone;
                                            return ETaskChasePedAction.ChaseOnFoot;
                                        }
                                    }
                                }
                                else
                                {
                                    // There's 2 or less officers tasing, so let this one tase as well. 
                                    this.internalTask = EInternalTaskID.CTaskComplexSeekEntityAiming;
                                    return ETaskChasePedAction.Tase;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If target is already cuffed, reduce number of cops
                    if (this.target.Wanted.IsCuffed)
                    {
                        if (this.target.Wanted.OfficersChasing > 3)
                        {
                            // Only if there are enough close cops
                            int count = this.target.Intelligence.GetPedsAround(10f, EPedSearchCriteria.CopsOnly | EPedSearchCriteria.HaveOwner | EPedSearchCriteria.NotAvailable).Count(cop => cop.Exists() && cop.IsAliveAndWell && cop.Intelligence.TaskManager.IsTaskActive(ETaskID.ChasePed));
                            if (count >= 3)
                            {
                                return ETaskChasePedAction.NoLongerNeeded;
                            }
                        }
                    }

                    internalTask = EInternalTaskID.CTaskComplexControlMovement;

                    // If very close to ped, we want the cop to run away, so we set the internal task to null so the ChaseOnFoot action will be processed again
                    if (distance < 3)
                    {
                        this.internalTask = EInternalTaskID.CTaskSimpleNone;
                    }

                    return ETaskChasePedAction.ChaseOnFoot;
                }
            }
            return ETaskChasePedAction.Unknown;
        }

        public override string ComponentName
        {
            get { return "TaskCopChasePed"; }
        }
    }

    internal enum ETaskChasePedAction
    {
        AskToDropWeapon,
        Bust,
        ChaseInHelicopter,
        ChaseInVehicle,
        ChaseOnFoot,
        DragOutOfVehicle,
        DriveToLastKnownPosition,
        GetIntoNewVehicle,
        Gunpoint,
        Kill,
        LeaveVehicle,
        LookForCriminal,
        NegotiateToDropWeapon,
        NoLongerNeeded,
        RequestVehicle,
        Tase,
        Unknown,
        WaitAtRoadblock,
    }
}
