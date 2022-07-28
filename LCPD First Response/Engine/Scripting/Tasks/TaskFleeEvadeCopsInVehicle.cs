namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Flee task in vehicle.
    /// </summary>
    internal class TaskFleeEvadeCopsInVehicle : PedTask
    {
        /// <summary>
        /// Whether the driver decided to stop.
        /// </summary>
        private bool decidedToStop;

        /// <summary>
        /// Whether the ped is shuffling seats.
        /// </summary>
        private bool isShufflingSeats;

        /// <summary>
        /// Whether the vehicle is stuck.
        /// </summary>
        private bool isStuck;

        /// <summary>
        /// The timer used to re-new the flee task when vehicle is stuck to get a new route.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// The timer used to wait before the retreat task is started.
        /// </summary>
        private NonAutomaticTimer startTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskFleeEvadeCopsInVehicle"/> class.
        /// </summary>
        public TaskFleeEvadeCopsInVehicle() : base(ETaskID.FleeEvadeCopsInVehicle)
        {
            this.timer = new NonAutomaticTimer(4000);
            this.startTimer = new NonAutomaticTimer(5000, ETimerOptions.OneTimeReturnTrue);
        }

        /// <summary>
        /// Gets or sets a value indicating whether weapons are allowed.
        /// </summary>
        public bool AllowWeapons { get; set; }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskFleeEvadeCopsInVehicle"; }
        }

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

            // Fire event that criminal is in vehicle
            new EventCriminalEnteredVehicle(ped);

            if (!ped.PedData.Flags.HasFlag(EPedFlags.PlayerDebug))
            {
                // Flee using basic task, to prevent the infamous "U-turn" the retreat task often uses and which most likely makes the driver crash
                ped.Task.FleeFromCharAnyMeans(CPlayer.LocalPlayer.Ped, 500.0f, 999999, 1, 1, 1, 100.0f);
            }
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            // If no longer in vehicle, terminate task
            if (!ped.IsSittingInVehicle())
            {
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                {
                    new EventCriminalLeftVehicle(ped);

                    // If retreat task is still running, kill to prevent crashes
                    if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask))
                    {
                        this.MakeAbortable(ped);
                    }
                    else
                    {
                        SetTaskAsDone();
                    }
                }
            }
            // Flee in vehicle
            else
            {
                if (ped.PedData.Flags.HasFlag(EPedFlags.PlayerDebug))
                {
                    return;
                }

                // If still getting into vehicle, return
                if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle))
                {
                    return;
                }

                // If visual is lost, cruise around
                if (ped.Wanted.VisualLost)
                {
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveWander))
                    {
                        Log.Debug("Process: Visual lost, cruising around", this);

                        ped.Task.CruiseWithVehicle(ped.CurrentVehicle, 15f, true);
                    }

                    return;
                }

                // Random chance to stop and leave vehicle, increased the more the vehicle is damaged
                int surrenderChance = Common.GetRandomValue(0, 4000 + (int)ped.CurrentVehicle.EngineHealth*2);

                // Increased chance for bursted tire
                if (ped.CurrentVehicle.IsTireBurst(VehicleWheel.FrontLeft) || ped.CurrentVehicle.IsTireBurst(VehicleWheel.FrontRight))
                {
                    surrenderChance = Common.GetRandomValue(0, 2500);
                }

                // Higher chance for boats. TODO: Make it customizable via Chase class.
                if (ped.CurrentVehicle.Model.IsBoat)
                {
                    if (ped.CurrentVehicle.Health < 750)
                    {
                        surrenderChance = Common.GetRandomValue(0, 1000 + (ped.CurrentVehicle.Health * 2));
                    }
                }

                if (surrenderChance == 1 && ped.CurrentVehicle.Speed < 5)
                {
                    Log.Debug("Process: Driver decided to stop vehicle", this);
                    this.decidedToStop = true;
                }

                if (this.decidedToStop)
                {
                    if (ped.CurrentVehicle.Speed > 3)
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                        {
                            ped.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 3000);
                        }
                    }
                    else
                    {
                        if (!ped.IsGettingOutOfAVehicle && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                        {
                            ped.Intelligence.AddVehicleToBlacklist(ped.CurrentVehicle, 20000);
                            ped.LeaveVehicle();
                        }
                    }

                    return;
                }

                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask) && this.startTimer.CanExecute(true))
                {
                    //Natives.SetDriveTaskCruiseSpeed(_suspect1, 55f);
                    ped.WillLeaveCarInCombat = false; //Sets 0x800000
                    //TypeConverter.ConvertToAPed(_suspect1).TaskCombatRetreatSubtask(TypeConverter.ConvertToAPed(Player.Character));
                    ped.CurrentVehicle.CanGoAgainstTraffic = true;
                    //Natives.TaskFleeCharAnyMeans(ped, Main.Player.Ped, 500.0f, 999999, 0, 0, 0, 100.0f);
                    //ped.Task.FleeFromCharAnyMeans(Main.Player.Ped, 500.0f, 999999, 0, 0, 0, 100.0f);

                    // If driver, retreat
                    if (ped.IsDriver)
                    {
                        ped.Task.ClearAll();
                        ped.Task.AlwaysKeepTask = true;
                        ped.APed.TaskCombatRetreatSubtask(CPlayer.LocalPlayer.Ped.APed);
                    }
                }

                // Passengers can return fire if there is a bullet in the area or if forced to
                if (ped.PedData.CurrentChase != null && this.AllowWeapons &&
                    !ped.IsDriver && (ped.PedData.CurrentChase.ForceSuspectsToFight || Natives.IsBulletInArea(ped.Position, 30f)))
                {
                    if (ped.CurrentVehicle.HasDriver)
                    {
                        if (ped.AreEnemiesAround(30))
                        {
                            ped.WillDoDrivebys = true;
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombat))
                            {
                                ped.EnsurePedHasWeapon();
                                ped.Task.ClearAll();

                                // Get target
                                CPed target = ped.Intelligence.GetClosestPed(EPedSearchCriteria.CopsOnly | EPedSearchCriteria.Player, 30f);
                                if (target != null && target.Exists())
                                {
                                    ped.Task.FightAgainst(target, 10000);
                                }
                            }
                        }
                    }
                }
                else
                {
                    ped.WillDoDrivebys = false;
                }

                if (ped.CurrentVehicle.IsStuck(5000))
                {
                    // This timer should only run when vehicle is stuck, so we reset the timer if calling for the first time
                    if (this.timer.CanExecute(true))
                    {
                        if (!this.isStuck)
                        {
                            ped.Task.ClearAll();
                            ped.APed.TaskCombatRetreatSubtask(CPlayer.LocalPlayer.Ped.APed);
                            this.timer = new NonAutomaticTimer(4000);
                        }
                        else
                        {
                            // If timer is executed and vehicle is already stuck, make driver exit
                            ped.Intelligence.AddVehicleToBlacklist(ped.CurrentVehicle, 20000);
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                            {
                                ped.Task.LeaveVehicle();
                            }
                        }

                        this.isStuck = true;
                    }
                }

                if (ped.CurrentVehicle.IsStuck(10000))
                {
                    ped.Intelligence.AddVehicleToBlacklist(ped.CurrentVehicle, 20000);
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                    {
                        ped.Task.LeaveVehicle();
                    }
                }

                // Check for speeding
                if (ped.Wanted.OfficersVisual > 0)
                {
                    if (ped.CurrentVehicle.Speed > 28.75f)
                    {
                        if (!ped.PedData.Speeding)
                        {
                            //Game.Console.Print("speeding event fired");
                            new EventCriminalSpeeding(ped);
                        }
                    }
                    else
                    {
                        if (ped.PedData.Speeding)
                        {
                            ped.PedData.Speeding = false;
                        }
                    }
                }

                // Exit vehicle if not driveable (broken, in water etc.). Probably not needed, as the driver does it on his own
                // If driver is dead, exit
                if (!ped.IsDriver)
                {
                    if (!ped.CurrentVehicle.HasDriver && !this.isShufflingSeats)
                    {
                        if (ped.CurrentVehicle.IsDriveable)
                        {
                            // If in right front seat, change to driver seat
                            if (ped.GetSeatInVehicle() == VehicleSeat.RightFront)
                            {
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexShuffleBetweenSeats))
                                {
                                    ped.Task.ShuffleToNextCarSeat(ped.CurrentVehicle);
                                    this.isShufflingSeats = true;
                                }
                            }
                            else
                            {
                                // If not, leave
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                                {
                                    ped.Task.LeaveVehicle();
                                }
                            }
                        }
                        else
                        {
                            // If not driveable, leave
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                            {
                                ped.Task.LeaveVehicle();
                            }
                        }
                    }
                    else
                    {
                        if (ped.CurrentVehicle.HasDriver && !ped.CurrentVehicle.Driver.IsAliveAndWell)
                        {
                            // Check if passenger could shuffle seats and continue driving
                            if (ped.CurrentVehicle.IsDriveable)
                            {
                                if (ped.GetSeatInVehicle() == VehicleSeat.RightFront)
                                {
                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexShuffleBetweenSeats))
                                    {
                                        ped.Task.ShuffleToNextCarSeat(ped.CurrentVehicle);
                                        this.isShufflingSeats = true;
                                    }
                                }
                            }
                            else
                            {
                                // Leave vehicle because vehicle is broken
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                                {
                                    ped.Task.LeaveVehicle();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
