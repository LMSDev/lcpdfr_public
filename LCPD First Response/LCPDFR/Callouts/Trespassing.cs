namespace LCPD_First_Response.LCPDFR.Callouts
{
    using GTA;
    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Scripts;
    using System;
    using System.Collections.Generic;
    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// The trespassing callout.
    /// The basic idea here is that the player responds to a call of someone trespassing on private property
    /// This can work in primarily one of two ways:
    /// 
    /// 1 / The player responds to a call of trespassing at a house
    /// 2 / The player responds to a call of trespassing at private property, like some of the places with security guards stationed
    /// 
    /// For both types, there's a chance that the player won't be able to find any suspect there and it will be a false alarm
    /// For the second type, the player will get called by the security guard, so his first job is to locate the security guard
    /// The guard will then show the player where the trespasser is (or was last seen)
    /// 
    /// For the first type, the player will get called by the people living at the house, so his job is to get to the house
    /// Once at the house, the player will probably find a preacher or hobo or crazy man causing a disturbance
    /// His job will then be to deal with the situation (sometimes by leading the guy off the property and giving a warning,
    /// and sometimes by arresting)
    /// </summary>
    [CalloutInfo("Trespass", ECalloutProbability.VeryHigh)]
    internal class Trespassing : Callout, IPedController
    {
        /// <summary>
        /// The models for criminals.
        /// </summary>
        private string[,] criminalModels = new string[,]
	    {
	        {"M_M_LOONYBLACK", "M_Y_DRUG_01"},
	        {"M_M_LOONYWHITE", "M_M_TRAMPBLACK"},
	        {"M_M_TRAMPBLACK", "M_M_SAXPLAYER_01"},
	        {"M_M_TRAMPBLACK", "F_Y_HOOKER_03"},
	        {"F_Y_TOURIST_01", "M_Y_DEALER"}
	    };

        private int lastSpeech;

        /// <summary>
        /// The models for houseowners and security guards.
        /// </summary>
        private string[,] securityModels = new string[,]
	    {
	        {"M_Y_PRICH_01", "M_M_SECURITYMAN"},
	        {"F_Y_PRICH_01", "M_M_SECURITYMAN"},
	        {"F_Y_SHOP_03", "M_M_SECURITYMAN"},
	        {"F_Y_SOCIALITE", "M_M_SECURITYMAN"},
	        {"M_M_TENNIS", "M_Y_CLUBFIT"}
	    };

        /// <summary>
        /// The spawn positions for criminals.
        /// </summary>
        private SpawnPoint[,] criminalPositions = new SpawnPoint[,]
	    {
	        {new SpawnPoint(270f, new Vector3(1305.339f, -802.1895f, 8.219299f)), new SpawnPoint(0f, new Vector3(0f, 0f, 0f))},
	    };

        /// <summary>
        /// The spawn positions for houseowners and security guards.
        /// </summary>
        private SpawnPoint[,] guardPositions = new SpawnPoint[,]
	    {
	        {new SpawnPoint(315f, new Vector3(1330.951f, -793.5337f, 8.215171f)), new SpawnPoint(0f, new Vector3(0f, 0f, 0f))},
	    };

        /// <summary>
        /// The blip positions for the calls
        /// </summary>
        private Vector3[,] blipPositions = new Vector3[,]
	    {
	        {new Vector3(1339.849f, -790.1414f, 8.201225f), new Vector3(0f,0f,0f)},
	    };

        /// <summary>
        /// The callout message
        /// </summary>
        private string[] calloutMessages = new string[]
	    {
	        "CALLOUT_TRESPASS_HOUSE_MESSAGE", "CALLOUT_TRESPASS_PROPERTY_MESSAGE"
	    };

        /// <summary>
        /// The blip of the position.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The criminals.
        /// </summary>
        private List<CPed> criminals;

        /// <summary>
        /// The caller/security.
        /// </summary>
        private CPed caller;

        /// <summary>
        /// The civilians.
        /// </summary>
        private List<CPed> civilians;

        /// <summary>
        /// The pursuit instance used in case suspect wants to flee.
        /// </summary>
        private Pursuit pursuit;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private Vector3 spawnPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mugging"/> class.
        /// </summary>
        public Trespassing()
        {
            if (Common.GetRandomBool(0, 2, 1))
            {
                // Do house
                this.callType = ETrespassType.HouseTrespass;
            }
            else
            {
                // Do property
                this.callType = ETrespassType.PropertyTrespass;   
            } 
        }

        /// <summary>
        /// The types of call.
        /// </summary>
        internal enum ETrespassType
        {   
            HouseTrespass,
            PropertyTrespass
        }

        /// <summary>
        /// The type of call.
        /// </summary>
        private ETrespassType callType;

        /// <summary>
        /// The trespass state.
        /// </summary>
        internal enum ETrespassState
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
            /// Player is almost there.
            /// </summary>
            PlayerArriving,

            /// <summary>
            /// Player has arrived
            /// </summary>
            PlayerArrived,

            /// <summary>
            /// Player has approached the caller
            /// </summary>
            ApproachedCaller,

            /// <summary>
            // Player is waiting on the caller to give instructions
            /// </summary>
            WaitingOnCaller,

            /// <summary>
            /// Player is approaching the suspect
            /// </summary>
            ApproachingSuspect,

            /// <summary>
            /// Player has approached the suspect
            /// </summary>
            ApproachedSuspect
        }

        /// <summary>
        /// Called just before the callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            this.spawnPoint = blipPositions[0, (int)callType];

            // Get area name
            string area = AreaHelper.GetAreaNameMeaningful(this.spawnPoint);
            this.CalloutMessage = string.Format(CultureHelper.GetText(calloutMessages[(int)callType]), area);
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
            this.pursuit.AllowSuspectWeapons = false;
            this.pursuit.AllowSuspectVehicles = false;

            // Create the criminal(s)

            this.criminals = new List<CPed>();
            int random = 0; //Common.GetRandomValue(2, 5);
            for (int i = 0; i < 1; i++)
            {
                CPed criminal = new CPed(this.criminalModels[random,(int)callType], this.criminalPositions[random,(int)callType].Position, EPedGroup.Criminal);
                if (criminal.Exists())
                {
                    criminal.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                    criminal.RelationshipGroup = RelationshipGroup.Special;
                    criminal.ChangeRelationship(RelationshipGroup.Special, Relationship.Companion);
                    criminal.Heading = this.criminalPositions[random, (int)callType].Heading;

                    // We don't want the criminal to flee yet
                    criminal.PedData.DisableChaseAI = true;

                    // this.pursuit.AddTarget(criminal);
                    this.criminals.Add(criminal);

                    // Pursuit and content manage
                    this.ContentManager.AddPed(criminal);
                    this.pursuit.AddTarget(criminal);
                }
            }

            this.civilians = new List<CPed>();
            for (int i = 0; i < 1; i++)
            {
                caller = new CPed(this.securityModels[random, (int)callType], this.guardPositions[random, (int)callType].Position, EPedGroup.MissionPed);
                if (caller.Exists())
                {
                    this.ContentManager.AddPed(caller);
                    caller.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                    caller.BlockPermanentEvents = true;
                    caller.Voice = "M_M_PRICH_01";
                    // this.pursuit.AddTarget(criminal);
                    caller.Animation.Play(new AnimationSet("amb@drugd_idl_b"), "idle_c", 4.0f, AnimationFlags.None);

                    CPed victim = new CPed(this.securityModels[1, (int)callType], new Vector3(1307.493f, -801.9657f, 8.202059f), EPedGroup.MissionPed);
                    if (victim.Exists())
                    {
                        this.ContentManager.AddPed(victim);
                        victim.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                        victim.BlockPermanentEvents = true;
                        victim.Voice = "F_Y_PRICH_01";
                        victim.Heading = 90f;
                        // this.pursuit.AddTarget(criminal);
                        this.civilians.Add(victim);
                        victim.Animation.Play(new AnimationSet("missdwayne1"), "nervous_idle", 4.0f, AnimationFlags.Unknown05);
                    }
                
                }
            }

            // Add blip
            this.blip = Blip.AddBlipContact(this.spawnPoint);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;
            this.blip.Icon = BlipIcon.Misc_Objective;

            // Add states
            this.RegisterStateCallback(ETrespassState.WaitingForPlayer, this.WaitingForPlayer);
            this.RegisterStateCallback(ETrespassState.PlayerArriving, this.PlayerArriving);
            this.RegisterStateCallback(ETrespassState.PlayerArrived, this.PlayerArrived);
            this.RegisterStateCallback(ETrespassState.ApproachedCaller, this.ApproachedCaller);
            this.RegisterStateCallback(ETrespassState.WaitingOnCaller, this.WaitingOnCaller);
            this.RegisterStateCallback(ETrespassState.ApproachingSuspect, this.ApproachSuspect);
            this.RegisterStateCallback(ETrespassState.ApproachedSuspect, this.ApproachedSuspect);
            this.State = ETrespassState.WaitingForPlayer;
            
            return true;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.pursuit.IsRunning)
            {
                this.End();
            }
            else
            {
                if ((ETrespassState)this.State == ETrespassState.ApproachingSuspect || (ETrespassState)this.State == ETrespassState.ApproachedCaller)
                {
                    foreach (CPed ped in criminals)
                    {
                        if (ped.Exists())
                        {
                            if (!ped.IsAmbientSpeechPlaying)
                            {
                                if (lastSpeech <= 15)
                                {
                                    ped.SayAmbientSpeech("RANT_E_" + lastSpeech.ToString("D2"));
                                    lastSpeech++;
                                }
                                else
                                {
                                    lastSpeech = 1;
                                    ped.SayAmbientSpeech("RANT_E_01");
                                }
                            }
                        }
                    }

                    foreach (CPed ped in civilians)
                    {
                        if (ped.Exists())
                        {
                            if (!ped.IsAmbientSpeechPlaying)
                            {
                                if (ped.Gender == Gender.Female)
                                {
                                    if (Common.GetRandomBool(0, 200, 1))
                                    {
                                        int speech = Common.GetRandomValue(0, 6);

                                        if (speech == 0)
                                        {
                                            ped.SayAmbientSpeech("GET_OUT");
                                        }
                                        else if (speech == 1)
                                        {
                                            ped.SayAmbientSpeech("INSULT_BUM");
                                        }
                                        else if (speech == 2)
                                        {
                                            ped.SayAmbientSpeech("GENERIC_FUCK_OFF");
                                        }
                                        else if (speech == 3)
                                        {
                                            ped.SayAmbientSpeech("HECKLE");
                                        }
                                        else if (speech == 4)
                                        {
                                            ped.SayAmbientSpeech("CONV_ARGUE_D");
                                        }
                                        else if (speech == 5)
                                        {
                                            ped.SayAmbientSpeech("VEHICLE_ATTACKED");
                                        }
                                    }
                                }
                            }

                            if (criminals[0].Exists())
                            {
                                if (!Natives.IsCharFacingChar(ped, criminals[0]))
                                {
                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord))
                                    {
                                        ped.Task.TurnTo(criminals[0]);
                                    }
                                    else
                                    {
                                        if (!ped.Animation.isPlaying(new AnimationSet("missdwayne1"), "nervous_idle"))
                                        {
                                            ped.Task.PlayAnimSecondaryUpperBody("nervous_idle", "missdwayne1", 4.0f, true);
                                        }
                                    }
                                }

                                if (!Natives.IsCharFacingChar(criminals[0], ped))
                                {
                                    if (!criminals[0].Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord))
                                    {
                                        criminals[0].Task.TurnTo(ped);
                                    }
                                }
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
            foreach (CPed ped in civilians)
            {
                if (ped.Exists())
                {
                    if (ped.Blip != null && ped.Blip.Exists())
                    {
                        ped.Blip.Delete();
                    }
                }
            }

            foreach (CPed ped in criminals)
            {
                if (ped.Exists())
                {
                    if (ped.Blip != null && ped.Blip.Exists())
                    {
                        ped.Blip.Delete();
                    }
                }
            }

            if (caller.Exists())
            {
                if (caller.Blip != null && caller.Blip.Exists())
                {
                    caller.Blip.Delete();
                }
            }

            base.End();

            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }

            //if (this.pursuit
            //this.pursuit.EndChase();
        }

        /// <summary>
        /// Waiting for player.
        /// </summary>
        private void WaitingForPlayer()
        {
            if ((ETrespassState)this.State != ETrespassState.WaitingForPlayer) return;

            if (caller!= null && caller.Exists())
            {
                if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(this.spawnPoint) < 5f)
                {
                    caller.Task.ClearAll();
                    GTA.TaskSequence sequence = new GTA.TaskSequence();
                    sequence.AddTask.TurnTo(CPlayer.LocalPlayer.Ped);
                    sequence.AddTask.PlayAnimation(new AnimationSet("amb@inquisitive"), "shock_d", 4.0f, AnimationFlags.None);
                    sequence.AddTask.PlayAnimation(new AnimationSet("amb@drugd_idl_a"), "idle_a", 4.0f, AnimationFlags.Unknown05);
                    caller.Task.PerformSequence(sequence);
                    caller.Task.AlwaysKeepTask = true;
                    this.State = ETrespassState.PlayerArriving;
                    Game.DisplayText("Player Arriving");

                    foreach (CPed criminal in criminals)
                    {
                        if (criminal.Exists())
                        {
                            if (criminal.Model == "M_M_LOONYBLACK" || criminal.Model == "M_M_LOONYWHITE")
                            {
                                criminal.Task.TurnTo(caller);
                                criminal.Task.PlayAnimation(new AnimationSet("amb@default"), "preacher_default", 4.0f, AnimationFlags.Unknown05);
                            }
                        }
                    }
                }
                else
                {
                    if (caller != null && caller.Exists())
                    {
                        if (!caller.Animation.isPlaying(new AnimationSet("amb@drugd_idl_b"), "idle_c"))
                        {
                            caller.Animation.Play(new AnimationSet("amb@drugd_idl_b"), "idle_c", 4.0f, AnimationFlags.None);
                        }
                    }
                }
            #region oldshit
            /*
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
             * */
            #endregion
            }
        }

        /// <summary>
        /// Player is arriving
        /// </summary>
        private void PlayerArriving()
        {
            if ((ETrespassState)this.State != ETrespassState.PlayerArriving) return;

            if (caller.Exists())
            {
                if (!caller.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord))
                {
                    if (!Natives.IsCharFacingChar(caller, CPlayer.LocalPlayer.Ped))
                    {
                        caller.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                    }
                    else
                    {
                        this.blip.Delete();
                        caller.SayAmbientSpeech("MUGGED_HELP");
                        TextHelper.PrintText("Go to the ~b~caller~w~.", 5000);
                        caller.AttachBlip().Friendly = true;
                        this.State = ETrespassState.PlayerArrived;
                        Game.DisplayText("Player Arrived");
                    }
                }
            }
        }        

        /// <summary>
        /// Player has arrived
        /// </summary>
        private void PlayerArrived()
        {
            if ((ETrespassState)this.State != ETrespassState.PlayerArrived) return;

            if (caller.Exists())
            {
                if (!CPlayer.LocalPlayer.Ped.IsInVehicle)
                {
                    if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(caller.Position) < 5f && caller.Intelligence.CanSeePed(CPlayer.LocalPlayer.Ped))
                    {
                        caller.Task.ClearAll();
                        foreach (CPed criminal in criminals)
                        {
                            if (criminal.Exists())
                            {
                                caller.Task.TurnTo(criminal);
                                this.State = ETrespassState.ApproachedCaller;
                                Game.DisplayText("Approached caller");
                                break;
                            }
                        }
                            
                    }
                }

                if (caller.Animation.isPlaying(new AnimationSet("amb@inquisitive"), "shock_d"))
                {
                    if (!caller.IsAmbientSpeechPlaying)
                    {
                        caller.SayAmbientSpeech("MUGGED_HELP");
                    }
                }
            }
        }

        /// <summary>
        /// Player has approached the caller
        /// </summary>
        private void ApproachedCaller()
        {
            if ((ETrespassState)this.State != ETrespassState.ApproachedCaller) return;

            GTA.Native.Function.Call("ALLOCATE_SCRIPT_TO_RANDOM_PED","ambpreacher", 379171768, 100, 1);
            GTA.Native.Function.Call("ALLOCATE_SCRIPT_TO_RANDOM_PED", "ambpreacher", 495499562, 100, 1);

            CPed ped = caller;
            if (ped.Exists())
            {
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord))
                {
                    if (!Natives.IsCharFacingChar(ped, criminals[0]))
                    {
                        ped.Task.TurnTo(criminals[0]);
                    }
                    else
                    {
                        // TextHelper.PrintText("Locate the ~r~suspect~w~.", 5000);
                        this.State = ETrespassState.WaitingOnCaller;
                        Game.DisplayText("Waiting on caller");
                    }
                }
            }
        }

        private void WaitingOnCaller()
        {
            if ((ETrespassState)this.State != ETrespassState.WaitingOnCaller) return;

            if (caller.Exists())
            {
                if (caller.Animation.isPlaying(new AnimationSet("missfrancis3"), "point_fwd"))
                {
                    if (!caller.IsAmbientSpeechPlaying)
                    {
                        caller.SayAmbientSpeech("PLAYER_OVER_THERE");
                    }
                    else
                    {
                        this.State = ETrespassState.ApproachingSuspect;
                        TextHelper.PrintText("Locate the ~r~suspect~w~.", 5000);
                    }
                }
                else
                {
                    caller.Animation.Play(new AnimationSet("missfrancis3"), "point_fwd", 4.0f, AnimationFlags.None);
                }
            }
        }

        /// <summary>
        /// Player is approaching the suspect
        /// </summary>
        private void ApproachSuspect()
        {
            if ((ETrespassState)this.State != ETrespassState.ApproachingSuspect) return;

            foreach (CPed criminal in criminals)
            {
                if (criminal.Exists())
                {
                    if (criminal.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 5.0f && CPlayer.LocalPlayer.Ped.Intelligence.CanSeePed(criminal))
                    {
                        // The suspect should decide at this point what to do
                        CPlayer.LocalPlayer.Ped.SayAmbientSpeech("SPOT_CRIME");

                        // For now, we'll just make him cooperate
                        criminal.Task.ClearAll();
                        GTA.TaskSequence sequence = new GTA.TaskSequence();
                        criminal.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                        //criminal.Task.PlayAnimation(new AnimationSet("missfrancis2"), "cower_idle", 4.0f, AnimationFlags.Unknown05);
                        criminal.Task.PerformSequence(sequence);
                        criminal.AttachBlip().Name = "Suspect";
                        this.State = ETrespassState.ApproachedSuspect;
                        Game.DisplayText("Approached suspect");
                    }
                }
            }

            if (caller.Exists())
            {
                if (!caller.Animation.isPlaying(new AnimationSet("missfrancis3"), "point_fwd"))
                {
                    if (caller.Position.DistanceTo2D(CPlayer.LocalPlayer.Ped.Position) > 5)
                    {
                        if (!caller.IsInGroup)
                        {
                            CPlayer.LocalPlayer.Group.AddMember(caller);
                        }
                    }
                    else
                    {
                        if (caller.Position.DistanceTo2D(CPlayer.LocalPlayer.Ped.Position) < 4)
                        {
                            if (caller.IsInGroup)
                            {
                                CPlayer.LocalPlayer.Group.RemoveMember(caller);
                            }

                            if (!caller.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord))
                            {
                                caller.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Player has approached the suspect
        /// </summary>
        private void ApproachedSuspect()
        {
            if ((ETrespassState)this.State != ETrespassState.ApproachedSuspect) return;

            foreach (CPed ped in civilians)
            {
                if (ped.Exists())
                {
                    if (ped.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 5.0f)
                    {
                        ped.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                        DelayedCaller.Call(delegate { ped.SayAmbientSpeech("SHOCKED"); }, 2000);
                    }
                    else
                    {
                        ped.Task.GoTo(CPlayer.LocalPlayer.Ped);
                    }
                }
            }

            foreach (CPed ped in criminals)
            {
                if (ped.Exists())
                {
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSequence))
                    {
                        this.pursuit.DontEnableCopBlips = false;
                        this.pursuit.AllowSuspectWeapons = false;
                        this.pursuit.AllowSuspectVehicles = false;
                        this.pursuit.CanCopsJoin = true;
                        this.pursuit.AllowMaxUnitsTolerance = false;
                        this.pursuit.HasBeenCalledIn = false;
                        this.pursuit.MakeActiveChase(2500, -1);
                        ped.PedData.DisableChaseAI = false;
                        ped.PedData.CanResistArrest = true;
                    }

                    /*
                    if (ped.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 5.0f)
                    {
                        ped.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                    }
                    else
                    {
                        TextHelper.PrintText("Go back to the ~r~suspect~w~.", 5000);
                        this.State = ETrespassState.ApproachingSuspect;
                        this.End();
                    }
                     * */
                }
            }

            if (caller.Exists())
            {
                if (caller.IsInGroup)
                {
                    CPlayer.LocalPlayer.Group.RemoveMember(caller);
                }
            }

            this.State = ETrespassState.None;
            // this.End();
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
        }
    }
}