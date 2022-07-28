namespace LCPD_First_Response.Engine
{
    using System;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using GTA;

    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Networking;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR;
    using LCPD_First_Response.LCPDFR.Scripts;

    using TaskManager = LCPD_First_Response.Engine.Scripting.Tasks.TaskManager;
    using LCPD_First_Response.LCPDFR.GUI;

    [Serializable]
    public class Main
    {
        internal const bool DEBUG_MODE = false;

        /// <summary>
        /// Gets the authentication class.
        /// </summary>
        internal static Authentication Authentication { get; private set; }

        internal static CopManager CopManager { get; private set; }
        internal static KeyWatchDog KeyWatchDog { get; private set; }
        internal static GUI.FormsManager FormsManager { get; private set; }
        internal static ServerCommunication LCPDFRServer { get; private set; }
        internal static ModelManager ModelManager { get; private set; }
        internal static NetworkManager NetworkManager { get; private set; }
        internal static PluginManager PluginManager { get; private set; }
        internal static PoolUpdaterUnmanaged PoolUpdater { get; private set; }

        /// <summary>
        /// Anonymous task manager instance without a valid ped reference. Used for anonymous tasks
        /// </summary>
        internal static TaskManager TaskManager { get; private set; }
        internal static TimerManager TimerManager { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current episode is TBOGT.
        /// </summary>
        internal static bool IsTbogt
        {
            get
            {
                return Game.CurrentEpisode == GameEpisode.TBOGT;
            }
        }

        /// <summary>
        /// Returns true if current game is either a single player game or if the player is the host of a network game
        /// </summary>
        internal static bool IsSinglePlayerOrHost
        {
            get
            {
                if (NetworkManager.IsNetworkSession)
                {
                    return NetworkManager.IsHost;
                }
                return true;
            }
        }

        private bool initialized;
        private Thread mainThread;
        private ManualResetEvent mainLoopStopFlag;
        private ManualResetEvent tickStopFlag;

        private MemoryValidator memoryValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Main"/> class.
        /// </summary>
        public Main()
        {
            // Listen to Tick event from script with interval 10
            //this.Tick += new EventHandler(Main_Tick);
        }

        /// <summary>
        /// Shutdowns and ends all objects that control entities and frees the entities, e.g. all tasks.
        /// </summary>
        internal static void FreeAllEntities()
        {
            // Clear all anonymous tasks
            Log.Debug("FreeAllEntities: Cleaning static task manager...", "Main");
            int i = 0;
            foreach (PedTask activeTask in TaskManager.GetActiveTasks())
            {
                // Don't count already finished scenario (since scenario is aborted, but TaskScenario is not when finishing via content manager)
                if (activeTask.TaskID == ETaskID.Scenario)
                {
                    TaskScenario taskScenario = (TaskScenario)activeTask;
                    if (!taskScenario.IsScenarioActive)
                    {
                        continue;
                    }
                }

                Log.Debug("FreeAllEntities: Task still active: " + activeTask.TaskID, "Main");
                i++;
            }

            Log.Debug("FreeAllEntities: Normally leaked tasks in static task manager: " + i, "Main");
            TaskManager.ClearTasks();

            // Clear all other tasks
            Log.Debug("FreeAllEntities: Cleaning all ped tasks...", "Main");
            i = 0;
            foreach (CPed ped in Pools.PedPool.GetAll())
            {
                if (ped.Exists())
                {
                    foreach (ETaskID activeTask in ped.Intelligence.TaskManager.GetActiveTaskIDs())
                    {
                        Log.Debug("FreeAllEntities: Task still active: " + activeTask, "Main");
                        i++;
                    }
                }

                ped.Intelligence.TaskManager.ClearTasks();
            }

            Log.Debug("FreeAllEntities: Normally leaked tasks in PedIntelligence: " + i, "Main");
        }

        private void Initialize()
        {
            // Attach logger
            Log.TextHasBeenLogged += this.Log_TextHasBeenLogged;

            Log.Info("Main: Initializing...", "Main");
            Log.Info("Main: LCPDFR Engine (C) 2011-2013 LMS", "Main");
            Log.Info("Main: DO NOT USE THIS WITHOUT PERMISSION. YOU MAY NOT SHARE, MODIFY OR DECOMPILE THIS FILE.", "Main");

            // Initialize authentication first before doing anything else, this might take a few seconds because it connects to the internet
            LCPDFRServer = new ServerCommunication();
            Authentication = new Authentication();

            if (!Authentication.CanStart)
            {
                GUI.Gui.PrintText("LCPDFR not available. See log file for further details.", 5000);
                return;
            }

            // Create important engine objects
            KeyWatchDog = new KeyWatchDog();
            FormsManager = new GUI.FormsManager();
            ModelManager = new ModelManager(IsTbogt);
            NetworkManager = new NetworkManager();
            CopManager = new CopManager(); // Create before the pools object to get all cops
            PoolUpdater = new PoolUpdaterUnmanaged();
            TaskManager = new TaskManager();
            TimerManager = new TimerManager();
            ContentManager.Initialize();

            // Create not so important engine objects
            PluginManager = new PluginManager();
            PluginManager.Initialize();

            // Assign console commands to functions with the ConsoleCommandAttribute
            ConsoleCommandAssigner.Initialize();

            // Setup main loop
            // Setup reset events
            this.mainLoopStopFlag = new ManualResetEvent(false);
            this.tickStopFlag = new ManualResetEvent(false);

            // Start new thread
            this.mainThread = new Thread(new ThreadStart(this.Process));
            this.mainThread.Start();

            DelayedCaller.Call(delegate { NetworkManager.Initialize(); }, 2500);

            this.memoryValidator = new MemoryValidator();
            this.memoryValidator.Start();

            //System.Reflection.Assembly a = System.Reflection.Assembly.LoadFrom("ExceptionTestDLL.dll");
            //foreach (Type type in a.GetTypes())
            //{
            //    string s = type.Name;
            //    object o = Activator.CreateInstance(type, new object[] { });
            //}

            Log.Info("Main: Initializing done", "Main");
        }

        /// <summary>
        /// Called when text has been logged.
        /// </summary>
        /// <param name="text">The text.</param>
        private void Log_TextHasBeenLogged(string text)
        {
            // Also print to console
            GTA.Game.Console.Print(text);
        }

        [Obfuscation(Exclude = true)]
        public void Main_Tick(object sender, EventArgs e)
        {
            this.Tick();
        }

        private void Tick()
        {
            // Initialize if not yet done
            if (!this.initialized)
            {
                // Initialize
                try
                {
                    Initialize();
                    this.initialized = true;
                }
                // Handle exceptions that are expected
                catch (NoServerConnectionException)
                {
                    ExceptionHandler.ReportException("LCPDFR encountered a critical error while initializing and was shut down: Couldn't connect to the LCPD:FR multiplayer server. Please make sure you are connected to the internet and there is no server maintenance. We are sorry for the inconvenience.");
                }
                // If an exception is raised while initializing, log and shutdown
                catch (Exception ex)
                {
                    ExceptionHandler.LogCriticalException(ex);
                    Engine.GUI.HelpBox.Print("LCPDFR encountered a critical error while initializing and was shut down. Please check the logfile.");
                    Shutdown();
                }
            }

            if (!Main.Authentication.CanStart)
            {
                return;
            }

            this.mainLoopStopFlag.Set();
            this.tickStopFlag.WaitOne();
            this.tickStopFlag.Reset();
        }

        private void Process()
        {
            while (true)
            {
                this.mainLoopStopFlag.WaitOne();
                this.mainLoopStopFlag.Reset();

                // ---------- Main loop ----------

                // For now, the main loop uses try-catch
                try
                {
                    // Manage actions
                    ActionScheduler.Process();

                    // Process timers
                    Timers.TaskManager.Process();
                    DelayedCaller.Process();

                    // Update keys
                    KeyWatchDog.Update();

                    // If in debug mode, attach debugger using "End" key
                    if (Main.DEBUG_MODE)
                    {
                        if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.End))
                        {
                            System.Diagnostics.Debugger.Launch();
                        }
                    }

                    // Update the entity pools
                    PoolUpdater.UpdatePools();

                    // Call core components - basically important engine classes
                    for (int i = 0; i < Pools.CoreTicks.Count; i++)
                    {
                        ICoreTickable coreTickable = Pools.CoreTicks[i];
                        coreTickable.Process();
                    }

                    // TODO: Move PedTaskManager and PedIntelligence calls here and no longer make them CoreTicks??

                    // Call 'normal' engine components - mostly script components such as Chase
                    for (int i = 0; i < Pools.Ticks.Count; i++)
                    {
                        ITickable coreTickable = Pools.Ticks[i];
                        coreTickable.Process();
                    }
                    
                    // Process plugins
                    PluginManager.Process();

                    // Scripts have finished, network sync
                    NetworkManager.Process();

                    this.Test();

                    /*
                    //if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F1))
                    //{
                    //    l = new Light(System.Drawing.Color.Red, 3f, 50f, Player.Ped.Position);
                    //    l.Enabled = true;
                    //    l = new Light(System.Drawing.Color.Red, 20f, 20f, Player.Ped.Position);
                    //    l.Enabled = true;

                    //    Game.DisplayText("Light done");
                    //}
                    //if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F3))
                    //{
                    //    l.Disable();
                    //    Game.DisplayText("Deleted");
                    //}

                    // Process scripts - TODO: move this into a new script class and also create scriptmanager which would be called here

                    //Game.LocalPlayer.Character.Weapons.Current.Ammo += 50;
                    //Game.LocalPlayer.Character.Weapons.AssaultRifle_M4.Ammo += 50;
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F3))
                    {
                        this.ped = new CPed("M_O_STREET_01", Player.Ped.Position.Around(8f), EPedGroup.Criminal);
                        if (this.ped.Exists())
                        {
                            this.ped.BlockPermanentEvents = true;
                            Chase chase = new Chase(false, false);
                            chase.AddTarget(this.ped);
                            this.ped.PedData.DropWeaponWhenAskedByCop = false;

                            // Increase health, so suspect will live a little longer
                            this.ped.MaxHealth = 300;
                            this.ped.Health = 300;
                            this.ped.Armor = 100;
                            this.ped.PedData.DefaultWeapon = Weapon.SMG_Uzi;

                            // Equips uzi
                            //this.ped.EnsurePedHasWeapon();

                            //TaskFleeEvadeCops task = new TaskFleeEvadeCops(true, false, false, false, false);
                            //ped.Intelligence.TaskManager.Assign(task, ETaskPriority.MainTask);
                            //ped.AttachBlip();
                        }
                        // (Walking style, 2 = go, 4 = run); Dont warp ped to pos = -1, Warp = >= 0 
                        //GTA.Native.Function.Call("TASK_FOLLOW_NAV_MESH_TO_COORD", p, pos.X, pos.Y, pos.Z, 4, -1, 1.0);

                    }
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F4))
                    {
                        // Create cop
                        //cop = new CPed(CModel.GetRandomCopModel(EUnitType.Police), Player.Ped.Position.Around(8f), EPedGroup.Cop);
                        //if (cop != null && cop.Exists())
                        //{
                        //    //cop.RelationshipGroup = RelationshipGroup.Cop;
                        //    cop.Weapons.Glock.Ammo = 100;
                        //    cop.Weapons.Glock.Select();

                        //    Game.WaitInCurrentScript(1000);

                        //    unsafe
                        //    {
                        //        IntPtr pedIntelligence = new IntPtr(cop.APed.GetPedIntelligence());
                        //        int* pPedIntelligence = (int*) pedIntelligence.ToPointer();
                        //        Log.Debug(pedIntelligence.ToString("X"), "Main");
                        //    }

                        //    TaskCopChasePed task = new TaskCopChasePed(ped, false, false, false);
                        //    task.AssignTo(cop, ETaskPriority.MainTask);
                        //}

                        //if (Player.Ped.IsInVehicle)
                        //{
                        //    CVehicle vehicle = Player.Ped.CurrentVehicle;
                        //    int id = vehicle.NetworkID;
                        //    Game.DisplayText(id.ToString());
                        //}


                        //Natives.RequestAnims("missgambetti1");
                        //GTA.Native.Function.Call("TASK_PLAY_ANIM_SECONDARY_NO_INTERRUPT", (GTA.Ped) Main.Player.Ped, "handsup", "missgambetti1", 4.0f, true, 0, 1, 0, -1);


                        //Main.Player.Ped.PedData = new PedDataCop(Main.Player.Ped);

                        //TaskCop taskCop = new TaskCop();
                        //taskCop.AssignTo(Main.Player.Ped, ETaskPriority.MainTask);
                        //TaskCopChasePed taskCopChasePed = new TaskCopChasePed(this.ped, false, false, false);
                        //taskCopChasePed.AssignTo(Main.Player.Ped, ETaskPriority.MainTask);

                        Vector3 position = new Vector3(0, 0.5f, 0);
                        ped = new CPed("M_Y_DRUG_01", Player.Ped.GetOffsetPosition(position), EPedGroup.Criminal);
                        CPed ped2 = new CPed("F_Y_PRICH_01", Player.Ped.GetOffsetPosition(position), EPedGroup.Pedestrian);
                        if (ped.Exists() && ped2.Exists())
                        {
                            GTA.Game.LocalPlayer.Model = "M_Y_COP";
                            GTA.Game.LocalPlayer.Character.Weapons.Uzi.Ammo = 4000;
                            Game.WaitInCurrentScript(10000);
                            ped.BlockPermanentEvents = true;
                            ped2.BlockPermanentEvents = true;
                            ScenarioHostageTaking scenarioHostageTaking = new ScenarioHostageTaking(ped, ped2);
                            TaskScenario scenario = new TaskScenario(scenarioHostageTaking);

                            //TaskFleeEvadeCops task = new TaskFleeEvadeCops(true, false, false, false, false);
                            //ped.Intelligence.TaskManager.Assign(task, ETaskPriority.MainTask);
                            //ped.AttachBlip();
                        }
                    }
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F5))
                    {
                        //CopManager.RequestDispatch(Player.Ped.Position, true, false, EUnitType.Police, 4, true, null);
                        //Vector3 pos = Player.Ped.Position;
                        //AdvancedHookManaged.AVehicle aVehicle = AdvancedHookManaged.AVehicle.CreateCarAroundPosition(pos.X, pos.Y, pos.Z, 0x94, 0);
                        //if (aVehicle != null)
                        //{
                        //    uint handle = aVehicle.Get();
                        //    CVehicle vehicle = new CVehicle((int)handle);
                        //    if (vehicle != null && vehicle.Exists())
                        //    {
                        //        vehicle.AttachBlip();
                        //    }
                        //}

                        // Delete
                        if (ped != null && ped.Exists())
                        {
                            ped.Delete();
                        }
                        Main.Player.Ped.Task.ClearAll();
                    }
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F6))
                    {
                        //if (ped.IsInVehicle())
                        //{
                        //    ped.CurrentVehicle.EngineHealth = -1;
                        //}
                        //else
                        //{
                        //    ped.EnsurePedHasWeapon();
                        //}
                        //ped = new CPed("M_M_SAXPLAYER_01", Player.Ped.Position.Around(4f), EPedGroup.Criminal);

                        //GTA.Ped gtaPed = Game.LocalPlayer.GetTargetedPed();
                        //if (gtaPed != null && gtaPed.Exists())
                        //{
                        //    CPed target = new CPed(gtaPed.pHandle);
                        //    if (target != null && target.Exists())
                        //    {
                        //        string s = "";
                        //        EInternalTaskID[] tasks = target.Intelligence.TaskManager.GetActiveInternalTasks();
                        //        foreach (EInternalTaskID eInternalTaskID in tasks)
                        //        {
                        //            s += eInternalTaskID.ToString() + " -- ";
                        //        }
                        //        Game.DisplayText(s);
                        //        Game.Console.Print(s);
                        //    }
                        //}

                        if (this.pedDebugging != null && this.pedDebugging.Exists())
                        {
                            if (this.pedDebugging.HasBlip)
                            {
                                this.pedDebugging.DeleteBlip();
                            }
                        }
                        this.pedDebugging = Main.Player.Ped.Intelligence.GetTargetedPed();
                    }
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F7))
                    {
                        //if (this.ped.Exists())
                        //{
                        //    this.ped.CurrentVehicle.FreezePosition = true;
                        //}

                        //CVehicle vehicle = Player.Ped.CurrentVehicle;
                        //if (vehicle != null)
                        //{
                            
                        //    int mem = ((GTA.Vehicle)vehicle).MemoryAddress;
                        //    Game.DisplayText(mem.ToString("X"));

                        //    IntPtr gearPointer = new IntPtr(mem + 0x10E0);

                        //    *(int*)gearPointer.ToPointer() = 1;
                        //}

                        if (Game.LocalPlayer.Model == "PLAYER")
                        {

                            Game.LocalPlayer.Model = "M_Y_COP";
                        }
                        else
                        {
                            Game.LocalPlayer.Model = "PLAYER";
                        }

                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 1, 1);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 2, 1);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 3, 1);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 4, 1);

                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 5, 0);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 6, 0);

                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 7, 1);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 8, 1);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 9, 1);

                        //AdvancedHookManaged.AVehicle.EnableTurnOffVehicleExtraDamage();


                        //if (this.b != null && this.b.Exists())
                        //{
                        //    this.b.Delete();
                        //}
                        //else
                        //{
                        //    this.b = Blip.AddBlip(Player.Ped.Position);
                        //    this.b.Scale = 200f;
                        //    this.b.Color = GTA.BlipColor.LightOrange;


                        //    this.b = Blip.AddBlip(Player.Ped.Position.Around(300));
                        //    this.b.Scale = 200f;
                        //    this.b.Color = GTA.BlipColor.Purple;
                        //}


                        //CPed target = Player.Ped.Intelligence.GetTargetedPed();
                        //if (target != null && target.Exists())
                        //{
                        //    target.Task.GoTo(target.GetOffsetPosition(new Vector3(0, 10, 0)), false);
                        //}

                        //Player.Ped.Animation.Play(new AnimationSet("amb@security_idles_c"), "idle_lookaround_a", 2);
                        //Player.Ped.Animation.Play(new AnimationSet("amb@security_idles_c"), "idle_lookaround_b", 2);
                        //Player.Ped.Animation.Play(new AnimationSet("missemergencycall"), "idle_lookaround_b", 2);
                        //CPed ped = Player.Ped.Intelligence.GetTargetedPed();

                        //CPed[] peds = Player.Ped.Intelligence.GetPedsAround(800f, EPedSearchCriteria.AmbientPed);
                        //foreach (CPed ped in peds)
                        //{
                        //    if (ped != null && ped.Exists())
                        //    {
                        //        if (ped.Handle == Player.Ped.Handle) continue;
                        //        if (!ped.IsAliveAndWell) continue;

                        //        ped.IsRequiredForMission = true;
                        //        ped.BecomeMissionCharacter();
                        //        ped.RelationshipGroup = RelationshipGroup.Gang_Albanian;
                        //        ped.ChangeRelationship(RelationshipGroup.Gang_Albanian, Relationship.Companion);
                        //        ped.EnsurePedHasWeapon();
                        //        //ped.MaxHealth = 1000;
                        //        //ped.Health = 1000;
                        //        ped.Armor = 10000;
                        //        ped.AttachBlip();
                        //        ped.Enemy = true;
                        //        ped.AlwaysFreeOnDeath = true;
                        //        ped.Task.AlwaysKeepTask = true;
                        //        ped.Weapons.MP5.Ammo = 4000;
                        //        ped.Task.SwapWeapon(GTA.Weapon.SMG_MP5);
                        //        ped.Task.FightAgainst(Player.Ped);
                        //    }
                        //}

                        //Player.Ped.Animation.Play(new AnimationSet("missgambetti1"), "handsup", 4f);

                        //CPed pedF7 = cop;
                        //if (pedF7 != null && pedF7.Exists())
                        //{
                        //    //foreach (DecisionMaker.CopyTemplate copyTemplate in (DecisionMaker.CopyTemplate[])Enum.GetValues(typeof(DecisionMaker.CopyTemplate)))
                        //    //{
                        //    DecisionMaker.CopyTemplate copyTemplate = DecisionMaker.CopyTemplate.Combat_cop_nrm;

                        //        Log.Debug("Loading " + copyTemplate.ToString(), "Main");
                        //        DecisionMaker dm = DecisionMaker.CopyCombatForGroupMembers(DecisionMaker.CopyTemplate.Combat_cop_wl1);
                        //        if (dm != null && dm.Exists())
                        //        {
                        //            dm.ApplyTo(pedF7);
                        //            int charDecMaker = pedF7.Intelligence.InternalIVPedIntelligence.m_dwCharDecisionMaker;
                        //            int groupCharDecMaker = pedF7.Intelligence.InternalIVPedIntelligence.m_dwGroupCharDecisionMaker;
                        //            int combatDecMaker = pedF7.Intelligence.InternalIVPedIntelligence.m_dwCombatDecisionMaker;
                        //            int groupCombatDecMaker = pedF7.Intelligence.InternalIVPedIntelligence.m_dwGroupCombatDecisionMaker;
                        //            Log.Debug("Values: " + charDecMaker + " -- " + groupCharDecMaker + " -- " + combatDecMaker + " -- " + groupCombatDecMaker, "Main");
                        //            Game.DisplayText("Logged " + groupCombatDecMaker.ToString());
                        //            //dm.Dispose();
                        //        }
                        //        else
                        //        {
                        //            Game.DisplayText("DecisionMaker doesn't exist");
                        //        }
                        //    //}
                        //}
                        //else
                        //{
                        //    Game.DisplayText("Ped doesn't exist");
                        //}

                        //GTA.Native.Function.Call("SET_NEXT_DESIRED_MOVE_STATE", 4);
                        //cop.Task.GoTo(Player.Ped);
                        //cop.Task.ClearAll();
                        //GTA.Native.Function.Call("MODIFY_CHAR_MOVE_STATE", (GTA.Ped)cop, 4);
                        //if (cop != null && cop.Exists())
                        //{
                        //    cop.Weapons.Glock.Ammo = 1000;
                        //    cop.Weapons.Glock.Select();
                        //    cop.Task.FightAgainst(ped);
                        //}
                    }
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F8))
                    {
                        this.ped.PedData.CurrentChase.AllowSuspectVehicles = true;

                        //CVehicle v = Player.Ped.CurrentVehicle;
                        //if (v != null)
                        //{
                        //    // Read window state
                        //    bool[] windowState = new bool[6];
                        //    for (int i = 0; i < 6; i++)
                        //    {
                        //        windowState[i] = true;
                        //        bool intact = GTA.Native.Function.Call<bool>("IS_VEH_WINDOW_INTACT", (GTA.Vehicle)v, i);
                        //        windowState[i] = intact;
                        //    }

                        //    // Read door state
                        //    Game.DisplayText(v.Door(VehicleDoor.LeftFront).Angle.ToString());

                        //    // Repair
                        //    v.Repair();

                        //    // Restore broken windows
                        //    for (int i = 0; i < 6; i++)
                        //    {
                        //        if (!windowState[i])
                        //        {
                        //            GTA.Native.Function.Call("REMOVE_CAR_WINDOW", (GTA.Vehicle)v, i);
                        //        }
                        //    }
                        //}


                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 1, 0);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 2, 0);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 3, 0);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 4, 0);

                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 5, 1);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 6, 1);

                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 7, 0);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 8, 0);
                        //GTA.Native.Function.Call("TURN_OFF_VEHICLE_EXTRA", (GTA.Vehicle)Player.Ped.CurrentVehicle, 9, 0);

                        //ped = new CPed(new CModel("M_Y_COP"), Player.Ped.Position, EPedGroup.Unknown);
                        //if (ped != null && ped.Exists())
                        //{
                        //    CVehicle vehicle = ped.Intelligence.GetClosestVehicle(EVehicleSearchCriteria.NoDriverOnly, 20f);
                        //    if (vehicle != null && vehicle.Exists())
                        //    {
                        //        vehicle.AttachBlip();
                        //        ped.WarpIntoVehicle(vehicle, VehicleSeat.Driver);
                        //    }
                        //}

                        //CVehicle vehicle = Player.Ped.CurrentVehicle;
                        //if (vehicle != null && vehicle.Exists())
                        //{
                        //    int address = ((GTA.Vehicle)vehicle).MemoryAddress;
                        //    Game.Console.Print(address.ToString("X"));
                        //    int* offset = (int*)address + 0x1337;
                        //    int value = *offset;
                        //    Game.DisplayText(value.ToString());
                        //}

                        //bool running =
                        //    Main.Player.Ped.Intelligence.TaskManager.IsInternalTaskActive(
                        //        EInternalTaskID.CTaskComplexCombatPullFromCarSubtask);
                        //if (!running)
                        //{
                        //GTA.Game.Console.Print("REAPPLIED");
                        //    AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                        //    Main.Player.Ped.APed.TaskCombatPullFromCarSubtask(this.ped.APed);
                        //}

                        //if (cop != null && cop.Exists())
                        //{
                        //    cop.Delete();
                        //}
                    }
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F9))
                    {
                        if (ActionScheduler.SchedulerStyle == ESchedulerStyle.Never)
                        {
                            ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Optimized);
                        }
                        else if (ActionScheduler.SchedulerStyle == ESchedulerStyle.Optimized)
                        {
                            ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Performance);
                        }
                        else if (ActionScheduler.SchedulerStyle == ESchedulerStyle.Performance)
                        {
                            ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Never);
                        }
                        Engine.GUI.ScriptFunctions.PrintHelpText("Scheduler mode changed to: " + ActionScheduler.SchedulerStyle.ToString());

                        //GTA.Native.Function.Call("SET_PLANE_THROTTLE", (GTA.Vehicle)v, false);

                        //Game.DisplayText(((GTA.Ped)Main.Player.Ped).MemoryAddress.ToString("X"));

                        //GTA.Native.Function.Call("SET_FAKE_WANTED_LEVEL", 0);

                        //  CPed ped = Player.Ped.Intelligence.GetTargetedPed();
                        //  if (ped != null && ped.Exists())
                        //  {
                        //      ped.Delete();
                        //  }

                        //Natives.TaskCarTempAction(Main.Player.Ped, Main.Player.Ped.CurrentVehicle,
                        //                          ECarTempActionType.SlowDownLeftSoftlyThenSpeedUpBackwardsLeft, 10000);

                        //Weapon w = Weapon.TBOGT_AdvancedMG;                     
                        //Player.Ped.Weapons[w].Ammo = 4000;
                        //Player.Ped.Weapons.RocketLauncher.Ammo = 4000;

                        //GTA.World.CreateVehicle(new Model(-1627000575), Game.LocalPlayer.Character.Position);

                        //w = Weapon.TBOGT_GoldenSMG;
                        //Player.Ped.Weapons[w].Ammo = 4000;

                        //bool active = cop.APed.IsTaskActive(0x76E);
                        //bool active = cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatAdvanceSubtask);
                        //Game.Console.Print("Active" + active.ToString());


                        //CPed target = Player.Ped.Intelligence.GetTargetedPed();
                        //if (target != null && target.Exists())
                        //{
                        //    target.AttachBlip();
                        //    target.Task.ClearAll();
                        //    TaskFleeEvadeCops task = new TaskFleeEvadeCops(true, false, false, false, false);
                        //    target.Intelligence.TaskManager.Assign(task, ETaskPriority.MainTask);
                        //    Game.DisplayText("Cops: " + CopManager.Cops.GetAll().Length);
                        //    foreach (CPed cop1 in CopManager.Cops.GetAll())
                        //    {
                        //        if (cop1 != null && cop1.Exists())
                        //        {
                        //            cop1.Task.FightAgainst(target);
                        //        }
                        //    }
                        //}
                    }
                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F10))
                    {
                        ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Optimized);
                        GTA.Game.DisplayText("Scheduler running in OPTIMIZED MODE");

                        //Player.Ped.Weapons.DesertEagle.Ammo = 4000;
                        //Player.Ped.Weapons.MP5.Ammo = 4000;
                        //Player.Ped.Weapons.BasicShotgun.Ammo = 4000;
                        //Player.Ped.Weapons.AssaultRifle_AK47.Ammo = 4000;
                        //Weapon w = Weapon.TBOGT_AdvancedMG;
                        //Player.Ped.Weapons[w].Ammo = 4000;
                        //w = Weapon.TBOGT_GrenadeLauncher;
                        //Player.Ped.Weapons[w].Ammo = 4000;
                        //w = Weapon.Thrown_Grenade;
                        //Player.Ped.Weapons[w].Ammo = 4000;

                        //foreach (Ped ped1 in Game.LocalPlayer.Group)
                        //{
                        //    if (ped1 != null && ped1.Exists())
                        //    {
                        //        ped1.Delete();
                        //    }
                        //}
                        //Game.LocalPlayer.Group.RemoveAllMembers();


                        //Main.Player.Ped.CurrentVehicle.AVehicle.Wheel(0).Health -= 100;
                        //Game.DisplayText(Main.Player.Ped.CurrentVehicle.AVehicle.Wheel(0).Health.ToString());
                        //Main.Player.Ped.CurrentVehicle.BurstTire(VehicleWheel.FrontLeft);
                        //Main.Player.Ped.CurrentVehicle.BurstTire(VehicleWheel.FrontRight);
                        //Main.Player.Ped.CurrentVehicle.BurstTire(VehicleWheel.RearLeft);
                        //Main.Player.Ped.CurrentVehicle.BurstTire(VehicleWheel.RearRight);
                        //    CPed target = Player.Ped.Intelligence.GetTargetedPed();
                        //    if (target != null && target.Exists())
                        //    {
                        //        int charDecMaker = target.Intelligence.InternalIVPedIntelligence.m_dwCharDecisionMaker;
                        //        int groupCharDecMaker = target.Intelligence.InternalIVPedIntelligence.m_dwGroupCharDecisionMaker;
                        //        int combatDecMaker = target.Intelligence.InternalIVPedIntelligence.m_dwCombatDecisionMaker;
                        //        int groupCombatDecMaker = target.Intelligence.InternalIVPedIntelligence.m_dwGroupCombatDecisionMaker;
                        //        Game.Console.Print(charDecMaker + " -- " + groupCharDecMaker + " -- " + combatDecMaker + " -- " + groupCombatDecMaker);
                        //    }
                    }
                    //if (KeyWatchDog.GetKey(System.Windows.Forms.Keys.LShiftKey).IsStillDown)
                    //{
                    //    if (vehicleData == null)
                    //    {
                    //        vehicleData = new Dictionary<CVehicle, float>();
                    //    }
                    //    // Freeze all units
                    //    foreach (CVehicle vehicle in Pools.VehiclePool.GetAll())
                    //    {
                    //        if (vehicle.Exists())
                    //        {
                    //            if (vehicle != Player.Ped.CurrentVehicle)
                    //            {
                    //                if (!vehicleData.ContainsKey(vehicle))
                    //                {
                    //                    vehicleData.Add(vehicle, vehicle.Speed);
                    //                    vehicle.FreezePosition = true;
                    //                }
                    //            }
                    //        }
                    //    }
                    //    foreach (CPed cPed in Pools.PedPool.GetAll())
                    //    {
                    //        if (cPed.Exists())
                    //        {
                    //            if (cPed != Player.Ped)
                    //            {
                    //                cPed.FreezePosition = true;
                    //                cPed.BlockWeaponSwitching = true;
                    //            }
                    //        }
                    //    }
                    //}
                    //else if (KeyWatchDog.GetKey(System.Windows.Forms.Keys.LShiftKey).IsUp)
                    //{
                    //    if (vehicleData == null)
                    //    {
                    //        vehicleData = new Dictionary<CVehicle, float>();
                    //    }
                    //    foreach (KeyValuePair<CVehicle, float> keyValuePair in vehicleData)
                    //    {
                    //        if (keyValuePair.Key.Exists())
                    //        {
                    //            keyValuePair.Key.FreezePosition = false;
                    //            keyValuePair.Key.Speed = keyValuePair.Value;
                    //        }
                    //    }
                    //    vehicleData.Clear();
                    //    foreach (CPed cPed in Pools.PedPool.GetAll())
                    //    {
                    //        if (cPed.Exists())
                    //        {
                    //            if (cPed != Player.Ped)
                    //            {
                    //                cPed.FreezePosition = false;
                    //                cPed.BlockWeaponSwitching = false;
                    //            }
                    //        }
                    //    }
                    //}

                    if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.G))
                    {
                        if (!Player.Ped.IsInVehicle())
                        {
                            CVehicle vehicle = Player.Ped.Intelligence.GetClosestVehicle(EVehicleSearchCriteria.DriverOnly, 15f);
                            if (vehicle != null && vehicle.Exists())
                            {
                                CPed ped = vehicle.Driver;
                                var taskSequence = new TaskSequence();
                                var taskWaitUntilPedIsInVehicle = new TaskWaitUntilPedIsInVehicle(Player.Ped, vehicle);
                                var taskFleeEvadeCopsInVehicle = new TaskFleeEvadeCopsInVehicle();
                                taskSequence.AddTask(taskWaitUntilPedIsInVehicle);
                                taskSequence.AddTask(taskFleeEvadeCopsInVehicle);
                                taskSequence.AssignTo(ped);
                                vehicle.MakeProofTo(true, true, true, true, true);
                                vehicle.CanBeDamaged = false;
                                ped.MakeProofTo(true, true, true, true, true);
                                ped.BlockPermanentEvents = true;
                                ped.Task.AlwaysKeepTask = true;
                                ped.WillFlyThroughWindscreen = false;
                                Player.Ped.Task.EnterVehicle(vehicle, VehicleSeat.AnyPassengerSeat);
                                Player.Ped.WillFlyThroughWindscreen = false;

                                if (vehicle.VehicleGroup == EVehicleGroup.Police)
                                {
                                    Player.Ped.WarpIntoVehicle(vehicle, VehicleSeat.RightFront);
                                }
                            }
                        }
                    }

                    if (this.pedDebugging != null && this.pedDebugging.Exists())
                    {
                        this.pedDebugging.AttachBlip().Color = BlipColor.Yellow;

                        // Collect data
                        StringBuilder sb = new StringBuilder();

                        string group = this.pedDebugging.PedGroup.ToString();
                        sb.Append("Group: " + group);
                        string subGroup = this.pedDebugging.PedSubGroup.ToString();
                        sb.Append("~n~SubGroup: " + subGroup);
                        string available = this.pedDebugging.PedData.Available.ToString();
                        sb.Append("~n~Available: " + available);
                        string required = this.pedDebugging.IsRequiredForMission.ToString();
                        sb.Append("~n~Required for mission: " + required);
                        // If cop
                        if (this.pedDebugging.PedGroup == EPedGroup.Cop)
                        {
                            var data = this.pedDebugging.GetPedData<PedDataCop>();
                            string state = data.CopState.ToString();
                            sb.Append("~n~CopState: " + state);
                        }
                        string debug = this.pedDebugging.Debug ?? string.Empty;
                        sb.Append("~n~Debug: " + debug);

                        string sTasks = "";
                        ETaskID[] tasks = this.pedDebugging.Intelligence.TaskManager.GetActiveTasks();
                        foreach (ETaskID eTaskID in tasks)
                        {
                            sTasks += eTaskID.ToString() + " ~n~";
                        }
                        sb.Append("~n~Tasks (" + tasks.Length.ToString() +  "): ~n~" + sTasks);

                        string sInternalTasks = "";
                        EInternalTaskID[] internalTasks = this.pedDebugging.Intelligence.TaskManager.GetActiveInternalTasks();
                        foreach (EInternalTaskID eInternalTaskID in internalTasks)
                        {
                            sInternalTasks += eInternalTaskID.ToString() + " ~n~ ";
                        }
                        sb.Append("~n~Internal Tasks(" + internalTasks.Length.ToString() + "): ~n~" + sInternalTasks);
                        AdvancedHookManaged.AGame.PrintText(sb.ToString());
                    }*/
                }
                catch (Exception ex)
                {
                    Log.Error("CRITICAL ERROR DURING MAINLOOP! REPORT THIS ISSUE AT LCPDFR.COM BY INCLUDING THIS LOGFILE.", "Main");
                    GUI.HelpBox.Print("CRITICAL ERROR DURING MAINLOOP!");
                    ExceptionHandler.ExceptionCaught(this, ex);
                }

                // ---------- Main loop end ----------

                this.tickStopFlag.Set();
            }
        }

        private void Test()
        {
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F4))
            {
                //TaskCopTasePed taskCopTase = new TaskCopTasePed(CPlayer.LocalPlayer.LastPedPulledOver[0], true);
                //taskCopTase.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.MainTask);

                //TaskCopChasePed taskCopChasePed = new TaskCopChasePed(CPlayer.LocalPlayer.LastPedPulledOver[0], true, true, true);
                //taskCopChasePed.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.MainTask);

                //CPlayer.LocalPlayer.Ped.PedData.HasBeenTased = false;
                //CPlayer.LocalPlayer.Ped.Wanted.Surrendered = false;
                //Game.Console.Print("Taser flag set to: " + CPlayer.LocalPlayer.Ped.PedData.HasBeenTased);
            }

            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F6))
            {

                //Set as invisible to chase AI
                 CPlayer.LocalPlayer.Ped.Wanted.Invisible = !CPlayer.LocalPlayer.Ped.Wanted.Invisible;
                /*
                if (Game.isGameKeyPressed(GameKey.Aim))
                {
                    CPed cop = CPlayer.LocalPlayer.GetPedAimingAt();

                    if (cop.Exists())
                    {
                        cop.Task.FightAgainst(testped);
                        cop.APed.TaskSearchForPedOnFoot(testped.APed);
                    }
                }
                 * */
            }

            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F7))
            {
                /*
                Blip pavement = Blip.AddBlip(World.GetNextPositionOnPavement(CPlayer.LocalPlayer.Ped.Position));
                Blip street = Blip.AddBlip(World.GetNextPositionOnStreet(CPlayer.LocalPlayer.Ped.Position));

                street.Icon = BlipIcon.Misc_TaxiRank;

                Blip safe = Blip.AddBlip(CPlayer.LocalPlayer.Ped.GetSafePositionAlternate());
                safe.Icon = BlipIcon.Misc_Trophy;
                safe.Display = BlipDisplay.ArrowAndMap;

                ArrowCheckpoint safeC = new ArrowCheckpoint(safe.Position, delegate { });

                DelayedCaller.Call(delegate { if (street != null) street.Delete(); }, 10000);
                DelayedCaller.Call(delegate { if (pavement != null) pavement.Delete(); }, 10000);
                DelayedCaller.Call(delegate { if (safe != null) safe.Delete(); }, 10000);
                DelayedCaller.Call(delegate { if (safeC != null) safeC.Delete(); }, 10000);


                //Game.DisplayText("Current interor: " + GTA.Native.Function.c);

                GTA.Native.Pointer interior = new GTA.Native.Pointer(typeof(int));
                GTA.Native.Function.Call("GET_INTERIOR_AT_COORDS", safe.Position.X, safe.Position.Y, safe.Position.Z, interior);
                //Game.DisplayText(                interior.Value.ToString());

                Game.DisplayText(CPlayer.LocalPlayer.Ped.Speed.ToString());
                */
  
                /*
                testped = new CPed("F_Y_SOCIALITE", CPlayer.LocalPlayer.Ped.GetSafePositionAlternate(), EPedGroup.Testing);
                testped.IsRequiredForMission = true;
                testped.Task.ClearAll();
                testped.CurrentRoom = CPlayer.LocalPlayer.Ped.CurrentRoom;
                 * */
                

                //TextHelper.PrintText("Player: " + CPlayer.LocalPlayer.Ped.Position.Z.ToString() + " | Highest Ground: " + World.GetGroundZ(CPlayer.LocalPlayer.Ped.Position, GroundType.Highest), 5000);

            }

            /*
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F4))
            {
                CPlayer.LocalPlayer.Ped.APed.TaskPlayUpperCombatAnim();
                CPlayer.LocalPlayer.CanControlCharacter = true;
                CPlayer.LocalPlayer.Ped.FreezePosition = false;
            }
            */
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F5))
            {
   

                /*
                CPed ped = new CPed("F_Y_SOCIALITE", CPlayer.LocalPlayer.Ped.Position.Around(8f), EPedGroup.Criminal);
                if (ped.Exists())
                {
                    ped.BecomeMissionCharacter();
                    ped.AlwaysFreeOnDeath = true;
                    ped.AttachBlip();

                    TaskFleeEvadeCops taskFleeEvadeCops = new TaskFleeEvadeCops(false, false, EVehicleSearchCriteria.All, false);
                    taskFleeEvadeCops.AssignTo(ped, ETaskPriority.MainTask);

                    CPlayer.LocalPlayer.LastPedPulledOver = new CPed[1] { ped };

                    ContentManager.DefaultContentManager.AddPed(ped, 20f, EContentManagerOptions.DeleteInsteadOfFree);

                    AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                }
                */
                /*
                CPed runPed = new CPed("F_Y_SOCIALITE", CPlayer.LocalPlayer.Ped.GetSafePositionAlternate(CPlayer.LocalPlayer.Ped.Position.Around(10.0f)), EPedGroup.Testing);
                CPed copPed = new CPed("M_Y_STROOPER", CPlayer.LocalPlayer.Ped.GetSafePositionAlternate(CPlayer.LocalPlayer.Ped.Position.Around(10.0f)), EPedGroup.Testing);
                CPed copPed2 = new CPed("M_Y_COP", CPlayer.LocalPlayer.Ped.GetSafePositionAlternate(CPlayer.LocalPlayer.Ped.Position.Around(10.0f)), EPedGroup.Testing);

                if (runPed.Exists() && copPed.Exists() && copPed2.Exists())
                {
                    Group group = new Group(runPed);
                    group.FormationSpacing = 0.5f;

                    group.AddMember(copPed);

                    runPed.APed.TaskCombatRetreatSubtask(copPed);

                    Group group2 = new Group(copPed);
                    group.FormationSpacing = 2.0f;
                    group2.AddMember(copPed2);
                }
                */
                /*
                for (int i = 0; i < 10; i++)
                {
                    CPed testPed = new CPed("M_Y_COP", CPlayer.LocalPlayer.Ped.GetSafePositionAlternate(CPlayer.LocalPlayer.Ped.Position.Around(20.0f)), EPedGroup.Cop);

                    if (testPed.Exists())
                    {
                        testPed.SetPathfinding(true, true, true);
                        CPlayer.LocalPlayer.Group.AddMember(testPed);
                        CPlayer.LocalPlayer.Group.FormationSpacing = 5.0f;
                        CPlayer.LocalPlayer.Group.SeparationRange = 100.0f;
                        ContentManager.DefaultContentManager.AddPed(testPed);
                    }
                }
                */

            }
            
            
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F9) && 1 == 0) //disabled
            {
                
                CPed ped = new CPed("F_Y_SOCIALITE", CPlayer.LocalPlayer.Ped.Position.Around(8f), EPedGroup.Criminal);
                if (ped.Exists())
                {   
                    ped.BlockPermanentEvents = true;
                    
                    Pursuit p = new Pursuit();
                    p.SetAsCurrentPlayerChase();
                    p.AddTarget(ped);
                    p.AllowSuspectVehicles = false;
                    p.AllowSuspectWeapons = false;
                    p.CanCopsJoin = true;
                    p.CallIn(AudioHelper.EPursuitCallInReason.Pursuit);
                    p.OnlyAIVisuals = true;
                    ped.Task.RunTo(World.GetNextPositionOnStreet(World.GetPositionAround(ped.Position, 100f)));

                    ped.Wanted.VisualLost = true;
                    ped.Wanted.VisualLostSince = 300;

                    // Increase health, so suspect will live a little longer
                    ped.MaxHealth = 300;
                    ped.Health = 300;
                    ped.Armor = 100;
                     

                    //ped.Animation.Play(new AnimationSet("cop"), "crim_cuffed", 100.0f);
                    /*
                    ped.BecomeMissionCharacter();
                    GTA.Native.Function.Call("REQEST_ANIMS", "MISSBERNIE1");
                    GTA.Native.Function.Call("TASK_PLAY_ANIM", ped.Handle, "ATTACKER_BEATTHENRUN", "MISSBERNIE1", 9999.00, 0, 0, 0, 1, -1);
                    GTA.Native.Function.Call("SET_CHAR_ANIM_SPEED", ped.Handle, "MISSBERNIE1", "ATTACKER_BEATTHENRUN", 1.00);
                    GTA.Native.Function.Call("SET_CHAR_ANIM_CURRENT_TIME", ped.Handle, "MISSBERNIE1", "ATTACKER_BEATTHENRUN", 0.73);
                     * */
                }
            
                // CPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("cop"), "copm_arrest_ground", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09 | AnimationFlags.Unknown06);

            }

           
            /*
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F4))
            {
                if (this.ped != null && this.ped.Exists())
                {
                    this.ped.PedData.CurrentChase.AllowSuspectVehicles = true;
                    Engine.GUI.HelpBox.Print("Vehicles allowed");
                }
            }
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F5))
            {
                if (this.ped != null && this.ped.Exists())
                {
                    this.ped.PedData.CurrentChase.AllowSuspectWeapons = true;
                    Engine.GUI.HelpBox.Print("Weapons allowed");
                }
            }
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F6))
            {
                if (this.ped != null && this.ped.Exists())
                {
                    this.ped.Delete();
                    this.ped = null;
                }
            }
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F7))
            {
                if (ActionScheduler.SchedulerStyle == ESchedulerStyle.Never)
                {
                    ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Optimized);
                }
                else if (ActionScheduler.SchedulerStyle == ESchedulerStyle.Optimized)
                {
                    ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Performance);
                }
                else if (ActionScheduler.SchedulerStyle == ESchedulerStyle.Performance)
                {
                    ActionScheduler.SetSchedulerStyle(ESchedulerStyle.Never);
                }

                Engine.GUI.HelpBox.Print("Scheduler mode changed to: " + ActionScheduler.SchedulerStyle.ToString());
            }
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F8))
            {

            }
            if (GTA.Game.isGameKeyPressed(GTA.GameKey.Jump))
            {
                //GTA.Native.Function.Call("SHAKE_PLAYERPAD_WHEN_CONTROLLER_DISABLED");
                //GTA.Native.Function.Call("SHAKE_PAD", 0, 5000, 1000f);
                //GTA.Game.DisplayText("JUMPAGE");
                //CPlayer.LocalPlayer.Ped.Task.CarTempAction(Scripting.Native.ECarTempActionType.SlowDownSoftlyTurnRight, 1000);
            }
            if (KeyWatchDog.IsKeyDown(System.Windows.Forms.Keys.F9))
            {
                return;
                foreach (CVehicle poolVehicle in CPlayer.LocalPlayer.Ped.Intelligence.GetVehiclesAround(15, EVehicleSearchCriteria.DriverOnly))
                {
                    if (poolVehicle.Exists())
                    {
                        if (poolVehicle == CPlayer.LocalPlayer.Ped.CurrentVehicle)
                        {
                            continue;
                        }

                        // Check if vehicle can be seen using 30 as fov so we only see vehicles straight infront of the ped
                        if (Common.CanPointBeSeenFromPoint(poolVehicle.Position, CPlayer.LocalPlayer.Ped.CurrentVehicle.Position, CPlayer.LocalPlayer.Ped.CurrentVehicle.Direction, 0.95f))
                        {
                            Scripting.Tasks.TaskSequence taskSequence = new Scripting.Tasks.TaskSequence();

                            TaskParkVehicle taskParkVehicle = new TaskParkVehicle(poolVehicle, EVehicleParkingStyle.RightSideOfRoadOnPavement, 15000);
                            TaskWaitUntilPedIsInVehicle taskWaitUntilPedIsInVehicle = new TaskWaitUntilPedIsInVehicle(CPlayer.LocalPlayer.Ped, poolVehicle, 5000);
                            taskSequence.AddTask(taskParkVehicle);
                            taskSequence.AddTask(taskWaitUntilPedIsInVehicle);
                            taskSequence.AssignTo(poolVehicle.Driver);
                            poolVehicle.AttachBlip();
                            break;
                        }
                    }
                }

                    //if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                    //{
                    //    CVehicle vehicle = CPlayer.LocalPlayer.Ped.CurrentVehicle;

                    //    TaskParkVehicle taskParkVehicle = new TaskParkVehicle(vehicle, EVehicleParkingStyle.RightSideOfRoadOnPavement, 10000);
                    //    taskParkVehicle.AssignTo(CPlayer.LocalPlayer.Ped, ETaskPriority.MainTask);
                    //}

                //GTA.Native.Function.Call("SOUND_CAR_HORN", (GTA.Vehicle)CPlayer.LocalPlayer.Ped.CurrentVehicle, 2147480646);
                //LCPD_First_Response.LCPDFR.LCPDFRSettings.ReadSettings();

                //if (CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode == ELightingMode.LeftRightToCenter)
                //{
                //    CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode = ELightingMode.LeftAndRightChanging;
                //}
                //else if (CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode == ELightingMode.LeftAndRightChanging)
                //{
                //    CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode = ELightingMode.LeftAndRightOnly;
                //}
                //else if (CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode == ELightingMode.LeftAndRightOnly)
                //{
                //    CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode = ELightingMode.LeftAndRightAndCenter;
                //}
                //else if (CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode == ELightingMode.LeftAndRightAndCenter)
                //{
                //    CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenManager.LightingMode = ELightingMode.LeftRightToCenter;
                //}
            }

            //if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            //{
            //    foreach (CVehicle poolVehicle in CPlayer.LocalPlayer.Ped.Intelligence.GetVehiclesAround(10, EVehicleSearchCriteria.All))
            //    {
            //        if (poolVehicle.Exists())
            //        {
            //            poolVehicle.DeleteBlip();
            //            if (poolVehicle == CPlayer.LocalPlayer.Ped.CurrentVehicle)
            //            {
            //                continue;
            //            }
            //        }
            //    }
            //}*/
             
        }

        private void Shutdown()
        {
            
        }
    }

    /// <summary>
    /// For things such as PerFrameDrawing and binding console commands
    /// </summary>
    class ScriptHelper
    {
        public static void BindConsoleCommandS(string command, ConsoleCommandDelegate methodToBindTo)
        {
            LCPDFR_Loader.PublicScript.BindConsoleCommandS(command, methodToBindTo);
        }

        public static void BindConsoleCommandS(string command, string description, ConsoleCommandDelegate methodToBindTo)
        {
            LCPDFR_Loader.PublicScript.BindConsoleCommandS(command, description, methodToBindTo);
        }
    }
}