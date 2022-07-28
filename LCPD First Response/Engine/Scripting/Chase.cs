namespace LCPD_First_Response.Engine.Scripting
{
    using System.Collections.Generic;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    using Main = LCPD_First_Response.Engine.Main;

    /// <summary>
    /// The chase tactic.
    /// </summary>
    internal enum EChaseTactic
    {
        /// <summary>
        /// Active means the cop cars will follow aggressive, that is trying to ram the suspect's vehicle.
        /// </summary>
        Active,

        /// <summary>
        /// Passive means the cop cars will just follow and won't ram the suspect's vehicle.
        /// </summary>
        Passive,
    }

    /// <summary>
    /// The helicopter tactic.
    /// </summary>
    internal enum EHeliTactic
    {
        /// <summary>
        /// Active means the helicopter will try get lower to shoot the suspect.
        /// </summary>
        Active,

        /// <summary>
        /// Passive means the helicopter will just follow high above.
        /// </summary>
        Passive,
    }

    /// <summary>
    /// Class that manages the behavior of cops and criminal during a chase. Provides certain functions to override to modify behavior.
    /// </summary>
    internal class Chase : BaseComponent, ITickable, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// Minimum distance before disbanding a vehicle
        /// </summary>
        private const float MinDisbandInVehicleDistance = 150;

        /// <summary>
        /// Minimum distance before disbanding an officer on foot
        /// </summary>
        private const float MinDisbandOnFootDistance = 50;

        /// <summary>
        /// Maximum distance for an officer to keep chasing the suspects
        /// </summary>
        private const float MaxChaseDistance = 250;

        /// <summary>
        /// Maximum distance for an officer to keep chasing the suspects in a helicopter
        /// </summary>
        private const float MaxChaseDistanceHelicopter = 850;

        /// <summary>
        /// List of all chases.
        /// </summary>
        private static List<Chase> chases; 

        /// <summary>
        /// Whether or not vehicles are allowd for the suspects
        /// </summary>
        private bool allowSuspectVehicles;

        /// <summary>
        /// Whether or not weapons are allowed for the suspects
        /// </summary>
        private bool allowSuspectWeapons;

        /// <summary>
        /// Scheduler object to schedule the entire chase logic
        /// </summary>
        private ScheduledAction chaseLogicScheduler;

        /// <summary>
        /// Timer to delay check for nearby combats for performance reasons.
        /// </summary>
        private NonAutomaticTimer timerNearbyCombats;

        /// <summary>
        /// Whether cops have been instructed to kill all suspects.
        /// </summary>
        private bool forceKilling;

        /// <summary>
        /// Initializes a new instance of the <see cref="Chase"/> class. TODO: Take more parameters of flee and chase task!
        /// </summary>
        /// <param name="allowSuspectVehicles">
        /// Whether or not the suspect can use vehicles.
        /// </param>
        /// <param name="allowSuspectWeapons">
        /// Whether or not the suspect can use weapons.
        /// </param>
        public Chase(bool allowSuspectVehicles, bool allowSuspectWeapons)
        {
            this.allowSuspectVehicles = allowSuspectVehicles;
            this.allowSuspectWeapons = allowSuspectWeapons;
            this.AllowMaxUnitsTolerance = true;
            this.AlwaysAddHelicopterUnits = true;
            this.chaseLogicScheduler = new ScheduledAction(500);
            this.ChaseTactic = EChaseTactic.Active;
            this.HeliTactic = EHeliTactic.Passive;
            this.CanCopsJoin = true;
            this.Cops = new List<CPed>();
            this.Criminals = new List<CPed>();
            this.MaxUnits = 20;
            this.MaxCars = 5;
            this.IsRunning = true;
            this.timerNearbyCombats = new NonAutomaticTimer(250);

            // Add to chase list
            if (chases == null)
            {
                chases = new List<Chase>();
            }

            chases.Add(this);

            // There's a chance ambient cops will take note of the chase and want to join, so we listen to this event
            EventCopReadyToChase.EventRaised += new EventCopReadyToChase.EventRaisedEventHandler(this.EventCopReadyToChase_EventRaised);
        }

        /// <summary>
        /// Chase ended.
        /// </summary>
        public delegate void ChaseEndedEventHandler();

        /// <summary>
        /// Event fired when chase has ended.
        /// </summary>
        public event ChaseEndedEventHandler ChaseEnded;

        /// <summary>
        /// Gets all chases.
        /// </summary>
        public static Chase[] AllChases
        {
            get
            {
                if (chases == null)
                {
                    return null;
                }

                return chases.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the suspects can use vehicles.
        /// </summary>
        public bool AllowSuspectVehicles
        {
            get
            {
                return this.allowSuspectVehicles;
            }

            set
            {
                this.allowSuspectVehicles = value;

                foreach (CPed criminal in this.Criminals)
                {
                    if (criminal.Exists())
                    {
                        TaskFleeEvadeCops taskFleeEvadeCops = (TaskFleeEvadeCops)criminal.Intelligence.TaskManager.FindTaskWithID(ETaskID.FleeEvadeCops);
                        if (taskFleeEvadeCops != null)
                        {
                            taskFleeEvadeCops.AllowVehicles = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the suspects can use weapons, e.g. when trying to get into a vehicle.
        /// </summary>
        public bool AllowSuspectWeapons
        {
            get
            {
                return this.allowSuspectWeapons;
            }

            set
            {
                this.allowSuspectWeapons = value;

                foreach (CPed criminal in this.Criminals)
                {
                    if (criminal.Exists())
                    {
                        TaskFleeEvadeCops taskFleeEvadeCops = (TaskFleeEvadeCops)criminal.Intelligence.TaskManager.FindTaskWithID(ETaskID.FleeEvadeCops);
                        if (taskFleeEvadeCops != null)
                        {
                            taskFleeEvadeCops.AllowWeapons = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether there can be more units than set via MaxUnits. So very close units can be added even though this would exceed MaxUnits.
        /// and far away ones would get disbanded then
        /// </summary>
        public bool AllowMaxUnitsTolerance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether helicopter units can be added to the chase ignoring <see cref="MaxUnits"/>.
        /// </summary>
        public bool AlwaysAddHelicopterUnits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether cops can join the chase.
        /// </summary>
        public bool CanCopsJoin { get; set; }

        /// <summary>
        /// Gets the chase tactic.
        /// </summary>
        public EChaseTactic ChaseTactic { get; private set; }

        /// <summary>
        /// Gets the heli tactic.
        /// </summary>
        public EHeliTactic HeliTactic { get; private set; }

        /// <summary>
        /// Gets a list of all cops involved.
        /// </summary>
        public List<CPed> Cops { get; private set; }

        /// <summary>
        /// Gets a list of all criminals involved.
        /// </summary>
        public List<CPed> Criminals { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether cops have been instructed to kill all suspects.
        /// </summary>
        public bool ForceKilling
        {
            get
            {
                return this.forceKilling;
            }

            
            set
            {
                this.forceKilling = value;

                // When disabling, make sure all cops really stop shooting
                if (!this.forceKilling)
                {
                    foreach (CPed criminal in this.Criminals)
                    {
                        if (criminal.Exists())
                        {
                            criminal.ClearCombat();
                            criminal.Wanted.WeaponUsed = false;
                        }                       
                    }

                    // Cops performing drive-by might still shoot, so kill all tasks of all passengers
                    foreach (CPed cop in this.Cops)
                    {
                        if (cop.IsInVehicle && !cop.IsDriver)
                        {
                            // Different treatment for cops in heli
                            if (cop.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsHelicopter))
                            {
                                CVehicle vehicle = cop.CurrentVehicle;
                                cop.Task.ClearAllImmediately();
                                cop.WarpFromCar(Vector3.Zero);
                                cop.WarpIntoVehicle(vehicle, VehicleSeat.AnyPassengerSeat);
                                cop.Task.Wait(10000);
                            }
                            else
                            {
                                cop.Task.ClearAll();
                                cop.Task.Wait(1000);
                            }
                        }
                        else
                        {
                            cop.Task.ClearAll();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether suspects should fight immediately.
        /// </summary>
        public bool ForceSuspectsToFight { get; set; }

        /// <summary>
        /// Gets a value indicating whether this chase is the only active one.
        /// </summary>
        public bool IsOnlyActiveChase
        {
            get
            {
                return AllChases.Length == 1;
            }
        }

        /// <summary>
        /// Gets a value indicating whether chase is still in progress.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the chase is the chase of the player.
        /// </summary>
        public bool IsPlayersChase
        {
            get
            {
                return CPlayer.LocalPlayer.Ped.PedData.CurrentChase == this;
            }
        }

        /// <summary>
        /// Gets or sets the max number of units allowed to chase the suspects.
        /// </summary>
        public int MaxUnits { get; set; }

        /// <summary>
        /// Component name.
        /// </summary>
        public override string ComponentName
        {
            get { return "Chase"; }
        }

        /// <summary>
        /// Gets or sets the max number of cars allowed to chase the suspects.
        /// </summary>
        public int MaxCars { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the player counts as an officer with a visual
        /// </summary>
        public bool OnlyAIVisuals { get; set; }

        /// <summary>
        /// Adds <paramref name="ped"/> to the list of chased peds.
        /// </summary>
        /// <param name="ped">Ped to chase</param>
        public virtual void AddTarget(CPed ped)
        {
            // Setup target
            ped.BlockPermanentEvents = true;
            ped.PedData.CurrentChase = this;
            ped.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
            ped.ChangeRelationship(RelationshipGroup.Player, Relationship.Hate);

            // Enables ped.Wanted.WeaponUsed flag, since it's only monitored for criminals
            ped.PedGroup = EPedGroup.Criminal;

            if (!ped.PedData.DisableChaseAI)
            {
                TaskFleeEvadeCops task = new TaskFleeEvadeCops(true, this.allowSuspectVehicles, EVehicleSearchCriteria.NoPlayersLastVehicle | EVehicleSearchCriteria.NoCarsWithCopDriver, this.allowSuspectWeapons);
                ped.Intelligence.TaskManager.Assign(task, ETaskPriority.MainTask);
            }

            // Add to local list
            this.Criminals.Add(ped);

            // Assign visual update task for the new target to all cops
            foreach (CPed cop in this.Cops)
            {
                if (cop.Exists() && cop.IsAliveAndWell)
                {
                    TaskCopUpdateVisualForTarget taskCopUpdateVisualForTarget = new TaskCopUpdateVisualForTarget(ped);
                    taskCopUpdateVisualForTarget.AssignTo(cop, ETaskPriority.SubTask);
                }
            }
        }

        /// <summary>
        /// Processes the chase logic.
        /// </summary>
        public virtual void Process()
        {
            if (this.IsChaseFinished())
            {
                this.EndChase();
                return;
            }

            if (this.chaseLogicScheduler.CanExecute())
            {
                // Get all available cops
                if (this.CanCopsJoin)
                {
                    CPed[] cops = Main.CopManager.RequestAllAvailableUnits(false);
                    foreach (CPed cop in cops)
                    {
                        this.SetupCop(cop, false);
                    }
                }

                List<CPed> copsToRemove = new List<CPed>();
                bool needToDisbandCops = this.MaxUnits < this.Cops.Count;

                // Now process all cops
                foreach (CPed cop in this.Cops)
                {
                    if (!cop.Exists())
                    {
                        copsToRemove.Add(cop);
                        continue;
                    }

                    if (!cop.IsAliveAndWell)
                    {
                        // Debug
                        this.FreeCopIfStillUseable(cop);
                        copsToRemove.Add(cop);
                    }

                    // Parse ped data
                    PedDataCop pedDataCop = cop.PedData as PedDataCop;
                    pedDataCop.ForceKilling = this.forceKilling;

                    // Although it is unlikey to happen, we check if we are still allowed to process the ped
                    if (!pedDataCop.IsPedStillUseable(this))
                    {
                        copsToRemove.Add(cop);
                        continue;
                    }

                    // Get the closest target
                    CPed target = this.GetClosestTarget(cop);

                    // If the distance to the closest target is too high, remove cop
                    if (target != null && target.Position.DistanceTo(cop.Position) > MaxChaseDistance)
                    {
                        bool remove = true;

                        // If cop is in a vehicle, check if distance is high enough for the cop to be removed
                        if (cop.IsInVehicle && cop.CurrentVehicle.Model.IsHelicopter
                            && target.Position.DistanceTo(cop.Position) < MaxChaseDistanceHelicopter)
                        {
                            remove = false;
                        }

                        if (cop.PedData.Flags.HasFlag(EPedFlags.IsRoadblockPed))
                        {
                            if (target.Position.DistanceTo(cop.Position) < MaxChaseDistance * 2)
                            {
                                remove = false;
                            }
                        }

                        if (remove)
                        {
                            this.FreeCopIfStillUseable(cop);
                            copsToRemove.Add(cop);
                            continue;
                        }
                    }

                    // Look if target is still the closest
                    if (target != null && pedDataCop.CurrentTarget != null)
                    {
                        if (pedDataCop.CurrentTarget != target)
                        {
                            // If there are less officers chasing than the current target, change. But only if that doesn't mean the current suspect can escape
                            if (target.Wanted.OfficersChasing < pedDataCop.CurrentTarget.Wanted.OfficersChasing && pedDataCop.CurrentTarget.Wanted.OfficersChasing > 1)
                            {
                                if (target.Wanted.ArrestedBy != cop)
                                {
                                    if (!cop.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
                                    {
                                        bool dontChange = false;

                                        // If helicopter unit, only be able to change if current target has more than 1 helicopter support unit
                                        if (cop.IsInVehicle && cop.CurrentVehicle.Model.IsHelicopter)
                                        {
                                            if (pedDataCop.CurrentTarget.Wanted.HelicoptersChasing <= 1)
                                            {
                                                dontChange = true;
                                            }
                                        }

                                        if (!dontChange)
                                        {
                                            cop.Intelligence.TaskManager.ClearTasks();
                                            TaskCopChasePed task = new TaskCopChasePed(target, false, false, false);
                                            task.AssignTo(cop, ETaskPriority.MainTask);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Make chase if not already doing so
                    if (!cop.Intelligence.TaskManager.IsTaskActive(ETaskID.ChasePed))
                    {
                        if (target != null && target.Exists())
                        {
                            if (cop.Intelligence.TaskManager.IsTaskActive(ETaskID.LeaveScene))
                            {
                                TaskLeaveScene taskLeaveScene = cop.Intelligence.TaskManager.FindTaskWithID(ETaskID.LeaveScene) as TaskLeaveScene;
                                taskLeaveScene.MakeAbortable(cop);
                            }

                            TaskCopChasePed task = new TaskCopChasePed(target, false, false, false);
                            task.AssignTo(cop, ETaskPriority.MainTask);
                        }
                    }

                    // If no target and not chasing, wander
                    if (target == null && !cop.Intelligence.TaskManager.IsTaskActive(ETaskID.ChasePed))
                    {
                        if (!cop.Intelligence.TaskManager.IsTaskActive(ETaskID.LeaveScene))
                        {
                            TaskLeaveScene taskLeaveScene = new TaskLeaveScene();
                            taskLeaveScene.AssignTo(cop, ETaskPriority.MainTask);
                        }
                    }

                    // Handles disbanding of cops
                    // TODO: Sort cops by distance and remove the ones that are furthest away
                    if (needToDisbandCops && !cop.PedData.Flags.HasFlag(EPedFlags.IgnoreMaxUnitsLimitInChase))
                    {
                        if (pedDataCop.CurrentTarget != null)
                        {
                            float distance = pedDataCop.CurrentTarget.Position.DistanceTo(cop.Position);
                            bool inVehicle = cop.IsInVehicle;
                            bool furthestAway = this.GetCopFurthestAway(pedDataCop.CurrentTarget) == cop;

                            if (inVehicle && (distance > MinDisbandInVehicleDistance || furthestAway))
                            {
                                // Don't disband helicopter support
                                if (!cop.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsHelicopter))
                                {
                                    this.FreeCopIfStillUseable(cop);
                                    copsToRemove.Add(cop);
                                }
                            }
                            else if (!inVehicle && (distance > MinDisbandOnFootDistance || furthestAway))
                            {
                                bool remove = !cop.PedData.Flags.HasFlag(EPedFlags.IsRoadblockPed);
                                if (remove)
                                {
                                    this.FreeCopIfStillUseable(cop);
                                    copsToRemove.Add(cop);
                                }
                            }
                        }
                    }
                }

                foreach (CPed ped in copsToRemove)
                {
                    this.Cops.Remove(ped);
                }

                // Process criminals
                foreach (CPed criminal in this.Criminals)
                {
                    if (criminal != null && criminal.Exists() && criminal.IsAliveAndWell)
                    {
                        // If not in combat, ensure flee task is running
                        if (!criminal.IsInCombat
                            && !criminal.Intelligence.TaskManager.IsTaskActive(ETaskID.FleeEvadeCops) && !criminal.PedData.DisableChaseAI)
                        {
                            // If criminal has already decided what to do and no officer has visual (so after a gunfight, if every cop is down, he can flee again)
                            if (!criminal.Wanted.IsDeciding && criminal.Wanted.OfficersVisual <= 0
                                && !criminal.Wanted.HasBeenArrested && !criminal.Wanted.IsCuffed && !criminal.Wanted.IsBeingArrestedByPlayer && !criminal.Wanted.IsStopped)
                            {
                                if (criminal.Intelligence.TaskManager.IsTaskActive(ETaskID.BeingBusted))
                                {
                                    Log.Warning("Process: Attempt to assign fleeing task while being busted", this);
                                }
                                else
                                {
                                    CPed[] copsAround = criminal.Intelligence.GetPedsAround(15f, EPedSearchCriteria.CopsOnly);

                                    // If no cops are around or all are death, resume fleeing
                                    if (copsAround.Length == 0 || CPed.GetNumberOfDeadPeds(copsAround, false) == copsAround.Length)
                                    {
                                        TaskFleeEvadeCops taskFleeEvadeCops = new TaskFleeEvadeCops(true, this.allowSuspectVehicles, EVehicleSearchCriteria.NoPlayersLastVehicle | EVehicleSearchCriteria.NoCarsWithCopDriver, this.allowSuspectWeapons);
                                        taskFleeEvadeCops.AssignTo(criminal, ETaskPriority.MainTask);

                                        // Also reset some flags
                                        criminal.Wanted.ResetArrestFlags();
                                    }
                                }
                            }
                        }

                        // If lost for too long, see if we can remove
                        //TODO: Change this
                        if (criminal.Wanted.VisualLost && criminal.Wanted.VisualLostSince > 800)
                        {
                            if (criminal.SearchArea != null)
                            {
                                if (criminal.Position.DistanceTo(criminal.SearchArea.GetPosition()) > criminal.SearchArea.Size)
                                {
                                    // Only remove if they have also escaped the search area
                                    Log.Debug("Process: Criminal removed. Reason: Visual lost and search area escape", this);

                                    new EventCriminalEscaped(criminal);

                                    // Safety first, criminal might be searchlight target
                                    CVehicle.SetHeliSearchlightTarget(null);

                                    // Also check if they have a car.  If so, remove that too.
                                    if (criminal.IsInVehicle)
                                    {
                                        if (criminal.CurrentVehicle != null && criminal.CurrentVehicle.Exists())
                                        {
                                            criminal.CurrentVehicle.Delete();
                                        }
                                    }

                                    if (criminal != CPlayer.LocalPlayer.Ped) criminal.Delete();
                                }
                            }
                            else
                            {
                                // This should never happen, but if so, just end the chase.
                                Log.Debug("Process: Criminal removed. Reason: Visual lost and search area failure", this);

                                new EventCriminalEscaped(criminal);

                                // Safety first, criminal might be searchlight target
                                CVehicle.SetHeliSearchlightTarget(null);

                                // Also check if they have a car.  If so, remove that too.
                                if (criminal.IsInVehicle)
                                {
                                    if (criminal.CurrentVehicle != null && criminal.CurrentVehicle.Exists())
                                    {
                                        criminal.CurrentVehicle.Delete();
                                    }
                                }

                                criminal.Delete();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Changes the chase tactic to <paramref name="chaseTactic"/>.
        /// </summary>
        /// <param name="chaseTactic">The new chase tactic.</param>
        public void ChangeTactics(EChaseTactic chaseTactic)
        {
            this.ChaseTactic = chaseTactic;

            // Reset driving task to reset driving style
            foreach (CPed cop in this.Cops)
            {
                if (cop.Exists() && cop.IsAliveAndWell)
                {
                    if (cop.Intelligence.TaskManager.IsTaskActive(ETaskID.ChasePed))
                    {
                        if (cop.IsDriver && cop.IsInVehicle)
                        {
                            cop.Task.ClearAll();

                            if (cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatPersueInCarSubtask))
                            {
                                CVehicle vehicle = cop.CurrentVehicle;
                                cop.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexCombatPersueInCarSubtask);
                                cop.WarpIntoVehicle(vehicle, VehicleSeat.Driver);
                                cop.Task.CruiseWithVehicle(vehicle, 100f, false);
                            }
                        }
                    }
                }
            }

            Log.Debug("ChangeTactics: Tactic changed to " + chaseTactic, this);
        }

        /// <summary>
        /// Changes the heli tactic to <paramref name="chaseTactic"/>.
        /// </summary>
        /// <param name="chaseTactic">The new helicopter tactic.</param>
        public void ChangeHeliTactics(EHeliTactic chaseTactic)
        {
            this.HeliTactic = chaseTactic;
            Log.Debug("ChangeTactics: Helicopter Tactic changed to " + chaseTactic, this);
        }

        /// <summary>
        /// Creates a roadblock at <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="vehicleModel">The model of the police vehicle.</param>
        /// <param name="pedModel">The model of the officer.</param>
        /// <param name="numberOfVehicles">The number of vehicles, usually 2.</param>
        /// <param name="numberOfOfficers">The number of officers, usually 4.</param>
        public void CreateRoadblock(Vector3 position, CModel vehicleModel, CModel pedModel, int numberOfVehicles, int numberOfOfficers)
        {
            Vector3 closestNodePosition = Vector3.Zero;
            float closestNodeHeading = 0f;
            if (CVehicle.GetClosestCarNodeWithHeading(position, ref closestNodePosition, ref closestNodeHeading))
            {
                // Node is the middle of the road
                float vehicleLength = vehicleModel.GetDimensions().Y;
                float space = 0.25f;

                // Calculate heading
                closestNodeHeading += 90;
                if (closestNodeHeading > 360)
                {
                    closestNodeHeading = closestNodeHeading - 360;
                }

                // Spawn vehicle
                CVehicle vehicle = new CVehicle(vehicleModel, closestNodePosition, EVehicleGroup.Police);
                if (vehicle.Exists())
                {
                    vehicle.SirenActive = true;
                    vehicle.Heading = closestNodeHeading;
                    vehicle.Position = vehicle.GetOffsetPosition(new Vector3(0, (vehicleLength / 2) + space, 0));

                    CPed cop = new CPed(pedModel, vehicle.GetOffsetPosition(new Vector3(2, 0, 0)), EPedGroup.Cop);
                    if (cop.Exists())
                    {
                        cop.PedData.DefaultWeapon = Weapon.Rifle_M4;
                        cop.EnsurePedHasWeapon();
                        cop.PedData.Flags = EPedFlags.IsRoadblockPed;
                        this.SetupCop(cop, true);
                    }
                }

                CVehicle vehicle2 = new CVehicle(vehicleModel, closestNodePosition, EVehicleGroup.Police);
                if (vehicle2.Exists())
                {
                    vehicle2.SirenActive = true;
                    vehicle2.Heading = closestNodeHeading;
                    vehicle2.Position = vehicle2.GetOffsetPosition(new Vector3(0, -((vehicleLength / 2) + space), 0));

                    CPed cop = new CPed(pedModel, vehicle2.GetOffsetPosition(new Vector3(2, 0, 0)), EPedGroup.Cop);
                    if (cop.Exists())
                    {
                        cop.PedData.DefaultWeapon = Weapon.Rifle_M4;
                        cop.EnsurePedHasWeapon();
                        cop.PedData.Flags = EPedFlags.IsRoadblockPed;
                        this.SetupCop(cop, true);
                    }
                }

                if (vehicle.Exists())
                {
                    vehicle.NoLongerNeeded();
                }

                if (vehicle2.Exists())
                {
                    vehicle2.NoLongerNeeded();
                }
            }
        }

        /// <summary>
        /// Called when ped has left, because of another more important action.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public void PedHasLeft(CPed ped)
        {
            this.FreeCop(ped, true);
        }

        /// <summary>
        /// Ends the chase.
        /// </summary>
        public virtual void EndChase()
        {
            if (!this.IsRunning)
            {
                return;
            }

            foreach (CPed cop in this.Cops)
            {
                if (cop != null && cop.Exists() && cop.GetPedData<PedDataCop>().IsPedStillUseable(this))
                {
                    this.FreeCop(cop);

                    if (!cop.Intelligence.TaskManager.IsTaskActive(ETaskID.LeaveScene))
                    {
                        TaskLeaveScene taskLeaveScene = new TaskLeaveScene();
                        taskLeaveScene.AssignTo(cop, ETaskPriority.MainTask);
                    }
                }
            }

            // Safety first, so we clear the searchlight target. TODO: Fix adv hook code so game won't crash when target is invalid (add hook with exists check)
            CVehicle.SetHeliSearchlightTarget(null);

            foreach (CPed criminal in this.Criminals)
            {
                if (criminal != null && criminal.Exists())
                {
                    criminal.Wanted.ClearPlacesToSearch();
                    criminal.PedData.CurrentChase = null;
                    if (!criminal.Wanted.IsBeingArrestedByPlayer)
                    {
                        criminal.Intelligence.TaskManager.ClearTasks();
                    }

                    // Also reset some flags
                    criminal.Wanted.ResetArrestFlags();
                }
            }

            this.IsRunning = false;

            if (this.ChaseEnded != null)
            {
                this.ChaseEnded();
            }

            this.Cops = null;
            this.Criminals = null;
            this.Delete();
            chases.Remove(this);
            Log.Debug("EndChase: Chase ended", this);
        }

        /// <summary>
        /// Checks whether <paramref name="suspect"/> is the only suspect available to chase, so all others are either dead, arrested, being arrested or visual is lost.
        /// </summary>
        /// <param name="suspect">The suspect.</param>
        /// <returns>True if the only one, false if not.</returns>
        public bool IsOnlyAvailableSuspect(CPed suspect)
        {
            // Quick check for performance reasons
            if (this.Criminals.Count == 1 && this.Criminals.Contains(suspect))
            {
                return true;
            }

            // Check other criminals
            foreach (CPed criminal in this.Criminals)
            {
                // If there's a criminal that is still visible and not being arrested, return false
                if (criminal.Exists() && criminal.IsAliveAndWell && !criminal.Wanted.VisualLost && !criminal.Wanted.IsBeingArrested
                    && !criminal.Wanted.IsBeingArrestedByPlayer && !criminal.Wanted.HasBeenArrested)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks and returns whether the pursuit occurs in water.
        /// </summary>
        /// <returns>Whther it's a water pursuit.</returns>
        public bool IsWaterPursuit()
        {
            foreach (CPed criminal in this.Criminals)
            {
                if (criminal.Exists() && criminal.IsAliveAndWell)
                {
                    if (criminal.IsInVehicle)
                    {
                        if (criminal.CurrentVehicle.Model.IsBoat)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (criminal.IsInWater)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Setups the <paramref name="cop"/> by requesting ownership. Also checks if adding the cop is allowed due to the max units limit.
        /// </summary>
        /// <param name="cop">The cop to add.</param>
        /// <param name="highPriority">If true <see cref="AllowMaxUnitsTolerance"/> is also true, the cop can be added even if it exceeds the max units limit.</param>
        public virtual void SetupCop(CPed cop, bool highPriority)
        {
            if (Main.NetworkManager.IsNetworkSession && !cop.IsControlledByUs)
            {
                return;
            }

            bool forceAdd = cop.PedData.Flags.HasFlag(EPedFlags.IgnoreMaxUnitsLimitInChase);

            // Check if cop is in helicopter
            if (cop.IsInVehicle && cop.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsHelicopter))
            {
                // Only add POLMAV
                if (cop.CurrentVehicle.Model != "POLMAV" && cop.CurrentVehicle.Model != "ANNIHILATOR")
                {
                    return;
                }

                if (this.AlwaysAddHelicopterUnits)
                {
                    forceAdd = true;
                }
            }

            bool canAdd = false;

            // If no target available, or too far away from one, don't even bother adding.
            CPed possibleTarget = this.GetClosestTarget(cop);
            if (possibleTarget == null || possibleTarget.Position.DistanceTo(cop.Position) > MaxChaseDistance)
            {
                if (!highPriority && !forceAdd)
                {
                    return;
                }
            }

            // Only process high priority requests if they are closer than the cop currecntly furthest away.
            if (highPriority && !forceAdd)
            {
                if (possibleTarget != null)
                {
                    CPed copFurthestAway = this.GetCopFurthestAway(possibleTarget);
                    if (copFurthestAway != null)
                    {
                        if (cop.Position.DistanceTo(possibleTarget.Position)
                            > copFurthestAway.Position.DistanceTo(possibleTarget.Position))
                        {
                            return;
                        }
                    }
                }
            }

            // Check if cop is on foot or in a car
            if (cop.IsInVehicle)
            {
                // Check if there aren't too many cars. Ignore this check if both, tolerance and highPriority is set
                if (this.GetNumberOfCopCars() < this.MaxCars || (highPriority && this.AllowMaxUnitsTolerance) || forceAdd)
                {
                    canAdd = true;
                }
            }
            else
            {
                if (this.Cops.Count < this.MaxUnits || (highPriority && this.AllowMaxUnitsTolerance) || forceAdd)
                {
                    canAdd = true;
                }
            }

            if (canAdd)
            {
                if (cop.GetPedData<PedDataCop>().RequestPedAction(ECopState.Chase, this))
                {
                    PedDataCop pedDataCop = cop.PedData as PedDataCop;
                    pedDataCop.CurrentChase = this;
                    cop.RequestOwnership(this);
                    // cop.AttachBlip().Friendly = true;

                    // We don't want the cop to respond to any events
                    cop.BlockPermanentEvents = true;

                    // Assign visual update task for all criminals
                    foreach (CPed criminal in this.Criminals)
                    {
                        if (criminal.Exists() && criminal.IsAliveAndWell)
                        {
                            TaskCopUpdateVisualForTarget taskCopUpdateVisualForTarget = new TaskCopUpdateVisualForTarget(criminal);
                            taskCopUpdateVisualForTarget.AssignTo(cop, ETaskPriority.SubTask);
                        }
                    }

                    // Also add local reference);
                    this.Cops.Add(cop);
                }
            }
        }

        /// <summary>
        /// Called when there's an ambient cop ready to join the chase.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventCopReadyToChase_EventRaised(EventCopReadyToChase @event)
        {
            // Ensure we're the first to receive the event
            if (!@event.Handled)
            {
                // If criminal is assigned to this chase
                if (@event.Criminal.PedData.CurrentChase == this)
                {
                    if (!this.CanCopsJoin)
                    {
                        return;
                    }

                    // Add cop and set event as handled
                    this.SetupCop(@event.Cop, true);
                    @event.Handled = true;
                }
            }
        }

        /// <summary>
        /// Frees the <paramref name="cop"/> by releasing the ownership and deleting the blip.
        /// </summary>
        /// <param name="cop">The cop.</param>
        /// <param name="hasLeft">If true, ped action is reset as well.</param>
        private void FreeCop(CPed cop, bool hasLeft = false)
        {
            if (cop.Exists())
            {
                if (!hasLeft)
                {
                    cop.GetPedData<PedDataCop>().ResetPedAction(this);
                    cop.Intelligence.TaskManager.ClearTasks();
                }

                // Free
                cop.ReleaseOwnership(this);

                // For some weird reason, cop ceases to exist here in MP session sometimes, so we better check again...
                if (cop.Exists())
                {
                    cop.BlockPermanentEvents = false;
                    cop.DeleteBlip();

                    // If in a vehicle as passenger, prevent from exiting the vehicle
                    if (cop.IsInVehicle && !cop.IsDriver)
                    {
                        cop.Task.Wait(5000);
                    }
                }
            }
        }

        /// <summary>
        /// Frees the <paramref name="cop"/> if the cop is still useable for our script.
        /// </summary>
        /// <param name="cop">The cop.</param>
        private void FreeCopIfStillUseable(CPed cop)
        {
            if (cop.GetPedData<PedDataCop>().IsPedStillUseable(this))
            {
                this.FreeCop(cop);
            }
        }

        /// <summary>
        /// Gets the closest criminal for the <paramref name="cop"/>.
        /// </summary>
        /// <param name="cop">The cop.</param>
        /// <returns>The closest criminal. Can be null.</returns>
        private CPed GetClosestTarget(CPed cop)
        {
            float closestDistance = float.MaxValue;
            CPed closestPed = null;
            foreach (CPed criminal in this.Criminals)
            {
                if (criminal.Exists() && cop.Exists() && criminal.IsAliveAndWell)
                {
                    // If either visual is lost or suspect is being arrested, he becomes a low priority suspect, that is all other suspects should rather be chased than this one
                    if (criminal.Wanted.VisualLost || criminal.Wanted.IsBeingArrested || criminal.Wanted.IsBeingArrestedByPlayer || criminal.Wanted.HasBeenArrested)
                    {
                        // Suspects already arrested, can be dropped completely
                        if (criminal.Wanted.HasBeenArrested)
                        {
                            continue;
                        }

                        // When there are other suspects as well, don't return this one
                        if (!this.IsOnlyAvailableSuspect(criminal))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // If suspect has surrendered already, make him low priority only when there is a combat ongoing
                        if (criminal.Wanted.Surrendered && this.timerNearbyCombats.CanExecute() && CPed.IsPedInCombatInArea(cop.Position, 25f))
                        {
                            continue;
                        }
                    }

                    // Don't assign if target is already cuffed and has least three officers assigned
                    if (criminal.Wanted.IsCuffed && criminal.Wanted.OfficersChasing >= 3)
                    {
                        continue;
                    }

                    // If helicopter unit, prefer targets without helicopter units assigned
                    if (closestPed != null && criminal.Wanted.HelicoptersChasing > closestPed.Wanted.HelicoptersChasing)
                    {
                        continue;
                    }

                    bool forceAdd = closestPed != null && criminal.Wanted.HelicoptersChasing < closestPed.Wanted.HelicoptersChasing;
                    float distance = criminal.Position.DistanceTo(cop.Position);
                    if (distance < closestDistance || forceAdd)
                    {
                        closestDistance = distance;
                        closestPed = criminal;
                    }
                }
            }
            return closestPed;
        }

        private CPed GetCopFurthestAway(CPed suspect)
        {
            if (suspect == null || !suspect.Exists()) return null;

            List<CPed> assignedCops = this.Cops.Where(cop => cop.GetPedData<PedDataCop>().CurrentTarget == suspect).ToList();
            var lengths = from element in assignedCops
                          where element.Exists()
                          orderby element.Position.DistanceTo2D(suspect.Position)
                          select element;
            return lengths.LastOrDefault();
        }

        private int GetNumberOfCopCars()
        {
            int number = 0;
            foreach (CPed cop in this.Cops)
            {
                if (cop != null && cop.Exists())
                {
                    if (cop.IsInVehicle && cop.IsDriver)
                    {
                        number++;
                    }
                }
            }
            return number;
        }

        /// <summary>
        /// Returns whether the chase has finished (all criminals are either dead or arrested).
        /// </summary>
        /// <returns>True if finished, false if not.</returns>
        private bool IsChaseFinished()
        {
            int crimsDone = 0;
            foreach (CPed criminal in this.Criminals)
            {
                if (!criminal.Exists())
                {
                    crimsDone++;
                    continue;
                }
                if (!criminal.IsAliveAndWell)
                {
                    crimsDone++;
                    continue;
                }
                if (criminal.Wanted.HasBeenArrested)
                {
                    crimsDone++;
                    continue;
                }
            }
            return crimsDone == this.Criminals.Count;
        }
    }
}
