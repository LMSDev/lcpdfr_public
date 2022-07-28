namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Drawing;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.Partners;
    using LCPD_First_Response.LCPDFR.Scripts.QuickActionMenu;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// Assigns and handles generic keybindings. Also handles the <see cref="QuickActionMenu"/> logic.
    /// </summary>
    [ScriptInfo("KeyBindings", true)]
    internal class KeyBindings : GameScript
    {
        /// <summary>
        /// The pursuit menu form handler.
        /// </summary>
        private PursuitMenuFormHandler pursuitMenuFormHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyBindings"/> class.
        /// </summary>
        public KeyBindings()
        {
            // QAM
            QuickActionMenuGroup generalGroup = new QuickActionMenuGroup(QuickActionMenuGroup.EMenuGroup.General);
            QuickActionMenuGroup backupGroup = new QuickActionMenuGroup(QuickActionMenuGroup.EMenuGroup.Backup);
            QuickActionMenuGroup partnerGroup = new QuickActionMenuGroup(QuickActionMenuGroup.EMenuGroup.Partner);
            QuickActionMenuGroup speechGroup = new QuickActionMenuGroup(QuickActionMenuGroup.EMenuGroup.Speech);
            partnerGroup.AddOption(new QuickActionMenuOption("BLUE / PRIMARY", Color.FromArgb(180, 69, 97, 153), partnerGroup, EPartnerGroup.PrimaryGroup));
            partnerGroup.AddOption(new QuickActionMenuOption("RED / SECONDARY", Color.FromArgb(180, 153, 69, 69), partnerGroup, EPartnerGroup.SecondaryGroup));

            Main.QuickActionMenu.AddEntry("PURSUIT RADIO UPDATE", generalGroup, this.PursuitRadioUpdate, () => LCPDFRPlayer.LocalPlayer.IsChasing); 
            Main.QuickActionMenu.AddEntry("EVACUATE AREA", generalGroup, this.EvacuateArea, () => !CPlayer.LocalPlayer.Ped.IsInVehicle);   
            Main.QuickActionMenu.AddEntry("STOP TRAFFIC", generalGroup, this.BlockTraffic);


            Main.QuickActionMenu.AddEntry("TARGET: DEPLOY TASER", partnerGroup, this.PartnerDeployTaser, () => this.PartnerEvaluateCurrentTarget(true));
            Main.QuickActionMenu.AddEntry("TARGET: COVER", partnerGroup, this.PartnerCoverPed, () => this.PartnerEvaluateCurrentTarget(true));
            Main.QuickActionMenu.AddEntry("TARGET: HOLD AT GUNPOINT", partnerGroup, this.PartnerHoldAtGunpoint, () => this.PartnerEvaluateCurrentTarget(true)); // TODO: Only if a targetted ped exists AND TODO: Add sub menu for actions on the ped          
            Main.QuickActionMenu.AddEntry("TARGET: ARREST", partnerGroup, this.PartnerArrest, () => this.PartnerEvaluateCurrentTarget(true));
            
            Main.QuickActionMenu.AddEntry("MOVE TO", partnerGroup, this.PartnerGoto);                                                   // (e.g. taser, arrest, frisk, and so on)
            Main.QuickActionMenu.AddEntry("HOLD POSITION", partnerGroup, this.PartnerStay);
            Main.QuickActionMenu.AddEntry("REGROUP", partnerGroup, this.PartnerRegroup);
            Main.QuickActionMenu.AddEntry("FOLLOW IN VEHICLE", partnerGroup, this.PartnerFollowInVehicle,
                delegate
                {
                    AimMarker aimMarker = Main.ScriptManager.GetRunningScriptInstances("AimMarker")[0] as AimMarker;
                    if (aimMarker != null)
                    {
                        if (!aimMarker.IsBeingDrawn)
                        {
                            aimMarker.PerformCheck();
                        }

                        if (aimMarker.HasTarget && aimMarker.TargetedEntity != null)
                        {
                            return aimMarker.TargetedEntity.EntityType == EEntityType.Vehicle;
                        }
                    }

                    return false;
                });
            QuickActionMenuItemBase item = Main.QuickActionMenu.AddEntry("PARTNER MUST BE CLOSER", partnerGroup, delegate(QuickActionMenuOption option) {  }, () => CPlayer.LocalPlayer.IsTargettingOrAimingAtPedOnFoot && !this.PartnerEvaluateCurrentTarget(true));
            item.CanBeSelected = false;

            Main.QuickActionMenu.AddEntry("REQUEST ROADBLOCK", backupGroup, this.CallForRoadblock, () => LCPDFRPlayer.LocalPlayer.IsChasing); 
            Main.QuickActionMenu.AddEntry("REQUEST HELICOPTER", backupGroup, this.RequestHelicopter, () => LCPDFRPlayer.LocalPlayer.IsChasing);
            Main.QuickActionMenu.AddEntry("REQUEST FIRE DEPT.", backupGroup, this.RequestFirefighter);
            Main.QuickActionMenu.AddEntry("REQUEST AMBULANCE", backupGroup, this.RequestAmbulance);
            Main.QuickActionMenu.AddEntry("REQUEST NOOSE TEAM", backupGroup, this.RequestNoose);
            Main.QuickActionMenu.AddEntry("REQUEST PATROL UNIT", backupGroup, this.RequestBackup);

            string[] speech = new string[] { "BLOCKED_PED", "BLOCKED_VEHICLE", "BUMP", "CHASE_IN_GROUP", "CHASE_SOLO", "DODGE", "INTIMIDATE", "SPOT_CRIME", "SPOT_SUSPECT", "SPOT_WEAPON" };

            foreach (String s in speech)
            {
                Main.QuickActionMenu.AddEntry(s.Replace("_", " "), speechGroup, (() => this.PlaySpeech(s)));
            }

            Main.QuickActionMenu.SetRenderer(new QuickActionMenuGroupStyle());
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            // Unregister everything
            this.DisposePursuitMenu();
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.ToggleBusy) && !LCPDFRPlayer.LocalPlayer.IsInTutorial)
            {
                LCPDFRPlayer.LocalPlayer.IsBusyForCallouts = !LCPDFRPlayer.LocalPlayer.IsBusyForCallouts;

                if (LCPDFRPlayer.LocalPlayer.IsBusyForCallouts)
                {
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALLOUTMANAGER_BUSY"));
                }
                else
                {
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALLOUTMANAGER_NOT_BUSY"));
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.UpdateCalloutState))
            {
                if (LCPDFRPlayer.LocalPlayer.AvailabilityState == EPlayerAvailabilityState.InCalloutFinished)
                {
                    TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie("NOTHING_TO_SEE");
                    taskWalkieTalkie.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.MainTask);
                    LCPDFRPlayer.LocalPlayer.AvailabilityState = EPlayerAvailabilityState.Idle;

                    DelayedCaller.Call(delegate
                        {
                            TextHelper.AddTextToTextWall(CultureHelper.GetText("CALLOUT_ROGER_THAT"), CultureHelper.GetText("POLICE_SCANNER_CONTROL"));
                        }, this, 4500);
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.ShowPursuitTacticsMenu))
            {
                if (CPlayer<LCPDFRPlayer>.LocalPlayer.IsChasing)
                {
                    if (this.pursuitMenuFormHandler == null)
                    {
                        this.pursuitMenuFormHandler = new PursuitMenuFormHandler();
                        this.pursuitMenuFormHandler.ItemSelected += new PursuitMenuFormHandler.ItemSelectedEventHandler(this.pursuitMenuFormHandler_ItemSelected);
                    }
                    else
                    {
                        this.DisposePursuitMenu();
                    }
                }
                else
                {
                    // Always be able to cancel the menu
                    this.DisposePursuitMenu();
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.CallInPursuit))
            {
                if (CPlayer<LCPDFRPlayer>.LocalPlayer.IsChasing)
                {
                    Chase chase = CPlayer.LocalPlayer.Ped.PedData.CurrentChase;
                    if (chase is Pursuit)
                    {
                        Pursuit pursuit = (Pursuit)chase;
                        if (!pursuit.HasBeenCalledIn)
                        {
                            pursuit.CallIn(AudioHelper.EPursuitCallInReason.Pursuit);
                        }
                    }
                }

                this.StartOrAddToChase();
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.ToggleHat))
            {
                LCPDFRPlayer.LocalPlayer.ToggleHat();
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.RequestRoadblock))
            {
                this.CallForRoadblock();
            }
        }

        /// <summary>
        /// Starts a new chase or adds fighting criminals to the current chase.
        /// </summary>
        private void StartOrAddToChase()
        {
            // Get all ambient peds
            CPed[] peds = CPlayer.LocalPlayer.Ped.Intelligence.GetPedsAround(100f, EPedSearchCriteria.AmbientPed);
            foreach (CPed ped in peds)
            {
                if (ped.Exists())
                {
                    if (ped.IsAliveAndWell)
                    {
                        if (ped.IsInCombatNotFleeing || (CPlayer.LocalPlayer.Ped.HasBeenDamagedBy(ped) && CPlayer.LocalPlayer.Ped.HasSpottedCharInFront(ped)))
                        {
                            ped.BecomeMissionCharacter();
                            ped.AlwaysFreeOnDeath = true;
                            ped.AttachBlip();
                            Chase chase = Pursuit.RegisterSuspect(ped, true);
                            CPed ped1 = ped;

                            // Free ped on end
                            chase.ChaseEnded +=
                                delegate
                                {
                                    if (ped1.Exists())
                                    {
                                        ped1.NoLongerNeeded();
                                    }
                                };
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when an item has been selected.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        void pursuitMenuFormHandler_ItemSelected(int index)
        {
            if (CPlayer<LCPDFRPlayer>.LocalPlayer.IsChasing)
            {
                // Toggle lethal force
                if (index == 0)
                {
                    CPlayer.LocalPlayer.Ped.PedData.CurrentChase.ForceKilling = !CPlayer.LocalPlayer.Ped.PedData.CurrentChase.ForceKilling;
                    if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase.ForceKilling)
                    {
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CHASE_LETHAL_FORCE_ALLOWED"));
                        AudioHelper.PlaySpeechInScannerFromPed(CPlayer<LCPDFRPlayer>.LocalPlayer.Ped, "WANTED_LEVEL_INC_TO_2");
                        Stats.UpdateStat(Stats.EStatType.LethalForceAllowed, 1);
                    }
                    else
                    {
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CHASE_LETHAL_FORCE_FORBIDDEN"));
                        Stats.UpdateStat(Stats.EStatType.LethalForceDenied, 1);
                    }
                }

                // Switch to aggressive mode
                if (index == 1)
                {
                    CPlayer.LocalPlayer.Ped.PedData.CurrentChase.ChangeTactics(EChaseTactic.Active);
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CHASE_TACTIC_AGGRESSIVE"));
                    Stats.UpdateStat(Stats.EStatType.UnitsSwitchedToActive, 1);
                }

                // Switch to passive mode
                if (index == 2)
                {
                    CPlayer.LocalPlayer.Ped.PedData.CurrentChase.ChangeTactics(EChaseTactic.Passive);
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CHASE_TACTIC_PASSIVE"));
                    Stats.UpdateStat(Stats.EStatType.UnitsSwitchedToPassive, 1);
                }

                // Switch to aggressive mode
                if (index == 3)
                {
                    CPlayer.LocalPlayer.Ped.PedData.CurrentChase.ChangeHeliTactics(EHeliTactic.Active);
                    TextHelper.PrintFormattedHelpBox("Air support switched to aggressive");
                    // TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CHASE_TACTIC_PASSIVE"));
                    Stats.UpdateStat(Stats.EStatType.AirUnitsSwitchedToActive, 1);
                }

                // Switch to passive mode
                if (index == 4)
                {
                    CPlayer.LocalPlayer.Ped.PedData.CurrentChase.ChangeHeliTactics(EHeliTactic.Passive);
                    TextHelper.PrintFormattedHelpBox("Air support switched to passive");
                    // TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CHASE_TACTIC_PASSIVE"));
                    Stats.UpdateStat(Stats.EStatType.AirUnitsSwitchedToPassive, 1);
                }
            }

            // Unregister everything
            this.DisposePursuitMenu();
        }

        /// <summary>
        /// Disposes the pursuit menu.
        /// </summary>
        private void DisposePursuitMenu()
        {
            if (this.pursuitMenuFormHandler != null)
            {
                this.pursuitMenuFormHandler.Dispose();
                this.pursuitMenuFormHandler.ItemSelected -= new PursuitMenuFormHandler.ItemSelectedEventHandler(this.pursuitMenuFormHandler_ItemSelected);
                this.pursuitMenuFormHandler = null;
            }
        }

        /// <summary>
        /// Setups (when in chase) a roadblock.
        /// </summary>
        private void CallForRoadblock()
        {
            if (LCPDFRPlayer.LocalPlayer.IsChasing)
            {
                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("REQUEST_BACKUP");
                Pursuit pursuit = CPlayer.LocalPlayer.Ped.PedData.CurrentChase as Pursuit;
                pursuit.DeployRoadblockForClosestSuspect();
                string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
                TextHelper.AddTextToTextWall(CultureHelper.GetText("ROADBLOCK_REQUESTED"), reporter);
            }
        }

        /// <summary>
        /// Makes the player call in an update on his current chase
        /// </summary>
        private void PlaySpeech(string speech)
        {
            if (CPlayer.LocalPlayer.Ped.IsAliveAndWell && !CPlayer.LocalPlayer.Ped.IsAmbientSpeechPlaying) CPlayer.LocalPlayer.Ped.SayAmbientSpeech(speech);
        }

        /// <summary>
        /// Makes the player call in an update on his current chase
        /// </summary>
        private void PursuitRadioUpdate()
        {
            if (LCPDFRPlayer.LocalPlayer.IsChasing)
            {

                Pursuit pursuit = CPlayer.LocalPlayer.Ped.PedData.CurrentChase as Pursuit;

                CPed suspect = pursuit.GetClosestSuspectForPlayer();

                if (suspect.Exists())
                {
                    if (suspect.Wanted.OfficersVisual > 0)
                    {
                        if (suspect.IsInVehicle)
                        {
                            if (suspect.CurrentVehicle.Model.IsBike)
                            {
                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("SUSPECT_IS_ON_BIKE");
                            }
                            else
                            {
                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("SUSPECT_IS_IN_CAR");
                            }
                        }
                        else
                        {
                            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("SUSPECT_IS_ON_FOOT");
                        }
                    }
                }

                DelayedCaller.Call(delegate { if (suspect != null && suspect.Exists()) AudioHelper.PlayDispatchChaseUpdateOnSuspect(suspect); }, Common.GetRandomValue(4000, 6000));
            }
        }

        /// <summary>
        /// Blocks the traffic from moving by utilizing the area blocker.
        /// </summary>
        private void BlockTraffic()
        {
            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("BLOCKED_VEHICLE");
            AreaBlocker.AddBlockedArea(CPlayer.LocalPlayer.Ped.Position, 15f, true);
        }

        /// <summary>
        /// Evacuates the whole area.
        /// </summary>
        private void EvacuateArea()
        {
            //CPlayer.LocalPlayer.Ped.Task.ClearAll();
            //CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("GO_AWAY", "GESTURES@MALE", 4.0f, false, 0, 0, 0, -1);

            DelayedCaller.Call(delegate { CPlayer.LocalPlayer.Ped.SayAmbientSpeech("EVACUATE_AREA"); }, this, 500);

            DelayedCaller.Call(
                delegate
                {
                    // Make close peds run away
                    foreach (CPed ped in CPlayer.LocalPlayer.Ped.Intelligence.GetPedsAround(15f, EPedSearchCriteria.AmbientPed))
                    {
                        if (ped.Exists())
                        {
                            if (!ped.IsInCombat && ped.Intelligence.IsFreeForAction(EPedActionPriority.ShockingEventFlee))
                            {
                                if (!ped.IsFleeing)
                                {
                                    if (ped.IsInVehicle)
                                    {
                                        ped.APed.TaskCombatRetreatSubtask(CPlayer.LocalPlayer.Ped.APed);
                                    }
                                    else
                                    {
                                        ped.Task.FleeFromChar(CPlayer.LocalPlayer.Ped);
                                    }
                                }
                            }
                        }
                    }
                },
                this, 
                2000);
        }

        /// <summary>
        /// Returns whether the player is currently aiming at a valid (close) ped.
        /// </summary>
        /// <returns></returns>
        private bool PartnerEvaluateCurrentTarget(bool noVehicles)
        {
            CPed ped = CPlayer.LocalPlayer.Ped.IsAiming ? CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed() : CPlayer.LocalPlayer.GetTargetedPed();
            if (ped != null && ped.Exists() && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCop))
            {
                if (noVehicles && ped.IsInVehicle)
                {
                    return false;
                }

                return Main.PartnerManager.Partners.Any(p => p.PartnerPed.Position.DistanceTo(ped.Position) < 12);
            }

            return false;
        }

        private void PartnerFollowInVehicle(QuickActionMenuOption option)
        {
            AimMarker aimMarker = Main.ScriptManager.GetRunningScriptInstances("AimMarker")[0] as AimMarker;
            if (aimMarker != null)
            {
                if (!aimMarker.IsBeingDrawn)
                {
                    aimMarker.PerformCheck();
                }

                if (aimMarker.HasTarget)
                {
                    CEntity target = aimMarker.TargetedEntity;
                    if (target.EntityType == EEntityType.Vehicle)
                    {
                        CVehicle vehicle = (CVehicle)aimMarker.TargetedEntity;
                        //if (vehicle.HasOwner) return;

                        EPartnerGroup group = EPartnerGroup.All;
                        if (option != null && option.Value != null)
                        {
                            group = (EPartnerGroup)option.Value;
                        }

                        CPlayer.LocalPlayer.Ped.SayAmbientSpeech("SPLIT_UP_AND_SEARCH");

                        foreach (Partner partner in Main.PartnerManager.GetPartnersByGroup(group))
                        {
                            Partner p = partner;
                            DelayedCaller.Call(delegate { p.FollowInVehicle(vehicle); }, this, Common.GetRandomValue(500, 1000));
                        }
                    }
                }
            }
        }

        private void PartnerArrest(QuickActionMenuOption option)
        {
            this.PartnerArrestOrGunpoint(true, option);
        }

        /// <summary>
        /// Cancels all tasks of the partner and makes him regroup.
        /// </summary>
        private void PartnerRegroup(QuickActionMenuOption option)
        {
            if (!Main.PartnerManager.HasActivePartner)
            {
                return;
            }

            //CPlayer.LocalPlayer.Ped.SayAmbientSpeech("REGROUP");

            EPartnerGroup group = EPartnerGroup.All;
            if (option != null && option.Value != null)
            {
                group = (EPartnerGroup)option.Value;
            }

            foreach (Partner partner in Main.PartnerManager.GetPartnersByGroup(group))
            {
                Partner p = partner;
                DelayedCaller.Call(delegate { p.Regroup(); }, this, Common.GetRandomValue(800, 1300));
            }

            Stats.UpdateStat(Stats.EStatType.PartnerOrderedRegroup, 1);
        }

        private void PartnerArrestOrGunpoint(bool arrest, QuickActionMenuOption option)
        {
            if (!Main.PartnerManager.HasActivePartner)
            {
                return;
            }

            // Make partner arrest target.
            CPed ped = null;
            ped = CPlayer.LocalPlayer.Ped.IsAiming ? CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed() : CPlayer.LocalPlayer.GetTargetedPed();
            if (ped != null && ped.Exists() && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCop) && !ped.IsInVehicle)
            {
                EPartnerGroup group = EPartnerGroup.All;
                if (option != null && option.Value != null)
                {
                    group = (EPartnerGroup)option.Value;
                }

                foreach (Partner partner in Main.PartnerManager.GetPartnersByGroup(group).Where(partner => partner.Intelligence.IsFreeForTask))
                {
                    Partner p = partner;
                    if (p.PartnerPed.Position.DistanceTo(ped.Position) < 15)
                    {
                        //CPlayer.LocalPlayer.Ped.SayAmbientSpeech("ARREST_PLAYER");

                        if (arrest)
                        {
                            DelayedCaller.Call(delegate { p.Arrest(ped); }, this, Common.GetRandomValue(500, 1000));
                        }
                        else
                        {
                            DelayedCaller.Call(delegate { p.HoldAtGunpoint(ped); }, this, Common.GetRandomValue(500, 1000));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Makes the partner stay.
        /// </summary>
        private void PartnerStay(QuickActionMenuOption option)
        {
            if (!Main.PartnerManager.HasActivePartner)
            {
                return;
            }

           // CPlayer.LocalPlayer.Ped.SayAmbientSpeech("COVER_ME");

            EPartnerGroup group = EPartnerGroup.All;
            if (option != null && option.Value != null)
            {
                group = (EPartnerGroup)option.Value;
            }

            foreach (Partner partner in Main.PartnerManager.GetPartnersByGroup(group))
            {
                Partner p = partner;
                DelayedCaller.Call(delegate { p.HoldPosition(); }, this, Common.GetRandomValue(800, 1300));
            }

            Stats.UpdateStat(Stats.EStatType.PartnerOrderedKeepPosition, 1);
        }

        /// <summary>
        /// Makes the partner regroup.
        /// </summary>
        private void PartnerGoto(QuickActionMenuOption option)
        {
            if (!Main.PartnerManager.HasActivePartner)
            {
                return;
            }

            //CPlayer.LocalPlayer.Ped.SayAmbientSpeech("MOVE_IN");
            AimMarker aimMarker = (AimMarker)Main.ScriptManager.GetRunningScriptInstances("AimMarker").FirstOrDefault();
            if (aimMarker != null)
            {
                aimMarker.PerformCheck();

                EPartnerGroup group = EPartnerGroup.All;
                Color color = Color.Orange;
                if (option != null && option.Value != null)
                {
                    group = (EPartnerGroup)option.Value;
                    color = option.Color;
                }

                foreach (Partner partner in Main.PartnerManager.GetPartnersByGroup(group))
                {
                    Partner p = partner;
                    DelayedCaller.Call(delegate { p.MoveToPosition(aimMarker.GetLastHitPosition()); }, this, Common.GetRandomValue(300, 800));
                }

                Checkpoint checkpoint = new Checkpoint(aimMarker.GetLastHitPosition(), color, 0.3f);
                DelayedCaller.Call(this.FadeOutCheckpoint, this, 10, checkpoint);
            }
            else
            {
                Log.Warning("PartnerGoTo: AimMarker script not running", this);
            }

            Stats.UpdateStat(Stats.EStatType.PartnerOrderedGoTo, 1);
        }

        private void PartnerDeployTaser(QuickActionMenuOption option)
        {
            if (!Main.PartnerManager.HasActivePartner)
            {
                return;
            }

            // Make partner arrest target.
            CPed ped = null;
            ped = CPlayer.LocalPlayer.Ped.IsAiming ? CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed() : CPlayer.LocalPlayer.GetTargetedPed();
            if (ped != null && ped.Exists() && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCop) && !ped.IsInVehicle)
            {
                EPartnerGroup group = EPartnerGroup.All;
                if (option != null && option.Value != null)
                {
                    group = (EPartnerGroup)option.Value;
                }

                foreach (Partner partner in Main.PartnerManager.GetPartnersByGroup(group))
                {
                    TaskCopTasePed taskCopTasePed = new TaskCopTasePed(ped, true);
                    taskCopTasePed.IgnoreSurrender = true;
                    taskCopTasePed.AssignTo(partner.PartnerPed, ETaskPriority.MainTask);
                }
            }
        }

        private void PartnerCoverPed(QuickActionMenuOption option)
        {
            CPed ped = null;
            ped = CPlayer.LocalPlayer.Ped.IsAiming ? CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed() : CPlayer.LocalPlayer.GetTargetedPed();
            if (ped != null && ped.Exists() && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCop))
            {
                EPartnerGroup group = EPartnerGroup.All;
                if (option != null && option.Value != null)
                {
                    group = (EPartnerGroup)option.Value;
                }

                foreach (Partner partner in Main.PartnerManager.GetPartnersByGroup(group).Where(partner => partner.Intelligence.IsFreeForTask))
                {
                    Partner p = partner;
                    if (p.PartnerPed.Position.DistanceTo(ped.Position) < 30)
                    {
                        p.CoverPedTarget(ped);
                    }
                }
            }
        }

        private void PartnerHoldAtGunpoint(QuickActionMenuOption option)
        {
            this.PartnerArrestOrGunpoint(false, option);
        }

        /// <summary>
        /// Called to fade out the checkpoint where the partner goes to.
        /// </summary>
        /// <param name="objects">The parameter.</param>
        private void FadeOutCheckpoint(params object[] objects)
        {
            Checkpoint checkpoint = objects[0] as Checkpoint;
            if (checkpoint.Color.A <= 10)
            {
                checkpoint.Disable();
            }
            else
            {
                checkpoint.Color = Color.FromArgb(checkpoint.Color.A - 8, checkpoint.Color);
                DelayedCaller.Call(this.FadeOutCheckpoint, this, 10, checkpoint);
            }          
        }

        /// <summary>
        /// Called when player called backup using quick action menu.
        /// </summary>
        private void RequestBackup()
        {
            string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
            Main.BackupManager.RequestPoliceBackup(CPlayer.LocalPlayer.Ped.Position, reporter);
        }

        /// <summary>
        /// Called when player requested noose backup using quick action menu.
        /// </summary>
        private void RequestNoose()
        {
            string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
            Main.BackupManager.RequestNooseBackup(CPlayer.LocalPlayer.Ped.Position, reporter, false, ECopState.Investigating, null);
        }

        /// <summary>
        /// Called when player requested helicopter backup using quick action menu.
        /// </summary>
        private void RequestHelicopter()
        {
            string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
            Main.BackupManager.RequestPoliceHelicopter(CPlayer.LocalPlayer.Ped.Position, reporter);
        }

        /// <summary>
        /// Requests an ambulance.
        /// </summary>
        private void RequestAmbulance()
        {
            Main.BackupManager.RequestAmbulance(CPlayer.LocalPlayer.Ped.Position, CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName());

        }

        /// <summary>
        /// Requests firefighter.
        /// </summary>
        private void RequestFirefighter()
        {
            Main.BackupManager.RequestFiretruck(CPlayer.LocalPlayer.Ped.Position, CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName());
        }
    }
}