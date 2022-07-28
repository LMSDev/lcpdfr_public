namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AdvancedHookManaged;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// The ped intelligence class, responsible for managing the ped's task and behavior.
    /// </summary>
    internal class PedIntelligence : BaseComponent, ICoreTickable
    {
        /// <summary>
        /// Returns a struct of type IVPedIntelligence using AdvancedHook
        /// </summary>
        //public unsafe IVPedIntelligence InternalIVPedIntelligence
        //{
        //    get
        //    {
        //        IntPtr pPedIntelligence = new IntPtr(this.ped.APed.GetPedIntelligence());
        //        var pedIntelligence = (IVPedIntelligence) Marshal.PtrToStructure(pPedIntelligence, typeof (IVPedIntelligence));
        //        return pedIntelligence;
        //    }
        //}

        /// <summary>
        /// The max angle a ped can see another entity
        /// </summary>
        public const double MaxViewAngle = 45;

        /// <summary>
        /// The max range a ped can see another entity
        /// </summary>
        public const float MaxViewRange = 30f;

        /// <summary>
        /// All blacklisted vehicles.
        /// </summary>
        private List<CVehicle> blacklistedVehicles;

        /// <summary>
        /// The priority of the current action.
        /// </summary>
        private EPedActionPriority currentActionPriority;

        /// <summary>
        /// Whether a text should be drawn above the ped's head.
        /// </summary>
        private bool drawTextEnabled;

        /// <summary>
        /// Use this to override the range at which drawn text above head disappears
        /// </summary>
        private int drawTextRange = 9;

        /// <summary>
        /// The text that is drawn above the ped's head.
        /// </summary>
        private string drawText;

        /// <summary>
        /// The draw text positions.
        /// </summary>
        private float drawX, drawY;

        /// <summary>
        /// The font to draw text.
        /// </summary>
        private static Font drawTextFont;

        /// <summary>
        /// Enables the Process function to be scheduled
        /// </summary>
        private ScheduledAction processScheduler;

        private CPed ped;
        private bool reportedDeath;

        private Dictionary<IExtendedIntelligence, int> registeredIntelligences; 

        /// <summary>
        /// Initializes a new instance of the <see cref="PedIntelligence"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public PedIntelligence(CPed ped)
        {
            this.ped = ped;
            this.blacklistedVehicles = new List<CVehicle>();
            this.processScheduler = new ScheduledAction(300);
            this.TaskManager = new TaskManager(ped);
            this.registeredIntelligences = new Dictionary<IExtendedIntelligence, int>();
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "PedIntelligence"; }
        }

        /// <summary>
        /// Gets the priority of the current action.
        /// </summary>
        public EPedActionPriority CurrentActionPriority
        {
            get
            {
                return this.currentActionPriority;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a text is drawn above the ped's head.
        /// </summary>
        public bool DrawTextEnabled
        {
            get
            {
                return this.drawTextEnabled;
            }
        }

        /// <summary>
        /// Gets or sets the ped controller.
        /// </summary>
        public IPedController PedController { get; protected set; }

        /// <summary>
        /// Gets the task manager.
        /// </summary>
        public TaskManager TaskManager { get; private set; }

        /// <summary>
        /// Setups the default ped group settings for the ped.
        /// </summary>
        public void SetupDefaultSettingsForPedGroup()
        {
            if (this.ped.PedGroup == EPedGroup.Cop)
            {
                this.ped.RelationshipGroup = RelationshipGroup.Cop;
                this.ped.EnsurePedHasWeapon();
                this.ped.Weapons.Select(Weapon.Unarmed);
                this.ped.SetPedWontAttackPlayerWithoutWantedLevel(true);

                new EventCopCreated(this.ped);
            }

            if (this.ped.PedGroup == EPedGroup.Pedestrian)
            {
                // Listen to fleeing criminal event
                EventPedBeingArrested.EventRaised += new EventPedBeingArrested.EventRaisedEventHandler(this.EventPedBeingArrested_EventRaised);
                EventFleeingCriminal.EventRaised += new EventFleeingCriminal.EventRaisedEventHandler(this.EventFleeingCriminal_EventRaised);
            }
        }

        // Public functions

        /// <summary>
        /// Blacklist <paramref name="vehicle"/> for <paramref name="duration"/> so the ped's intelligence functions can no longer make use of that vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="duration">The duration.</param>
        public void AddVehicleToBlacklist(CVehicle vehicle, int duration)
        {
            Log.Debug("AddVehicleToBlacklist: Vehicle blacklisted", this);
            this.blacklistedVehicles.Add(vehicle);
            DelayedCaller.Call(delegate { this.blacklistedVehicles.Remove(vehicle); }, duration);
        }

        /// <summary>
        /// Whether the point can be seen by the ped. Warning: Doesn't include possible objects in the line of sight.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="fov">The field of view.</param>
        /// <returns>Whether or not the point can be seen.</returns>
        public bool CanSeePosition(Vector3 position, double fov = MaxViewAngle)
        {
            // Calculate distance (2D here, peds don't always look in the sky)
            float distance = this.ped.Position.DistanceTo2D(position);
            // If too far away, return false
            if (distance > MaxViewRange) return false;

            // Find direction from the first position to the second
            Vector3 dir = position - this.ped.Position;
            dir.Normalize();

            // Return if the position is in the FOV of our ped
            return Vector3.Dot(this.ped.Direction, dir) >= Math.Cos(90 - MaxViewAngle);
        }

        /// <summary>
        /// Whether the point can be seen by the ped. Warning: Doesn't include possible objects in the line of sight.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="fov">The field of view.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <returns>Whether or not the point can be seen.</returns>
        public bool CanSeePosition(Vector3 position, float maxDistance, double fov = MaxViewAngle)
        {
            // Calculate distance (2D here, peds don't always look in the sky)
            float distance = this.ped.Position.DistanceTo2D(position);
            // If too far away, return false
            if (distance > maxDistance) return false;

            // Find direction from the first position to the second
            Vector3 dir = position - this.ped.Position;
            dir.Normalize();

            // Return if the position is in the FOV of our ped
            return Vector3.Dot(this.ped.Direction, dir) >= Math.Cos(90 - MaxViewAngle);
        }


        /// <summary>
        /// Warning: Doesn't include possible objects in the line of sight
        /// </summary>
        /// <param name="cPed"></param>
        /// <returns></returns>
        public bool CanSeePed(CPed cPed)
        {
            return CanSeePosition(cPed.Position);
        }

        /// <summary>
        /// Warning: Doesn't include possible objects in the line of sight
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public bool CanSeeVehicle(CVehicle vehicle)
        {
            return CanSeePosition(vehicle.Position);
        }

        /// <summary>
        /// Changes the current action priority to <paramref name="newPriority"/> if current controller is <paramref name="controller"/>.
        /// </summary>
        /// <param name="newPriority">The new priority.</param>
        /// <param name="controller">The current controller.</param>
        /// <returns>True on success.</returns>
        public bool ChangeActionPriority(EPedActionPriority newPriority, IPedController controller)
        {
            if (this.IsStillAssignedToController(controller))
            {
                this.currentActionPriority = newPriority;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns if the ped could see the given ped, so if it would be realistic that the ped would take notice of the given ped. This is only distance based
        /// </summary>
        /// <param name="cPed"></param>
        /// <returns></returns>
        public bool CouldSeePed(CPed cPed)
        {
            // Calculate distance (2D here, peds don't always look in the sky)
            float distance = this.ped.Position.DistanceTo2D(cPed.Position);
            // If too far away, return false
            if (distance > MaxViewRange) return false;
            return true;
        }

        /// <summary>
        /// Properly deletes the PedIntelligence object by freeing all resources and events
        /// </summary>
        public new void Delete()
        {
            this.TaskManager.ClearTasks();

            // Shutdown extension logic.
            foreach (var registeredIntelligence in registeredIntelligences)
            {
                registeredIntelligence.Key.Shutdown();
            }

            // Unregister all events
            EventPedBeingArrested.EventRaised -= new EventPedBeingArrested.EventRaisedEventHandler(this.EventPedBeingArrested_EventRaised);
            EventFleeingCriminal.EventRaised -= new EventFleeingCriminal.EventRaisedEventHandler(this.EventFleeingCriminal_EventRaised);
            base.Delete();
        }

        public CPed GetClosestPed(EPedSearchCriteria pedSearchCriteria, float distance)
        {
            return CPed.GetClosestPed(pedSearchCriteria, distance, this.ped.Position);
        }

        public CVehicle GetClosestVehicle(EVehicleSearchCriteria vehicleSearchCriteria, float distance)
        {
            return CVehicle.GetClosestVehicle(vehicleSearchCriteria, distance, this.ped.Position, blacklistedVehicles);
        }

        public CPed[] GetPedsAround(float distance, EPedSearchCriteria pedSearchCriteria)
        {
            return CPed.GetPedsAround(distance, pedSearchCriteria, this.ped.Position);
        }

        public CVehicle[] GetVehiclesAround(float distance, EVehicleSearchCriteria vehicleSearchCriteria)
        {
            return CVehicle.GetVehiclesAround(distance, vehicleSearchCriteria, this.ped.Position);
        }

        /// <summary>
        /// Returns the ped, the player is aiming at. Doesn't work for melee weapons.
        /// </summary>
        /// <returns>The ped aiming at.</returns>
        public CPed GetTargetedPed()
        {
            APed p = this.ped.APed.GetCharCharIsAimingAt();
            CPed targetedPed;

            if (p == null || p.Get() == 0)
            {
                if (this.ped.PedGroup == EPedGroup.Player)
                {
                    targetedPed = CPlayer.LocalPlayer.GetPedAimingAt();
                    if (targetedPed != null)
                    {
                        return targetedPed;
                    }
                }

                return null;
            }

            targetedPed = Pools.PedPool.GetPedFromPool(p);
            if (targetedPed != null && targetedPed.Exists())
            {
                return targetedPed;
            }

            return null;
        }

        /// <summary>
        /// Checks whether the ped is free for an action with <paramref name="actionPriority"/>.
        /// </summary>
        /// <param name="actionPriority">The action priority.</param>
        /// <returns>True if free, false if not.</returns>
        public bool IsFreeForAction(EPedActionPriority actionPriority)
        {
            int currentPriority = (int)this.currentActionPriority;
            return (int)actionPriority > currentPriority;
        }

        /// <summary>
        /// Returns whether the ped is still assigned to <paramref name="pedController"/>.
        /// </summary>
        /// <param name="pedController">
        /// The ped controller.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsStillAssignedToController(IPedController pedController)
        {
            return this.PedController == pedController;
        }

        /// <summary>
        /// Returns whether <paramref name="vehicle"/> is blacklisted for the ped. Keep in mind that blacklisted vehicles are cleared after
        /// a certain amount of time.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <returns>True if blacklisted, false if not.</returns>
        public bool IsVehicleBlacklisted(CVehicle vehicle)
        {
            return this.blacklistedVehicles.Contains(vehicle);
        }

        /// <summary>
        /// Requests the ped to perform an action with <paramref name="actionPriority"/> for <paramref name="pedController"/>.
        /// </summary>
        /// <param name="actionPriority">
        /// The action priority.
        /// </param>
        /// <param name="pedController">
        /// The ped controller.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RequestForAction(EPedActionPriority actionPriority, IPedController pedController)
        {
            if (this.IsFreeForAction(actionPriority))
            {
                if (this.PedController != null)
                {
                    // Let old controller know
                    this.ResetAction(this.PedController);
                }

                this.currentActionPriority = actionPriority;
                this.PedController = pedController;
                return true;
            }
            else
            {
                Log.Debug("RequestForAction: Attempt to request " + actionPriority + " while currently at " + this.currentActionPriority, this);
            }

            return false;
        }

        /// <summary>
        /// Resets the action for <paramref name="pedController"/>. If the current controller does not match, nothing is done.
        /// </summary>
        /// <param name="pedController">The ped controller.</param>
        public void ResetAction(IPedController pedController)
        {
            this.ResetAction(pedController, false);
        }

        /// <summary>
        /// Resets the action for <paramref name="pedController"/>. If the current controller does not match, nothing is done.
        /// </summary>
        /// <param name="pedController">The ped controller.</param>
        /// <param name="force">When this is true, the controller check is bypassed.</param>
        public void ResetAction(IPedController pedController, bool force)
        {
            if (this.PedController == pedController || force)
            {
                if (this.PedController != null)
                {
                    IPedController controller = this.PedController;
                    this.PedController = null;
                    controller.PedHasLeft(this.ped);
                    this.currentActionPriority = EPedActionPriority.Idle;
                }
            }
        }

        /// <summary>
        /// Makes the ped say <paramref name="text"/> by displaying it above its head for <paramref name="time"/> milliseconds.
        /// </summary>
        /// <param name="text">The ped.</param>
        /// <param name="time">The time</param>
        public void SayText(string text, int time)
        {
            this.SetDrawTextAbovePedsHead(text);
            this.SetDrawTextAbovePedsHeadEnabled(true);

            DelayedCaller.Call(delegate { this.SetDrawTextAbovePedsHeadEnabled(false); }, this, time);
        }

        /// <summary>
        /// Sets whether a text can be drawn above the ped's head.
        /// </summary>
        /// <param name="enabled">True if enabled, false otherwise.</param>
        public void SetDrawTextAbovePedsHeadEnabled(bool enabled)
        {
            this.drawTextEnabled = enabled;

            if (this.drawTextEnabled)
            {
                if (drawTextFont == null)
                {
                    drawTextFont = new Font(18f, FontScaling.Pixel);
                }

                GUI.Gui.PerFrameDrawing += this.Gui_PerFrameDrawing;
            }
            else
            {
                GUI.Gui.PerFrameDrawing -= this.Gui_PerFrameDrawing;
            }
        }

        /// <summary>
        /// Sets the range of text drawn above the ped's head
        /// </summary>
        /// <param name="range">The range (Default is 9)</param>
        public void SetDrawTextAbovePedsHeadRange(int range)
        {
            this.drawTextRange = range;
        }

        /// <summary>
        /// Sets the <paramref name="text"/> which should be drawn above the ped's head.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetDrawTextAbovePedsHead(string text)
        {
            this.drawText = text;
        }

        public void OnBeingArrested(CPed by)
        {
            // Chance our ped will surrender. Wanted.CanSurrender?
            if (this.ped.Wanted.Surrendered) return;

            // Get number of armed cops around
            CPed[] cops = this.ped.Intelligence.GetPedsAround(20f, EPedSearchCriteria.CopsOnly | EPedSearchCriteria.NotAvailable | EPedSearchCriteria.HaveOwner | EPedSearchCriteria.Player);
            int chance = 0;
            if (cops != null)
            {
                chance = cops.Length;
            }

            bool surrender = Common.GetRandomValue(0, chance) != 0;
            if (this.ped.PedData.WillSurrender || surrender)
            {            
                this.ped.Intelligence.TaskManager.ClearTasks();
                this.ped.Task.ClearAll();
                this.ped.Wanted.Surrendered = true;
            }
            else
            {
                // Abort all activity. Note: In a chase, the chase class has to start all necessary tasks again
                this.ped.Intelligence.TaskManager.ClearTasks();

                // Cancel all tasks
                this.ped.Task.ClearAllImmediately();

                this.ped.Wanted.TimesArrestResisted++;
                this.ped.Wanted.IsDeciding = true;
                this.ped.Wanted.ResistedArrest = true;
                this.ped.Task.HandsUp(2500);

                DelayedCaller.Call(this.OnBeingArrestedCallback, 1500, by);
            }
        }

        private void OnBeingArrestedCallback(params object[] data)
        {
            if (this.ped.Exists())
            {
                this.ped.EnsurePedHasWeapon();
                this.ped.BlockPermanentEvents = false;
                this.ped.Wanted.IsDeciding = false;
                this.ped.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                this.ped.Task.FightAgainst((CPed)data[0]);
            }
        }

        public void OnCopAsksToDropWeapon(params object[] data)
        {
            if (this.ped.Wanted.Surrendered || this.ped.Wanted.ResistedDropWeapon) return;

            // Get number of armed cops around
            CPed[] cops = this.ped.Intelligence.GetPedsAround(20f, EPedSearchCriteria.CopsOnly | EPedSearchCriteria.NotAvailable | EPedSearchCriteria.HaveOwner | EPedSearchCriteria.Player);
            int chance = 0;
            if (cops != null)
            {
                chance = cops.Length;
            }

            bool surrender = Common.GetRandomValue(0, chance) != 0;
            if (this.ped.PedData.DropWeaponWhenAskedByCop || surrender)
            {
                // Drop weapon and surrender
                this.ped.Intelligence.TaskManager.ClearTasks();
                this.ped.Task.HandsUp(10000);
                this.ped.Wanted.Surrendered = true;
                this.ped.Wanted.IsDeciding = true;
                DelayedCaller.Call(this.OnCopsAsksToDropWeaponCallback, 4500);
            }
            else
            {
                // Abort all activity. Note: In a chase, the chase class has to start all necessary tasks again
                this.ped.Intelligence.TaskManager.ClearTasks();

                // Cancel all tasks
                this.ped.Task.ClearAllImmediately();
                this.ped.Task.ClearAll();
                this.ped.Wanted.ResistedDropWeapon = true;
                this.ped.Wanted.IsDeciding = true;
                this.ped.Task.HandsUp(2500);

                DelayedCaller.Call(this.OnBeingArrestedCallback, 1500, (CPed)data[0]);
            }
        }

        /// <summary>
        /// Sets Wanted.IsDeciding to false, so the bust task can process. This delay is used, so the user can't tell if criminal will resist or surrender
        /// </summary>
        /// <param name="data">Leave null</param>
        private void OnCopsAsksToDropWeaponCallback(params object[] data)
        {
            if (this.ped.Exists())
            {
                this.ped.Wanted.IsDeciding = false;
                this.ped.EnsurePedHasNoWeapon();
            }
        }

        /// <summary>
        /// Makes the ped surrender.
        /// </summary>
        public void Surrender()
        {
            this.ped.PedData.AlwaysSurrender = true;
            this.OnBeingArrested(null);
            this.ped.Wanted.IsStopped = true;
            this.ped.PedData.DisableChaseAI = true;
        }

        public void RegisterExtendedIntelligence(IExtendedIntelligence intelligence, int priority)
        {
            // Although we want to index the dictionary by IExtendedIntelligence, we want it to be sorted by priority.
            var tempSort = this.registeredIntelligences.ToDictionary(entry => entry.Key, entry => entry.Value);
            this.registeredIntelligences.Clear();

            tempSort.Add(intelligence, priority);
            var sortedDict = from entry in tempSort orderby entry.Value ascending select entry;
            foreach (var pair in sortedDict)
            {
                this.registeredIntelligences.Add(pair.Key, pair.Value);
            }

            // Initialize logic.
            intelligence.Initialize();
        }

        public void Process()
        {
            if (!this.processScheduler.CanExecute())
            {
                return;
            }

            // Safety first
            if (!this.ped.Exists())
            {
                this.ped.Delete();
                return;
            }

            if (!this.ped.IsAliveAndWell)
            {
                if (!this.reportedDeath)
                {
                    new EventPedDead(this.ped);
                    this.reportedDeath = true;

                    // TODO: Either move or add event to this class, as receiving the generic EventPedDead event and if ped is 'this' would eat some performance
                    if (!this.ped.DontRemoveBlipOnDeath)
                    {
                        this.ped.DeleteBlip();
                    }
                    if (this.ped.AlwaysFreeOnDeath)
                    {
                        this.ped.NoLongerNeeded();
                    }

                    foreach (var registeredIntelligence in registeredIntelligences)
                    {
                        registeredIntelligence.Key.HasBeenKilled();
                    }
                }

                if (this.drawTextEnabled)
                {
                    this.SetDrawTextAbovePedsHeadEnabled(false);
                }

                this.TaskManager.Shutdown();

                return;
            }

            // Check for things for flags - not for ambient peds though (would be quite too much to do for a little CPU :)
            if (this.ped.PedGroup == EPedGroup.Criminal)
            {
                if (this.ped.IsShooting)
                {
                    this.ped.Wanted.WeaponUsed = true;
                }
            }

            // Reset reportedDeath when player is alive
            if (this.ped.PedGroup == EPedGroup.Player)
            {
                if (this.ped.IsAliveAndWell)
                {
                    this.reportedDeath = false;
                }
            }

            if (this.drawTextEnabled)
            {
                Vector2 screenPosition;
                Vector3 worldPosition = this.ped.GetBonePosition(Bone.Head);

                // Slightly above ped's head
                float distance = Game.CurrentCamera.Position.DistanceTo(this.ped.Position);
                worldPosition.Z += 0.08f * distance;

                // Cap distance
                if (distance > this.drawTextRange || !this.ped.IsOnScreen)
                {
                    this.drawX = 0;
                    this.drawY = 0;
                }
                else
                {
                    bool ret = GUI.Gui.GetRelativeScreenPositionFromWorldPosition(worldPosition, EViewportID.CViewportGame, out screenPosition);
                    this.drawX = screenPosition.X;
                    this.drawY = screenPosition.Y;
                }
            }

            // Don't run any logic for peds not controlled by us in a MP session.
            if (Main.NetworkManager.IsNetworkSession && !this.ped.IsControlledByUs)
            {
                return;
            }

            this.TaskManager.Process();

            // Now that core logic has executed, run extension modules.
            foreach (var registeredIntelligence in registeredIntelligences)
            {
                registeredIntelligence.Key.Process();
            }
        }

        /// <summary>
        /// Called when a ped is being arrested.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventPedBeingArrested_EventRaised(EventPedBeingArrested @event)
        {
            if (!this.ped.Exists() || !@event.Ped.Exists()) return;
            if (@event.Ped == this.ped) return;

            if (this.ped.PedGroup == EPedGroup.Pedestrian && !this.ped.IsInVehicle())
            {
                // Dont execute task when in the middle of a street or if we are fleeing
                if (this.ped.IsOnStreet() 
                    || this.ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask)
                    || this.ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                {
                    return;
                }

                // If not already looking, look at now
                if (!this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.LookAtPed) && this.IsFreeForAction(EPedActionPriority.ShockingEventWatch))
                {
                    // It is realistic that the ped would also turn around to see the criminal even if he's not in the current field of view (since our peds may turn, the criminal is loud etc.)
                    // So we set onlyIfPedCouldSeePed to true
                    // 50/50 chance whether ped stands still or not
                    bool standStill = Common.GetRandomBool(0, 2, 1);
                    TaskLookAtPed task = new TaskLookAtPed(@event.Ped, false, true, standStill, Common.GetRandomValue(2500, 7000));
                    this.ped.Intelligence.TaskManager.Assign(task, ETaskPriority.SubTask);
                }
            }
        }

        /// <summary>
        /// Called when a criminal is fleeing.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventFleeingCriminal_EventRaised(EventFleeingCriminal @event)
        {
            // Warning: Pedestrians may have already despawned, so check for existence here
            if (!this.ped.Exists() || !@event.Criminal.Exists()) return;
            if (@event.Criminal == this.ped) return;

            if (this.ped.PedGroup == EPedGroup.Pedestrian && !this.ped.IsInVehicle())
            {
                // If too close, flee
                if (this.ped.Position.DistanceTo(@event.Criminal.Position) < 10 && this.IsFreeForAction(EPedActionPriority.ShockingEventFlee))
                {
                    if (!this.ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                    {
                        this.ped.Task.FleeFromChar(@event.Criminal);
                    }
                }
                else
                {
                    // Dont execute task when in the middle of a street or if we are fleeing
                    if (this.ped.IsOnStreet()
                        || this.ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask)
                        || this.ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity))
                    {
                        return;
                    }

                    // If not already looking, look at now
                    if (!this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.LookAtPed) && this.IsFreeForAction(EPedActionPriority.ShockingEventWatch))
                    {
                        // It is realistic that the ped would also turn around to see the criminal even if he's not in the current field of view (since our peds may turn, the criminal is loud etc.)
                        // So we set onlyIfPedCouldSeePed to true
                        // 50/50 chance whether ped stands still or not
                        bool standStill = Common.GetRandomBool(0, 2, 1);
                        TaskLookAtPed task = new TaskLookAtPed(@event.Criminal, false, true, standStill, Common.GetRandomValue(2500, 7000));
                        this.ped.Intelligence.TaskManager.Assign(task, ETaskPriority.SubTask);
                    }
                }
            }
        }

        /// <summary>
        /// Called every frame to draw on top of the rendered D3D frame.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The graphics event.</param>
        private void Gui_PerFrameDrawing(object sender, GraphicsEventArgs e)
        {
            if (this.drawTextEnabled)
            {
                if (this.drawX != 0 && this.drawY != 0)
                {
                    float realX = this.drawX * Game.Resolution.Width;
                    float realY = this.drawY * Game.Resolution.Height;

                    e.Graphics.DrawText(this.drawText, realX, realY, System.Drawing.Color.White, drawTextFont);
                }
            }
        }
    }

    /*
    [StructLayout(LayoutKind.Sequential, Size = 0x2F0), Serializable]
    internal struct IVPedIntelligence
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)] 
        public byte[] pad0;
        [MarshalAs(UnmanagedType.I4)] 
        public int m_pPed;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        public byte[] m_pedTaskManager;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x48)]
        public byte[] m_eventGroup;
        [MarshalAs(UnmanagedType.I4)] 
        public int field_CC;
        [MarshalAs(UnmanagedType.I4)] 
        public int m_dwCharDecisionMaker;
        [MarshalAs(UnmanagedType.I4)]
        public int m_dwGroupCharDecisionMaker;
        [MarshalAs(UnmanagedType.I4)]
        public int m_dwCombatDecisionMaker;
        [MarshalAs(UnmanagedType.I4)]
        public int m_dwGroupCombatDecisionMaker;
        [MarshalAs(UnmanagedType.R4)]
        public float m_fSenseRange2;
        [MarshalAs(UnmanagedType.R4)]
        public float m_fSenseRange1;
        // PAD THE REST TIL 0x2F0
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x208)]
        public byte[] pad1;
    }*/

    /// <summary>
    /// Flags to specify a ped search.
    /// </summary>
    [Flags]
    enum EPedSearchCriteria
    {
        /// <summary>
        /// Includes all peds. Warning: This ignores all other flags
        /// </summary>
        All = 0x1,
        /// <summary>
        /// Includes all peds, that are no cops and that are available
        /// </summary>
        AmbientPed = 0x2,
        /// <summary>
        /// Include cops
        /// </summary>
        Cops = 0x4,
        /// <summary>
        /// Only allow cops
        /// </summary>
        CopsOnly = 0x8,
        /// <summary>
        /// Include peds that have an owner
        /// </summary>
        HaveOwner = 0x10,
        /// <summary>
        /// Include peds that are not available (such as cop on chase)
        /// </summary>
        NotAvailable = 0x20,
        /// <summary>
        /// Include player
        /// </summary>
        Player = 0x40,
        /// <summary>
        /// Include cops that are transporting a suspect
        /// </summary>
        SuspectTransporter = 0x80,

        /// <summary>
        /// Ped mustn't be in a vehicle.
        /// </summary>
        NotInVehicle = 0x100,
    }


    // TODO: Restructure (like the assembly binding flags)
    /// <summary>
    /// Search criteria for the vehicles.
    /// </summary>
    [Flags]
    internal enum EVehicleSearchCriteria
    {
        /// <summary>
        /// All vehicles.
        /// </summary>
        All = 0x0,

        /// <summary>
        /// Include broken vehicles.
        /// </summary>
        Broken = 0x1,

        /// <summary>
        /// Include cop vehicles.
        /// </summary>
        CopOnly = 0x2,

        /// <summary>
        /// Must have a driver.
        /// </summary>
        DriverOnly = 0x4,

        /// <summary>
        /// Must have a free rear seat (either left or right).
        /// </summary>
        FreeRearSeatOnly = 0x8,

        /// <summary>
        /// Musn't have a driver.
        /// </summary>
        NoDriverOnly = 0x10,

        /// <summary>
        /// Don't include the current or last vehicle of the player.
        /// </summary>
        NoPlayersLastVehicle = 0x20,

        /// <summary>
        /// Only allow stopped vehicles.
        /// </summary>
        StoppedOnly = 0x40,

        /// <summary>
        /// No cop vehicles.
        /// </summary>
        NoCop = 0x80,

        /// <summary>
        /// No vehicles with cop as driver.
        /// </summary>
        NoCarsWithCopDriver = 0x100,
    }

    [Flags]
    enum VehicleSearchCriteria
    {
        Broken = 0x1,
        Cop = 0x2,
        Driver,
        FreeRearOnly,
        NoDriver,
        Pedestrian,
        Stopped,
    }

    /// <summary>
    /// Priorities for ped actions. The names are not associated with any direct actions, but should just give a general idea.
    /// </summary>
    internal enum EPedActionPriority
    {
        /// <summary>
        /// Ped does nothing.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Ped is running an ambient task, that can be dropped easily. Ped will still respond to shocking events.
        /// </summary>
        AmbientTask = 10,

        /// <summary>
        /// Ped is running a more important ambient task. Ped will still respond to shocking events.
        /// </summary>
        AmbientTaskImportant = 20,

        /// <summary>
        /// Ped is watching a shocking event.
        /// </summary>
        ShockingEventWatch = 30,

        /// <summary>
        /// Ped is fleeing because of a shocking event.
        /// </summary>
        ShockingEventFlee = 40,

        /// <summary>
        /// Ped is required by a script.
        /// </summary>
        RequiredByScript = 100,

        /// <summary>
        /// Ped is required by a script critical for gameplay.
        /// </summary>
        RequiredByScriptCritical = 200,

        /// <summary>
        /// Ped is required for direct user interaction.
        /// </summary>
        RequiredForUserInteraction = 1000,
    }
}