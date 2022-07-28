namespace API_Example.Callouts
{
    using System;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Callouts;

    /// <summary>
    /// The robbery callout, also works as grand theft auto callout or simple pursuit. Due to this we use <see cref="ECalloutProbability.Always"/> so
    /// the different callout types are frequent enough.
    /// </summary>
    [CalloutInfo("Pursuit", ECalloutProbability.Always)]
    internal class Pursuit : Callout
    {
        /// <summary>
        /// The internal type of the callout.
        /// </summary>
        private ECalloutType calloutType;

        /// <summary>
        /// Criminal models that can be used.
        /// </summary>
        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };

        /// <summary>
        /// Vehicle models that can be used.
        /// </summary>
        private string[] vehicleModels = new string[] { "ADMIRAL", "BANSHEE", "BLISTA", "FUTO", "INGOT", "FACTION", "LANDSTALKER", "ORACLE", "SENTINEL", "PCJ", "FELTZER" };

        /// <summary>
        /// The pursuit.
        /// </summary>
        private LHandle pursuit;

        /// <summary>
        /// The robbers.
        /// </summary>
        private LPed[] robbers;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private LVehicle vehicle;

        /// <summary>
        /// The position at which the vehicles are spawned
        /// </summary>
        private Vector3 spawnPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pursuit"/> class.
        /// </summary>
        public Pursuit()
        {
            // Determine type
            this.calloutType = (ECalloutType)Common.GetRandomEnumValue(typeof(ECalloutType));

            // Get a good position
            this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));

            while (this.spawnPosition.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100.0f)
            {
                this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));
            }

            if (this.spawnPosition == Vector3.Zero)
            {
                // It obviously failed, set the position to be the player's position and the distance check will catch it.
                this.spawnPosition = LPlayer.LocalPlayer.Ped.Position;
            }

            // Show user where the pursuit is about to happen
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);

            // Set up message
            if (this.calloutType == ECalloutType.GrandTheftAuto)
            {
                this.CalloutMessage = string.Format(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_GTA_MESSAGE"), Functions.GetAreaStringFromPosition(this.spawnPosition));
                int randomValue = Common.GetRandomValue(0, 6);
                switch (randomValue)
                {
                    case 0:
                        Functions.PlaySoundUsingPosition("THIS_IS_CONTROL ATTENTION_ALL_UNITS INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_A_GRAND_THEFT_AUTO IN_OR_ON_POSITION", this.spawnPosition);
                        break;
                    case 1:
                        Functions.PlaySoundUsingPosition("ATTENTION_ALL_UNITS INS_WEVE_GOT CRIM_A_CRIMINAL_IN_A_STOLEN_CAR IN_OR_ON_POSITION", this.spawnPosition);
                        break;
                    case 2:
                        Functions.PlaySoundUsingPosition("INS_AVAILABLE_UNITS_RESPOND_TO CRIM_A_CRIMINAL_IN_A_STOLEN_VEHICLE IN_OR_ON_POSITION", this.spawnPosition);
                        break;
                    case 3:
                        Functions.PlaySoundUsingPosition("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR CRIM_A_SUSPECT_RESISTING_ARREST IN_OR_ON_POSITION", this.spawnPosition);
                        break;
                    case 4:
                        Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_A_CRIMINAL_IN_A_STOLEN_VEHICLE IN_OR_ON_POSITION", this.spawnPosition);
                        break;
                    case 5:
                        Functions.PlaySoundUsingPosition("ATTENTION_ALL_UNITS ASSISTANCE_REQUIRED FOR CRIM_A_GRAND_THEFT_AUTO IN_OR_ON_POSITION", this.spawnPosition);
                        break;
                }
            }
            else if (this.calloutType == ECalloutType.Pursuit)
            {
                this.CalloutMessage = string.Format(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_PURSUIT_MESSAGE"), Functions.GetAreaStringFromPosition(this.spawnPosition));
                Functions.PlaySoundUsingPosition(ESound.PursuitAcknowledged, this.spawnPosition);
            }
            else if (this.calloutType == ECalloutType.Robbery)
            {
                this.CalloutMessage = string.Format(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_ROBBERY_MESSAGE"), Functions.GetAreaStringFromPosition(this.spawnPosition));
                Functions.PlaySoundUsingPosition(ESound.PursuitAcknowledged, this.spawnPosition);
            }
        }

        /// <summary>
        /// The internal type of the callout.
        /// </summary>
        private enum ECalloutType
        {
            /// <summary>
            /// A grand theft auto.
            /// </summary>
            GrandTheftAuto,

            /// <summary>
            /// A normal pursuit.
            /// </summary>
            Pursuit,

            /// <summary>
            /// A robbery.
            /// </summary>
            Robbery,
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

            // Create pursuit instance
            this.pursuit = Functions.CreatePursuit();

            // Create 
            this.vehicle = new LVehicle(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.vehicleModels));
            if (this.vehicle.Exists())
            {
                // Ensure vehicle is freed on end
                Functions.AddToScriptDeletionList(this.vehicle, this);
                this.vehicle.PlaceOnNextStreetProperly();

                int peds = Common.GetRandomValue(1, 3);
                if (this.calloutType == ECalloutType.Robbery)
                {
                    peds = 2;
                }

                // Create suspects
                this.robbers = new LPed[peds];
                for (int i = 0; i < this.robbers.Length; i++)
                {
                    // Spawn ped
                    this.robbers[i] = new LPed(World.GetNextPositionOnStreet(this.vehicle.Position), Common.GetRandomCollectionValue<string>(this.criminalModels), LPed.EPedGroup.Criminal);
                    if (this.robbers[i].Exists())
                    {
                        // If vehicle doesn't have a driver yet, warp robber as driver
                        if (!this.vehicle.HasDriver)
                        {
                            this.robbers[i].WarpIntoVehicle(this.vehicle, VehicleSeat.Driver);
                        }
                        else
                        {
                            this.robbers[i].WarpIntoVehicle(this.vehicle, VehicleSeat.AnyPassengerSeat);
                        }

                        // Make ignore all events and give default weapon
                        this.robbers[i].BlockPermanentEvents = true;
                        this.robbers[i].Task.AlwaysKeepTask = true;

                        if (this.calloutType == ECalloutType.Robbery)
                        {
                            this.robbers[i].EquipWeapon();
                        }
                        else
                        {
                            // When not a robbery, 1/6 chance of weapons allowed only
                            bool allowWeapons = Common.GetRandomBool(0, 7, 1);
                            Functions.SetPursuitAllowWeaponsForSuspects(this.pursuit, allowWeapons);
                        }

                        // Add to deletion list and to pursuit
                        Functions.AddToScriptDeletionList(this.robbers[i], this);
                        Functions.AddPedToPursuit(this.pursuit, this.robbers[i]);

                        // Parity check, just to show how to normally use this API. No need if you just created the ped,
                        // since no other script could own it already
                        if (!Functions.DoesPedHaveAnOwner(this.robbers[i]))
                        {
                            // Bind ped to script so it can't be used by other scripts, such as random scenarios
                            // Because we now own this script, we also have to define behavior what we want to do when another script
                            // takes over control, e.g. when being arrested. That's why we implement PedLeftScript below
                            Functions.SetPedIsOwnedByScript(this.robbers[i], this, true);
                        }
                    }
                }

                // Create Police
                LVehicle copCar = new LVehicle(World.GetNextPositionOnStreet(this.vehicle.Position), "POLICE");
                if (copCar.Exists())
                {
                    Functions.AddToScriptDeletionList(copCar, this);
                    copCar.PlaceOnNextStreetProperly();

                    LPed copDriver = copCar.CreatePedOnSeat(VehicleSeat.Driver);

                    if (copDriver != null && copDriver.Exists())
                    {
                        Functions.AddToScriptDeletionList(copDriver, this);
                        copCar.SirenActive = true;
                        copDriver.SayAmbientSpeech("PULL_OVER_WARNING");
                    }
                }

                // Since we want other cops to join, set as called in already and also active it for player
                Functions.SetPursuitCalledIn(this.pursuit, true);
                Functions.SetPursuitIsActiveForPlayer(this.pursuit, true);

                // Show message to the player
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_CATCH_UP"), 25000);
            }

            return true;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            // Print text message when all suspect have been arrested
            int arrestCount = this.robbers.Count(robber => robber.Exists() && robber.HasBeenArrested);
            if (arrestCount == this.robbers.Length)
            {
                Functions.PrintText("All arrested!", 5000);
                this.SetCalloutFinished(true, true, true);
                this.End();
            }

            // End this script is pursuit is no longer running, e.g. because all suspects are dead
            if (!Functions.IsPursuitStillRunning(this.pursuit))
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

            // End pursuit if still running
            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
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