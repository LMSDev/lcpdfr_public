namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    using Main = LCPD_First_Response.LCPDFR.Main;

    [ScriptInfo("NeverendingRiot", true)]
    internal class NeverendingRiot : GameScript
    {
        private int MaxCops = 1;

        private int MaxRioters = 1;

        /// <summary>
        /// Static instance.
        /// </summary>
        private static NeverendingRiot neverendingRiot;

        /// <summary>
        /// All cops.
        /// </summary>
        private List<CPed> cops;

        /// <summary>
        /// Cop heli
        /// </summary>
        private CVehicle heli;

        /// <summary>
        /// All rioters.
        /// </summary>
        private List<CPed> rioters;

        /// <summary>
        /// Police vehicle.
        /// </summary>
        private CVehicle policeVehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeverendingRiot"/> class.
        /// </summary>
        public NeverendingRiot()
        {
            this.cops = new List<CPed>();
            this.rioters = new List<CPed>();
        }

        public override void Process()
        {
            base.Process();

            GTA.Native.Function.Call("SET_DEAD_PEDS_DROP_WEAPONS", false);

            if (Input.KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.Oemplus))
            {
                MaxCops += 2;
                GTA.Game.DisplayText("MaxCops: " + MaxCops.ToString());
            }
            if (Input.KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.OemMinus))
            {
                MaxCops -= 2;
                GTA.Game.DisplayText("MaxCops: " + MaxCops.ToString());
            }

            if (Input.KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.PageUp))
            {
                MaxRioters += 2;
                GTA.Game.DisplayText("MaxRioters: " + MaxRioters.ToString());
            }
            if (Input.KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.PageDown))
            {
                MaxRioters -= 2;
                GTA.Game.DisplayText("MaxRioters: " + MaxRioters.ToString());
            }

            if (Input.KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.NumPad9))
            {
                if (!CPlayer.LocalPlayer.Ped.IsSittingInVehicle(this.heli))
                {
                    CPlayer.LocalPlayer.Ped.WarpIntoVehicle(this.heli, VehicleSeat.RightFront);
                }
            }

            if (this.policeVehicle == null || !this.policeVehicle.Exists())
            {
                this.policeVehicle = new CVehicle("POLICE2", new Vector3(-168.42f, -397.87f, 14.37f), EVehicleGroup.Police);

                this.policeVehicle.Heading = 90;
                this.policeVehicle.AllowSirenWithoutDriver = true;
                this.policeVehicle.SirenActive = true;
                this.ContentManager.AddVehicle(this.policeVehicle);
            }

            if (this.heli == null || !this.heli.Exists())
            {
                this.heli = new CVehicle("POLMAV", new Vector3(-262.77f, -253.47f, 58.81f), EVehicleGroup.Police);

                if (this.heli.Exists())
                {
                    this.heli.Heading = 213;
                    this.heli.SetHeliBladesFullSpeed();
                    this.ContentManager.AddVehicle(this.heli);
                }
            }

            if (this.heli.Exists() && this.heli.IsAlive)
            {
                if (this.rioters != null)
                {
                    CPed target = null;
                    foreach (CPed rioter in this.rioters)
                    {
                        if (rioter.Exists() && rioter.IsAliveAndWell)
                        {
                            target = rioter;
                            break;
                        }
                    }

                    if (target != null)
                    {
                        CVehicle.SetHeliSearchlightTarget(target);
                        this.heli.AVehicle.HeliSearchlightOn = true;
                    }
                    else
                    {
                        CVehicle.SetHeliSearchlightTarget(null);
                    }
                }

                if (!this.heli.HasDriver)
                {
                    CPed pilot = new CPed("M_Y_NHELIPILOT", this.heli.Position, EPedGroup.Cop);
                    if (pilot.Exists())
                    {
                        pilot.AlwaysFreeOnDeath = true;
                        pilot.AttachBlip().Icon = BlipIcon.Activity_HeliTour;
                        pilot.DontRemoveBlipOnDeath = false;
                        pilot.BlockPermanentEvents = false;
                        pilot.WarpIntoVehicle(this.heli, VehicleSeat.Driver);

                        Route route = new Route();
                        route.AddWaypoint(new Vector3(-209.01f, -334.15f, 45.38f));
                        route.AddWaypoint(new Vector3(-171.44f, -388.40f, 43.83f));
                        route.AddWaypoint(new Vector3(-137.52f, -450.30f, 29.33f));
                        route.AddWaypoint(new Vector3(-137.85f, -491.95f, 29.85f));
                        route.AddWaypoint(new Vector3(-156.88f, -482.25f, 32.72f));
                        route.AddWaypoint(new Vector3(-170.97f, -441.30f, 36.24f));
                        route.AddWaypoint(new Vector3(-199.55f, -379.07f, 36.65f));

                        // Add repeated route
                        TaskHeliFollowRoute taskHeliFollowRoute = new TaskHeliFollowRoute(this.heli, route, true);
                        taskHeliFollowRoute.Speed = 10f;
                        taskHeliFollowRoute.StopAtWaypoints = true;
                        taskHeliFollowRoute.StopTime = 8000;
                        taskHeliFollowRoute.AssignTo(pilot, ETaskPriority.MainTask);
                        this.ContentManager.AddPed(pilot);

                        this.heli.SetHeliBladesFullSpeed();
                        this.heli.Stabilize();
                    }

                    // Create gunners
                    for (int i = 0; i < 2; i++)
                    {
                        CPed gunner = new CPed("M_Y_SWAT", this.heli.Position, EPedGroup.Cop);
                        if (gunner.Exists())
                        {
                            gunner.Accuracy = 100;
                            gunner.Armor = 200;
                            gunner.AlwaysFreeOnDeath = true;
                            gunner.WillDoDrivebys = true;
                            gunner.SenseRange = 450f;

                            if (this.heli.IsSeatFree(VehicleSeat.LeftRear))
                            {
                                gunner.WarpIntoVehicle(this.heli, VehicleSeat.LeftRear);
                            }
                            else
                            {
                                gunner.WarpIntoVehicle(this.heli, VehicleSeat.RightRear);
                            }

                            gunner.PedData.DefaultWeapon = Weapon.Rifle_M4;
                            gunner.EnsurePedHasWeapon();
                            gunner.Task.Wait(100000);
                            this.ContentManager.AddPed(gunner);
                        }
                    }
                }
            }

            int copsAlive = 0;
            foreach (CPed ped in this.cops)
            {
                if (ped.Exists() && ped.IsAliveAndWell)
                {
                    copsAlive++;
                }
            }

            if (copsAlive < this.MaxCops)
            {
                for (int i = 0; i < this.MaxCops - copsAlive; i++)
                {
                    // Get spawnpoint
                    Vector3 spawnPoint = Vector3.Zero;

                    int randomValue = Common.GetRandomValue(0, 2);
                    if (randomValue == 0)
                    {
                        spawnPoint = new Vector3(-161.93f, -345.57f, 14.31f);
                    }
                    else
                    {
                        spawnPoint = new Vector3(-196.93f, -345.06f, 14.31f);
                    }

                    CPed cop = new CPed("M_Y_SWAT", spawnPoint.Around(1f), EPedGroup.Cop);
                    if (cop.Exists())
                    {
                        cop.PedData.DefaultWeapon = Weapon.Rifle_M4;
                        cop.EnsurePedHasWeapon();

                        cop.Accuracy = 100;
                        cop.Armor = 200;
                        cop.AlwaysFreeOnDeath = true;
                        cop.AttachBlip().Friendly = true;
                        cop.DontRemoveBlipOnDeath = false;
                        cop.BlockPermanentEvents = false;

                        Route route = new Route();
                        route.AddWaypoint(this.policeVehicle.Position);
                        route.AddWaypoint(new Vector3(-162.27f, -432.67f, 14.73f));
                        route.AddWaypoint(new Vector3(-147.52f, -466.06f, 14.73f));
                        TaskFightToPoint taskFightToPoint = new TaskFightToPoint(route);
                        taskFightToPoint.AssignTo(cop, ETaskPriority.MainTask);

                        GTA.Native.Function.Call("SET_CHAR_DROPS_WEAPONS_WHEN_DEAD", (GTA.Ped)cop, false);
                        GTA.Native.Function.Call("SET_DEATH_WEAPONS_PERSIST", (GTA.Ped)cop, false);
                        this.ContentManager.AddPed(cop);
                    }

                    this.cops.Add(cop);
                }
            }

            int riotersAlive = 0;
            foreach (CPed ped in this.rioters)
            {
                if (ped.Exists() && ped.IsAliveAndWell)
                {
                    riotersAlive++;
                }
            }

            if (riotersAlive < this.MaxRioters)
            {
                for (int i = 0; i < this.MaxRioters - riotersAlive; i++)
                {
                    // Get spawnpoint
                    //Vector3 spawnPoint = new Vector3(-147.01f, -452.36f, 15.24f);
                    Vector3 spawnPoint = new Vector3(-149.01f, -513.36f, 14.73f);

                    CPed cop = new CPed("M_Y_HARLEM_01", spawnPoint.Around(1f), EPedGroup.Criminal);
                    if (cop.Exists())
                    {
                        cop.PedData.DefaultWeapon = Weapon.Handgun_Glock;
                        if (Common.GetRandomBool(0, 3, 1))
                        {
                            cop.PedData.DefaultWeapon = Weapon.Thrown_Molotov;
                        }
                        else if (Common.GetRandomBool(0, 6, 1))
                        {
                            cop.PedData.DefaultWeapon = Weapon.Rifle_AK47;
                        }
                        else if (Common.GetRandomBool(0, 10, 1))
                        {
                            cop.PedData.DefaultWeapon = Weapon.Heavy_RocketLauncher;
                        }

                        cop.EnsurePedHasWeapon();

                        cop.Health = 30;
                        cop.AlwaysFreeOnDeath = true;
                        cop.AttachBlip();
                        cop.DontRemoveBlipOnDeath = false;
                        cop.RelationshipGroup = RelationshipGroup.Criminal;
                        cop.BlockPermanentEvents = false;

                        Route route = new Route();
                        route.AddWaypoint(new Vector3(-147.52f, -466.06f, 14.73f));
                        route.AddWaypoint(new Vector3(-162.27f, -432.67f, 14.73f));
                        route.AddWaypoint(this.policeVehicle.Position);
                        TaskFightToPoint taskFightToPoint = new TaskFightToPoint(route);
                        taskFightToPoint.AssignTo(cop, ETaskPriority.MainTask);

                        GTA.Native.Function.Call("SET_CHAR_DROPS_WEAPONS_WHEN_DEAD", (GTA.Ped)cop, false);
                        GTA.Native.Function.Call("SET_DEATH_WEAPONS_PERSIST", (GTA.Ped)cop, false);
                        this.ContentManager.AddPed(cop);
                    }

                    this.rioters.Add(cop);
                }
            }
        }

        [ConsoleCommand("riot", true)]
        private static void riot(GTA.ParameterCollection parameterCollection)
        {
            if (neverendingRiot != null)
            {
                Log.Debug("Stopping...", "NeverendingRiot");
                Main.ScriptManager.StopScript("NeverendingRiot");
                neverendingRiot = null;
                return;
            }

            neverendingRiot = Main.ScriptManager.StartScript<NeverendingRiot>("NeverendingRiot");
        }
    }
}