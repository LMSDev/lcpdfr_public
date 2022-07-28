namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Drawing;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Scripts.Events;

    using Main = LCPD_First_Response.LCPDFR.Main;
    using LCPD_First_Response.LCPDFR.Scripts.Scenarios;

    /// <summary>
    /// The in-game tutorial.
    /// </summary>
    [ScriptInfo("Tutorial", true)]
    internal class Tutorial : GameScript, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// The area of the tutorial defined by four positions.
        /// </summary>
        private Vector3[] tutorialArea = new Vector3[]
            {
                new Vector3(-1087.23f, -552.31f, 2.87f), new Vector3(-1083.74f, -607.24f, 2.89f), new Vector3(-998.97f, -604.77f, 2.94f), new Vector3(-997.48f, -545.51f, 3.04f)
            };

        /// <summary>
        /// Position where player was teleported from.
        /// </summary>
        private Vector3 oldPosition;

        /// <summary>
        /// Old vehicle of the player.
        /// </summary>
        private CVehicle oldVehicle;

        /// <summary>
        /// The police vehicle the player uses.
        /// </summary>
        private CVehicle policeVehicle;


        /// <summary>
        /// The ambient cop at the trunk of the ambient cop car
        /// </summary>
        private CPed trunkCop;

        /// <summary>
        /// The cop talking beside the ambient cop car
        /// </summary>
        private CPed talkingCop1;

        /// <summary>
        /// The cop talking to the other cop beside the ambient cop car
        /// </summary>
        private CPed talkingCop2;

        /// <summary>
        /// The ambient cop that follows the player around and watches
        /// </summary>
        private CPed observingCop;

        /// <summary>
        /// The obeserving cop's coffee
        /// </summary>
        private GTA.Object coffee;

        /// <summary>
        /// The ambient cop car
        /// </summary>
        private CVehicle copCar;

        /// <summary>
        /// Whether or not the prompt for step 2 of frisking has been shown
        /// </summary>
        private bool friskStep2Shown;

        /// <summary>
        /// Whether or not the tutorial is restarting
        /// </summary>
        private bool tutorialRestarting;

        /// <summary>
        /// The vehicle the player should pull over.
        /// </summary>
        private CVehicle pulloverVehicle;

        /// <summary>
        /// The start position for the player.
        /// </summary>
        private Vector3 startPositon = new Vector3(-843.3301f, 1314.639f, 21.97743f);

        /// <summary>
        /// The ped used for tests such as arresting.
        /// </summary>
        private CPed testPed;

        /// <summary>
        /// The tutorial state.
        /// </summary>
        private ETutorialState tutorialState;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tutorial"/> class.
        /// </summary>
        public Tutorial()
        {
            this.tutorialState = ETutorialState.None;
            this.Setup();
            Stats.UpdateStat(Stats.EStatType.TutorialPlayed, 1);
        }

        /// <summary>
        /// The state of the tutorial.
        /// </summary>
        internal enum ETutorialState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None,

            /// <summary>
            /// Player should pull out a weapon.
            /// </summary>
            PullOutWeapon,

            /// <summary>
            /// The player has  a weapon.
            /// </summary>
            HasWeapon,

            /// <summary>
            /// The player should stop a ped.
            /// </summary>
            StopPed,

            /// <summary>
            /// The player should frisk the ped.
            /// </summary>
            FriskPed,

            /// <summary>
            /// The player is frisking.
            /// </summary>
            IsFrisking,

            /// <summary>
            /// The player should arrest the ped.
            /// </summary>
            ArrestPed,

            /// <summary>
            /// The ped is cuffed.
            /// </summary>
            IsCuffed,

            /// <summary>
            /// The player should grab the ped
            /// </summary>
            GrabPed,

            /// <summary>
            /// The ped is grabbed
            /// </summary>
            IsGrabbing,

            /// <summary>
            /// Player should pullover.
            /// </summary>
            PulloverVehicle,

            /// <summary>
            /// Player is pulling over.
            /// </summary>
            PullingOver,
        }

        /// <summary>
        /// Called when the user typed "StartTutorial".
        /// </summary>
        /// <param name="parameterCollection">
        /// The parameter Collection.
        /// </param>
        [ConsoleCommand("StartTutorial")]
        public static void StartTutorialConsoleCallback(ParameterCollection parameterCollection)
        {
            Main.ScriptManager.StartScript<Tutorial>("Tutorial");
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            if (tutorialRestarting)
            {
                // Don't process if the tutorial is restarting;
                return;
            }

            // Port back if player left area
            if (CPlayer.LocalPlayer.Ped.Position.DistanceTo2D(this.startPositon) > 100.0f)
            {
                // When doing pullover, we use a distance check to the ped in the vehicle instead
                if (this.tutorialState == ETutorialState.PullingOver)
                {
                    if (CPlayer.LocalPlayer.Ped.Position.DistanceTo(this.testPed.Position) > 30f)
                    {
                        // Fade screen out
                        Game.FadeScreenOut(3000, true);
                        CPlayer.LocalPlayer.Ped.SetPositionDontWarpGang(this.testPed.GetOffsetPosition(new Vector3(0, -8, 0)));
                        CPlayer.LocalPlayer.Ped.CurrentVehicle.PlaceOnGroundProperly();
                        Game.FadeScreenIn(3000, true);
                    }
                }
                else
                {
                    if (this.tutorialState != ETutorialState.None)
                    {
                        // Fade screen out
                        Game.FadeScreenOut(3000, true);
                        CPlayer.LocalPlayer.Ped.SetPositionDontWarpGang(this.startPositon);
                        Game.FadeScreenIn(3000, true);
                    }
                }
            }

            if (this.observingCop != null && this.observingCop.Exists())
            {
                if (this.observingCop.HasBeenDamagedBy(CPlayer.LocalPlayer.Ped))
                {
                    // Bad!
                    this.observingCop.ClearLastDamageEntity();
                    if (!this.observingCop.IsAmbientSpeechPlaying) this.observingCop.SayAmbientSpeech("SHOCKED");

                    RestartTutorial();
                    return;
                }

                if (this.observingCop != null && this.observingCop.Exists())
                {
                    if (this.observingCop.Position.DistanceTo(new Vector3(-852.4323f, 1321.693f, 21.97742f)) < 1.75f)
                    {
                        if (!Natives.IsCharFacingChar(this.observingCop, CPlayer.LocalPlayer.Ped))
                        {
                            this.observingCop.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                        }
                    }
                }
            }

            if (this.testPed != null && this.testPed.Exists())
            {
                if (this.testPed.HasBeenDamagedBy(CPlayer.LocalPlayer.Ped))
                {

                    this.testPed.ClearLastDamageEntity();

                    if (!this.testPed.IsAmbientSpeechPlaying)
                    {
                        this.testPed.SayAmbientSpeech("DRUGS_REJECT");
                    }

                    if (this.observingCop.Exists())
                    {
                        this.observingCop.Task.ClearAll();
                        this.observingCop.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                        this.observingCop.SayAmbientSpeech("SHOCKED");
                    }

                    RestartTutorial();
                    return;
                }
            }

            switch (this.tutorialState)
            {
                case ETutorialState.PullOutWeapon:
                    if (CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Handgun_Glock)
                    {
                        // Lock weapon
                        CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = true;
                        CPlayer.LocalPlayer.CanControlCharacter = false;
                        CPlayer.LocalPlayer.Ped.Task.SwapWeapon(Weapon.Handgun_Glock);

                        // Clear text and print new
                        TextHelper.PrintText(string.Empty, 1);
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_STOP_PEDS"));
                        this.tutorialState = ETutorialState.HasWeapon;
                        DelayedCaller.Call(this.HasWeaponCallback, 10000);
                    }
                    break;

                case ETutorialState.StopPed:
                    if (this.testPed.Wanted.IsStopped)
                    {
                        CPlayer.LocalPlayer.CanControlCharacter = false;
                        CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = false;
                        this.testPed.PedData.Flags = EPedFlags.OnlyAllowFrisking;

                        TextHelper.PrintText(string.Empty, 1);
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_ARREST_PEDS"));
                        DelayedCaller.Call(this.StoppedPedCallback, 7000);
                        this.tutorialState = ETutorialState.FriskPed;
                    }

                    break;

                case ETutorialState.FriskPed:
                    if (this.testPed.Wanted.IsBeingFrisked)
                    {
                        this.testPed.PedData.Flags = EPedFlags.None;
                        this.tutorialState = ETutorialState.IsFrisking;
                    }
                    else
                    {
                        if (!friskStep2Shown && CPlayer<LCPDFRPlayer>.LocalPlayer.IsViewingArrestOptions)
                        {
                            TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_DO_FRISK_STEP_2"), 10000);
                            friskStep2Shown = true;
                        }
                    }

                    if (this.testPed.Wanted.IsBeingArrested)
                    {
                        CPlayer.LocalPlayer.CanControlCharacter = false;
                        TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_FAILED_OBJECTIVE"), 5000);
                        CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.ClearTasks();

                        if (this.observingCop.Exists())
                        {
                            this.observingCop.Task.ClearAll();
                            this.observingCop.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                            this.observingCop.SayAmbientSpeech("SHOCKED");
                        }

                        RestartTutorial();
                    }

                    break;

                case ETutorialState.IsFrisking:
                    if (!this.testPed.Wanted.IsBeingFrisked)
                    {
                        this.testPed.Task.StandStill(int.MaxValue);
                        TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_DO_ARREST"), 5000);
                        this.tutorialState = ETutorialState.ArrestPed;
                    }

                    break;

                case ETutorialState.ArrestPed:
                    if (this.testPed.Wanted.IsBeingArrested)
                    {
                        if (this.testPed.Wanted.IsCuffed)
                        {
                            // Cancel arrest
                            BaseScript[] scripts = Main.ScriptManager.GetRunningScriptInstances("Arrest");
                            foreach (BaseScript baseScript in scripts)
                            {
                                baseScript.End();
                            }

                            // Hack: Make ped cuffed
                            TaskPlayAnimationAndRepeat taskPlayAnimationAndRepeat = new TaskPlayAnimationAndRepeat("idle", "move_m@h_cuffed", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                            taskPlayAnimationAndRepeat.AssignTo(this.testPed, ETaskPriority.MainTask);
                            this.testPed.Wanted.IsCuffed = false;
                            CPlayer.LocalPlayer.CanControlCharacter = false;
                            this.testPed.BecomeMissionCharacter();
                            this.testPed.Task.ClearAll();
                            this.testPed.Task.StandStill(int.MaxValue);
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_PED_CUFFED"));


                            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_PED_GRABBING")); }, 8000);

                            DelayedCaller.Call(this.IsCuffedCallback, 15000);
                            this.tutorialState = ETutorialState.IsCuffed;
                        }
                    }

                    if (this.testPed.Wanted.IsBeingFrisked)
                    {
                        CPlayer.LocalPlayer.CanControlCharacter = false;
                        TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_FAILED_OBJECTIVE"), 5000);
                        CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.ClearTasks();

                        if (this.observingCop.Exists())
                        {
                            this.observingCop.Task.ClearAll();
                            this.observingCop.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                            this.observingCop.SayAmbientSpeech("SHOCKED");
                        }

                        RestartTutorial();
                    }

                    break;

                case ETutorialState.GrabPed:
                    if (this.testPed.IsGrabbed)
                    {
                        if (this.copCar.Exists())
                        {
                            this.copCar.AttachBlip().Friendly = true;
                        }

                        TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_DO_TAKE_PED_TO_CAR"), 7500);
                        this.tutorialState = ETutorialState.IsGrabbing;
                    }
                    break;

                case ETutorialState.IsGrabbing:
                    if (this.copCar.Exists())
                    {
                        if (this.testPed.IsInVehicle(copCar))
                        {
                            if (this.copCar.Blip != null && this.copCar.Blip.Exists())
                            {
                                this.copCar.Blip.Delete();
                            }

                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_PED_TAKEN_TO_CAR"));
                            DelayedCaller.Call(this.IsSeatedCallback, 10000);
                            this.tutorialState = ETutorialState.IsCuffed;
                        }
                    }
                    break;

                case ETutorialState.PulloverVehicle:
                    // If pulling over, change state
                    if (CPlayer<LCPDFRPlayer>.LocalPlayer.IsPullingOver)
                    {
                        this.tutorialState = ETutorialState.PullingOver;
                    }

                    break;

                case ETutorialState.PullingOver:

                    if (this.observingCop.Exists())
                    {
                        if (this.observingCop.Position.DistanceTo(new Vector3(-883.4521f, 1339.972f, 22.09773f)) < 1.75f)
                        {
                            if (!Natives.IsCharFacingChar(this.observingCop, CPlayer.LocalPlayer.Ped))
                            {
                                this.observingCop.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                            }
                        }
                    }

                    if (!CPlayer<LCPDFRPlayer>.LocalPlayer.IsPullingOver)
                    {
                        // No longer pulling over, end
                        CPlayer.LocalPlayer.CanControlCharacter = false;

                        if (this.testPed.Exists() && this.pulloverVehicle.Exists())
                        {
                            this.testPed.Task.ClearAll();
                            this.testPed.Task.DriveTo(new Vector3(-879.6153f, 1316.808f, 21.6944f), 2.5f, true);
                        }

                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_READY"));
                        DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_CALLOUTS")); }, 5000);
                        DelayedCaller.Call(this.PulloverEndCallback, 15000);
                        this.tutorialState = ETutorialState.None;
                    }

                    break;
            }

            base.Process();
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            DeleteScene();

            // Reset callouts
            Main.CalloutManager.AllowRandomCallouts = Settings.CalloutsEnabled;

            // Reset scenarios
            AmbientScenarioManager ambientScenarioManager = LCPDFR.Main.ScriptManager.GetRunningScriptInstances("AmbientScenarioManager")[0] as AmbientScenarioManager;
            if (ambientScenarioManager != null) ambientScenarioManager.AllowAmbientScenarios = true;

            // Remove tutorial car
            CPlayer<LCPDFRPlayer>.LocalPlayer.TutorialCar = null;

            // Reset time lock
            World.UnlockDayTime();
        }

        /// <summary>
        /// Deletes all peds and vehicles
        /// </summary>
        private void DeleteScene()
        {
            if (this.pulloverVehicle != null && this.pulloverVehicle.Exists())
            {
                this.pulloverVehicle.Delete();
            }

            if (this.policeVehicle != null && this.policeVehicle.Exists())
            {
                this.policeVehicle.Delete();
            }

            if (this.talkingCop1 != null && this.talkingCop1.Exists())
            {
                this.talkingCop1.Delete();
            }

            if (this.talkingCop2 != null && this.talkingCop2.Exists())
            {
                this.talkingCop2.Delete();
            }

            if (this.observingCop != null && this.observingCop.Exists())
            {
                this.observingCop.Delete();
            }

            if (this.copCar != null && this.copCar.Exists())
            {
                this.copCar.Delete();
            }

            if (this.trunkCop != null && this.trunkCop.Exists())
            {
                this.trunkCop.Delete();
            }

            if (this.coffee != null && this.coffee.Exists())
            {
                this.coffee.Delete();
            }

            if (this.testPed != null && this.testPed.Exists())
            {
                this.testPed.Delete();
            }
        }

        /// <summary>
        /// Setups everything.
        /// </summary>
        private void Setup(bool firstRun=true)
        {
            if (firstRun)
            {
                // Fade screen out
                Game.FadeScreenOut(3000, true);

                // Preload scene
                World.LoadEnvironmentNow(this.startPositon);

                // Store positon and vehicle, also make it required for mission so it won't get deleted
                this.oldPosition = CPlayer.LocalPlayer.Ped.Position;
                if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                {
                    this.oldVehicle = CPlayer.LocalPlayer.Ped.CurrentVehicle;
                    this.oldVehicle.IsRequiredForMission = true;
                    CPlayer.LocalPlayer.Ped.WarpFromCar(this.startPositon);
                }
                else
                {
                    CPlayer.LocalPlayer.Ped.SetPositionDontWarpGang(this.startPositon);
                }
            }
            else
            {
                // If not the first run, just teleport them.
                if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                {
                    CPlayer.LocalPlayer.Ped.WarpFromCar(this.startPositon);
                }
                else
                {
                    CPlayer.LocalPlayer.Ped.SetPositionDontWarpGang(this.startPositon);
                }
            }

            friskStep2Shown = false;

            CPlayer.LocalPlayer.Ped.Heading = 90f;
            CPlayer.LocalPlayer.CanControlCharacter = false;

            // Force no weapon
            CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
            CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = true;

            // Prepare area
            AreaHelper.ClearArea(this.startPositon, 100f, true, true);
            World.LockDayTime(11, 00);
            World.Weather = Weather.Sunny;

            // Block callouts
            Main.CalloutManager.AllowRandomCallouts = false;

            // Block scenarios
            AmbientScenarioManager ambientScenarioManager = LCPDFR.Main.ScriptManager.GetRunningScriptInstances("AmbientScenarioManager")[0] as AmbientScenarioManager;
            if (ambientScenarioManager != null) ambientScenarioManager.AllowAmbientScenarios = false;

            // Create scene
            // [DEBUG - 8:31:42 PM] [Plugin.Main] ( POLICE) new Vector3 (-861.7153, 1313.952, 21.59198) Heading: 63.43775 // car1
            // [DEBUG - 8:32:27 PM] [Plugin.Main] ( M_Y_STROOPER) new Vector3 (-858.7452, 1312.511, 21.97742) Heading: 65.48777 // copAtCar1Boot
            // [DEBUG - 8:33:18 PM] [Plugin.Main] ( M_Y_STROOPER) new Vector3 (-862.7002, 1316.143, 21.97742) Heading: 293.8606 // cop2FacingCop3
            // [DEBUG - 8:33:36 PM] [Plugin.Main] ( M_Y_STROOPER) new Vector3 (-861.4683, 1316.156, 21.97742) Heading: 41.11607 // cop3FacingCop2
            // [Plugin.Main] ( M_Y_STROOPER) new Vector3 (-852.4323, 1321.693, 21.97742) Heading: 203.7451 // ObserverCopPos

            // Preload models
            this.ContentManager.PreloadModel("M_Y_STREET_03", true);
            this.ContentManager.PreloadModel("M_M_FATCOP_01", true);
            this.ContentManager.PreloadModel("amb_coffee", true);
            this.ContentManager.PreloadModel("POLICE", true);
            this.ContentManager.PreloadModel("M_Y_STROOPER", true);

            this.testPed = new CPed("M_Y_STREET_03", new Vector3(-858.0467f, 1315.786f, 22.00173f), EPedGroup.Testing);
            if (this.testPed != null && this.testPed.Exists())
            {
                this.testPed.BecomeMissionCharacter();
                this.testPed.Heading = 270;
                this.testPed.PedData.AlwaysSurrender = true;
                this.testPed.PedData.ComplianceChance = 100;
                this.testPed.BlockPermanentEvents = true;
                this.testPed.PedData.Luggage = PedData.EPedLuggage.StolenCards;
                this.ContentManager.AddPed(this.testPed);

                this.observingCop = new CPed("M_M_FATCOP_01", this.testPed.GetOffsetPosition(new Vector3(0.25f, 2f, 0f)), EPedGroup.MissionPed);

                if (this.observingCop != null && this.observingCop.Exists())
                {
                    this.observingCop.BecomeMissionCharacter();
                    this.observingCop.FixCopClothing();
                    this.observingCop.RequestOwnership(this);
                    this.observingCop.Skin.Component.UpperBody.ChangeIfValid(0, 0);
                    this.observingCop.Skin.Component.LowerBody.ChangeIfValid(0, 0);
                    this.observingCop.Skin.Component.Head.ChangeIfValid(0, 0);
                    this.observingCop.Voice = "M_M_FATCOP_01_WHITE";
                    this.observingCop.PutHatOn(true);
                    this.observingCop.Task.TurnTo(this.testPed);
                    this.ContentManager.AddPed(this.observingCop);

                    this.coffee = World.CreateObject("amb_coffee", this.observingCop.GetOffsetPosition(new Vector3(0f, 0f, 5f)));

                    if (this.coffee.Exists())
                    {
                        this.coffee.AttachToPed(this.observingCop, Bone.RightHand, Vector3.Zero, Vector3.Zero);
                        this.observingCop.Animation.Play(new AnimationSet("amb@coffee_idle_m"), "drink_a", 4.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                    }
                }
            }

            this.copCar = new CVehicle("POLICE", new Vector3(-861.7153f, 1313.952f, 21.59198f), EVehicleGroup.Police);

            if (this.copCar != null && this.copCar.Exists())
            {
                this.copCar.Heading = 64.44f;
                this.copCar.Door(VehicleDoor.Trunk).Open();

                // Allow the player to use this for the grabbing script.
                CPlayer<LCPDFRPlayer>.LocalPlayer.TutorialCar = this.copCar;
                
                this.ContentManager.AddVehicle(this.copCar);
            }

            this.trunkCop = new CPed("M_Y_STROOPER", new Vector3(-858.7452f, 1312.511f, 21.97742f), EPedGroup.MissionPed);

            if (this.trunkCop != null && this.trunkCop.Exists())
            {
                this.trunkCop.Heading = 64.44f;
                this.trunkCop.Animation.Play(new AnimationSet("amb@default"), "boot_default", 1.0f, AnimationFlags.Unknown05);
                this.trunkCop.FixCopClothing();
                if (Common.GetRandomBool(0, 2, 1)) this.trunkCop.PutHatOn(true);
                this.ContentManager.AddPed(this.trunkCop);
            }

            this.talkingCop1 = new CPed("M_Y_STROOPER", new Vector3(-862.7002f, 1316.143f, 21.97742f), EPedGroup.MissionPed);

            if (this.talkingCop1 != null && this.talkingCop1.Exists())
            {
                this.talkingCop1.Heading = 315.8606f;
                this.talkingCop1.Animation.Play(new AnimationSet("amb@nightclub_ext"), "street_chat_a", 1.0f, AnimationFlags.Unknown05);
                this.talkingCop1.FixCopClothing();
                if (Common.GetRandomBool(0, 2, 1)) this.talkingCop1.PutHatOn(true);
                this.ContentManager.AddPed(this.talkingCop1);
            }

            this.talkingCop2 = new CPed("M_Y_STROOPER", new Vector3(-861.4683f, 1316.156f, 21.97742f), EPedGroup.MissionPed);

            if (this.talkingCop2 != null && this.talkingCop2.Exists())
            {
                this.talkingCop2.Heading = 45.11607f;
                this.talkingCop2.Animation.Play(new AnimationSet("amb@nightclub_ext"), "street_chat_b", 1.0f, AnimationFlags.Unknown05);
                this.talkingCop2.FixCopClothing();
                if (Common.GetRandomBool(0, 2, 1)) this.talkingCop2.PutHatOn(true);
                this.ContentManager.AddPed(this.talkingCop2);
            }

            if (this.talkingCop1.Exists()) CameraHelper.FocusGameCamOnPed(this.talkingCop1, true, 3000, 10000);
            Game.WaitInCurrentScript(250);
            Game.FadeScreenIn(3000, true);
            tutorialRestarting = false;

            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_WELCOME"));
            DelayedCaller.Call(this.PullOutWeaponCallback, 10000);
        }

        /// <summary>
        /// Finishes the tutorial.
        /// </summary>
        private void Finish()
        {
            // Preload scene
            World.LoadEnvironmentNow(this.oldPosition);

            // Warp player back into vehicle or to position
            if (this.oldVehicle != null && this.oldVehicle.Exists())
            {
                CPlayer.LocalPlayer.Ped.WarpIntoVehicle(this.oldVehicle, VehicleSeat.Driver);
                this.oldVehicle.IsRequiredForMission = false;
                new EventPlayerWarped(this.oldVehicle.Position);
            }
            else
            {
                CPlayer.LocalPlayer.Ped.SetPositionDontWarpGang(this.oldPosition);
                new EventPlayerWarped(this.oldPosition);
            }

            // Cleanup
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();
            CPlayer.LocalPlayer.CanControlCharacter = true;
            this.End();
            Game.FadeScreenIn(3000, true);
        }

        /// <summary>
        /// The pull out weapon callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void PullOutWeaponCallback(object[] parameter)
        {
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();
            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = false;
            TextHelper.ClearHelpbox();
            TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_PISTOL"), 5000);
            this.tutorialState = ETutorialState.PullOutWeapon;

            if (this.observingCop != null && this.observingCop.Exists())
            {
                this.observingCop.Task.ClearAll();
                this.observingCop.Task.GoTo(new Vector3(-852.4323f, 1321.693f, 21.97742f), EPedMoveState.Walk);

                if (this.coffee.Exists())
                {
                    this.observingCop.Animation.Play(new AnimationSet("amb@coffee_hold"), "hold_coffee", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                }
            }
        }

        /// <summary>
        /// The has weapon callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void HasWeaponCallback(object[] parameter)
        {
            CPlayer.LocalPlayer.CanControlCharacter = true;

            // Spawn ped
            if (this.testPed != null && this.testPed.Exists())
            {
                this.testPed.Task.GoTo(this.testPed.GetOffsetPosition(new Vector3(0, 10, 0)));
                this.testPed.AttachBlip();
            }

            TextHelper.ClearHelpbox();
            TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_DO_STOP"), 5000);
            this.tutorialState = ETutorialState.StopPed;
        }

        /// <summary>
        /// The stopped ped callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void StoppedPedCallback(object[] parameter)
        {
            CPlayer.LocalPlayer.CanControlCharacter = true;
            TextHelper.ClearHelpbox();
            TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_DO_FRISK_STEP_1"), 10000);
        }

        /// <summary>
        /// The ped is cuffed callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void IsCuffedCallback(object[] parameter)
        {
            CPlayer.LocalPlayer.CanControlCharacter = true;

            if (this.testPed != null && this.testPed.Exists())
            {
                // this.testPed.Task.TurnTo(CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0f, 10f, 0f)));
                this.testPed.Task.ClearAll();
                this.testPed.Task.StandStill(int.MaxValue);
                this.testPed.Wanted.IsCuffed = true;
                this.testPed.Wanted.IsBeingArrestedByPlayer = true;
            }

            TextHelper.ClearHelpbox();
            TextHelper.PrintText(CultureHelper.GetText("TUTORIAL_DO_GRAB"), 5000);
            this.tutorialState = ETutorialState.GrabPed;
        }

        /// <summary>
        /// The ped is in the back of the cop car callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void IsSeatedCallback(object[] parameter)
        {
            this.testPed.Intelligence.TaskManager.ClearTasks();

            // [DEBUG - 9:24:41 PM] [Plugin.Main] ( POLICE) new Vector3 (-926.0281, 1330.097, 24.09121) Heading: 269.0994 // PlayerCarPos
            // [DEBUG - 9:24:58 PM] [Plugin.Main] ( POLICE) new Vector3 (-913.3858, 1329.936, 23.77521) Heading: 269.6431 // SuspectCarPos

            // Spawn vehicles for pullover
            Game.FadeScreenOut(3000, true);
            this.policeVehicle = new CVehicle("POLICE2", new Vector3(-926.0281f, 1330.097f, 24.09121f), EVehicleGroup.Police);
            if (this.policeVehicle.Exists())
            {
                this.policeVehicle.Heading = 270;
                this.ContentManager.AddVehicle(this.policeVehicle);
            }

            this.pulloverVehicle = new CVehicle("ADMIRAL", new Vector3(-913.3858f, 1329.936f, 23.77521f), EVehicleGroup.Normal);
            if (this.pulloverVehicle.Exists())
            {
                this.pulloverVehicle.Heading = 270;
                this.pulloverVehicle.AttachBlip();
                this.ContentManager.AddVehicle(this.pulloverVehicle);
            }

            if (this.observingCop.Exists())
            {
                this.observingCop.Task.GoTo(new Vector3(-883.4521f, 1339.972f, 22.09773f), EPedMoveState.Walk); // observingcoppos2

                if (this.coffee.Exists())
                {
                    this.observingCop.Animation.Play(new AnimationSet("amb@coffee_hold"), "hold_coffee", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                }
            }

            if (!this.policeVehicle.Exists())
            {
                // Houston, we have a problem!
                int attempts = 0;
                while (attempts < 10)
                {
                    this.policeVehicle = new CVehicle("POLICE2", new Vector3(-926.0281f, 1330.097f, 24.09121f), EVehicleGroup.Police);
                    if (this.policeVehicle.Exists())
                    {
                        this.policeVehicle.Heading = 270;
                        this.ContentManager.AddVehicle(this.policeVehicle);
                        break;
                    }
                    attempts++;
                }

                if (!this.policeVehicle.Exists())
                {
                    Game.FadeScreenOut(3000, true);
                    this.Finish();
                    return;
                }
            }

            if (!this.pulloverVehicle.Exists())
            {
                // Houston, we have a problem!
                int attempts = 0;
                while (attempts < 10)
                {
                    this.pulloverVehicle = new CVehicle("ADMIRAL", new Vector3(-913.3858f, 1329.936f, 23.77521f), EVehicleGroup.Normal);
                    if (this.pulloverVehicle.Exists())
                    {
                        this.pulloverVehicle.Heading = 270;
                        this.pulloverVehicle.AttachBlip();
                        this.ContentManager.AddVehicle(this.pulloverVehicle);
                    }
                    attempts++;
                }

                if (!this.pulloverVehicle.Exists())
                {
                    this.Finish();
                    return;
                }
            }

            // Warp peds
            CPlayer.LocalPlayer.Ped.WarpIntoVehicle(this.policeVehicle, VehicleSeat.Driver);
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();
            this.testPed.WarpIntoVehicle(this.pulloverVehicle, VehicleSeat.Driver);

            // Ensure ped won't drive away
            this.testPed.Task.ClearAll();
            this.testPed.BecomeMissionCharacter();
            this.testPed.Task.CarTempAction(ECarTempActionType.SlowDownSoftly, 5000);

            TextHelper.ClearHelpbox();
            Game.FadeScreenIn(3000, true);
            this.policeVehicle.Speed = 0.5f;
            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_DO_PULLOVER"));
            this.tutorialState = ETutorialState.PulloverVehicle;

            DelayedCaller.Call(
                delegate
                {
                    // Print again since the police computer helpbox might have fucked it up
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_DO_PULLOVER")); 
                    CPlayer.LocalPlayer.CanControlCharacter = true;
                }, 
                this, 
                5000);
            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("TUTORIAL_SOUND_HORN")); }, this, 12000);
        }

        /// <summary>
        /// The pullover has ended callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void PulloverEndCallback(object[] parameter)
        {
            // Fade screen out
            Game.FadeScreenOut(3000, true);

            // Delete vehicles
            this.pulloverVehicle.Delete();
            this.policeVehicle.Delete();
            this.testPed.Delete();
            this.Finish();
        }

        /// <summary>
        /// The callback to reset to stopping ped.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void ResetToStoppingPedCallback(object[] parameter)
        {
            this.HasWeaponCallback(null);
            Game.FadeScreenIn(3000, true);
        }

        /// <summary>
        /// This restarts the tutorial from the beginning
        /// </summary>
        private void RestartTutorial()
        {
            this.tutorialRestarting = true;
            this.tutorialState = ETutorialState.None;
            DelayedCaller.Call(delegate
            {
                Game.FadeScreenOut(3000, true);
                DeleteScene();
                this.Setup(false);
            }, 3000);
        }
    }
}