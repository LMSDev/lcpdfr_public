namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// A testing scenario.
    /// </summary>
    internal class ScenarioDrugdeal : Scenario, IAmbientScenario, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// The animations.
        /// </summary>
        private List<Animation> animations;

        /// <summary>
        /// The blip for the drug deal.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The ped trying to buy drugs.
        /// </summary>
        private CPed customer;

        /// <summary>
        /// Whether the deal was interruped by player.
        /// </summary>
        private bool interruped;

        /// <summary>
        /// Whether the ped is chatting.
        /// </summary>
        private bool isChatting;

        /// <summary>
        /// Whether player has approached driver for the first time.
        /// </summary>
        private bool notFirstApproach;

        /// <summary>
        /// The scenario ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The possible pursuit when suspects flee.
        /// </summary>
        private Pursuit pursuit;

        /// <summary>
        /// The position.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ScenarioDrugDeal";
            }
        }

        /// <summary>
        /// Initializes the scenario.
        /// </summary>
        public override void Initialize()
        {
            this.ped.RequestOwnership(this);
            this.ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);

            //this.ped.AttachBlip().Color = BlipColor.White;
            this.ped.Task.StandStill(int.MaxValue);

            this.animations = new List<Animation>();
            this.animations.Add(new Animation("amb@drugd_idl_a", "idle_a"));
            this.animations.Add(new Animation("amb@drugd_idl_a", "idle_b"));
            this.animations.Add(new Animation("amb@drugd_idl_b", "idle_c"));
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            base.MakeAbortable();

            if (this.ped.Exists())
            {
                this.ped.Intelligence.TaskManager.ClearTasks();
            }

            if (this.customer != null && this.customer.Exists())
            {
                this.customer.Intelligence.TaskManager.ClearTasks();
            }

            if (this.pursuit != null && this.pursuit.IsRunning)
            {
                this.pursuit.EndChase();
            }

            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }
        }

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public override void Process()
        {
            if (this.OwnedEntities.Count == 0)
            {
                this.MakeAbortable();
                return;
            }

            // Deleted, dead or arrest -> End
            if (this.ped != null && (!this.ped.Exists() || !this.ped.IsAliveAndWell || this.ped.Wanted.HasBeenArrested))
            {
                this.PedHasLeft(this.ped);
            }

            if (this.customer != null && (!this.customer.Exists() || !this.customer.IsAliveAndWell || this.customer.Wanted.HasBeenArrested))
            {
                this.PedHasLeft(this.customer);
            }

            // Look for customer
            if (this.customer == null)
            {
                if (!this.ped.Exists())
                {
                    this.MakeAbortable();
                    return;
                }

                // Look for peds meeting the requirements
                CPed[] peds = CPed.GetPedsAround(10f, EPedSearchCriteria.AmbientPed | EPedSearchCriteria.NotInVehicle, this.ped.Position);
                foreach (CPed ped in peds)
                {
                    if (ped.Intelligence.IsFreeForAction(EPedActionPriority.RequiredByScript) && ped.IsAliveAndWell && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.HasJob))
                    {
                        this.customer = ped;
                        this.customer.RequestOwnership(this);
                        this.customer.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                        this.customer.Task.ClearAll();
                        // this.customer.AttachBlip().Color = BlipColor.Orange;
                        break;
                    }
                }
            }

            // If customer exists, make him talk to drug dealer
            if (this.customer != null && this.customer.Exists())
            {
                if (!this.customer.IsAliveAndWell)
                {
                    this.customer = null;
                    return;
                }

                // If customer is not chatting, start task
                if (!this.customer.Intelligence.TaskManager.IsTaskActive(ETaskID.Chat) && !this.isChatting)
                {
                    // Mark crime area
                    // this.blip = AreaBlocker.CreateAreaBlip(this.ped.Position, 40f, BlipColor.Red);
                    this.ped.PedData.Luggage = PedData.EPedLuggage.Drugs;

                    // Setup chat task with one animation
                    TaskChat taskChat = new TaskChat(this.ped);
                    taskChat.SetAnimations(new Animation[] { Common.GetRandomCollectionValue<Animation>(this.animations.ToArray()) });
                    taskChat.AssignTo(this.customer, ETaskPriority.MainTask);
                }
                else if (!this.isChatting)
                {
                    // If customer has the chatting task running and the conversation has started, so he is in position
                    TaskChat taskChat = this.customer.Intelligence.TaskManager.FindTaskWithID(ETaskID.Chat) as TaskChat;
                    if (taskChat.InConversation)
                    {
                        // Start chatting task for our ped
                        if (!this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.Chat))
                        {
                            taskChat = new TaskChat(this.customer, true);
                            taskChat.SetAnimations(new Animation[] { Common.GetRandomCollectionValue<Animation>(this.animations.ToArray()) });
                            taskChat.AssignTo(this.ped, ETaskPriority.MainTask);
                            this.isChatting = true;

                            // After some time, play the give/take animations and cancel
                            Action action = delegate
                                {
                                    // If not yet interrupted
                                    if (!this.interruped)
                                    {
                                        // Verify whether peds still exist
                                        if (this.customer != null && this.customer.Exists() && this.ped != null && this.ped.Exists())
                                        {
                                            this.customer.Intelligence.TaskManager.ClearTasks();
                                            this.ped.Intelligence.TaskManager.ClearTasks();
                                            this.customer.Task.PlayAnimation(new AnimationSet("amb@drugd_sell"), "buy_drugs", 4f);
                                            this.ped.Task.PlayAnimation(new AnimationSet("amb@drugd_sell"), "sell_drugs", 4f);
                                            this.customer.PedData.Luggage = PedData.EPedLuggage.Drugs;

                                            DelayedCaller.Call(
                                            delegate
                                            {
                                                // If not yet interrupted
                                                if (!this.interruped)
                                                {
                                                    this.MakeAbortable();
                                                }
                                            },
                                            3000);
                                        }
                                        else
                                        {
                                            Log.Warning("Process: Peds have been disposed during scenario", this);
                                        }
                                    }
                                };

                            DelayedCaller.Call(delegate { action(); }, 10000);
                        }
                    }
                }
            }

            if (!this.notFirstApproach)
            {
                this.notFirstApproach = CameraHelper.PerformEventFocus(this.ped, true, 1000, 3500, true, false, true);
            }

            // If player is close and targeting at one of the peds, make them flee. If player is aiming, chance to resist or fight
            if (this.isChatting && !this.interruped && CPlayer.LocalPlayer.Ped.Position.DistanceTo(this.ped.Position) < 5 && CPlayer.LocalPlayer.Ped.HasSpottedCharInFront(this.ped))
            {
                // If player is targetting, flee
                if (CPlayer.LocalPlayer.IsTargettingChar(this.ped) || CPlayer.LocalPlayer.IsTargettingChar(this.customer))
                {
                    // Simple on foot chase
                    this.pursuit = new Pursuit();
                    this.pursuit.AllowSuspectVehicles = false;
                    this.pursuit.AllowSuspectWeapons = false;
                    this.pursuit.CanCopsJoin = false;

                    if (this.blip != null && this.blip.Exists())
                    {
                        this.blip.Delete();
                    }

                    this.interruped = true;
                    this.customer.Intelligence.TaskManager.ClearTasks();
                    this.customer.Task.ClearAllImmediately();
                    this.ped.Intelligence.TaskManager.ClearTasks();
                    this.ped.Task.ClearAllImmediately();

                    if (Common.GetRandomBool(0, 4, 1))
                    {
                        this.customer.Intelligence.Surrender();
                        this.customer.BlockPermanentEvents = true;
                        this.customer.Task.HandsUp(int.MaxValue);
                        this.customer.AttachBlip();
                    }
                    else
                    {
                        // So player can't arrest for a few seconds
                        this.customer.PedData.CanBeArrestedByPlayer = false;
                        this.pursuit.AddTarget(this.customer);
                        DelayedCaller.Call(delegate { this.customer.PedData.CanBeArrestedByPlayer = true; }, 5000);
                    }

                    if (Common.GetRandomBool(0, 6, 1))
                    {
                        this.ped.Intelligence.Surrender();
                        this.ped.BlockPermanentEvents = true;
                        this.ped.Task.HandsUp(int.MaxValue);
                        this.ped.AttachBlip();
                    }
                    else
                    {
                        // So player can't arrest for a few seconds
                        this.ped.PedData.CanBeArrestedByPlayer = false;
                        this.pursuit.AddTarget(this.ped);
                        DelayedCaller.Call(delegate { this.ped.PedData.CanBeArrestedByPlayer = true; }, 5000);
                    }

                    // If there are any criminals
                    if (this.pursuit.Criminals.Count > 0)
                    {
                        DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALL_IN_PURSUIT")); }, 2500);
                        this.pursuit.MakeActiveChase(0, -1);
                    }
                    else
                    {
                        this.pursuit.EndChase();
                    }
                }

                if (CPlayer.LocalPlayer.GetPedAimingAt() == this.customer || CPlayer.LocalPlayer.GetPedAimingAt() == this.ped)
                {
                    // Simple on foot chase
                    this.pursuit = new Pursuit();
                    this.pursuit.AllowSuspectVehicles = false;
                    this.pursuit.AllowSuspectWeapons = false;
                    this.pursuit.CanCopsJoin = false;

                    if (this.blip != null && this.blip.Exists())
                    {
                        this.blip.Delete();
                    }

                    // Surrender or fight
                    this.interruped = true;
                    this.customer.Intelligence.TaskManager.ClearTasks();
                    this.customer.Task.ClearAllImmediately();
                    this.ped.Intelligence.TaskManager.ClearTasks();
                    this.ped.Task.ClearAllImmediately();

                    if (Common.GetRandomBool(0, 4, 1))
                    {
                        this.customer.Intelligence.Surrender();
                        this.customer.BlockPermanentEvents = true;
                        this.customer.Task.HandsUp(int.MaxValue);
                        this.customer.AttachBlip();
                    }
                    else
                    {
                        // So player can't arrest for a few seconds
                        this.customer.PedData.CanBeArrestedByPlayer = false;
                        this.pursuit.AddTarget(this.customer);
                        DelayedCaller.Call(delegate { this.customer.PedData.CanBeArrestedByPlayer = true; }, 5000);

                        if (Common.GetRandomBool(0, 3, 1))
                        {
                            this.customer.PedData.ComplianceChance = Common.GetRandomValue(0, 60);
                            this.pursuit.AllowSuspectVehicles = true;
                        }
                    }

                    if (Common.GetRandomBool(0, 4, 1))
                    {
                        this.ped.Intelligence.Surrender();
                        this.ped.BlockPermanentEvents = true;
                        this.ped.Task.HandsUp(int.MaxValue);
                        this.ped.AttachBlip();
                    }
                    else
                    {
                        this.ped.PedData.CanBeArrestedByPlayer = false;
                        this.pursuit.AddTarget(this.ped);
                        DelayedCaller.Call(delegate { this.ped.PedData.CanBeArrestedByPlayer = true; }, 5000);

                        if (Common.GetRandomBool(0, 3, 1))
                        {
                            this.ped.PedData.ComplianceChance = Common.GetRandomValue(0, 60);
                            this.pursuit.AllowSuspectVehicles = true;
                        }
                    }

                    // If there are any criminals
                    if (this.pursuit.Criminals.Count > 0)
                    {
                        DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALL_IN_PURSUIT")); }, 2500);
                        this.pursuit.MakeActiveChase(0, -1);
                    }
                    else
                    {
                        this.pursuit.EndChase();
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the scenario can start at the position depending on available peds.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <returns>
        /// True if yes, otherwise false.
        /// </returns>
        public bool CanScenarioStart(Vector3 position)
        {
            this.position = position;

            // Look for peds meeting the requirements
            CPed[] peds = CPed.GetPedsAround(30f, EPedSearchCriteria.AmbientPed | EPedSearchCriteria.NotInVehicle, position);
            foreach (CPed ped in peds)
            {
                if (ped.Exists() && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.HasJob) && ped.IsAliveAndWell)
                {
                    if (ped.Intelligence.IsFreeForAction(EPedActionPriority.RequiredByScript) && !ped.IsOnStreet() && !ped.IsInBuilding())
                    {
                        this.ped = ped;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the scenario can be disposed now, most likely because player got too far away.
        /// </summary>
        /// <returns>True if yes, false otherwise.</returns>
        public bool CanBeDisposedNow()
        {
            // Only allow disposing when not yet chasing
            if (this.pursuit == null || !this.pursuit.IsRunning)
            {
                // Calcuate distance to player
                if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(this.position) > 150)
                {
                    this.MakeAbortable();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
            if (ped == this.customer)
            {
                this.customer.ReleaseOwnership(this);
                this.customer.Intelligence.ResetAction(this);
                if (this.customer.HasBlip) this.customer.DeleteBlip();
            }

            if (ped == this.ped)
            {
                this.ped.ReleaseOwnership(this);
                this.ped.Intelligence.ResetAction(this);
                if (this.ped.HasBlip) this.ped.DeleteBlip();
            }

            // If we are not chatting yet, abort whole task
            if (!this.isChatting)
            {
                this.MakeAbortable();
            }
        }
    }
}