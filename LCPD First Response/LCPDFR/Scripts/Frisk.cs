namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.Events;

    /// <summary>
    /// Handles the frisking process.
    /// </summary>
    [ScriptInfo("Frisk", true)]
    internal class Frisk : GameScript
    {
        /// <summary>
        /// The frisking state.
        /// </summary>
        private EFriskState friskState;

        /// <summary>
        /// The target.
        /// </summary>
        private CPed target;

        /// <summary>
        /// Whether the ped has turned away from player already.
        /// </summary>
        private bool turnedFromPlayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Frisk"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public Frisk(CPed target)
        {
            this.target = target;
            this.StartFrisking();
        }

        /// <summary>
        /// The frisking state.
        /// </summary>
        private enum EFriskState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None,

            /// <summary>
            /// Frisking has started and player is selecting an action.
            /// </summary>
            SelectingAction,

            /// <summary>
            /// The player is approaching the suspect.
            /// </summary>
            ApproachingSuspect,

            /// <summary>
            /// Frisking is really starting now.
            /// </summary>
            StartFrisk,

            /// <summary>
            /// The player is asking for the ID.
            /// </summary>
            AskingForID,

            /// <summary>
            /// The player has asked for the ID.
            /// </summary>
            AskedForID,

            /// <summary>
            /// Displaying the ID.
            /// </summary>
            DisplayID,
        }

        /// <summary>
        /// Gets a value indicating whether the ped shouldn't be freed.
        /// </summary>
        public bool DontFreePed { get; private set; }

        /// <summary>
        /// Gets the suspect.
        /// </summary>
        public CPed Suspect
        {
            get
            {
                return this.target;
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.target.IsAliveAndWell)
            {
                this.End();
                return;
            }

            // TODO: Add keys to LCPDFRKeys
            switch (this.friskState)
            {
                case EFriskState.SelectingAction:
                    //if (KeyHandler.IsKeyDown(ELCPDFRKeys.FriskHold))
                    //{
                    //    // Player has pressed F9, therefore they have chosen to hold the suspect
                    //    string[] randomAnimations = new string[] { "I_SAID_NO", "IM_TALKING_2_YOU", "ITS_MINE", "THAT_WAY" };
                    //    CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody(Common.GetRandomCollectionValue<string>(randomAnimations), "gestures@male", 4.0f, true, 0, 0, 0, 3000);
                    //    TextHelper.ClearHelpbox();
                    //    this.DontFreePed = true;
                    //    this.End();
                    //}

                    //if (KeyHandler.IsKeyDown(ELCPDFRKeys.FriskStartFrisking))
                    //{
                    //    // Player has pressed F10, therefore they have chosen to frisk the suspect
                    //    TextHelper.PrintText(CultureHelper.GetText("FRISK_GET_BEHIND_SUSPECT"), 5000);
                    //    this.target.DontActivateRagdollFromPlayerImpact = true;
                    //    CPlayer.LocalPlayer.Ped.SayAmbientSpeech("PLACE_HANDS_ON_HEAD");
                    //    this.friskState = EFriskState.ApproachingSuspect;
                    //}

                    //if (KeyHandler.IsKeyDown(ELCPDFRKeys.FriskAskForID))
                    //{
                    //    // Player has pressed F11 therefore they have chosen to ask for ID
                    //    CPlayer.LocalPlayer.Ped.SayAmbientSpeech("ASK_FOR_ID");
                    //    TextHelper.ClearHelpbox();
                    //    this.friskState = EFriskState.AskingForID;
                    //}

                    //if (KeyHandler.IsKeyDown(ELCPDFRKeys.FriskRelease))
                    //{
                    //    // Player has pressed F12 therefore they have chosen to cancel
                    //    CPlayer.LocalPlayer.Ped.SayAmbientSpeech("FOUND_NOTHING");
                    //    TextHelper.ClearHelpbox();
                    //    this.End();
                    //}

                    break;

                case EFriskState.ApproachingSuspect:
                    if (!CPlayer.LocalPlayer.Ped.IsAmbientSpeechPlaying)
                    {
                        if (!this.target.Animation.isPlaying(new AnimationSet("cop"), "armsup_2_searched_pose"))
                        {
                            this.target.BlockGestures = true;
                            this.target.Task.PlayAnimation(new AnimationSet("cop"), "armsup_2_searched_pose", 1.0f, AnimationFlags.Unknown06);
                        }
                    }

                    if (!this.turnedFromPlayer)
                    {
                        this.target.Task.AchieveHeading(CPlayer.LocalPlayer.Ped.Heading);
                        this.turnedFromPlayer = true;
                    }

                    if (this.target.Animation.isPlaying(new AnimationSet("cop"), "armsup_2_searched_pose") &&
                        CPlayer.LocalPlayer.Ped.Position.DistanceTo2D(this.target.GetOffsetPosition(new Vector3(0, -0.50f, 0))) < 0.25f)
                    {
                        TextHelper.ClearHelpbox();

                        // Disable player control and teleport
                        CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = false;
                        CPlayer.LocalPlayer.CanControlCharacter = false;
                        CPlayer.LocalPlayer.Ped.Heading = this.target.Heading;
                        CPlayer.LocalPlayer.Ped.SetPositionDontWarpGang(this.target.GetOffsetPosition(new Vector3(0, -0.9f, -1)));

                        // Remove weapon and start anim
                        CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
                        CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet("cop"), "cop_search", 1.0f);

                        this.friskState = EFriskState.StartFrisk;
                    }

                    break;

                case EFriskState.StartFrisk:
                    // When animation has finished, re-enable player control
                    if (!CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("cop"), "cop_search"))
                    {
                        CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = true;
                        CPlayer.LocalPlayer.CanControlCharacter = true;

                        // Smarter frisking, step one is to check their luggage.
                        bool drugs, cards, weapons, nothing;

                        drugs = false;
                        cards = false;
                        weapons = false;
                        nothing = false;

                        if (this.target.PedData.Luggage.HasFlag(PedData.EPedLuggage.Weapons) || this.target.IsArmed())
                        {
                            weapons = true;
                        }
                        else if (this.target.PedData.Luggage.HasFlag(PedData.EPedLuggage.Drugs))
                        {
                            drugs = true;
                        }
                        else if (this.target.PedData.Luggage.HasFlag(PedData.EPedLuggage.StolenCards))
                        {
                            cards = true;
                        }

                        if (!cards && !drugs && !weapons)
                        {
                            // If they don't have any luggage flags, we do some model flag checking.
                            int jobModifier = 0;
                            int classModifier = 0;
                            int ageModifier = 0;

                            if (this.target.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsWealthUpperClass) || this.target.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsWealthMidClass))
                            {
                                // Upper class peds chances are decreased by 3
                                classModifier = 3;
                                //Game.Console.Print("Ped is upper or middle class");
                            }
                            else if (this.target.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsWealthPoor))
                            {
                                // Lower class peds chances are increased by 3
                                classModifier = -3;
                                //Game.Console.Print("Ped is lower class");
                            }

                            if (this.target.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsOld))
                            {
                                // Old peds chances are decreased by 3
                                ageModifier = 3;
                                //Game.Console.Print("Ped is lower class");
                            }

                            if (this.target.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.HasJob))
                            {
                                // Decreases chances of finding anything if the ped has a job by 5.
                                jobModifier = 5;
                            }

                            // Calculate the total modifier by adding them all up
                            int totalModifier = jobModifier + classModifier + ageModifier;

                            // Now use the modifier to calculate posession.
                            if (Common.GetRandomBool(0, totalModifier + 9, 1)) weapons = true;
                            if (Common.GetRandomBool(0, totalModifier + 9, 1)) drugs = true;
                            if (Common.GetRandomBool(0, totalModifier + 9, 1)) cards = true;

                            // And some overrides
                            if (this.target.Model == "F_Y_STRIPPERC01" || this.target.Model == "F_Y_STRIPPERC02")
                            {
                                // Not many places for a stripper to hide a gun or bank cards
                                weapons = false;
                                cards = false;
                            }
                            else if (this.target.Model == "M_M_GUNNUT_01")
                            {
                                // He has grenades on his model...
                                weapons = true;
                            }
                            else if (this.target.Model == "M_M_SECURITYMAN" || this.target.Model == "M_Y_CLUBFIT")
                            {
                                // Nothing for the security dude.
                                weapons = false;
                                drugs = false;
                                cards = false;
                            }
                        }

                        // TODO: Chance based on area
                        if (weapons || cards || drugs)
                        {
                            if (weapons)
                            {
                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("FOUND_WEAPON_ON_PED");
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("FRISK_FOUND_WEAPON"));
                            }
                            else if (drugs)
                            {
                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech(CPlayer.LocalPlayer.Ped.VoiceData.FoundDrugsSpeech);
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("FRISK_FOUND_DRUGS"));
                            }
                            else
                            {
                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("FOUND_STOLEN_BANK_CARDS");
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("FRISK_FOUND_CC"));
                            }

                            this.target.PedData.Luggage = PedData.EPedLuggage.Nothing;
                            this.target.PedData.Flags |= EPedFlags.HasBeenFrisked;
                            this.DontFreePed = true;
                            this.target.Weapons.RemoveAll();
                            this.End();
                        }
                        else
                        {
                            // Didn't find anything, so no weapon as well, thus we will set compliance value to 100
                            this.target.PedData.AlwaysSurrender = true;
                            this.target.PedData.Flags |= EPedFlags.HasBeenFrisked;
                            this.target.Weapons.RemoveAll();

                            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("FOUND_NOTHING");
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("FRISK_FOUND_NOTHING"));
                            this.End();
                        }
                    }

                    break;

                case EFriskState.AskingForID:

                    if (!CPlayer.LocalPlayer.Ped.IsAmbientSpeechPlaying)
                    {
                        this.friskState = EFriskState.AskedForID;
                    }

                    break;

                case EFriskState.AskedForID:
                    GTA.TaskSequence targetTask = new GTA.TaskSequence();
                    targetTask.AddTask.TurnTo(CPlayer.LocalPlayer.Ped);
                    targetTask.AddTask.PlayAnimation(new AnimationSet("amb@nightclub_ext"), "clubber_id_check", 4f, AnimationFlags.None);
                    targetTask.Perform(this.target);

                    GTA.TaskSequence copTask = new GTA.TaskSequence();
                    copTask.AddTask.TurnTo(this.target);
                    copTask.AddTask.PlayAnimation(new AnimationSet("amb@nightclub_ext"), "bouncer_a_checkid", 4f, AnimationFlags.None);
                    copTask.Perform(CPlayer.LocalPlayer.Ped);

                    CPlayer.LocalPlayer.LastPedPulledOver = CPlayer.LocalPlayer.LastPedPulledOver = new CPed[] { this.target };
                    string name = this.target.PedData.Persona.Forename + " " + this.target.PedData.Persona.Surname;
                    DateTime birthDay = this.target.PedData.Persona.BirthDay;
                    string data = string.Format(CultureHelper.GetText("FRISK_ID"), name, birthDay.ToLongDateString());
                    DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(data); }, 4000);

                    this.friskState = EFriskState.DisplayID;
                    break;

                case EFriskState.DisplayID:
                    if (!this.target.Animation.isPlaying(new AnimationSet("amb@nightclub_ext"), "clubber_id_check"))
                    {
                        DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("FRISK_START_OPTIONS")); }, 8000);
                        this.friskState = EFriskState.SelectingAction;
                    }

                    break;
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            this.target.ReleaseOwnership(this);
            base.End();
            this.target.Wanted.IsBeingFrisked = false;
        }

        /// <summary>
        /// Starts the frisk.
        /// </summary>
        private void StartFrisking()
        {
            // TODO: Resist frisk and start on foot chase

            // Fire event
            new EventPlayerStartedFrisk(this.target, this);

            // Make mission char and add to content manager
            this.target.RequestOwnership(this);
            this.target.AlwaysFreeOnDeath = true;
            this.target.Wanted.IsBeingFrisked = true;
            this.ContentManager.AddPed(this.target);

            // Prevent fleeing
            this.target.BlockPermanentEvents = true;
            this.target.Task.ClearAll();
            this.target.Intelligence.TaskManager.ClearTasks();

            TextHelper.ClearHelpbox();
            TextHelper.PrintText(CultureHelper.GetText("FRISK_GET_BEHIND_SUSPECT"), 5000);
            this.target.DontActivateRagdollFromPlayerImpact = true;
            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("PLACE_HANDS_ON_HEAD");
            Stats.UpdateStat(Stats.EStatType.Frisks, 1);

            // Play audio and proceed after a short delay, so audio was already played and it looks more realistic
            DelayedCaller.Call(delegate { this.friskState = EFriskState.ApproachingSuspect; }, 1000);
        }
    }
}