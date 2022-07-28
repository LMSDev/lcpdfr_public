namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    /// <summary>
    /// Test callout.
    /// </summary>
    [CalloutInfo("TestCallout", ECalloutProbability.Never)]
    internal class TestCallout : Callout
    {
        /// <summary>
        /// Test ped.
        /// </summary>
        private CPed testPed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCallout"/> class.
        /// </summary>
        public TestCallout()
        {
            this.CalloutMessage = "Attention all units, we have two criminals fleeing by car in your vicinity";
        }

        /// <summary>
        /// Called when just before callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            Log.Debug("OnBeforeCalloutDisplayed", this);

            return true;
        }

        /// <summary>
        /// Called when the callout has been accepted. Call base when overriding.
        /// </summary>
        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            this.testPed = new CPed("M_Y_COP", CPlayer.LocalPlayer.Ped.Position, EPedGroup.MissionPed);
            this.ContentManager.AddPed(this.testPed, 15f, EContentManagerOptions.KillBeforeFree | EContentManagerOptions.DontDeleteBlip);
            if (this.testPed.Exists())
            {
                this.testPed.AttachBlip().Friendly = true;
                this.testPed.DontRemoveBlipOnDeath = true;
            }

            return true;
        }

        /// <summary>
        /// Called when the callout message is being displayed. Call base when overriding.
        /// </summary>
        public override void OnCalloutDisplayed()
        {
            base.OnCalloutDisplayed();

            Log.Debug("OnCalloutDisplayed", this);
        }

        /// <summary>
        /// Called when the callout hasn't been accepted. Call base when overriding.
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();

            Log.Debug("OnCalloutNotAccepted", this);
        }

        /// <summary>
        /// Called every tick to process all callout logic.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.testPed.Intelligence.TaskManager.IsTaskActive(ETaskID.FleeEvadeCopsOnFoot))
            {
                TaskFleeEvadeCopsOnFoot taskFleeEvadeCopsOnFoot = new TaskFleeEvadeCopsOnFoot(false, false, false, EVehicleSearchCriteria.All);
                taskFleeEvadeCopsOnFoot.AssignTo(this.testPed, ETaskPriority.MainTask);
            }
        }
    }
}
