namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Collections.Generic;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using GTA;
    using System;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using System.Linq;

    /// <summary>
    /// Grabbing test
    /// </summary>
    [ScriptInfo("Grab", true)]
    internal class Grab : GameScript
    {

        /// <summary>
        /// The ped being grabbed
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The last ped to be grabbed
        /// </summary>
        private CPed oldPed;

        /// <summary>
        /// Whether or not the last ped to be grabbed has had their ragdoll impact with player set back to normal
        /// </summary>
        private bool oldPedRagdollSet;

        /// <summary>
        /// Whether or not the player is grabbing anyone
        /// </summary>
        public static bool grabbing;

        /// <summary>
        /// The attach object rotation
        /// </summary>
        private Vector3 rotation = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// The attach object position for when the ped is idling
        /// </summary>
        private Vector3 positionIdle = new Vector3(0.28f, 0.4f, 0);
        
        /// <summary>
        /// The attach object position for when the ped is moving
        /// </summary>
        private Vector3 positionMoving = new Vector3(0.24f, 0.42f, 0);

        /// <summary>
        /// The attach object
        /// </summary>
        private GTA.Object grabObject;

        /// <summary>
        /// Animation set for idling for the ped being grabbed
        /// </summary>
        private AnimationSet animationSetIdle = new AnimationSet("move_m@h_cuffed");

        /// <summary>
        /// Animation ses for moving for the ped being grabbed
        /// </summary>
        private AnimationSet animationSetMoving = new AnimationSet("move_m@h_cuffed");

        /// <summary>
        /// The player's anim group before grabbing
        /// </summary>
        private string playerAnimGroup;

        /// <summary>
        /// The grabbing animation
        /// </summary>
        private string grabbingAnimation = "walk_hold";

        /// <summary>
        /// The grabbing animation set
        /// </summary>
        private AnimationSet grabbingAnimationSet = new AnimationSet("amb@umbrella_hold");

        /// <summary>
        /// Whether the animation task was running.
        /// </summary>
        private bool hadAnimationTaskRunning;

        /// <summary>
        /// Whether or not the helpbox for releasing a ped has been shown
        /// </summary>
        private bool hasReleasingHelpBeenShown;

        /// <summary>
        /// Whether or not the helpbox for grabbing a ped has been shown
        /// </summary>
        private bool hasGrabbingHelpBeenShown;

        /// <summary>
        /// Whether or not the helpbox for seating a ped in car has been shown
        /// </summary>
        private bool hasSeatingHelpBeenShown;

        /// <summary>
        /// Whether or not the helpbox for opening the door to seat a ped in car has been shown
        /// </summary>
        private bool hasDoorHelpBeenShown;

        /// <summary>
        /// Whether or not the ped and player are idle for the first time
        /// </summary>
        private bool firstIdle;

        /// <summary>
        /// If the ped was required for a mission before being grabbed
        /// </summary>
        private bool wasPedRequiredForMission;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hardcore"/> class.
        /// </summary>
        public Grab()
        {
            // Nothing here, go away!
        }

        /// <summary>
        /// Releases the currently grabbed ped
        /// </summary>
        /// <param name="fullRelease">Whether or not the ped should be marked as no longer needed if appropriate</param>
        private void Release(bool fullRelease = false, bool shutdown = false)
        {
            // This basically just undos the stuff we did to the player and ped when we called StartGrab
            grabbing = false;

            if (ped != null)
            {
                ped.IsGrabbed = false;
            }

            oldPed = ped;
            oldPedRagdollSet = false;

            if (grabObject != null && grabObject.Exists())
            {
                grabObject.Delete();
            }

            if (!string.IsNullOrEmpty(this.playerAnimGroup))
            {
                CPlayer.LocalPlayer.AnimGroup = playerAnimGroup;
            }

            CPlayer.LocalPlayer.Ped.CanSwitchWeapons = true;
            CPlayer.LocalPlayer.Ped.BlockGestures = false;
            CPlayer.LocalPlayer.Ped.APed.DisableVehicleEntry(false);

            if (CPlayer.LocalPlayer.Ped.Animation.isPlaying(grabbingAnimationSet, grabbingAnimation))
            {
                CPlayer.LocalPlayer.Ped.Task.ClearSecondary();
            }

            GTA.Native.Function.Call("DISABLE_PLAYER_SPRINT", Game.LocalPlayer, false);
            GTA.Native.Function.Call("DISABLE_PLAYER_LOCKON", Game.LocalPlayer, false);
            GTA.Native.Function.Call("SET_PLAYER_CAN_USE_COVER", Game.LocalPlayer, true);
            CPlayer.LocalPlayer.Ped.APed.DisableCrouch(false);

            if (this.ped != null && this.ped.Exists())
            {
                ped.Detach();
                ped.Task.ClearAll();
                ped.Task.StandStill(1);
                ped.BlockPermanentEvents = false;
                ped.BlockGestures = false;
                ped.WillUseCover(true);
                GTA.Native.Function.Call("SET_CHAR_NEVER_TARGETTED", ped.Handle, false);

                if (this.hadAnimationTaskRunning)
                {
                    TaskPlayAnimationAndRepeat taskPlayAnimationAndRepeat = new TaskPlayAnimationAndRepeat("idle", "move_m@h_cuffed", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                    taskPlayAnimationAndRepeat.AssignTo(this.ped, ETaskPriority.MainTask);
                }
            }
 
            if (fullRelease || shutdown)
            {
                if (this.ped != null && this.ped.Exists())
                {
                    if (!wasPedRequiredForMission)
                    {
                        ped.IsRequiredForMission = false;
                        ped.NoLongerNeeded();
                    }

                    if (shutdown) ped.DontActivateRagdollFromPlayerImpact = false;
                }
            }
        }

        /// <summary>
        /// Starts grabbing a ped
        /// </summary>
        /// <param name="grabPed">The ped</param>
        private void StartGrabbing(CPed grabPed)
        {
            // Set the currently grabbed ped to be this one
            ped = grabPed;

            if (ped.Exists())
            {
                // Set up the grabbing flags and everything
                ped.IsGrabbed = true;
                wasPedRequiredForMission = ped.IsRequiredForMission;

                hasSeatingHelpBeenShown = false;
                hasReleasingHelpBeenShown = false;
                firstIdle = true;

                // Important to stop the player doing some things while grabbing
                CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
                CPlayer.LocalPlayer.Ped.CanSwitchWeapons = false;
                CPlayer.LocalPlayer.Ped.BlockGestures = true;
                CPlayer.LocalPlayer.Ped.APed.DisableVehicleEntry(true);
                GTA.Native.Function.Call("DISABLE_PLAYER_SPRINT", Game.LocalPlayer, true);
                GTA.Native.Function.Call("DISABLE_PLAYER_LOCKON", Game.LocalPlayer, true);
                GTA.Native.Function.Call("SET_PLAYER_CAN_USE_COVER", Game.LocalPlayer, false);
                CPlayer.LocalPlayer.Ped.APed.DisableCrouch(true);

                // Save old player animation group
                playerAnimGroup = CPlayer.LocalPlayer.AnimGroup;

                // We use the f@fat animation group to stop the cop idle animations and because it looks quite good
                CPlayer.LocalPlayer.AnimGroup = "move_f@fat";

                // Make ped a mission char
                ped.BecomeMissionCharacter();

                // Important to stop the ped from doing some things too.
                ped.BlockPermanentEvents = true;
                ped.BlockGestures = true;
                ped.DontActivateRagdollFromPlayerImpact = true;
                ped.WillUseCover(false);

                // Kill possible cuffed animation task
                if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.PlayAnimationAndRepeat))
                {
                    //Game.Console.Print("Cuffed task was running");
                    this.ped.Intelligence.TaskManager.Abort(this.ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.PlayAnimationAndRepeat));
                    this.hadAnimationTaskRunning = true;
                }

                if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.PlayAnimation))
                {
                    //Game.Console.Print("Altnerative? Cuffed task was running");
                    this.ped.Intelligence.TaskManager.Abort(this.ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.PlayAnimation));
                    this.hadAnimationTaskRunning = true;
                }

                ped.Task.ClearAll();

                // Really important to stop the player targetting the ped, otherwise if they do it spins them round in circles
                GTA.Native.Function.Call("SET_CHAR_NEVER_TARGETTED", (Ped)ped, true);

                // If the ped is the old ped, we can set the old ragdoll flag to true
                if (oldPed != null && oldPed.Exists())
                {
                    if (oldPed == ped)
                    {
                        oldPedRagdollSet = true;
                    }
                }

                // Create the grabbing attach object
                grabObject = World.CreateObject("amb_walkietalkie", CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0, 0, 100)));

                if (grabObject != null && grabObject.Exists())
                {
                    // Attach the grab object
                    grabObject.Visible = false;
                    grabObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Root, positionIdle, rotation);
                }
                else
                {
                    // CRY!
                    Release();
                    Log.Warning("The grab object wasn't created", this);
                    return;
                }

                if (grabObject.isAttachedSomewhere)
                {
                    CPlayer.LocalPlayer.Ped.Task.PlayAnimation(grabbingAnimationSet, grabbingAnimation, 4.0f, AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                    GTA.Native.Function.Call("ATTACH_PED_TO_OBJECT", ped.Handle, grabObject, 0, 0, 0, 0, 0, 0, 0, 0);
                    grabbing = true;
                }
                else
                {
                    // MORE CRYING!
                    Release();
                    Log.Warning("The grab object wasn't attached", this);
                    return;
                }

                Stats.UpdateStat(Stats.EStatType.SuspectGrabbed, 1);
            }

        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();
            Release(true, true);
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            #region Old code for getting positions and shit
            /*
            if (grabbing)
            {
                if (Game.isKeyPressed(System.Windows.Forms.Keys.Space))
                {
                    if (Game.isKeyPressed(System.Windows.Forms.Keys.O))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            positionIdle.X += 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.I))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            positionIdle.X -= 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.L))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            positionIdle.Y += 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.K))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            positionIdle.Y -= 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.M))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            positionIdle.Z += 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.N))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            positionIdle.Z -= 0.01f;
                        }
                    }

                    grabObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Root, positionIdle, rotation);
                    GTA.Native.Function.Call("ATTACH_PED_TO_OBJECT", ped.Handle, grabObject, 0, 0, 0, 0, 0, 0, 0, 0);

                    TextHelper.PrintText(positionIdle.ToString() + " / " + rotation.ToString(), 2000);
                }


                if (Game.isKeyPressed(System.Windows.Forms.Keys.C))
                {
                    if (Game.isKeyPressed(System.Windows.Forms.Keys.O))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            rotation.X += 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.I))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            rotation.X -= 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.L))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            rotation.Y += 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.K))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            rotation.Y -= 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.M))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            rotation.Z += 0.01f;
                        }
                    }
                    else if (Game.isKeyPressed(System.Windows.Forms.Keys.N))
                    {
                        if (ped.Exists() && grabObject.Exists())
                        {
                            rotation.Z -= 0.01f;
                        }
                    }

                    grabObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Root, positionIdle, rotation);
                    GTA.Native.Function.Call("ATTACH_PED_TO_OBJECT", ped.Handle, grabObject, 0, 0, 0, 0, 0, 0, 0, 0);

                    TextHelper.PrintText(positionIdle.ToString() + " / " + rotation.ToString(), 2000);
                }
            }
            */
            #endregion

            // If the old ped hasn't had their ragdoll impact with player set back to normal, see if we can do this
            if (!oldPedRagdollSet)
            {
                if (oldPed != null && oldPed.Exists())
                {
                    if (oldPed.Position.DistanceTo2D(CPlayer.LocalPlayer.Ped.Position) > 2.0f)
                    {
                        // Only do it if they are at least 2.0f away from player
                        oldPed.DontActivateRagdollFromPlayerImpact = false;
                        oldPedRagdollSet = true;
                        hasGrabbingHelpBeenShown = false;
                    }
                }
                else
                {
                    oldPedRagdollSet = true;
                    hasGrabbingHelpBeenShown = false;
                }
            }


            if (grabbing)
            {
                if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexJump))
                {
                    if (!CPlayer.LocalPlayer.Ped.IsInWater)
                    {
                        // Stop them from jumping and entering vehicles.
                        CPlayer.LocalPlayer.Ped.Task.ClearAllImmediately();

                        // Release suspect and tell the cunt it has to climb.
                        CPed p = this.ped;
                        this.Release();
                        p.Heading = CPlayer.LocalPlayer.Ped.Heading;
                        p.SetPathfinding(true, true, true);
                        DelayedCaller.Call(delegate(object[] parameter) { p.Task.Climb(EJumpType.Front); }, this, 300);
                    }
                    else
                    {
                    }
                }

                if (ped.Exists() && grabObject.Exists())
                {
                    bool isInWater = CPlayer.LocalPlayer.Ped.IsInWater;

                    if (isInWater)
                    {
                        if (!this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.PlayAnimationAndRepeat))
                        {
                            TaskPlayAnimationAndRepeat taskPlayAnimationAndRepeat = new TaskPlayAnimationAndRepeat("idle", "move_m@h_cuffed", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                            taskPlayAnimationAndRepeat.AssignTo(this.ped, ETaskPriority.MainTask);
                        }
                    }
                    else
                    {
                        if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.PlayAnimationAndRepeat))
                        {
                            this.ped.Intelligence.TaskManager.Abort(this.ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.PlayAnimationAndRepeat));
                        }
                    }

                    // If both the ped being grabbed and the attaching object exists, process the animations
                    float speed = CPlayer.LocalPlayer.Ped.Speed;

                    if (speed < 4 && speed > 0 || CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("move_cop"), "walk")
                        || CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("swimming"), "walk"))
                    {
                        // Walking
                        if (isInWater)
                        {
                            if (!ped.Animation.isPlaying(new AnimationSet("swimming"), "walk"))
                            {
                                ped.Task.PlayAnimation(new AnimationSet("swimming"), "walk", 2.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown02);
                            }
                        }
                        else
                        {
                            if (!ped.Animation.isPlaying(animationSetMoving, "walk"))
                            {
                                ped.Task.PlayAnimation(animationSetMoving, "walk", 2.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown02);
                            }
                        }

                        if (CPlayer.LocalPlayer.AnimGroup != playerAnimGroup)
                        {
                            // Use normal animation set for walking with subject
                            CPlayer.LocalPlayer.AnimGroup = playerAnimGroup;
                        }

                        firstIdle = false;
                        grabObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Root, positionMoving, rotation);
                    }
                    else if (speed > 1 || CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("move_cop"), "run") || (playerAnimGroup == "move_cop" && speed > 0))
                    {
                        // Running or sprinting
                        if (isInWater)
                        {
                            if (!ped.Animation.isPlaying(new AnimationSet("swimming"), "run"))
                            {
                                ped.Task.PlayAnimation(new AnimationSet("swimming"), "run", 4.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown02);
                            }
                        }
                        else
                        {
                            if (!ped.Animation.isPlaying(animationSetMoving, "run"))
                            {
                                ped.Task.PlayAnimation(animationSetMoving, "run", 4.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown02);
                            }
                        }

                        if (CPlayer.LocalPlayer.AnimGroup != playerAnimGroup)
                        {
                            // Use normal animation set for running with subject
                            CPlayer.LocalPlayer.AnimGroup = playerAnimGroup;
                        }

                        firstIdle = false;
                        grabObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Root, positionMoving, rotation);
                    }
                    else
                    {
                        // Idle
                        if (isInWater)
                        {
                            if (!ped.Animation.isPlaying(new AnimationSet("swimming"), "idle"))
                            {
                                ped.Task.PlayAnimation(new AnimationSet("swimming"), "idle", 4.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown02);
                                ped.Task.AlwaysKeepTask = true;
                            }
                        }
                        else
                        {
                            if (!ped.Animation.isPlaying(animationSetIdle, "idle"))
                            {
                                ped.Task.PlayAnimation(animationSetIdle, "idle", 4.0f, AnimationFlags.Unknown05 | AnimationFlags.Unknown02);
                                ped.Task.AlwaysKeepTask = true;
                            }
                        }

                        if (CPlayer.LocalPlayer.AnimGroup != "move_f@fat")
                        {
                            // We use the f@fat animation group to stop the cop idle animations and because it looks quite good
                            CPlayer.LocalPlayer.AnimGroup = "move_f@fat";
                        }

                        if (!firstIdle && !hasReleasingHelpBeenShown)
                        {
                            // If this is not the first time idle and help for releasing hasn't been shown, show it!
                            TextHelper.PrintFormattedHelpBox("Press ~KEY_ARREST~ to let go of the suspect.", true);
                            hasReleasingHelpBeenShown = true;
                        }

                        grabObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Root, positionIdle, rotation);
                    }

                    // Make sure the player is playing the grab animations
                    if (!CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("amb@umbrella_hold"), "walk_hold"))
                    {
                        CPlayer.LocalPlayer.Ped.Task.PlayAnimation(grabbingAnimationSet, grabbingAnimation, 4.0f, AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                    }

                    // Attach everything together again
                    GTA.Native.Function.Call("ATTACH_PED_TO_OBJECT", ped.Handle, grabObject, 0, 0, 0, 0, 0, 0, 0, 0);

                    // This is the code to put grabbed peds in the player's last car (thanks babe, your door code is fucking awesome!)
                    if ((CPlayer.LocalPlayer.LastVehicle != null && CPlayer.LocalPlayer.LastVehicle.Exists()) || (LCPDFRPlayer.LocalPlayer.IsInTutorial && LCPDFRPlayer.LocalPlayer.TutorialCar != null && LCPDFRPlayer.LocalPlayer.TutorialCar.Exists()))
                    {
                        CVehicle lastVeh = CPlayer.LocalPlayer.LastVehicle;

                        if (LCPDFRPlayer.LocalPlayer.IsInTutorial)
                        {
                            lastVeh = LCPDFRPlayer.LocalPlayer.TutorialCar;
                        }

                        if (lastVeh.Exists())
                        {
                            // Only if the car is driveable, etc.
                            if (lastVeh.IsDriveable && lastVeh.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar) && lastVeh.PassengerSeats > 1)
                            {
                                // Only if doors are open
                                //if (lastVeh.Door(VehicleDoor.LeftRear).isFullyOpen || lastVeh.Door(VehicleDoor.RightRear).isFullyOpen)
                                //{
                                    float vehicleWidth = lastVeh.Model.GetDimensions().X;
                                    float vehicleLength = 0;

                                    // Adjust length for stockade
                                    if (lastVeh.Model.ModelInfo.Name.Contains("STOCKADE"))
                                    {
                                        vehicleLength = -lastVeh.Model.GetDimensions().Y;
                                    }

                                    Vector3 leftRearDoorPosition = lastVeh.GetOffsetPosition(new Vector3(-vehicleWidth / 2, vehicleLength / 2, 0));
                                    Vector3 rightRearDoorPosition = lastVeh.GetOffsetPosition(new Vector3(vehicleWidth / 2, vehicleLength / 2, 0));

                                    // Get closest door
                                    List<KeyValuePair<int, float>> distances = new List<KeyValuePair<int, float>>();
                                    distances.Add(new KeyValuePair<int, float>(1, CPlayer.LocalPlayer.Ped.Position.DistanceTo(leftRearDoorPosition)));
                                    distances.Add(new KeyValuePair<int, float>(2, CPlayer.LocalPlayer.Ped.Position.DistanceTo(rightRearDoorPosition)));
                                    var dict = from entry in distances orderby entry.Value ascending select entry;
                                    KeyValuePair<int, float> closest = dict.First();

                                    VehicleSeat seat = (VehicleSeat)closest.Key;
                                    VehicleDoor door = lastVeh.GetDoorFromSeat(seat);

                                    // If close
                                    if (closest.Value < 2f)
                                    {
                                        if (lastVeh.GetPedOnSeat(seat) == null)
                                        {
                                            if (lastVeh.Door(door).Angle < 0.8f)
                                            {
                                                // Vehicle door isn't open enough
                                                if (!hasDoorHelpBeenShown)
                                                {
                                                    TextHelper.PrintFormattedHelpBox("You can place suspects that you are holding in your vehicle by letting go of them, opening a door and then dragging them to the open door.");
                                                    hasDoorHelpBeenShown = true;
                                                }
                                                else
                                                {
                                                    if (!TextHelper.IsHelpboxBeingDisplayed)
                                                    {
                                                        TextHelper.PrintFormattedHelpBox("You can place suspects that you are holding in your vehicle by letting go of them, opening a door and then dragging them to the open door.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (!hasSeatingHelpBeenShown)
                                                {
                                                    TextHelper.PrintFormattedHelpBox("Press ~KEY_ARREST~ to place the suspect in the vehicle.", true);
                                                    hasSeatingHelpBeenShown = true;
                                                }
                                                else
                                                {
                                                    if (!TextHelper.IsHelpboxBeingDisplayed)
                                                    {
                                                        TextHelper.PrintFormattedHelpBox("Press ~KEY_ARREST~ to place the suspect in the vehicle.");
                                                    }
                                                }
                                            }

                                            if (KeyHandler.IsKeyDown(ELCPDFRKeys.GrabSuspect))
                                            {
                                                // Release and put them in the car
                                                Release(false);

                                                if (ped.Exists())
                                                {
                                                    ped.Task.ClearAll();
                                                    ped.Task.EnterVehicle(lastVeh, seat);
                                                }

                                                // Clear player tasks
                                                CPlayer.LocalPlayer.Ped.Task.ClearAllImmediately();
                                                return;
                                            }
                                        }
                                    }
                                //}
                            }
                        }
                    }

                    if (CPlayer.LocalPlayer.Ped.IsRagdoll || !CPlayer.LocalPlayer.Ped.IsAliveAndWell || Hardcore.playerInjured || CPlayer.LocalPlayer.Ped.Position.DistanceTo2D(ped.Position) > 2.0f || CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexMelee) || KeyHandler.IsKeyDown(ELCPDFRKeys.GrabSuspect))
                    {
                        // If the player is ragdoll, dead, injured or too far away, make them release the ped
                        Release(true);
                    }
                }
            }
            else
            {
                #region Old Testing Code
                // Otherwise, if the player isn't grabbing a ped
                /*
                if (KeyHandler.IsKeyDown(ELCPDFRKeys.GrabSuspect))
                {
                    if (!Game.isKeyPressed(System.Windows.Forms.Keys.LMenu) && !CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun))
                    {
                        ped = new CPed("F_Y_SOCIALITE", CPlayer.LocalPlayer.Ped.GetSafePositionAlternate(), EPedGroup.Testing);

                        if (ped.Exists())
                        {
                            Game.DisplayText(ped.IsRequiredForMission.ToString());
                           
                            wasPedRequiredForMission = ped.IsRequiredForMission;

                            CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
                            CPlayer.LocalPlayer.Ped.CanSwitchWeapons = false;
                            CPlayer.LocalPlayer.Ped.BlockGestures = true;

                            ped.BecomeMissionCharacter();
                            ped.Task.ClearAll();
                            ped.BlockPermanentEvents = true;
                            ped.BlockGestures = true;
                            ped.DontActivateRagdollFromPlayerImpact = true;
                            ped.Wanted.IsCuffed = true;

                            GTA.Native.Function.Call("DISABLE_PLAYER_SPRINT", Game.LocalPlayer, true);
                            GTA.Native.Function.Call("DISABLE_PLAYER_LOCKON", Game.LocalPlayer, true);
                            GTA.Native.Function.Call("SET_PLAYER_CAN_USE_COVER", Game.LocalPlayer, false);

                            grabObject = World.CreateObject("amb_walkietalkie", CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0, 0, 100)));

                            if (grabObject.Exists())
                            {
                                grabObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Root, positionIdle, rotation);
                            }

                            if (grabObject.Exists() && grabObject.isAttachedSomewhere)
                            {
                                if (ped.Exists())
                                {
                                    CPlayer.LocalPlayer.Ped.Task.PlayAnimation(grabbingAnimationSet, grabbingAnimation, 4.0f, AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                                    GTA.Native.Function.Call("ATTACH_PED_TO_OBJECT", ped.Handle, grabObject, 0, 0, 0, 0, 0, 0, 0, 0);
                                    grabbing = true;
                                    playerAnimGroup = CPlayer.LocalPlayer.AnimGroup;
                                    CPlayer.LocalPlayer.AnimGroup = "move_f@fat";
                                    GTA.Native.Function.Call("SET_CHAR_NEVER_TARGETTED", ped.Handle, true);
                                    ped.WillUseCover(false);
                                }
                            }
                        }
                    }                  
                }
                */
                #endregion

                // Code for showing prompt and grabbing cuffed peds

                if (!CPlayer.LocalPlayer.Ped.IsInVehicle)
                {
                    foreach (CPed closePed in CPed.GetPedsAround(1.0f, EPedSearchCriteria.All, CPlayer.LocalPlayer.Ped.Position))
                    {
                        bool pullFromWater = closePed.IsInWater && closePed.PedGroup != EPedGroup.Cop
                                             && closePed.PedGroup != EPedGroup.Player;

                        // Get all really close peds and check if they are cuffed and alive and not in a vehicle
                        if ((closePed.Exists() && closePed.Wanted.IsCuffed && closePed.IsAliveAndWell && !closePed.IsInVehicle && !closePed.IsRagdoll && closePed.Wanted.IsBeingArrestedByPlayer
                            && !closePed.IsGettingIntoAVehicle && !closePed.IsGettingOutOfAVehicle) || pullFromWater)
                        {
                            if (!hasGrabbingHelpBeenShown)
                            {
                                if (pullFromWater)
                                {
                                    TextHelper.PrintFormattedHelpBox("Press ~KEY_ARREST~ to rescue the ped.", true);
                                }
                                else
                                {
                                    TextHelper.PrintFormattedHelpBox("Press ~KEY_ARREST~ to take hold of the suspect.", true);
                                }

                                hasGrabbingHelpBeenShown = true;
                            }
                            else
                            {
                                if (!TextHelper.IsHelpboxBeingDisplayed)
                                {
                                    TextHelper.PrintFormattedHelpBox("Press ~KEY_ARREST~ to take hold of the suspect.", false);
                                }
                            }

                            if (KeyHandler.IsKeyDown(ELCPDFRKeys.GrabSuspect))
                            {
                                if (!Game.isKeyPressed(System.Windows.Forms.Keys.LMenu) && !CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun))
                                {
                                    // Grab them
                                    StartGrabbing(closePed);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
