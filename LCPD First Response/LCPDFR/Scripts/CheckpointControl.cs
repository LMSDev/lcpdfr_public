namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.QuickActionMenu;

    using Main = LCPD_First_Response.LCPDFR.Main;
    using Object = GTA.Object;

    /// <summary>
    /// The LCPDFR checkpoint control.
    /// </summary>
    [ScriptInfo("CheckpointControl", true)]
    internal class CheckpointControl : GameScript
    {
        // Idea is to set up a checkpoint where all vehicles will stop and can be waved through one by one after checking their papers.
        // add random speeding etc. for some action

        /// <summary>
        /// Whether the checkpoint is active.
        /// </summary>
        private bool isActive;

        /// <summary>
        /// Whether the checkpoint is being set up.
        /// </summary>
        private bool isSettingUp;

        /// <summary>
        /// The camera used to set up the checkpoint.
        /// </summary>
        private Camera setupCamera;

        /// <summary>
        /// The position where the checkpoint set up was started at.
        /// </summary>
        private Vector3 startingPosition;

        /// <summary>
        /// The vehicle that is used temporarily to find a location for the checkpoint.
        /// </summary>
        private CVehicle tempPlacementVehicle;

        /// <summary>
        /// The checkpoint vehicle;
        /// </summary>
        private CVehicle checkpointVehicle;

        /// <summary>
        /// The slowed down vehicles.
        /// </summary>
        private List<CVehicle> slowedDownVehicles;

        /// <summary>
        /// Helper object to measure distances.
        /// </summary>
        private GTA.Object distanceHelper;

        /// <summary>
        /// The cones.
        /// </summary>
        private List<GTA.Object> cones;

        /// <summary>
        /// The police car model used.
        /// </summary>
        private CModel model;

        /// <summary>
        /// Animations used for waving cars through
        /// </summary>
        private readonly string[,] waveAnims = new string[,] { { "gestures@male", "indicate_back" }, { "gestures@niko", "indicate_back" }, { "gestures@female", "indicate_bwd" } };
        
        /// <summary>
        /// Animations used for stopping cars
        /// </summary>
        private readonly string[] stopAnims = new string[] { "gestures@male", "gestures@female", "gestures@niko" };

        private List<QuickActionMenuItemBase> addedItems; 

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointControl"/> class.
        /// </summary>
        public CheckpointControl()
        {
            this.cones = new List<Object>();
            this.slowedDownVehicles = new List<CVehicle>();
            this.model = new CModel("POLICE2");
            this.addedItems = new List<QuickActionMenuItemBase>();
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.CheckpointControlStart))
            {
                if (!this.isActive)
                {
                    this.StartSetupUpCheckpoint();
                }
                else
                {
                    this.EndCheckpointControl();
                }
            }

            if (this.isSettingUp)
            {
                float step = 1.0f;

                if (KeyHandler.IsKeyboardKeyStillDown(Keys.LShiftKey))
                {
                    step = 0.2f;
                }

                bool changeRotation = KeyHandler.IsKeyStillDown(ELCPDFRKeys.CheckpointControlRotate);

                // Be able to move the vehicle
                if (KeyHandler.IsKeyDown(ELCPDFRKeys.CheckpointControlUp))
                {
                    Vector3 position = this.tempPlacementVehicle.GetOffsetPosition(new Vector3(0, step, 0));
                    if (position.DistanceTo2D(this.startingPosition) < 6f)
                    {
                        this.tempPlacementVehicle.Position = position;
                    }
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.CheckpointControlDown))
                {
                    Vector3 position = this.tempPlacementVehicle.GetOffsetPosition(new Vector3(0, -step, 0));
                    if (position.DistanceTo2D(this.startingPosition) < 6f)
                    {
                        this.tempPlacementVehicle.Position = position;
                    }
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.CheckpointControlLeft))
                {
                    if (changeRotation)
                    {
                        this.tempPlacementVehicle.Heading -= 10f * step;
                    }
                    else
                    {
                        Vector3 position = this.tempPlacementVehicle.GetOffsetPosition(new Vector3(-step, 0, 0));
                        if (position.DistanceTo2D(this.startingPosition) < 6f)
                        {
                            this.tempPlacementVehicle.Position = position;
                        }
                    }
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.CheckpointControlRight))
                {
                    if (changeRotation)
                    {
                        this.tempPlacementVehicle.Heading += 10f * step;
                    }
                    else
                    {
                        Vector3 position = this.tempPlacementVehicle.GetOffsetPosition(new Vector3(step, 0, 0));
                        if (position.DistanceTo2D(this.startingPosition) < 6f)
                        {
                            this.tempPlacementVehicle.Position = position;
                        }
                    }
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.CheckpointControlConfirm))
                {
                    // Unpause and reset camera
                    Game.Unpause();

                    DelayedCaller.Call(
                        delegate
                        {
                            Natives.SetInterpFromScriptToGame(true, 2500);
                            this.setupCamera.Deactivate();
                            this.tempPlacementVehicle.Delete();
                        },
                        this,
                        100);

                    this.isSettingUp = false;

                    Vector3 position = this.tempPlacementVehicle.Position;
                    float heading = this.tempPlacementVehicle.Heading;
                    this.tempPlacementVehicle.Delete();

                    // Spawn vehicle and assisting officer
                    this.checkpointVehicle = new CVehicle("POLICE2", position, EVehicleGroup.Police);
                    this.checkpointVehicle.Heading = heading;
                    this.checkpointVehicle.SirenActive = true;

                    // Spawn cones on the left side
                    // Get left outer corner of vehicle
                    Vector3 dimensions = this.checkpointVehicle.Model.GetDimensions();
                    float vehicleWidth = dimensions.X;
                    float vehicleLength = dimensions.Y;
                    Vector3 leftOuterCorner = this.checkpointVehicle.GetOffsetPosition(new Vector3((-vehicleWidth / 2) - 1, (vehicleLength / 2) - 0.8f, 0));
                    leftOuterCorner = new Vector3(leftOuterCorner.X, leftOuterCorner.Y, World.GetGroundZ(leftOuterCorner, GroundType.NextBelowCurrent));

                    // Preload cone model
                    CModel coneModel = new CModel("CJ_CONE_SM");
                    coneModel.LoadIntoMemory(false);

                    Object cone = World.CreateObject(coneModel, leftOuterCorner);
                    cone.Heading = this.checkpointVehicle.Heading;
                    this.PlaceObjectOnGround(cone);
                    this.cones.Add(cone);

                    cone = World.CreateObject(coneModel, cone.GetOffsetPosition(new Vector3(-0.9f, 0, 0)));
                    cone.Heading = this.checkpointVehicle.Heading;
                    this.PlaceObjectOnGround(cone);
                    this.cones.Add(cone);

                    cone = World.CreateObject(coneModel, cone.GetOffsetPosition(new Vector3(-0.9f, -0.8f, 0)));
                    cone.Heading = this.checkpointVehicle.Heading;
                    this.PlaceObjectOnGround(cone);
                    this.cones.Add(cone);

                    cone = World.CreateObject(coneModel, cone.GetOffsetPosition(new Vector3(-0.9f, -0.8f, 0)));
                    cone.Heading = this.checkpointVehicle.Heading;
                    this.PlaceObjectOnGround(cone);
                    this.cones.Add(cone);

                    cone = World.CreateObject(coneModel, cone.GetOffsetPosition(new Vector3(-0.9f, -0.8f, 0)));
                    cone.Heading = this.checkpointVehicle.Heading;
                    this.PlaceObjectOnGround(cone);
                    this.cones.Add(cone);

                    cone = World.CreateObject(coneModel, cone.GetOffsetPosition(new Vector3(-0.9f, -0.8f, 0)));
                    cone.Heading = this.checkpointVehicle.Heading;
                    this.PlaceObjectOnGround(cone);
                    this.cones.Add(cone);

                    Vector3 closestNodePosition = Vector3.Zero;
                    float closestNodeHeading = 0f;
                    if (CVehicle.GetClosestCarNodeWithHeading(this.checkpointVehicle, ref closestNodePosition, ref closestNodeHeading))
                    {
                        this.distanceHelper = World.CreateObject(coneModel, closestNodePosition);
                        this.distanceHelper.Heading = closestNodeHeading;
                        this.distanceHelper.Visible = false;
                        this.distanceHelper.Collision = false;
                    }
                }
            }

            if (this.isActive && !this.isSettingUp)
            {
                // Slow down whole traffic on this street
                string street = World.GetStreetName(this.checkpointVehicle.Position);

                // Split
                if (street.Contains(","))
                {
                    street = street.Substring(0, street.IndexOf(","));
                }

                foreach (CPed ped in CPed.GetPedsAround(35f, EPedSearchCriteria.AmbientPed, this.checkpointVehicle.Position))
                {
                    //if (!ped.HasBlip)
                    //{
                    //    ped.AttachBlip();
                    //}
                    //else
                    //{
                    //    ped.Blip.Color = BlipColor.DarkTurquoise;
                    //}

                    // If on same street, reduce cruise speed
                    bool isOnSameStreet = World.GetStreetName(ped.Position).StartsWith(street);

                    if (isOnSameStreet && ped.IsInVehicle)
                    {
                        //ped.Blip.Color = BlipColor.LightOrange;

                        CVehicle vehicle = ped.CurrentVehicle;
                        if (!vehicle.Flags.HasFlag(EVehicleFlags.CanBypassCheckpoint))
                        {
                            float distance = vehicle.Position.DistanceTo(this.checkpointVehicle.Position);

                            // Workaround for bridges or large roads, we don't want traffic from lanes too far away
                            float distanceFront = vehicle.GetOffsetPosition(new Vector3(0, distance, 0)).DistanceTo(this.checkpointVehicle.Position);
                            float distanceBack = vehicle.GetOffsetPosition(new Vector3(0, -distance, 0)).DistanceTo(this.checkpointVehicle.Position);
                            if (distanceFront < 8f || distanceBack < 8f)
                            {
                                GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)ped, 2.5f);
                                if (!this.slowedDownVehicles.Contains(vehicle))
                                {
                                    this.slowedDownVehicles.Add(vehicle);
                                }

                                // If very close to checkpoint, stop completely
                                if (distance < 14f)
                                {
                                    //ped.Blip.Color = BlipColor.LightRed;
                                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                                    {
                                        ped.Task.CarTempAction(ECarTempActionType.SlowDownSoftly, 2000);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (CVehicle slowedDownVehicle in this.slowedDownVehicles)
                {
                    if (!slowedDownVehicle.Exists() || !slowedDownVehicle.HasDriver || slowedDownVehicle.Driver == null || !slowedDownVehicle.Driver.Exists())
                    {
                        continue;
                    }

                    float distance = slowedDownVehicle.Position.DistanceTo(this.checkpointVehicle.Position);
                    if (distance > 35)
                    {
                        slowedDownVehicle.Driver.Task.ClearAll();
                        slowedDownVehicle.Driver.NoLongerNeeded();
                        GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)slowedDownVehicle.Driver, 15f);
                    }
                    else if (slowedDownVehicle.Flags.HasFlag(EVehicleFlags.CanBypassCheckpoint) && distance > 15)
                    {
                        slowedDownVehicle.Driver.Task.ClearAll();
                        slowedDownVehicle.Driver.NoLongerNeeded();
                        GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)slowedDownVehicle.Driver, 15f);
                    }
                }
            }
        }

        private void WaveDriverThrough()
        {
            // Targetting vehicles works by utilizing the aim marker
            AimMarker aimMarker = LCPDFR.Main.ScriptManager.GetRunningScriptInstances("AimMarker")[0] as AimMarker;
            if (aimMarker == null)
            {
                Log.Error("Process: AimMarker instance shut down. Did it crash?", this);
                return;
            }
            else
            {
                if (aimMarker.IsBeingDrawn && aimMarker.HasTarget)
                {
                    if (aimMarker.TargetedEntity != null && aimMarker.TargetedEntity.Exists())
                    {
                        if (aimMarker.TargetedEntity.EntityType == EEntityType.Vehicle)
                        {
                            // Upcast the targeted entity
                            CVehicle vehicle = aimMarker.TargetedEntity as CVehicle;

                            int random = Common.GetRandomValue(0, this.waveAnims.GetLength(0));
                            //CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody(waveAnims[random, 1], waveAnims[random, 0], 4.0f, false);
                            CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet(waveAnims[random, 0]), waveAnims[random, 1], 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);

                            vehicle.Flags |= EVehicleFlags.CanBypassCheckpoint;
                            //vehicle.AttachBlip().Color = BlipColor.DarkGreen;
                            this.DriveThroughCheckpoint(vehicle, vehicle.Driver);
                        }
                    }
                }
            }
        }

        private void MakeDriverStop()
        {
            // Targetting vehicles works by utilizing the aim marker
            AimMarker aimMarker = LCPDFR.Main.ScriptManager.GetRunningScriptInstances("AimMarker")[0] as AimMarker;
            if (aimMarker == null)
            {
                Log.Error("Process: AimMarker instance shut down. Did it crash?", this);
                return;
            }
            else
            {
                if (aimMarker.IsBeingDrawn && aimMarker.HasTarget)
                {
                    if (aimMarker.TargetedEntity != null && aimMarker.TargetedEntity.Exists())
                    {
                        if (aimMarker.TargetedEntity.EntityType == EEntityType.Vehicle)
                        {
                            // Upcast the targeted entity
                            CVehicle vehicle = aimMarker.TargetedEntity as CVehicle;
                            if (vehicle.Flags.HasFlag(EVehicleFlags.ForcedToStop))
                            {
                                int random = Common.GetRandomValue(0, this.waveAnims.GetLength(0));
                                //CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody(waveAnims[random, 1], waveAnims[random, 0], 4.0f, false);

                                CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet(waveAnims[random, 0]), waveAnims[random, 1], 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);

                                vehicle.Driver.Task.ClearAll();

                                if (vehicle.Driver.Intelligence.TaskManager.IsTaskActive(ETaskID.WaitUntilPedIsInVehicle))
                                {
                                    TaskWaitUntilPedIsInVehicle taskWaitUntilPedIsInVehicle = (TaskWaitUntilPedIsInVehicle)vehicle.Driver.Intelligence.TaskManager.FindTaskWithID(ETaskID.WaitUntilPedIsInVehicle);
                                    taskWaitUntilPedIsInVehicle.MakeAbortable(vehicle.Driver);
                                }

                                //vehicle.AttachBlip().Color = BlipColor.LightOrange;
                            }
                            else
                            {
                                //CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("negative", Common.GetRandomCollectionValue<string>(stopAnims) ,4.0f, false);
                                CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet(Common.GetRandomCollectionValue<string>(stopAnims)), "negative", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);

                                vehicle.Flags |= EVehicleFlags.ForcedToStop;
                                //vehicle.AttachBlip().Color = BlipColor.Red;
                                if (!vehicle.Driver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                                {
                                    TaskWaitUntilPedIsInVehicle taskWaitUntilPedIsInVehicle = new TaskWaitUntilPedIsInVehicle(CPlayer.LocalPlayer.Ped, vehicle);
                                    taskWaitUntilPedIsInVehicle.AssignTo(vehicle.Driver, ETaskPriority.MainTask);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Makes <paramref name="driver"/> drive through the checkpoint slowly and tries to prevent collision with cop vehicles.
        /// </summary>
        /// <param name="vehicle">The vehicle,</param>
        /// <param name="driver">The driver.</param>
        private void DriveThroughCheckpoint(CVehicle vehicle, CPed driver)
        {
            // Check which side we are on
            EStreetSide ourSide = vehicle.GetSideOfStreetVehicleIsAt();

            // Check side of police vehicle, which is a little more difficult due to the heading
            EStreetSide policeSide = EStreetSide.None;

            // Check which distance is the closest to find street side
            float distanceFront = vehicle.GetOffsetPosition(new Vector3(0, 12, 0)).DistanceTo(this.checkpointVehicle.Position);
            float distanceLeftFront = vehicle.GetOffsetPosition(new Vector3(-1, 12, 0)).DistanceTo(this.checkpointVehicle.Position);
            float distanceRightFront = vehicle.GetOffsetPosition(new Vector3(1, 12, 0)).DistanceTo(this.checkpointVehicle.Position);

            // If front is lowest diff
            if (distanceFront < distanceLeftFront && distanceFront < distanceRightFront)
            {
                policeSide = ourSide;
            }

            // If left is lowest diff
            if (distanceLeftFront < distanceFront && distanceLeftFront < distanceRightFront)
            {
                policeSide = EStreetSide.Left;
            }

            // If Right is lowest diff
            if (distanceRightFront < distanceFront && distanceRightFront < distanceLeftFront)
            {
                policeSide = EStreetSide.Right;
            }

            // If not on the same side, proceed as usual
            if (policeSide != ourSide)
            {
                DelayedCaller.Call(
                    delegate
                        {
                            if (vehicle.Exists() && vehicle.HasDriver)
                            {
                                vehicle.Driver.Task.ClearAll();
                                vehicle.Driver.NoLongerNeeded();
                                GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)vehicle.Driver, 5f);
                            }
                        },
                    this,
                    1500);
            }
            else
            {
                // On the same side, so possible block. Try to steer around
                if (ourSide == EStreetSide.Left)
                {
                    // Get point on the right
                    Vector3 positionRight = vehicle.GetOffsetPosition(new Vector3(5, 12, 0));

                    DelayedCaller.Call(
                        delegate
                        {
                            if (vehicle.Exists() && vehicle.HasDriver)
                            {
                                vehicle.Driver.Task.ClearAll();
                                vehicle.Driver.NoLongerNeeded();
                                GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)vehicle.Driver, 5f);
                                vehicle.Driver.Task.DriveTo(positionRight, 5f, false, false);
                            }
                        },
                        this,
                        1500);
                }
                else if (ourSide == EStreetSide.Right)
                {
                    // Get point on the left
                    Vector3 positionLeft = vehicle.GetOffsetPosition(new Vector3(-5, 12, 0));

                    DelayedCaller.Call(
                        delegate
                        {
                            if (vehicle.Exists() && vehicle.HasDriver)
                            {
                                vehicle.Driver.Task.ClearAll();
                                vehicle.Driver.NoLongerNeeded();
                                GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)vehicle.Driver, 5f);
                                vehicle.Driver.Task.DriveTo(positionLeft, 5f, false, false);
                            }
                        },
                        this,
                        1500);
                }
            }
        }

        private void StartSetupUpCheckpoint()
        {
            // Preload model
            this.model.LoadIntoMemory(false);
            if (!this.model.IsInMemory)
            {
                Log.Warning("StartSetupUpCheckpoint: Failed to load model. Please verify " + this.model.ModelInfo.Name + " is a valid model and there's enough memory available", this);
                return;
            }

            // Get closest road position
            Vector3 closestNodePosition = Vector3.Zero;
            float closestNodeHeading = 0f;
            if (CVehicle.GetClosestCarNodeWithHeading(CPlayer.LocalPlayer.Ped.Position, ref closestNodePosition, ref closestNodeHeading))
            {
                this.tempPlacementVehicle = new CVehicle(model, closestNodePosition, EVehicleGroup.Police);
                this.tempPlacementVehicle.Visible = false;
                this.tempPlacementVehicle.FreezePosition = true;
                this.tempPlacementVehicle.Collision = false;

                // Calculate heading
                closestNodeHeading += 90;
                if (closestNodeHeading > 360)
                {
                    closestNodeHeading = closestNodeHeading - 360;
                }

                this.tempPlacementVehicle.Heading = closestNodeHeading;

                // Switch camera to top down view
                GTA.Camera camera = new GTA.Camera();
                camera.Position = this.tempPlacementVehicle.GetOffsetPosition(new Vector3(0, 0, 20));
                camera.Rotation = this.tempPlacementVehicle.Rotation;
                camera.Heading = this.tempPlacementVehicle.Heading;
                camera.FOV = Game.CurrentCamera.FOV;
                Natives.SetInterpFromGameToScript(true, 2500);
                camera.Activate();
                camera.LookAt((GTA.Vehicle)this.tempPlacementVehicle);
                this.setupCamera = camera;
                this.startingPosition = this.tempPlacementVehicle.Position;
                this.isSettingUp = true;
                this.slowedDownVehicles = new List<CVehicle>();

                DelayedCaller.Call(
                    delegate
                        {
                            this.tempPlacementVehicle.Visible = true;
                            Game.Pause();

                            TextHelper.PrintFormattedHelpBox("Place the vehicle to block parts of the road.");
                        }, 
                        this, 
                        2500);
            }

            QuickActionMenuGroup generalGroup = Main.QuickActionMenu.GetGroupByType(QuickActionMenuGroup.EMenuGroup.General);
            if (generalGroup != null)
            {
                this.addedItems.Add(Main.QuickActionMenu.AddEntry("WAVE DRIVER THROUGH", generalGroup, this.WaveDriverThrough));
                this.addedItems.Add(Main.QuickActionMenu.AddEntry("TELL DRIVER TO STOP", generalGroup, this.MakeDriverStop));
            }

            this.isActive = true;
        }

        private void PlaceObjectOnGround(GTA.Object obj)
        {
            Vector3 position = obj.Position;
            position = World.GetGroundPosition(position, GroundType.NextBelowCurrent);
            obj.Position = position;
        }

        private void EndCheckpointControl()
        {
            if (this.isSettingUp)
            {
                Game.Unpause();

                DelayedCaller.Call(
                    delegate
                        {
                            Natives.SetInterpFromScriptToGame(true, 2500);
                            this.setupCamera.Deactivate();
                            this.tempPlacementVehicle.Delete();
                        },
                    this,
                    100);
            }
            else
            {
                if (this.checkpointVehicle != null && this.checkpointVehicle.Exists())
                {
                this.checkpointVehicle.Delete();
                    }
            }

            foreach (CVehicle slowedDownVehicle in this.slowedDownVehicles)
            {
                if (slowedDownVehicle.Exists() && slowedDownVehicle.HasDriver)
                {
                    slowedDownVehicle.Driver.Task.ClearAll();
                    slowedDownVehicle.Driver.NoLongerNeeded();
                    GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)slowedDownVehicle.Driver, 15f);
                }
            }

            foreach (Object cone in cones)
            {
                if (cone.Exists())
                {
                    cone.Delete();
                }
            }

            foreach (QuickActionMenuItemBase itemBase in addedItems)
            {
                Main.QuickActionMenu.RemoveEntry(itemBase);
            }

            this.isActive = false;
            this.isSettingUp = false;
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            this.EndCheckpointControl();
        }
    }
}