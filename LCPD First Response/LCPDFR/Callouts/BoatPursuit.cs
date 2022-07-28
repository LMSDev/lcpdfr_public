using System;
using System.Windows.Forms.VisualStyles;
using LCPD_First_Response.Engine.Scripting;

namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System.Collections.Generic;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.LCPDFR.API;

    /// <summary>
    /// The boat pursuit.
    /// </summary>
    [CalloutInfo("BoatPursuit", ECalloutProbability.Always)]
    internal class BoatPursuit : Callout
    {
        /// <summary>
        /// The boat models available.
        /// </summary>
        private string[] boatModels = new string[] { "DINGHY", "TROPIC", "JETMAX", "REEFER", "SQUALO" };

        /// <summary>
        /// The pursuit.
        /// </summary>
        private LHandle pursuit;

        /// <summary>
        /// The suspects.
        /// </summary>
        private List<LPed> suspects;

        /// <summary>
        /// The boat.
        /// </summary>
        private LVehicle boat;

        /// <summary>
        /// The position at which the boat is spawned.
        /// </summary>
        private Vector3 spawnPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoatPursuit"/> class.
        /// </summary>
        public BoatPursuit()
        {
            if (Settings.ForceStaticPositionForBoats)
            {
                this.spawnPosition = CVehicle.FindClosestWaterPositionStatic(LPlayer.LocalPlayer.Ped.Position, 200f);
            }
            else
            {
                this.spawnPosition = CVehicle.FindWaterPosition(LPlayer.LocalPlayer.Ped.Position, 100f);
            }

            this.suspects = new List<LPed>();

            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);


        }

        /// <summary>
        /// Called just before the callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            if (Settings.DisableBoatCallouts)
            {
                return false;
            }

            this.CalloutMessage = CultureHelper.GetText("CALLOUT_WATER_DISTURBANCE_MESSAGE");

            // Play audio
            string introMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
            string[] crimeMessages = new string[] { "CRIM_SUSPICIOUS_OFFSHORE_ACTIVITY", "CRIM_A_VESSEL_CAUSING_TROUBLE", "CRIM_SUSPICIOUS_ACTIVITY" };
            string crimeMessage = Common.GetRandomCollectionValue<String>(crimeMessages);
            string audioMessage = introMessage + crimeMessage;
            if (Common.GetRandomBool(0, 2, 1) || crimeMessage == "CRIM_SUSPICIOUS_ACTIVITY")
            {
                audioMessage += " AT_SEA";
            }

            Functions.PlaySound(audioMessage, true, true);

            return base.OnBeforeCalloutDisplayed();
        }

        /// <summary>
        /// The on callout accepted.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool OnCalloutAccepted()
        {
            this.pursuit = Functions.CreatePursuit();
            this.boat = new LVehicle(this.spawnPosition, Common.GetRandomCollectionValue<string>(this.boatModels));
            if (this.boat.Exists())
            {
                Functions.AddToScriptDeletionList(this.boat, this);
                LPed driver = new LPed(new CPed(CModel.GetRandomModel(EModelFlags.IsCivilian | EModelFlags.IsPed), this.boat.Position, EPedGroup.Criminal));
                if (driver.Exists())
                {
                    Functions.AddToScriptDeletionList(driver, this);

                    // 33% to have a gunner.
                    if (Common.GetRandomBool(0, 3, 0))
                    {
                        LPed enemyGunner = new LPed(new CPed(CModel.GetRandomModel(EModelFlags.IsCivilian | EModelFlags.IsPed), this.boat.Position, EPedGroup.Criminal));
                        if (enemyGunner.Exists())
                        {
                            Functions.AddToScriptDeletionList(enemyGunner, this);
                            enemyGunner.WarpIntoVehicle(this.boat, VehicleSeat.AnyPassengerSeat);
                            enemyGunner.WillDoDrivebys = true;
                            enemyGunner.Weapons.Uzi.Ammo = 5000;
                            enemyGunner.Task.AlwaysKeepTask = true;
                            enemyGunner.BlockPermanentEvents = true;
                            this.suspects.Add(enemyGunner);

                            // 50% chance to fight on sight.
                            Functions.SetPursuitForceSuspectsToFight(this.pursuit, Common.GetRandomBool(0, 2, 0));
                        }
                    }

                    driver.WarpIntoVehicle(this.boat, VehicleSeat.Driver);
                    driver.Task.AlwaysKeepTask = true;
                    driver.Task.CruiseWithVehicle(this.boat, 50f, false);
                    driver.BlockPermanentEvents = true;
                    this.suspects.Add(driver);
                    Functions.SetPursuitAllowWeaponsForSuspects(this.pursuit, true);

                    // Assign to script.
                    foreach (LPed suspect in this.suspects)
                    {
                        Functions.AddPedToPursuit(this.pursuit, suspect);
                        if (!Functions.DoesPedHaveAnOwner(suspect))
                        {
                            Functions.SetPedIsOwnedByScript(suspect, this, true);
                        }
                    }

                    // Since we want other cops to join, set as called in already and also active it for player.
                    Functions.SetPursuitCalledIn(this.pursuit, true);
                    Functions.SetPursuitIsActiveForPlayer(this.pursuit, true);

                    // Show message to the player.
                    Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_CATCH_UP"), 25000);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

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