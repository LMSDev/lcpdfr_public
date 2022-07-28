using GTA;

namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Timers;

    internal class TaskFleeEvadeCopsOnFoot : PedTask
    {
        private const float CopScanDistance = 40;
        private const float VehicleScanRange = 15;

        private bool allowVehicles;
        private bool allowTakingHostages;
        private bool allowWeapons;
        private CPed fleeingFrom;

        private bool hasWeapon;

        private EVehicleSearchCriteria vehicleSearchCriteria;

        /// <summary>
        /// Timer to check if suspect should look for close cops
        /// </summary>
        private NonAutomaticTimer lookForCloseCopsTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskFleeEvadeCopsOnFoot"/> class.
        /// </summary>
        /// <param name="allowVehicles">
        /// Whether vehicles are allowed.
        /// </param>
        /// <param name="allowTakingHostages">
        /// Whether suspect can take hostages.
        /// </param>
        /// <param name="allowWeapons">
        /// Whether weapons are allowed.
        /// </param>
        /// <param name="vehicleSearchCriteria">
        /// The vehicle search criteria.
        /// </param>
        public TaskFleeEvadeCopsOnFoot(bool allowVehicles, bool allowTakingHostages, bool allowWeapons, EVehicleSearchCriteria vehicleSearchCriteria) : base(ETaskID.FleeEvadeCopsOnFoot)
        {
            this.allowVehicles = allowVehicles;
            this.allowTakingHostages = allowTakingHostages;
            this.allowWeapons = allowWeapons;
            this.vehicleSearchCriteria = vehicleSearchCriteria;
            this.lookForCloseCopsTimer = new NonAutomaticTimer(400);
        }

        /// <summary>
        /// Gets or sets a value indicating whether vehicles are allowed.
        /// </summary>
        public bool AllowVehicles
        {
            get
            {
                return this.allowVehicles;
            }

            set
            {
                this.allowVehicles = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether weapons are allowed.
        /// </summary>
        public bool AllowWeapons
        {
            get
            {
                return this.allowWeapons;
            }

            set
            {
                this.allowWeapons = value;
            }
        }

        public override void MakeAbortable(CPed ped)
        {
            // Prevent ped from fleeing or from wandering
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            if (ped.PedData.Flags.HasFlag(EPedFlags.PlayerDebug))
            {
                return;
            }

            if (ped.Wanted.VisualLost)
            {
                this.fleeingFrom = null;
                /*
                if (ped.Armor > 80)
                {
                    if (ped.Position.DistanceTo2D(CPlayer.LocalPlayer.Ped.Position) > 5)
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                        {
                            ped.Task.GoToCharAiming(CPlayer.LocalPlayer.Ped, 0.5f, 0.6f);
                        }
                    }
                }
                else
                {
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill))
                    {
                        ped.Task.StandStill(-1);
                    }
                }
                 * */
                /*
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
                {

                    ped.Task.WanderAround();
                }
                */

                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
                {

                    //ped.Task.
                }
                if (ped.IsArmed())
                {
                    // Suspect is trying to hide, so make him unarmed
                    ped.Weapons.Select(Weapon.Unarmed);
                    this.hasWeapon = true;
                }
                return;
            }

            // Get closest cop and run away or fight
            if (this.lookForCloseCopsTimer.CanExecute())
            {
                bool canFight = this.allowWeapons && (Natives.IsBulletInArea(ped.Position, ped.PedData.SenseRange) || ped.PedData.CurrentChase.ForceSuspectsToFight);

                // If in a vehicle, don't do anything, but wait for the main task to cancel
                if (ped.IsInVehicle)
                {
                    return;
                }

                // If getting into vehicle, cancel everything else
                if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle) || ped.IsGettingIntoAVehicle)
                {
                    // If there are no close enemies, keep the task running
                    if (!ped.AreEnemiesAround(CopScanDistance / 3))
                    {
                        return;
                    }
                    else
                    {
                        // If there are enemies, ignore them when not allowed to fight
                        if (!canFight)
                        {
                            return;
                        }

                        // If there are enemies and the GetInVehicle task is active, but not the real internal task, so suspect is still not close to vehicle, abort it
                        if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle) && !ped.IsGettingIntoAVehicle)
                        {
                            TaskGetInVehicle taskGetInVehicle = ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.GetInVehicle) as TaskGetInVehicle;
                            taskGetInVehicle.MakeAbortable(ped);
                            return;
                        }
                    }
                }

                // If the ped is not armed, but the weapon has been toggled before, re-equip
                if (!ped.IsArmed() && this.hasWeapon && this.allowWeapons)
                {
                    ped.EnsurePedHasWeapon(); 
                }  
                
                // If in chase and weapons are allowed, checked if either cops are forced to shoot or suspects are
                if (ped.PedData.CurrentChase != null && canFight)
                {
                    if (ped.AreEnemiesAround(ped.PedData.SenseRange))
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatClosestTargetInArea))
                        {
                            ped.EnsurePedHasWeapon();
                            ped.Task.ClearAll();
                            ped.Task.FightAgainstHatedTargets(ped.PedData.SenseRange);
                        }

                        return;
                    }
                }

                // Search for suitable vehicles around
                if (this.allowVehicles && Common.GetRandomValue(0, ped.PedData.ComplianceChance / 2) == 0)
                {
                    CVehicle closestVehicle = ped.Intelligence.GetClosestVehicle(this.vehicleSearchCriteria, VehicleScanRange);
                    if (closestVehicle != null && closestVehicle.Exists() && closestVehicle.IsOnAllWheels && closestVehicle.IsDriveable)
                    {
                        // If enter vehicle task is not running
                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                        {
                            // Assign new task
                            TaskGetInVehicle task = new TaskGetInVehicle(closestVehicle, this.allowWeapons, VehicleScanRange);
                            task.EnterStyle = EPedMoveState.Sprint;
                            ped.Intelligence.TaskManager.Assign(task, ETaskPriority.SubTask);
                        }

                        return;
                    }
                }

                // Search for close cops. They are marked as not available and have an owner, so we explicitly search for those
                CPed closestCop = ped.Intelligence.GetClosestPed(EPedSearchCriteria.CopsOnly | EPedSearchCriteria.NotAvailable | EPedSearchCriteria.HaveOwner | EPedSearchCriteria.Player, CopScanDistance * 2);
                if (closestCop != null && closestCop.Exists())
                {
                    // If either target has changed or no fleeing task is active
                    if (this.fleeingFrom != closestCop || (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask) 
                        && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity)))
                    {
                        // Retreat for player may crash, so we use the flee task here
                        //if (closestCop.PedGroup == EPedGroup.Player)
                        //{
                        //    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                        //    {
                        //        ped.Task.FleeFromChar(closestCop, true);
                        //    }
                        //}
                        //else
                        {
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask))
                            {
                                ped.Task.ClearAll();
                                AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                                ped.APed.TaskCombatRetreatSubtask(closestCop.APed);
                            }
                        }

                        this.fleeingFrom = closestCop;
                    }
                }
                else
                {
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
                    {
                        ped.Task.WanderAround();
                    }
                }
            }
        }

        public override string ComponentName
        {
            get { return "TaskFleeEvadeCopsOnFoot"; }
        }
    }
}
