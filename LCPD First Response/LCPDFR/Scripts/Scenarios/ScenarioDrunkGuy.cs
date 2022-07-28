namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// The scenario where a drunken guy is spawned.
    /// </summary>
    internal class ScenarioDrunkGuy : Scenario, IAmbientScenario, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The position.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The texts the drunkard can say.
        /// </summary>
        private string[] drunkenTexts = { "God, I'm so drunk.", "Can you help me mister?", "I'm alright, officer!", "Leave me alone!" };

        /// <summary>
        /// The texts peds close to the drunkard can say.
        /// </summary>
        private string[] drunkenPedsTexts = { "Fuckin drunkard...", "Get sober, idiot!", "You smell like shit!", "Want me to call 911?" };

        /// <summary>
        /// Whether player has approached driver for the first time.
        /// </summary>
        private bool notFirstApproach;

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ScenarioDrunkGuy";
            }
        }

        /// <summary>
        /// This is called immediately before the scenario is executed the first time.
        /// </summary>
        public override void Initialize()
        {
            this.ped.RequestOwnership(this);
            this.ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
            this.ped.PedData.ComplianceChance = 30;
            this.ped.BlockPermanentEvents = true;
            this.ped.PedData.CanResistArrest = false;
            // this.ped.AttachBlip().Color = BlipColor.White;
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            base.MakeAbortable();

            if (this.ped != null && this.ped.Exists())
            {
                if (this.ped.Intelligence.IsStillAssignedToController(this))
                {
                    this.ped.ReleaseOwnership(this);
                    this.ped.Intelligence.ResetAction(this);

                    this.ped.Intelligence.TaskManager.ClearTasks();
                    this.ped.NoLongerNeeded();
                }
                else
                {
                    // this.ped.DeleteBlip();
                }
            }
        }

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public override void Process()
        {
            if (this.ped == null || !this.ped.Exists() || !this.ped.IsAliveAndWell)
            {
                this.MakeAbortable();
                return;
            }

            if (this.ped.Wanted.HasBeenArrested)
            {
                this.MakeAbortable();
                return;
            }

            if (Common.GetRandomBool(0, 500, 1) && !this.ped.Intelligence.DrawTextEnabled)
            {
                this.ped.Intelligence.SetDrawTextAbovePedsHead(Common.GetRandomCollectionValue<string>(this.drunkenTexts));
                this.ped.Intelligence.SetDrawTextAbovePedsHeadEnabled(true);

                DelayedCaller.Call(delegate { this.ped.Intelligence.SetDrawTextAbovePedsHeadEnabled(false); }, this, 5000);
            }

            // Make closest random ped complain
            CPed closestPed = this.ped.Intelligence.GetClosestPed(EPedSearchCriteria.AmbientPed | EPedSearchCriteria.NotInVehicle, 10f);
            if (closestPed != null && closestPed.Exists() && closestPed != this.ped)
            {
                if (!closestPed.Intelligence.DrawTextEnabled)
                {
                    closestPed.Intelligence.SetDrawTextAbovePedsHead(Common.GetRandomCollectionValue<string>(this.drunkenPedsTexts));
                    closestPed.Intelligence.SetDrawTextAbovePedsHeadEnabled(true);
                    if (!closestPed.IsAmbientSpeechPlaying)
                    {
                        closestPed.SayAmbientSpeech("BUMP");
                    }

                    DelayedCaller.Call(delegate { closestPed.Intelligence.SetDrawTextAbovePedsHeadEnabled(false); }, this, 5000);
                }
            }

            if (!this.notFirstApproach)
            {
                this.notFirstApproach = CameraHelper.PerformEventFocus(this.ped, true, 1000, 3500, true, false, true);
            }

            if (!this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkDrunk))
            {
                TaskWalkDrunk taskWalkDrunk = new TaskWalkDrunk();
                taskWalkDrunk.AssignTo(this.ped, ETaskPriority.MainTask);
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
            CPed[] peds = CPed.GetPedsAround(80f, EPedSearchCriteria.AmbientPed | EPedSearchCriteria.NotInVehicle, position);
            foreach (CPed ped in peds)
            {
                // Ped should not be seen by player at the moment
                if (ped.Intelligence.IsFreeForAction(EPedActionPriority.RequiredByScript) && !ped.IsOnStreet() && !CPlayer.LocalPlayer.HasVisualOnSuspect(ped) 
                    && !ped.IsInBuilding() && ped.IsAliveAndWell)
                {
                    this.ped = ped;
                    return true;
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
            // Only if ped has not been arrested yet
            if (this.ped.Exists() && !this.ped.Wanted.HasBeenArrested && this.ped.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 150)
            {
                this.MakeAbortable();
                return true;
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
            this.ped.ReleaseOwnership(this);
            this.ped.Intelligence.ResetAction(this);
        }
    }
}