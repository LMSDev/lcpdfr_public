namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Scripts;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// The drug deal callout.
    /// </summary>
    [CalloutInfo("DrugDeal", ECalloutProbability.Medium)]
    internal class DrugDeal : Callout, IPedController
    {
        /// <summary>
        /// The models for criminals.
        /// </summary>
        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };

        /// <summary>
        /// The bike a criminal has used.
        /// </summary>
        private CVehicle bike;

        /// <summary>
        /// The blip of the position.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The criminals.
        /// </summary>
        private List<CPed> criminals;

        /// <summary>
        /// The pursuit instance used in case suspect wants to flee.
        /// </summary>
        private Pursuit pursuit;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private SpawnPoint spawnPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrugDeal"/> class.
        /// </summary>
        public DrugDeal()
        {
            this.CalloutMessage = CultureHelper.GetText("CALLOUT_DRUGDEAL_MESSAGE");
        }

        /// <summary>
        /// The drug deal state.
        /// </summary>
        [Flags]
        internal enum EDrugDealState
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
            /// Player is close.
            /// </summary>
            PlayerVeryClose = 0x4,

            /// <summary>
            /// Criminals are fleeing.
            /// </summary>
            Fleeing = 0x8,
        }

        /// <summary>
        /// Called just before the callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            this.spawnPoint = Callout.GetSpawnPointInRange(CPlayer.LocalPlayer.Ped.Position, 100, 400);

            if (this.spawnPoint == SpawnPoint.Zero)
            {
                return false;
            }

            // Show user where callout is
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPoint.Position, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPoint.Position);

            // Get area name
            string area = Functions.GetAreaStringFromPosition(this.spawnPoint.Position);
            this.CalloutMessage = string.Format(CultureHelper.GetText("CALLOUT_DRUGDEAL_MESSAGE"), area);

            // Play audio
            string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
            Functions.PlaySoundUsingPosition(audioMessage + "CRIM_A_DRUG_DEAL IN_OR_ON_POSITION", this.spawnPoint.Position);
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

            this.pursuit = new Pursuit();
            this.pursuit.CanCopsJoin = false;
            this.pursuit.DontEnableCopBlips = true;

            // Add blip
            this.blip = AreaBlocker.CreateAreaBlip(this.spawnPoint.Position, 30f);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;

            // Chance there's a bike
            if (Common.GetRandomBool(0, 5, 1))
            {
                this.bike = new CVehicle("NRG900", World.GetNextPositionOnPavement(this.spawnPoint.Position), EVehicleGroup.Normal);
                this.ContentManager.AddVehicle(this.bike);
            }

            // Create criminals
            this.criminals = new List<CPed>();
            int random = Common.GetRandomValue(2, 5);
            for (int i = 0; i < random; i++)
            {
                CPed criminal = new CPed(Common.GetRandomCollectionValue<string>(this.criminalModels), this.spawnPoint.Position, EPedGroup.Criminal);
                if (criminal.Exists())
                {
                    this.ContentManager.AddPed(criminal);
                    criminal.RequestOwnership(this);
                    criminal.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                    criminal.RelationshipGroup = RelationshipGroup.Special;
                    criminal.ChangeRelationship(RelationshipGroup.Special, Relationship.Companion);
                    criminal.Position = criminal.Position.Around(2f);

                    // Small chance of getting a better weapon
                    int randomValue = Common.GetRandomValue(0, 100);
                    if (randomValue < 20)
                    {
                        criminal.PedData.DefaultWeapon = Weapon.Rifle_AK47;
                    }
                    else
                    {
                        criminal.PedData.DefaultWeapon = Weapon.SMG_Uzi;
                    }

                    // We don't want the criminal to flee yet
                    criminal.PedData.DisableChaseAI = true;

                    this.pursuit.AddTarget(criminal);
                    this.criminals.Add(criminal);
                }
            }

            if (this.criminals.Count < 2)
            {
                return false;
            }

            // Add states
            this.RegisterStateCallback(EDrugDealState.WaitingForPlayer, this.WaitingForPlayer);
            this.RegisterStateCallback(EDrugDealState.PlayerClose, this.PlayerClose);
            this.RegisterStateCallback(EDrugDealState.PlayerVeryClose, this.PlayerVeryClose);
            this.RegisterStateCallback(EDrugDealState.Fleeing, this.Fleeing);
            this.State = EDrugDealState.WaitingForPlayer;
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

            if (this.criminals != null)
            {
                foreach (CPed criminal in this.criminals)
                {
                    if (criminal.Intelligence.IsStillAssignedToController(this))
                    {
                        criminal.Intelligence.ResetAction(this);
                    }
                }
            }

            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }

            if (this.pursuit != null)
            {
                this.pursuit.EndChase();
            }
        }

        /// <summary>
        /// Waiting for player.
        /// </summary>
        private void WaitingForPlayer()
        {
            if (this.spawnPoint.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 120)
            {
                foreach (CPed criminal in this.criminals)
                {
                    // Look at each other
                    if (criminal != this.criminals[0])
                    {
                        criminal.Task.TurnTo(this.criminals[0]);
                    }
                }

                this.State = EDrugDealState.PlayerClose;
            }
        }

        /// <summary>
        /// The player is close.
        /// </summary>
        private void PlayerClose()
        {
            AnimationSet animSetArgue = new AnimationSet("amb@argue");
            if (this.spawnPoint.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 100)
            {
                // If player is getting close
                foreach (CPed criminal in this.criminals)
                {
                    criminal.Task.PlayAnimation(animSetArgue, "argue_a", 7f, AnimationFlags.Unknown05);
                }

                this.State = EDrugDealState.PlayerVeryClose;
            }
        }

        /// <summary>
        /// The player is very close.
        /// </summary>
        private void PlayerVeryClose()
        {
            // If player can be seen
            bool playerHasBeenSpotted = false;

            // Check whether player has been spotted
            foreach (CPed criminal in this.criminals)
            {
                if (criminal.Exists())
                {
                    if (criminal.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 50 && criminal.HasSpottedChar(CPlayer.LocalPlayer.Ped))
                    {
                        playerHasBeenSpotted = true;
                        break;
                    }
                }
            }

            // If player has been spotted
            if (playerHasBeenSpotted)
            {
                this.blip.Delete();

                // Various things can happen: All dealers can surrender, all dealers can fight/flee some flee/some surrender
                foreach (CPed criminal in this.criminals)
                {
                    criminal.PedData.DisableChaseAI = false;

                    // Chance criminal will surrender
                    bool willSurrender = Common.GetRandomBool(0, 5, 1);
                    if (willSurrender)
                    {
                        criminal.Task.ClearAllImmediately();
                        criminal.Intelligence.Surrender();
                        criminal.Task.HandsUp(int.MaxValue);
                    }
                    else
                    {
                        bool lowSenseRange = Common.GetRandomBool(0, 3, 1);
                        if (lowSenseRange)
                        {
                            criminal.PedData.SenseRange = 30f;
                        }
                        else
                        {
                            criminal.PedData.SenseRange = 120f;
                        }
                    }
                }

                this.pursuit.DontEnableCopBlips = false;
                this.pursuit.AllowSuspectWeapons = true;
                this.pursuit.ForceSuspectsToFight = true;
                this.pursuit.MakeActiveChase(2500, 5000);
                this.pursuit.HasBeenCalledIn = true;

                // Request backup units automatically
                // Main.BackupManager.RequestPoliceBackup(CPlayer.LocalPlayer.Ped.Position, null);
                // Main.BackupManager.RequestPoliceBackup(CPlayer.LocalPlayer.Ped.Position, null);
                // AudioHelper.PlayPoliceBackupRequested();
                TextHelper.PrintText(CultureHelper.GetText("CALLOUT_SHOOTOUT_FIGHT_SUSPECTS"), 5000);

                this.State = EDrugDealState.Fleeing;
            }
        }

        /// <summary>
        /// The criminals are fleeing.
        /// </summary>
        private void Fleeing()
        {
            if (!this.pursuit.IsRunning)
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
            this.ContentManager.RemovePed(ped);
            ped.ReleaseOwnership(this);
        }
    }
}