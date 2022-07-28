namespace LCPD_First_Response.LCPDFR
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Callouts;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts;
    using LCPD_First_Response.LCPDFR.Scripts.Partners;
    using LCPD_First_Response.LCPDFR.Scripts.PoliceComputer;
    using LCPD_First_Response.LCPDFR.Scripts.Scenarios;

    using TaskSequence = GTA.TaskSequence;
    using Timer = LCPD_First_Response.Engine.Timers.Timer;

    /// <summary>
    /// Entry point of the LCPDFR mod
    /// </summary>
    [PluginInfo("Main", true, true)]
    internal class Main : Plugin
    {
        /// <summary>
        /// Whether player can be wanted.
        /// </summary>
        private bool allowWantedLevel;

        /// <summary>
        /// Whether LCPDFR has been started.
        /// </summary>
        private bool isStarted;

        /// <summary>
        /// Whether the ped information are shown.
        /// </summary>
        private bool showPedDevInformation;

        /// <summary>
        /// The timer to check for first start.
        /// </summary>
        private Timer firstStartTimer;

        /// <summary>
        /// The main networking class.
        /// </summary>
        private Networking.Networking networking;

        /// <summary>
        /// Whether user requested to load a session.
        /// </summary>
        private bool userRequestedLoad;

        /// <summary>
        /// Whether user requested to save a session.
        /// </summary>
        private bool userRequestedSave;

        /// <summary>
        /// Finalizes an instance of the <see cref="Main"/> class. 
        /// </summary>
        ~Main()
        {
            // Peform garbage collection
            ContentManager.DefaultContentManager.ReleaseAll();
        }

        /// <summary>
        /// Gets the instance of <see cref="Main"/>.
        /// </summary>
        public static Main Instance { get; private set; }

        /// <summary>
        /// Gets the arrest and frisk manager.
        /// </summary>
        public static AimingManager AimingManager { get; private set; }

        /// <summary>
        /// Gets the backup manager.
        /// </summary>
        public static BackupManager BackupManager { get; private set; }

        /// <summary>
        /// Gets the callout manager.
        /// </summary>
        public static CalloutManager CalloutManager { get; private set; }

        /// <summary>
        /// Gets the go on duty script.
        /// </summary>
        public static GoOnDuty GoOnDutyScript { get; private set; }

        /// <summary>
        /// Gets the partner script.
        /// </summary>
        public static PartnerManager PartnerManager { get; private set; }

        /// <summary>
        /// Gets the police computer.
        /// </summary>
        public static PoliceComputer PoliceComputer { get; private set; }

        /// <summary>
        /// Gets the police department manager.
        /// </summary>
        public static PoliceDepartmentManager PoliceDepartmentManager { get; private set; }

        /// <summary>
        /// Gets the pullover manager.
        /// </summary>
        public static PulloverManager PulloverManager { get; private set; }

        /// <summary>
        /// Gets the script manager.
        /// </summary>
        public static ScriptManager ScriptManager { get; private set; }

        /// <summary>
        /// Gets the text wall.
        /// </summary>
        public static TextWallFormHandler TextWall { get; private set; }

        /// <summary>
        /// Gets the quick action menu.
        /// </summary>
        public static QuickActionMenuManager QuickActionMenu { get; private set; }

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public static string Version
        {
            get
            {
                string s = "LCPDFR 1.1 2011-2015 LMS";
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                s += " (" + v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision + ") ";
                var buildDateTime = new DateTime(2000, 1, 1).Add(new TimeSpan(TimeSpan.TicksPerDay * v.Build + TimeSpan.TicksPerSecond * 2 * v.Revision));
                s += buildDateTime.ToShortDateString() + " " + buildDateTime.ToLongTimeString();

                if (Engine.Main.DEBUG_MODE)
                {
                    s += " (DEV MODE)";
                }

                return s;
            }
        }

        /// <summary>
        /// Called when the plugin has been created successfully.
        /// </summary>
        public override void Initialize()
        {
            Instance = this;

            // Attach exception handler
            ExceptionHandler.SetGeneralExceptionHandler(this.ExceptionCaught);
            this.RegisterConsoleCommands();

            Log.Debug("Setting language to en-US", this);
            CultureHelper.SetLanguage("en-US");

            // Create scriptmanager for lcpdfr scripts and look in this assembly for scripts
            ScriptManager = new ScriptManager();
            ScriptManager.LookForScriptsInCurrentAssembly();

            // This script is required to run all the time.
            PoliceDepartmentManager = ScriptManager.StartScript<PoliceDepartmentManager>("PoliceDepartmentManager");
            PoliceDepartmentManager.UpdatePDBlips = false;
            PoliceDepartmentManager.ShowAllPDs = false;

            // Create important LCPDFR objects
            Settings.Initialize();
            KeyHandler.Initialize();
            PoliceScanner.Initialize();
            Stats.Initialize();
            this.allowWantedLevel = true;

            this.networking = new Networking.Networking();

            // Read language settings
            string language = Settings.Language;
            Log.Info("Changing language to " + language, this);
            CultureHelper.SetLanguage(language);

            Log.Debug("Initialized", this);

            Log.Debug("Checking for an old session to resume", this);
            if (File.Exists("oldsession.fr"))
            {
                Log.Info("Old session found, resuming...", this);
                Savegame.LoadGameFromSaveFile("oldsession.fr");
                File.Delete("oldsession.fr");
            }
        }

        private unsafe void SetAllowPlayerShooting(bool allow)
        {
            IntPtr baseAddress = new IntPtr(System.Diagnostics.Process.GetCurrentProcess().MainModule.BaseAddress.ToInt32() - 0x400000);
            byte* jmpPatch = (byte*)(baseAddress + 0x00A8F33E);
            byte* inputValPatch = (byte*)(baseAddress + 0x00A8EE5B + 0x4);

            if (allow)
            {
                *jmpPatch = 0x74;
                *inputValPatch = 0x0;
            }
            else
            {
                *jmpPatch = 0xEB;
                *inputValPatch = 0x1;
            }
        }

        public void DrawLight(int a1, int a2, int a3, Vector3 a4, Vector3 a5, Vector3 a6, Vector3 a7, float a8, float a9, int a10, float range, float a12, float diffusion, float intensity, float a15, int a16, int a17, int a18)
        {
            AdvancedHookManaged.ManagedVector3 vector1 = new AdvancedHookManaged.ManagedVector3 { X = a4.X, Y = a4.Y, Z = a4.Z };
            AdvancedHookManaged.ManagedVector3 vector2 = new AdvancedHookManaged.ManagedVector3 { X = a5.X, Y = a5.Y, Z = a5.Z };
            AdvancedHookManaged.ManagedVector3 vector3 = new AdvancedHookManaged.ManagedVector3 { X = a6.X, Y = a6.Y, Z = a6.Z };
            AdvancedHookManaged.ManagedVector3 vector4 = new AdvancedHookManaged.ManagedVector3 { X = a7.X, Y = a7.Y, Z = a7.Z };
            AdvancedHookManaged.AGame.DrawLight(a1, a2, a3, vector1, vector2, vector3, vector4, a8, a9, a10, range, a12, diffusion, intensity, a15, a16, a17, a18);
        }

        /// <summary>
        /// Called every tick to process all plugin logic.
        /// </summary>
        public override void Process()
        {
            // Process all classes
            ScriptManager.Process();
            this.ProcessStart();

            // Reset wanted level
            if (!this.allowWantedLevel)
            {
                Game.WantedMultiplier = 0.0f;
                Game.LocalPlayer.WantedLevel = 0;
            }

            if (Globals.IsOnDuty)
            {
                CalloutManager.Process();
                TextWall.Process();

                if (KeyHandler.IsKeyboardKeyDown(Keys.B))
                {
                    //if (!CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsTaskActive(ETaskID.Flashlight))
                    //{
                    //    TaskFlashlight taskFlashlight = new TaskFlashlight(false, Bone.RightHand, 0.5f, 25.0f, true);
                    //    taskFlashlight.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.SubTask);
                    //}

                    //Vector3 vector1 = Game.CurrentCamera.Direction;
                    //Vector3 vector3 = CPlayer.LocalPlayer.Ped.GetBonePosition(Bone.RightHand);
                    //Vector3 vector4 = new Vector3 { X = 1.0f, Y = 1.0f, Z = 1.0f };
                    //LightHelper.DrawLightCone(vector3, vector1, vector4, 25.0f, 10.0f, 20.0f);

                    //Vector3 vector1_n = Game.CurrentCamera.Direction;
                    //vector1_n.Normalize();
                    //vector3 = vector3 + (vector1_n * 0.5f);
                    //vector4 = new Vector3(1.0f, 1.0f, 1.0f);
                    //LightHelper.DrawLight(vector3, vector1, vector4, 30.92f, 20.0f, 25.0f, true);

                    //PartnerManager.ExecuteTask(EPartnerGroup.PrimaryGroup, ped => ped.Task.Jump(EJumpType.Front));

                    //this.SetAllowPlayerShooting(true);
                    //GTA.Native.Function.Call("FAKE_DEATHARREST");
                    //CPlayer.LocalPlayer.Ped.Delete();
                }
                if (KeyHandler.IsKeyboardKeyDown(Keys.N))
                {
                    //PartnerManager.MoveToPosition(EPartnerGroup.SecondaryGroup, CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0, 10, 0)));
                    //this.SetAllowPlayerShooting(false);
                    //GTA.Native.Function.Call("FAKE_DEATHARREST");
                    //CPlayer.LocalPlayer.Ped.Delete();
                }
            }

            if (this.showPedDevInformation)
            {
                CPed pedDebugging = CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed();

                if (pedDebugging == null)
                {
                    pedDebugging = CPlayer.LocalPlayer.Ped;
                }

                if (pedDebugging != null && pedDebugging.Exists())
                {
                    // Collect data
                    StringBuilder sb = new StringBuilder();

                    int appereancesInPool = Engine.Pools.PedPool.GetAll().Where(ped => ped.Handle == pedDebugging.Handle).ToArray().Length;
                    sb.Append("Handle: " + pedDebugging.Handle + " [" + (((GTA.Ped)pedDebugging).MemoryAddress.ToString("X")) + "] (" + appereancesInPool + ")");
                    sb.Append("~n~Group: " + pedDebugging.PedGroup);
                    sb.Append("~n~SubGroup: " + pedDebugging.PedSubGroup);
                    sb.Append("~n~Available: " + pedDebugging.PedData.Available);
                    sb.Append("~n~Required for mission: " + pedDebugging.IsRequiredForMission);

                    // If cop
                    if (pedDebugging.PedGroup == EPedGroup.Cop)
                    {
                        var data = pedDebugging.GetPedData<PedDataCop>();
                        sb.Append("~n~CopState: " + data.CopState);
                    }

                    sb.Append("~n~ActionPriority: " + pedDebugging.Intelligence.CurrentActionPriority);

                    if (pedDebugging.Intelligence.PedController != null)
                    {
                        sb.Append("~n~Controller: " + pedDebugging.Intelligence.PedController);
                    }

                    if (pedDebugging.HasOwner)
                    {
                        sb.Append("~n~Owner: " + pedDebugging.Owner);
                    }

                    string debug = pedDebugging.Debug ?? string.Empty;
                    sb.Append("~n~Debug: " + debug);

                    string sTasks = "";
                    ETaskID[] tasks = pedDebugging.Intelligence.TaskManager.GetActiveTaskIDs();
                    foreach (ETaskID eTaskID in tasks)
                    {
                        sTasks += eTaskID + " ~n~";
                    }
                    sb.Append("~n~Tasks (" + tasks.Length + "): ~n~" + sTasks);

                    string sInternalTasks = "";
                    EInternalTaskID[] internalTasks = pedDebugging.Intelligence.TaskManager.GetActiveInternalTasks();
                    foreach (EInternalTaskID eInternalTaskID in internalTasks)
                    {
                        sInternalTasks += eInternalTaskID + " ~n~ ";
                    }
                    sb.Append("~n~Internal Tasks(" + internalTasks.Length.ToString() + "): ~n~" + sInternalTasks);
                    TextHelper.PrintFormattedHelpBox(sb.ToString());
                }
            }

            if (this.userRequestedLoad)
            {
                Savegame.LoadGameFromSaveFile();
                this.userRequestedLoad = false;
            }

            if (this.userRequestedSave)
            {
                Savegame.SaveCurrentGameToFile();
                this.userRequestedSave = false;
            }

            // ADD TASER FOR PARTNER ON FOOT CHASE

            // MENU: HOLD DOWN KEY: MENU APPEARS 1, 2, 3 - select option
        }

        /// <summary>
        /// Called when the plugin is being disposed, e.g. because an unhandled exception occured in Process. Free all resources here!
        /// </summary>
        public override void Finally()
        {
            Log.Debug("Finally", this);
        }

        /// <summary>
        /// Starts core scripts that run even when not on duty.
        /// </summary>
        public void PerformInitialLCPDFRStartUp()
        {
            if (this.isStarted)
            {
               return;
            }

            GoOnDutyScript = ScriptManager.StartScript<GoOnDuty>("GoOnDuty");
            GoOnDutyScript.PlayerWentOnDuty += new Action(this.GoOnDutyScript_PlayerWentOnDuty);
            GoOnDutyScript.PlayerWentOffDuty += new Action(this.GoOnDutyScript_PlayerWentOffDuty);
            GoOnDutyScript.PedModelSelectionFinished += new Action(this.GoOnDutyScript_PedModelSelectionFinished);
            PoliceDepartmentManager.ShowAllPDs = true;
            PoliceDepartmentManager.UpdatePDBlips = true;
            PoliceDepartmentManager.UpdateBlips();

            Game.Console.Print("// LCPD FIRST RESPONSE");
            Game.Console.Print("// " + Version);
            Game.Console.Print("// " + CultureHelper.GetText("LCPDMAIN_DEVELOPED_BY"));
            Game.Console.Print("// " + CultureHelper.GetText("LCPDMAIN_SCRIPT_CONTRIBUTIONS"));
            Game.Console.Print("// " + CultureHelper.GetText("LCPDMAIN_SPECIAL_THANKS"));
            Game.Console.Print("// © Copyright 2010-2015, G17 Media");
            Game.Console.Print("---------------------------------------------------------------------");
            //Game.Console.Print("// " + string.Format(CultureHelper.GetText("LCPDMAIN_ELS"), Global.ELSInstalled));
            Game.Console.Print("// " + string.Format(CultureHelper.GetText("LCPDMAIN_HARDMODE"), Settings.HardcoreEnabled));
            Game.Console.Print(CultureHelper.GetText("LCPDMAIN_TRANSLATOR_THANKS"));

            Log.Info("Running LCPD First Response " + Version, this);
            Log.Info("Copyright © 2010-2015, G17 Media, www.lcpdfr.com", this);

            // Preload models if told so
            if (Settings.PreloadAllModels)
            {
                Log.Info("Initialize: Model preloading is activated. This may cause texture loss or less vehicle models spawning", this);

                // Preload cop peds and vehicles
                CModelInfo[] modelInfos = Engine.Main.ModelManager.GetModelInfosByFlags(EModelFlags.IsCop);
                foreach (CModelInfo modelInfo in modelInfos)
                {
                    Log.Debug("Initialize: Preloading " + modelInfo.Name, this);
                    ContentManager.DefaultContentManager.PreloadModel(new CModel(modelInfo));
                }

                modelInfos = Engine.Main.ModelManager.GetModelInfosByFlags(EModelFlags.IsCopCar);
                foreach (CModelInfo modelInfo in modelInfos)
                {
                    Log.Debug("Initialize: Preloading (vehicle) " + modelInfo.Name, this);
                    if (Engine.Main.IsTbogt && (modelInfo.Name == "POLICE3" || modelInfo.Name == "POLICE4" || modelInfo.Name == "POLICEB"))
                    {
                        Log.Debug("Initialize: Skipping model: " + modelInfo.Name, this);
                        continue;
                    }

                    ContentManager.DefaultContentManager.PreloadModel(new CModel(modelInfo));
                }
            }

            UpdateCheck doUpdateCheck = new UpdateCheck();
            doUpdateCheck.Initalize();

            this.isStarted = true;
            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("LCPDFR_STARTED"));
        }

        public void SetUserIssuedSave()
        {
            this.userRequestedSave = true;
        }

        public void SetUserIssuedLoad()
        {
            this.userRequestedLoad = true;
        }

        /// <summary>
        /// Processes the start of LCPDFR, that is checking for the start key combo.
        /// </summary>
        private void ProcessStart()
        {
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.Start))
            {
                if (!this.isStarted)
                {
                    this.PerformInitialLCPDFRStartUp();
                }
                else if (!PoliceDepartmentManager.IsPlayerInPoliceDepartment)
                {
                    // TODO: Shutting down this way is not recommend, since there might be still Timers or DelayedCaller instances running
                    // Either delete them or remove this way of stopping in official release

                    if (Globals.IsOnDuty)
                    {
                        GoOnDutyScript.GoOffDuty(true);

                        // Free textwall
                        TextWall.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Stops LCPDFR.
        /// </summary>
        private void StopLCPDFR()
        {
            // Stop go on duty script
            GoOnDutyScript.PlayerWentOnDuty -= new Action(this.GoOnDutyScript_PlayerWentOnDuty);
            GoOnDutyScript.PlayerWentOffDuty -= new Action(this.GoOnDutyScript_PlayerWentOffDuty);
            ScriptManager.StopScript(GoOnDutyScript);
            PoliceDepartmentManager.ShowAllPDs = false;
            PoliceDepartmentManager.UpdatePDBlips = false;

            // Restore wanted level
            this.allowWantedLevel = true;
            Game.WantedMultiplier = 1.0f;

            this.isStarted = false;
            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("LCPDFR_STOPPED"));
        }

        /// <summary>
        /// Called when the player went on duty. Start all scripts that should run on duty.
        /// </summary>
        private void GoOnDutyScript_PlayerWentOnDuty()
        {
            TextWall = new TextWallFormHandler();
            CalloutManager = ScriptManager.StartScript<CalloutManager>("CalloutManager");
            BackupManager = ScriptManager.StartScript<BackupManager>("BackupManager");
            AimingManager = ScriptManager.StartScript<AimingManager>("AimingManager");
            PulloverManager = ScriptManager.StartScript<PulloverManager>("PulloverManager");
            PoliceComputer = ScriptManager.StartScript<PoliceComputer>("PoliceComputer");
            QuickActionMenu = ScriptManager.StartScript<QuickActionMenuManager>("QuickActionMenu");
            ScriptManager.StartScript<KeyBindings>("KeyBindings");
            ScriptManager.StartScript<Ambient>("Ambient");
            ScriptManager.StartScript<ParkingTickets>("ParkingTickets");
            ScriptManager.StartScript<Taser>("Taser");
            ScriptManager.StartScript<FastWalk>("FastWalk");
            ScriptManager.StartScript<ReportEvents>("ReportEvents");
            ScriptManager.StartScript<AmbientScenarioManager>("AmbientScenarioManager");
            ScriptManager.StartScript<LicenseNumberScanner>("LicenseNumberScanner");
            ScriptManager.StartScript<Hardcore>("Hardcore");
            ScriptManager.StartScript<Lights>("Lights");
            ScriptManager.StartScript<AimMarker>("AimMarker");
            ScriptManager.StartScript<EasterEggs>("EE");
            ScriptManager.StartScript<VehicleTrunk>("VehicleTrunk");

            ScriptManager.StartScript<Grab>("Grab");
            ScriptManager.StartScript<CheckpointControl>("CheckpointControl");

            // Initialize static stuff
            AreaBlocker.Initialize();
            this.allowWantedLevel = false;
            API.Functions.InitializeLCPDFRSpecific();
            API.Functions.InvokeOnDutyStateChanged(true);

            // Lower cop density a little.
            PopulationManager.SetRandomCopsDensity(0.8f);

            // Show first start help
            if (Globals.IsFirstStart)
            {
                Action action = delegate
                {
                    TextHelper.PrintFormattedHelpBox("If this is your first time playing LCPDFR, you can press ~KEY_TUTORIAL_START~ to begin a short tutorial.  You can also do this at any time inside a police station.");
                    this.firstStartTimer = new Timer(1, this.StartTutorialCallback, DateTime.Now);
                    this.firstStartTimer.Start();

                    // Update status
                    Globals.IsFirstStart = false;
                };

                DelayedCaller.Call(delegate { action(); }, 2500);
            }
        }

        /// <summary>
        /// Called when the player went off duty. Finish everything in here.
        /// </summary>
        private void GoOnDutyScript_PlayerWentOffDuty()
        {
            this.StopLCPDFR();
            ScriptManager.Shutdown();
            AreaBlocker.FlushBlockedAreas();

            Engine.Main.CopManager.ClearRequests();

            // Free all entities. Note that this should be removed in final release and all scripts should release entities themselves
            Engine.Main.FreeAllEntities();

            // Restart police manager
            PoliceDepartmentManager = ScriptManager.StartScript<PoliceDepartmentManager>("PoliceDepartmentManager");
            PoliceDepartmentManager.UpdatePDBlips = false;
            PoliceDepartmentManager.ShowAllPDs = false;
            API.Functions.InvokeOnDutyStateChanged(false);
        }

        /// <summary>
        /// Called when the player has finished the ped model selection.
        /// </summary>
        private void GoOnDutyScript_PedModelSelectionFinished()
        {
            // Start partner script
            PartnerManager = ScriptManager.StartScript<PartnerManager>("PartnerManager");
        }

        /// <summary>
        /// Called to check whether the user wants a tutorial.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void StartTutorialCallback(object[] parameter)
        {
            // Check if timer is still valid
            DateTime startTime = (DateTime)parameter[0];
            TimeSpan difference = DateTime.Now - startTime;
            if (difference.Seconds > 10)
            {
                // Stop timer
                this.firstStartTimer.Stop();
                TextHelper.ClearHelpbox();
                return;
            }
            
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.TutorialStart))
            {
                // Stop timer
                this.firstStartTimer.Stop();

                // Start tutorial
                Tutorial tutorial = ScriptManager.StartScript<Tutorial>("Tutorial");
            }
        }

        [ConsoleCommand("TogglePedDev")]
        private void TogglePedDevInfo(ParameterCollection parameterCollection)
        {
            this.showPedDevInformation = !this.showPedDevInformation;
            Log.Debug("showPedDevInformation: " + this.showPedDevInformation.ToString(), this);
        }

        [ConsoleCommand("PlayAnimation")]
        private void PlayAnimation(ParameterCollection parameterCollection)
        {
            string animSet = parameterCollection[0];
            string name = parameterCollection[1];
            AnimationSet animationSet = new AnimationSet(animSet);
            CPlayer.LocalPlayer.Ped.Animation.Play(animationSet, name, 1f);
        }

        [ConsoleCommand("Test")]
        private void Test(ParameterCollection parameterCollection)
        {
            Log.Debug("Test: Called", this);
        }

        [ConsoleCommand("StartCallout", false)]
        private void TestCallout(ParameterCollection parameterCollection)
        {
            string name = string.Empty;
            if (parameterCollection.Count > 0)
            {
                name = parameterCollection[0];
            }

            CalloutManager.StartCallout(name);
        }

        [ConsoleCommand("EndCallout", false)]
        private void EndCallout(ParameterCollection parameterCollection)
        {
            CalloutManager.StopCallout();
        }

        [ConsoleCommand("ListScripts")]
        private void ListScripts(ParameterCollection parameterCollection)
        {
            Log.Debug("All running scripts: ", "ListScripts");
            foreach (BaseScript runningScript in ScriptManager.GetAllRunningScripts())
            {
                Log.Debug(runningScript.ScriptInfo.Name, "ListScripts");
            }
        }

        [ConsoleCommand("ListRegisteredScripts")]
        private void ListRegisteredScripts(ParameterCollection parameterCollection)
        {
            Log.Debug("All registered scripts:", "ListRegisteredScripts");
            foreach (Type registeredScript in ScriptManager.GetAllRegisteredScripts())
            {
                Log.Debug(registeredScript.Name, "ListRegisteredScripts");
            }
        }

        [ConsoleCommand("testy", "test command!")]
        private void testy(ParameterCollection parameterCollection)
        {
            float y = 0;
            float z = 0;
            if (parameterCollection.Count > 0)
            {
                y = Convert.ToSingle(parameterCollection[0]);
                z = Convert.ToSingle(parameterCollection[1]);
            }
            CPlayer.LocalPlayer.Ped.ApplyForceRelative(new Vector3(0, y, z), CPlayer.LocalPlayer.Ped.Direction);
        }

        [ConsoleCommand("revive", "test command!")]
        private void revive(ParameterCollection parameterCollection)
        {
            Game.Console.Print("revive command");

            foreach (GTA.Ped ped in GTA.World.GetAllPeds())
            {
                if (ped != Game.LocalPlayer.Character)
                {
                    if (ped.Exists())
                    {
                        ped.Die();
                        ped.Health = 100;
                        GTA.Native.Function.Call("SWITCH_PED_TO_ANIMATED", ped, true);
                        ped.PreventRagdoll = false;
                        ped.Task.ClearAll();
                        GTA.Native.Function.Call("REVIVE_INJURED_PED", ped);
                        ped.ForceRagdoll(1000, true);

                        if (ped.isRequiredForMission)
                        {
                            ped.isRequiredForMission = false;
                            ped.NoLongerNeeded();
                        }

                        ped.Task.WanderAround();
                        Game.Console.Print("reviving");
                    }
                }
            }
        }

        [ConsoleCommand("ReloadSettings", "Reloads all LCPDFR settings.", false)]
        private void ReloadSettings(ParameterCollection parameterCollection)
        {
            Settings.Initialize();
            KeyHandler.Initialize();
        }

        [ConsoleCommand("WriteDefaultSettings", "Writes all default LCPDFR settings into the settings file.")]
        private void WriteDefaultSettings(ParameterCollection parameterCollection)
        {
            Settings.WriteDefaultIniSettings();
            KeyHandler.WriteDefaultIniSettings();
        }

        [ConsoleCommand("SetPerformanceMode", "Sets the performance mode for "
                                              + "LCPDFR which dynamically suspends expensive tasks if performance impact is too big based on the current setting."
                                              + " Valid options are: Never, Optimized, Performance.", false)]
        private void SetSchedulerStyle(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string param = parameterCollection[0].ToLower();
                if (param == "never")
                {
                    ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Never);
                    Game.Console.Print("SetSchedulerStyle: Mode switched to Never. Performance optimizations are disabled.");
                }
                else if (param == "optimized")
                {
                    ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Optimized);
                    Game.Console.Print("SetSchedulerStyle: Mode switched to Optimized. Performance optimizations are partly enabled for expensive tasks.");
                }
                else if (param == "performance")
                {
                    ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Performance);
                    Game.Console.Print("SetSchedulerStyle: Mode switched to Performance. Performance optimizations enabled.");
                }
            }
            else
            {
                Game.Console.Print("SetSchedulerStyle: Invalid number of arguments.");
            }
        }

        [ConsoleCommand("SetLanguage", "Sets the language to the given language code, e.g. de-DE for German.", false)]
        private void SetLanguage(ParameterCollection parameterCollection)
        {
            CultureHelper.SetLanguage(parameterCollection[0]);
        }

        [ConsoleCommand("ForceDuty", "Forces on duty state and skips going to a police department.", false)]
        private void ForceDuty(ParameterCollection parameterCollection)
        {
            if (!Globals.IsOnDuty && this.isStarted)
            {
                GoOnDutyScript.ForceOnDuty();
            }
        }

        [ConsoleCommand("FBI")]
        private void fbi(ParameterCollection parameterCollection)
        {
            CPlayer.LocalPlayer.Ped.RandomizeOutfit();
            if (Game.CurrentEpisode == GameEpisode.TBOGT)
            {
                CPlayer.LocalPlayer.Model = "M_Y_CIADLC_02";
            }
            else
            {
                CPlayer.LocalPlayer.Model = "M_M_FBI";
            }
            GoOnDuty.AdjustPlayerModel();
        }

        [ConsoleCommand("Business")]
        private void Business(ParameterCollection parameterCollection)
        {
            CPlayer.LocalPlayer.Ped.Weapons.AssaultRifle_M4.Ammo = 4000;
            CPlayer.LocalPlayer.Ped.Weapons.BarettaShotgun.Ammo = 4000;
            CPlayer.LocalPlayer.Ped.Weapons[GTA.Weapon.TBOGT_GoldenSMG].Ammo = 4000;
            CPlayer.LocalPlayer.Ped.Weapons.Glock.Ammo = 4000;
            CPlayer.LocalPlayer.Ped.Weapons.RocketLauncher.Ammo = 4000;
            CPlayer.LocalPlayer.Ped.Weapons.BasicSniperRifle.Ammo = 4000;
            CPlayer.LocalPlayer.Ped.MakeProofTo(true, false, false, false, false);
        }

        [ConsoleCommand("ToggleWanted")]
        private void Wanted(ParameterCollection parameterCollection)
        {
            this.allowWantedLevel = !this.allowWantedLevel;

            if (this.allowWantedLevel)
            {
                Game.WantedMultiplier = 1.0f;
                GTA.Game.LocalPlayer.WantedLevel = 0;
            }

            Log.Debug("Wanted: " + this.allowWantedLevel, "Main");
        }

        [ConsoleCommand("SetComponent")]
        private void SetComponent(ParameterCollection parameterCollection)
        {
            int id = Convert.ToInt32(parameterCollection[0]);
            int id2 = Convert.ToInt32(parameterCollection[1]);
            int id3 = Convert.ToInt32(parameterCollection[2]);


            GTA.Native.Function.Call("SET_CHAR_COMPONENT_VARIATION", (GTA.Ped)CPlayer.LocalPlayer.Ped, id, id2, id3);
        }

        [ConsoleCommand("ChasePlayer", true)]
        private void ChasePlayer(ParameterCollection parameterCollection)
        {
            Pursuit p = new Pursuit();
            CPlayer.LocalPlayer.Ped.PedData.Flags = EPedFlags.PlayerDebug;
            p.SetAsCurrentPlayerChase();
            p.AddTarget(CPlayer.LocalPlayer.Ped);
            p.CallIn(AudioHelper.EPursuitCallInReason.Pursuit);
            p.ForceKilling = false;
            p.MaxUnits = 60;
            p.MaxCars = 10;
            p.OnlyAIVisuals = true;
            p.AllowSuspectVehicles = false;

            // TODO: Ped.SetPedWontAttackPlayerWithoutWantedLevel() for all cops!
            // Also fix busting and crashing when chase ends

            CPlayer.LocalPlayer.Ped.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
            CPlayer.LocalPlayer.Ped.RelationshipGroup = RelationshipGroup.Player;
            CPlayer.LocalPlayer.Ped.MakeFriendsWithCops(false);
            CPlayer.LocalPlayer.Model = "M_Y_THIEF";
        }

        [ConsoleCommand("Hash")]
        private void GetHashKey(ParameterCollection parameterCollection)
        {
            string s = parameterCollection[0];
            int id = GTA.Native.Function.Call<int>("GET_HASH_KEY", s);
            GTA.Game.DisplayText(id.ToString("X"));
        }

        [ConsoleCommand("SaveSession", false)]
        private void SaveWorld(ParameterCollection parameterCollection)
        {
            this.SetUserIssuedSave();
        }

        [ConsoleCommand("RestoreSession", false)]
        private void RestoreSession(ParameterCollection parameterCollection)
        {
            this.SetUserIssuedLoad();
        }

        [ConsoleCommand("LastSaved", false)]
        private void GetTimeSinceLastSave(ParameterCollection parameterCollection)
        {
            if (Savegame.LastSave.HasValue)
            {
                TimeSpan diff = DateTime.Now - Savegame.LastSave.Value;
                Log.Info(string.Format("Last save occured {0} seconds ago ({1})", diff.TotalSeconds, Savegame.LastSave.Value.ToLongTimeString()), "Main");
            }
            else
            {
                Log.Info("No save occured yet", "Main");
            }
        }

        /// <summary>
        /// Called when there was an exception caught in the code. At this point the exception has already been logged.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="ex">The exception.</param>
        private void ExceptionCaught(object sender, Exception ex)
        {
            if (sender == this || sender.GetType() == typeof(Engine.Main) || sender.GetType().Assembly == Assembly.GetExecutingAssembly())
            {
                ExceptionHandler.ReportException(
                    "LCPDFR has encountered an error, which has been logged to LCPDFR.log. Please see the logfile for further details and report the issue at LCPDFR.com. This error may lead to unexpected behavior and it is recommended to restart the game. We're sorry for the inconvenience.");
            }
            else
            {
                ExceptionHandler.ReportException(
                 "A third party plugin has been shut down due to an error (" + sender.GetType().Name + "). See log for details.");
            }

            // Log IL offset
            string il = ExceptionHandler.GetStackTraceWithILOffset(ex);

            // Calculate error report hash
            byte[] bytes = Encoding.UTF8.GetBytes(il);
            string hash = BitConverter.ToString(Encryption.HashDataSHA1(bytes)).Replace("-", string.Empty);
            il += "Error hash: " + hash; 

            Log.Error(il, "ExceptionHandler");
        }
    }
}