namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Scripts;

    /// <summary>
    /// The disturbance callout.
    /// </summary>
    [CalloutInfo("Disturbance", ECalloutProbability.High)]
    internal class Disturbance : Callout
    {
        /// <summary>
        /// The model.
        /// </summary>
        private const string Model = "M_M_GENBUM_01";

        /// <summary>
        /// The animations.
        /// </summary>
        private string[] animations = new string[] { "wasteda", "wastedb", "wastedc", "wastedd", "bum_fight" };

        /// <summary>
        /// The blip.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private SpawnPoint spawnPoint;

        /// <summary>
        /// The peds.
        /// </summary>
        private List<LPed> peds;

        /// <summary>
        /// The drug deal state.
        /// </summary>
        [Flags]
        internal enum EDisturbanceState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None = 0x0,

            /// <summary>
            /// Waiting for player.
            /// </summary>
            WaitingForPlayer = 0x1,

            /// <summary>
            /// Player is close.
            /// </summary>
            PlayerClose = 0x2,

            /// <summary>
            /// Player is very close.
            /// </summary>
            PlayerVeryClose = 0x4,

            /// <summary>
            /// Player has arrived.
            /// </summary>
            Arrived = 0x8,
        }

        /// <summary>
        /// Called just before the callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            this.spawnPoint = Callout.GetSpawnPointInRange(LPlayer.LocalPlayer.Ped.Position, 100, 400);

            if (this.spawnPoint == SpawnPoint.Zero)
            {
                return false;
            }

            // Show user where callout is
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPoint.Position, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPoint.Position);

            // Get area name
            string area = AreaHelper.GetAreaNameMeaningful(this.spawnPoint.Position);
            this.CalloutMessage = string.Format(CultureHelper.GetText("CALLOUT_DISTURBANCE_MESSAGE"), area);

            // Play audio
            string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
            string crimeMessage = "CRIM_A_DRUNKEN_DISTURBANCE";
            if (Common.GetRandomBool(0, 2, 1))
            {
                crimeMessage = "CRIM_CRIMINALS_DISTURBING_THE_PEACE";
            }

            Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPoint.Position);

            return base.OnBeforeCalloutDisplayed();
        }

        /// <summary>
        /// Called when the callout has been accepted. Call base to set state to Running.
        /// </summary>
        /// <returns>
        /// True if callout was setup properly, false if it failed. Calls <see cref="Callout.End"/> when failed.
        /// </returns>
        public override bool OnCalloutAccepted()
        {
            base.OnCalloutAccepted();

            // Add blip
            this.blip = AreaBlocker.CreateAreaBlip(this.spawnPoint.Position, 30f);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;

            // Spawn peds
            this.peds = new List<LPed>();
            int number = Common.GetRandomValue(0, 5);
            for (int i = 0; i < number; i++)
            {
                LPed ped = new LPed(this.spawnPoint.Position.Around(3f), Model, LPed.EPedGroup.Criminal);
                if (ped.Exists())
                {
                    Functions.AddToScriptDeletionList(ped, this);
                    Functions.SetPedIsOwnedByScript(ped, this, true);

                    // Ensure ped is not in a building
                    if (ped.EnsurePedIsNotInBuilding(this.spawnPoint.Position))
                    {
                        // Small chance of getting a weapon
                        int randomValue = Common.GetRandomValue(0, 100);
                        if (randomValue < 20)
                        {
                            ped.Ped.PedData.DefaultWeapon = Weapon.Melee_Knife;
                            ped.EquipWeapon();
                        }

                        this.peds.Add(ped);
                    }
                    else
                    {
                        Log.Debug("OnCalloutAccepted: Failed to place ped properly outside of building", this);
                        ped.Delete();
                    }
                }
            }

            // If no ped could be created, abort
            if (this.peds.Count < 1)
            {
                Log.Warning("OnCalloutAccepted: Failed to spawn at least one ped", this);
                return false;
            }

            // Add states
            this.RegisterStateCallback(EDisturbanceState.WaitingForPlayer, this.WaitingForPlayer);
            this.RegisterStateCallback(EDisturbanceState.PlayerClose, this.PlayerClose);
            this.RegisterStateCallback(EDisturbanceState.Arrived, this.Arrived);
            this.State = EDisturbanceState.WaitingForPlayer;
            Functions.PrintText(CultureHelper.GetText("CALLOUT_GET_TO_CRIME_SCENE"), 8000);

            return true;
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the calloutmanager to shutdown the callout or can be called by the 
        /// callout itself to execute the cleanup code. Call base to set state to None.
        /// </summary>
        public override void End()
        {
            base.End();

            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }
        }

        /// <summary>
        /// Waiting for player.
        /// </summary>
        private void WaitingForPlayer()
        {
            if (this.spawnPoint.Position.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100)
            {
                AnimationSet animationSet = new AnimationSet("amb@drunk");

                foreach (LPed ped in this.peds)
                {
                    // Play an animation
                    string animation = Common.GetRandomCollectionValue<string>(this.animations);
                    ped.Task.PlayAnimation(animationSet, animation, 4.0f, AnimationFlags.Unknown05);
                }

                this.State = EDisturbanceState.PlayerClose;
            }
        }

        /// <summary>
        /// The player is close.
        /// </summary>
        private void PlayerClose()
        {
            // If player can be seen
            bool playerHasBeenSpotted = false;

            // Check whether player has been spotted
            foreach (LPed ped in this.peds)
            {
                if (ped.Position.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 50 && ped.HasSpottedPed(LPlayer.LocalPlayer.Ped, false))
                {
                    playerHasBeenSpotted = true;
                    break;
                }
            }

            // If player has been spotted, present him with task
            if (playerHasBeenSpotted)
            {
                // Attach blips to peds
                foreach (LPed ped in this.peds)
                {
                    ped.AttachBlip();
                }

                Functions.PrintText(CultureHelper.GetText("CALLOUT_DISTURBANCE_MAKE_LEAVE"), 12000);
                this.State = EDisturbanceState.Arrived;
            }
        }

        /// <summary>
        /// The player has arrived and is dealing with the homeless people.
        /// </summary>
        private void Arrived()
        {
            if (this.peds.Count == 0)
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
                return;
            }

            for (int i = 0; i < this.peds.Count; i++)
            {
                LPed ped = this.peds[i];
                if (ped.Exists())
                {
                    // If 20 metres or further away, free ped
                    if (ped.Position.DistanceTo(this.spawnPoint.Position) > 20 || !ped.IsAliveAndWell)
                    {
                        // Free ped if we are still controlling it
                        if (Functions.IsStillControlledByScript(ped, this))
                        {
                            Functions.RemoveFromDeletionList(ped, this);
                            Functions.SetPedIsOwnedByScript(ped, this, false);
                        }

                        this.peds.Remove(ped);
                    }
                }
                else
                {
                    this.peds.Remove(ped);
                }
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
            if (ped != null && ped.Exists())
            {
                ped.DeleteBlip();
            }

            this.peds.Remove(ped);
        }
    }
}