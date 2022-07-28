namespace LCPD_First_Response.LCPDFR.Callouts
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Scripts.Scenarios;

    /// <summary>
    /// A drunk driver spotted.
    /// </summary>
    [CalloutInfo("DrunkDriver", ECalloutProbability.High)]
    internal class DrunkDriver : Callout, IPedController
    {
        /// <summary>
        /// The ped.
        /// </summary>
        private LPed ped;

        /// <summary>
        /// The drunk driver scenario.
        /// </summary>
        private ScenarioDrunkDriver scenarioDrunkDriver;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private Vector3 spawnPosition;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private LVehicle vehicle;

        /// <summary>
        /// Vehicle models that can be used.
        /// </summary>
        private string[] vehicleModels = new string[] { "ADMIRAL", "BANSHEE", "BLISTA", "FUTO", "INGOT", "FACTION", "LANDSTALKER", "ORACLE", "SENTINEL", "PCJ", "FELTZER" };

        /// <summary>
        /// Initializes a new instance of the <see cref="DrunkDriver"/> class.
        /// </summary>
        public DrunkDriver()
        {
            this.CalloutMessage = CultureHelper.GetText("CALLOUT_DRUNK_DRIVER_MESSAGE");
        }

        /// <summary>
        /// Called just before the callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            // Get a good position
            this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(200.0f));

            while (this.spawnPosition.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100.0f)
            {
                this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(250.0f));
            }

            // Show user where callout is
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);

            // Get area name
            string area = AreaHelper.GetAreaNameMeaningful(this.spawnPosition);
            this.CalloutMessage = string.Format(CultureHelper.GetText("CALLOUT_DRUNK_DRIVER_MESSAGE"), area);

            // Play audio
            string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
            string crimeMessage = "CRIM_A_RECKLESS_DRIVER";
            if (Common.GetRandomBool(0, 2, 1))
            {
                crimeMessage = "CRIM_DANGEROUS_DRIVING";
            }

            Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPosition);

            return base.OnBeforeCalloutDisplayed();
        }

        /// <summary>
        /// Called when the callout has been accepted. Call base to set state to Running.
        /// </summary>
        /// <returns>
        /// True if callout was setup properly, false if it failed. Calls <see cref="End"/> when failed.
        /// </returns>
        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            // Create 
            this.vehicle = new LVehicle(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.vehicleModels));
            if (this.vehicle.Exists())
            {
                // Ensure vehicle is freed on end
                Functions.AddToScriptDeletionList(this.vehicle, this);
                this.vehicle.PlaceOnNextStreetProperly();

                // Spawn ped
                this.ped = new LPed(World.GetNextPositionOnStreet(this.vehicle.Position), CModel.GetRandomModel(EModelFlags.IsPed | EModelFlags.IsCivilian).ModelInfo.Name, LPed.EPedGroup.Criminal);
                if (this.ped.Exists())
                {
                    // Add to deletion list and to pursuit
                    Functions.AddToScriptDeletionList(this.ped, this);
                    Functions.SetPedIsOwnedByScript(this.ped, this, true);

                    this.ped.WarpIntoVehicle(this.vehicle, VehicleSeat.Driver);

                    // Start scenario
                    this.scenarioDrunkDriver = new ScenarioDrunkDriver(this.ped.CPed);
                    this.scenarioDrunkDriver.Initialize();
                    AmbientScenarioManager ambientScenarioManager = LCPDFR.Main.ScriptManager.GetRunningScriptInstances("AmbientScenarioManager")[0] as AmbientScenarioManager;
                    if (ambientScenarioManager != null)
                    {
                        ambientScenarioManager.AddRunningScenario(this.scenarioDrunkDriver);
                    }


                    Functions.PrintText(CultureHelper.GetText("CALLOUT_DRUNK_DRIVER_CATCH_UP"), 25000);
                    this.ped.AttachBlip();
                    return true;
                }
                else
                {
                    this.vehicle.Delete();
                }
            }

            return false;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.scenarioDrunkDriver.Active)
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the calloutmanager to shutdown the callout or can be called by the 
        /// callout itself to execute the cleanup code. Call base to set state to None.
        /// </summary>
        public override void End()
        {
            base.End();

            // Free ped
            if (this.ped != null)
            {
                Functions.RemoveFromDeletionList(this.ped, this);
            }

            if (this.vehicle != null)
            {
                Functions.RemoveFromDeletionList(this.vehicle, this);
            }

            if (this.ped != null && this.ped.Exists())
            {
                this.ped.NoLongerNeeded();
                Functions.SetPedIsOwnedByScript(this.ped, this, false);
            }

            if (this.vehicle != null && this.vehicle.Exists())
            {
                this.vehicle.NoLongerNeeded();
            }

            if (this.scenarioDrunkDriver != null)
            {
                this.scenarioDrunkDriver.MakeAbortable();
            }
        }

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        public override void PedLeftScript(LPed ped)
        {
            base.PedLeftScript(ped);

            // Free ped
            Functions.RemoveFromDeletionList(ped, this);
            Functions.SetPedIsOwnedByScript(ped, this, false);
        }
    }
}