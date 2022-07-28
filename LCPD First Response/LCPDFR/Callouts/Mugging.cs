namespace LCPD_First_Response.LCPDFR.Callouts
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Scripts;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// The mugging callout.
    /// </summary>
    [CalloutInfo("Mugging", ECalloutProbability.High)]
    internal class Mugging : Callout, IPedController
    {
        /// <summary>
        /// The models for criminals.
        /// </summary>
        private string[] criminalModels = { "M_Y_THIEF", "M_Y_THIEF", "M_Y_GRUS_LO_01", "M_Y_GRU2_LO_01", "M_Y_GMAF_LO_01", "M_Y_GMAF_HI_01", "M_Y_GTRI_LO_01", "M_Y_GTRI_LO_02", "M_Y_GALB_LO_01", "M_Y_GALB_LO_02" };

        /// <summary>
        /// The blip of the position.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The criminal.
        /// </summary>
        private CPed criminal;

        /// <summary>
        /// The civillian.
        /// </summary>
        private CPed civillian;

        /// <summary>
        /// The pursuit instance used in case suspect wants to flee.
        /// </summary>
        private Pursuit pursuit;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private SpawnPoint spawnPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mugging"/> class.
        /// </summary>
        public Mugging()
        {
            this.CalloutMessage = CultureHelper.GetText("CALLOUT_MUGGING_MESSAGE");
        }

        /// <summary>
        /// The mugging state.
        /// </summary>
        internal enum EMuggingState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None,

            /// <summary>
            /// Waiting for player.
            /// </summary>
            WaitingForPlayer,

            /// <summary>
            /// Suspect is fleeing.
            /// </summary>
            Fleeing,

            /// <summary>
            /// In combat.
            /// </summary>
            InCombat,
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
            string area = AreaHelper.GetAreaNameMeaningful(this.spawnPoint.Position);
            this.CalloutMessage = string.Format(CultureHelper.GetText("CALLOUT_MUGGING_MESSAGE"), area);

            // Play audio
            string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
            string crimeMessage = "CRIM_A_MUGGING";
            Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPoint.Position);

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

            this.criminal = new CPed(Common.GetRandomCollectionValue<string>(this.criminalModels), this.spawnPoint.Position, EPedGroup.Criminal);
            if (!this.criminal.Exists())
            {
                return false;
            }

            this.criminal.RequestOwnership(this);
            this.criminal.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
            this.criminal.PedData.DisableChaseAI = true;
            this.ContentManager.AddPed(this.criminal);
            this.pursuit.AddTarget(this.criminal);

            this.civillian = new CPed(CModel.GetRandomModel(EModelFlags.IsYoung), this.criminal.GetOffsetPosition(new Vector3(0, 2, 0)), EPedGroup.MissionPed);
            if (!this.civillian.Exists())
            {
                return false;
            }

            this.civillian.RequestOwnership(this);
            this.civillian.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
            this.ContentManager.AddPed(this.civillian);
            this.civillian.BlockPermanentEvents = true;
            this.civillian.PedData.CanBeArrestedByPlayer = false;

            if (Common.GetRandomValue(0, 100) > 50)
            {
                this.criminal.PedData.DefaultWeapon = Weapon.Handgun_Glock;
            }
            else
            {
                this.criminal.PedData.DefaultWeapon = Weapon.Melee_Knife;
            }

            this.criminal.EnsurePedHasWeapon();

            // Add blip
            this.blip = AreaBlocker.CreateAreaBlip(this.spawnPoint.Position, 30f);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;

            // Add states
            this.RegisterStateCallback(EMuggingState.WaitingForPlayer, this.WaitingForPlayer);
            this.State = EMuggingState.WaitingForPlayer;
            Functions.PrintText(CultureHelper.GetText("CALLOUT_GET_TO_CRIME_SCENE"), 8000);

            return true;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.pursuit != null)
            {
                if (!this.pursuit.IsRunning)
                {
                    this.SetCalloutFinished(true, true, true);
                    this.End();
                }
                else
                {
                    if ((EMuggingState)this.State == EMuggingState.Fleeing)
                    {
                        // If no weapons allowed, small chance to allow
                        if (!this.pursuit.AllowSuspectWeapons)
                        {
                            if (Common.GetRandomBool(0, 300, 1))
                            {
                                this.pursuit.AllowSuspectWeapons = true;
                            }
                        }

                        // If no vehicles allowed, small chance to allow
                        if (!this.pursuit.AllowSuspectVehicles)
                        {
                            if (Common.GetRandomBool(0, 200, 1))
                            {
                                this.pursuit.AllowSuspectVehicles = true;
                            }
                        }
                    }
                }
            }
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

            if (this.pursuit != null)
            {
                this.pursuit.EndChase();
            }
        }

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        internal override void PedLeftScript(CPed ped)
        {
            base.PedLeftScript(ped);

            this.ContentManager.RemovePed(ped);
            ped.ReleaseOwnership(this);
        }

        /// <summary>
        /// Waiting for player.
        /// </summary>
        private void WaitingForPlayer()
        {
            if (this.criminal.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 120)
            {
                if (!this.criminal.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGun))
                {
                    this.criminal.Task.AimAt(this.civillian, int.MaxValue);
                }

                if (!this.civillian.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHandsUp))
                {
                    this.civillian.Task.HandsUp(int.MaxValue);
                }
            }

            if (this.criminal.HasSpottedChar(CPlayer.LocalPlayer.Ped) && this.criminal.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 40)
            {
                this.State = EMuggingState.Fleeing;
                this.blip.Delete();

                // Small chance of getting a better weapon
                int randomValue = Common.GetRandomValue(0, 100);
                if (randomValue < 20)
                {
                    this.criminal.PedData.DefaultWeapon = Weapon.SMG_Uzi;
                    this.criminal.EnsurePedHasWeapon();
                }

                // Flee (60%), Flee using weapon (25%), Attack civllian, then fight (15%)
                randomValue = Common.GetRandomValue(0, 100);
                if (randomValue < 60)
                {
                    // Chance of 50% suspect won't steal vehicles
                    randomValue = Common.GetRandomValue(0, 100);
                    if (randomValue < 50)
                    {
                        this.pursuit.AllowSuspectVehicles = false;
                    }

                    this.pursuit.DontEnableCopBlips = false;
                    this.criminal.PedData.DisableChaseAI = false;
                    this.criminal.PedData.CanBeArrestedByPlayer = true;
                    this.civillian.Task.Cower();
                    this.pursuit.MakeActiveChase(2500, 5000);
                }
                else if (randomValue < 85)
                {
                    this.pursuit.DontEnableCopBlips = false;
                    this.criminal.PedData.DisableChaseAI = false;
                    this.pursuit.AllowSuspectWeapons = true;
                    this.pursuit.ForceSuspectsToFight = true;
                    this.civillian.Task.Cower();
                    this.pursuit.MakeActiveChase(2500, 5000);
                }
                else
                {
                    this.civillian.Task.FleeFromChar(this.criminal);
                    this.criminal.Task.FightAgainst(this.civillian);
                    DelayedCaller.Call(
                        delegate
                            {
                                this.pursuit.DontEnableCopBlips = false;
                                this.criminal.Task.ClearAll();
                                this.criminal.PedData.DisableChaseAI = false;
                                this.pursuit.AllowSuspectWeapons = true;
                                this.pursuit.ForceKilling = true;
                                this.pursuit.MakeActiveChase(2500, 5000);
                            }, 
                    2000);
                }
            }
        }
    }
}