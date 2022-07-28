namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    using Main = LCPD_First_Response.LCPDFR.Main;
    using Timer = LCPD_First_Response.Engine.Timers.Timer;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// Manages the process of taking a suspect to a PD. Doesn't own any entities, this has to be done by the calling class!
    /// </summary>
    [ScriptInfo("PlayerSuspectTransport", true)]
    internal class PlayerSuspectTransport : GameScript
    {
        /// <summary>
        /// The number of script instances running.
        /// </summary>
        private static int instances;

        /// <summary>
        /// The camera used for the sequence.
        /// </summary>
        private Camera camera;

        /// <summary>
        /// The cop in the sequence.
        /// </summary>
        private CPed cop;

        /// <summary>
        /// The other cop in the sequence.
        /// </summary>
        private CPed cop2;

        /// <summary>
        /// The other cop's coffee in the sequence.
        /// </summary>
        private GTA.Object coffee;

        /// <summary>
        /// The checkpoint of the pd.
        /// </summary>
        private ArrowCheckpoint checkpoint;

        /// <summary>
        /// The closest pd.
        /// </summary>
        private PoliceDepartment closestPD;

        /// <summary>
        /// Whether the close to PD text has just been displayed.
        /// </summary>
        private bool justDisplayedCloseToPD;

        /// <summary>
        /// Whether the four doors text has just been displayed.
        /// </summary>
        private bool justDisplayedFourDoors;

        /// <summary>
        /// Whether the open door text has just been displayed.
        /// </summary>
        private bool justDisplayedOpenDoor;

        /// <summary>
        /// Whether the suspect is in a vehicle.
        /// </summary>
        private bool isInVehicle;

        /// <summary>
        /// Whether the sequence is running.
        /// </summary>
        private bool isSequenceRunning;

        /// <summary>
        /// Whether suspect is leaving the vehicle in the sequence.
        /// </summary>
        private bool leavingVehicle;

        /// <summary>
        /// The timer used to control the suspect's movement.
        /// </summary>
        private Timer movementTimer;

        /// <summary>
        /// Whether this script should be passive and not show the actual cutscene because another script does already.
        /// </summary>
        private bool passiveScript;

        /// <summary>
        /// Whether seats have been shuffled.
        /// </summary>
        private bool shuffledSeats;

        /// <summary>
        /// The suspect.
        /// </summary>
        private CPed suspect;

        /// <summary>
        /// Timeout timer in case the suspect never gets off screen.
        /// </summary>
        private NonAutomaticTimer timeoutTimer;

        /// <summary>
        /// Timeout timer in case cop never reaches door.
        /// </summary>
        private NonAutomaticTimer timeoutOpenDoorTimer;

        /// <summary>
        /// Ticks since the door was opened but suspect hasn't left vehicle
        /// </summary>
        private int timeElapsed;

        /// <summary>
        /// Whether or not we have had to force them to leave the vehicle
        /// </summary>
        private bool forcedToLeaveVehicle;

        /// <summary>
        /// The position which cop 2 will walk to initially
        /// </summary>
        private Vector3 cop2Position;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerSuspectTransport"/> class.
        /// </summary>
        /// <param name="suspect">
        /// The suspect.
        /// </param>
        public PlayerSuspectTransport(CPed suspect)
        {
            this.suspect = suspect;
            instances++;

            if (!CPlayer.LocalPlayer.Ped.IsInVehicle && !this.suspect.IsInVehicle)
            {
                this.suspect.Task.GoTo(CPlayer.LocalPlayer.Ped.Position.Around(2f));
            }

            // Apply cuffed animation if ped is cuffed
            if (this.suspect.Wanted.IsCuffed && !this.suspect.Intelligence.TaskManager.IsTaskActive(ETaskID.PlayAnimationAndRepeat))
            {
                TaskPlayAnimationAndRepeat taskPlayAnimationAndRepeat = new TaskPlayAnimationAndRepeat("idle", "move_m@h_cuffed", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                taskPlayAnimationAndRepeat.AssignTo(this.suspect, ETaskPriority.MainTask);
            }

            // Create movement timer
            this.movementTimer = new Timer(4000, this.MovementTimerCallback);
            this.movementTimer.Start();
            this.timeoutTimer = new NonAutomaticTimer(10000, ETimerOptions.OneTimeReturnTrue);
            this.timeoutOpenDoorTimer = new NonAutomaticTimer(10000, ETimerOptions.OneTimeReturnTrue);

            // Display text
            if (!CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                TextHelper.PrintText(CultureHelper.GetText("ARREST_GET_A_VEHICLE"), 4000);
            }
            else
            {
                TextHelper.PrintText(CultureHelper.GetText("ARREST_TAKE_TO_PD"), 6000);
            }

            // Disable PDs
            Main.PoliceDepartmentManager.PlayerCloseToPD += this.PoliceDepartmentManager_PlayerCloseToPD;
        }

        /// <summary>
        /// The suspect in custody event handler.
        /// </summary>
        public delegate void SuspectInCustodyEventHandler();

        /// <summary>
        /// The event fired when the suspect is in custody.
        /// </summary>
        public event SuspectInCustodyEventHandler SuspectInCustody;

        /// <summary>
        /// Gets a value indicating whether the script has finished.
        /// </summary>
        public bool Finished { get; private set; }

        /// <summary>
        /// Forces the suspect to leave the vehicle.
        /// </summary>
        public void ForceLeaveVehicle()
        {
            // Door has to be open
            bool isDoorOpen = (this.suspect.GetSeatInVehicle() == VehicleSeat.LeftRear && this.suspect.CurrentVehicle.Door(VehicleDoor.LeftRear).isOpen)
                || (this.suspect.GetSeatInVehicle() == VehicleSeat.RightRear && this.suspect.CurrentVehicle.Door(VehicleDoor.RightRear).isOpen);

            if (!isDoorOpen)
            {
                TextHelper.PrintText(CultureHelper.GetText("ARREST_OPEN_DOOR_LEAVE"), 5000);
                return;
            }

            this.suspect.Task.LeaveVehicle(this.suspect.CurrentVehicle, false);
            this.isInVehicle = false;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.suspect == null || !this.suspect.Exists())
            {
                this.End();
                return;
            }

            if (!this.suspect.HasBlip && this.suspect.IsAliveAndWell)
            {
                // Add small blip for suspect
                this.suspect.AttachBlip();
                if (this.suspect.HasBlip)
                {
                    this.suspect.Blip.Display = BlipDisplay.MapOnly;
                    this.suspect.Blip.Scale = 0.5f;
                    this.suspect.Blip.Name = "Suspect";
                }
            }

            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                if (!this.isInVehicle)
                {
                    // If not getting into a vehicle and not in player's, get into it
                    if (!this.suspect.IsGettingIntoAVehicle && !this.suspect.IsSittingInVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle))
                    {
                        if (!CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                        {
                            if (!this.justDisplayedFourDoors)
                            {
                                TextHelper.PrintText(CultureHelper.GetText("ARREST_NO_COP_CAR"), 5000);
                                this.justDisplayedFourDoors = true;
                                DelayedCaller.Call(delegate { this.justDisplayedFourDoors = false; }, this, 15000);
                            }

                            return;
                        }

                        if (CPlayer.LocalPlayer.Ped.CurrentVehicle.PassengerSeats <= 1)
                        {
                            if (!this.justDisplayedFourDoors)
                            {
                                TextHelper.PrintText(CultureHelper.GetText("ARREST_FOUR_SEATS_VEHICLE"), 5000);
                                this.justDisplayedFourDoors = true;
                                DelayedCaller.Call(delegate { this.justDisplayedFourDoors = false; }, this, 15000);
                            }

                            return;
                        }

                        // The left or right rear door has to be open
                        bool isRightDoorOpen = CPlayer.LocalPlayer.Ped.CurrentVehicle.Door(VehicleDoor.RightRear).isOpen;
                        bool isLeftDoorOpen = CPlayer.LocalPlayer.Ped.CurrentVehicle.Door(VehicleDoor.LeftRear).isOpen;
                        bool isDoorOpen = isRightDoorOpen || isLeftDoorOpen;

                        if (!isDoorOpen)
                        {
                            if (!this.justDisplayedOpenDoor)
                            {
                                TextHelper.PrintText(CultureHelper.GetText("ARREST_OPEN_DOOR_ENTER"), 5000);
                                this.justDisplayedOpenDoor = true;
                                DelayedCaller.Call(delegate { this.justDisplayedOpenDoor = false; }, this, 15000);
                            }

                            if (this.suspect.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                            {
                                this.suspect.Intelligence.TaskManager.FindTaskWithID(ETaskID.GetInVehicle).MakeAbortable(this.suspect);
                            }

                            return;
                        }

                        if (!this.suspect.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                        {
                            // Only pass seats to task where doors are open
                            VehicleSeat seat = VehicleSeat.None;
                            VehicleSeat alternativeSeat = VehicleSeat.None;
                            if (isLeftDoorOpen)
                            {
                                seat = VehicleSeat.LeftRear;
                            }

                            if (isRightDoorOpen)
                            {
                                seat = VehicleSeat.RightRear;
                            }

                            if (isRightDoorOpen && isLeftDoorOpen)
                            {
                                alternativeSeat = VehicleSeat.LeftRear;
                            }
                            else
                            {
                                alternativeSeat = seat;
                            }

                            TaskGetInVehicle taskGetInVehicle = new TaskGetInVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle, seat, alternativeSeat, true, true);
                            taskGetInVehicle.AssignTo(this.suspect, ETaskPriority.MainTask);
                        }
                    }

                    // If in player's vehicle, take to PD
                    if (this.suspect.IsSittingInVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle))
                    {
                        TextHelper.PrintText(CultureHelper.GetText("ARREST_TAKE_TO_PD"), 6000);
                        this.isInVehicle = true;
                    }
                }
                else
                {
                    // Show route to closest pd
                    if (Main.PoliceDepartmentManager.ClosestPoliceDepartment != this.closestPD)
                    {
                        // Remove old blip, if any
                        if (this.checkpoint != null)
                        {
                            this.checkpoint.Delete();
                        }

                        this.closestPD = Main.PoliceDepartmentManager.ClosestPoliceDepartment;
                        this.checkpoint = new ArrowCheckpoint(this.closestPD.VehiclePosition, this.InCheckpointCallback);
                        this.checkpoint.ArrowColor = System.Drawing.Color.Blue;
                        this.checkpoint.BlipIcon = BlipIcon.Building_Garage;
                        this.checkpoint.BlipColor = BlipColor.Cyan;
                        this.checkpoint.RouteActive = true;

                        TextHelper.PrintText(CultureHelper.GetText("ARREST_TAKE_TO_PD"), 10000);
                    }
                    else if (!this.isSequenceRunning)
                    {
                        // If getting closer, display additional information
                        if (this.closestPD.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 40)
                        {
                            if (!this.justDisplayedCloseToPD)
                            {
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_CLOSE_TO_PD_DROPOFF"));
                                this.justDisplayedCloseToPD = true;
                                DelayedCaller.Call(delegate { this.justDisplayedCloseToPD = false; }, this, 30000);
                            }
                        }
                    }
                }
            }
            else
            {
                // If not in vehicle
                this.justDisplayedFourDoors = false;
                this.justDisplayedOpenDoor = false;

                // Code to keep suspect in vehicle if appropriate
                if (this.suspect.IsInVehicle && this.suspect.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                {
                    this.isInVehicle = true;
                }
            }

            // Process sequence logic
            if (this.isSequenceRunning)
            {
                if (KeyHandler.IsKeyDown(ELCPDFRKeys.AbortSuspectTransportCutscene))
                {
                    Game.FadeScreenOut(3000, true);

                    this.isSequenceRunning = false;
                    this.suspect.Delete();

                    if (this.SuspectInCustody != null)
                    {
                        this.SuspectInCustody();
                    }

                    this.End();
                    TextHelper.ClearHelpbox();
                    return;
                }

                bool leftRear = this.suspect.IsInVehicle && this.suspect.GetSeatInVehicle() == VehicleSeat.LeftRear;
                VehicleDoor door = VehicleDoor.RightRear;
                if (leftRear)
                {
                    door = VehicleDoor.LeftRear;
                }

                // If passive script, shuffle seats when possible and exit vehicle too
                if (this.passiveScript)
                {
                    // Still in vehicle
                    if (this.suspect.IsInVehicle && !this.suspect.IsGettingOutOfAVehicle && !this.leavingVehicle)
                    {
                        // Get which seat will be left by the ped of the other instance
                        VehicleSeat freeSeat = VehicleSeat.LeftRear;
                        if (leftRear)
                        {
                            freeSeat = VehicleSeat.RightRear;
                        }

                        // If suspect didn't shuffle yet, shuffle to next seat
                        if (!this.shuffledSeats)
                        {
                            // But only if seat is free already (so other suspect has left)
                            if (this.suspect.CurrentVehicle.IsSeatFree(freeSeat))
                            {
                                // Shuffle seats
                                this.suspect.Task.ShuffleToNextCarSeat(this.suspect.CurrentVehicle);
                                this.shuffledSeats = true;
                            }
                        }
                        else if (!this.suspect.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexShuffleBetweenSeats))
                        {
                            // Leave vehicle and go
                            GTA.TaskSequence taskSequence = new GTA.TaskSequence();
                            taskSequence.AddTask.StandStill(800);
                            taskSequence.AddTask.LeaveVehicle(this.suspect.CurrentVehicle, false);
                            taskSequence.AddTask.StandStill(1500);
                            taskSequence.AddTask.GoTo(Main.PoliceDepartmentManager.ClosestPoliceDepartment.Position, false);
                            taskSequence.Perform(this.suspect);
                            taskSequence.Dispose();
                            this.leavingVehicle = true;
                        }
                    }
                    else if (!this.suspect.IsInVehicle && !this.suspect.IsGettingOutOfAVehicle)
                    {
                        // If ped is out of vehicle and player can be controlled again, delete (since cutscene has finished then)
                        if (CPlayer.LocalPlayer.CanControlCharacter)
                        {
                            this.isSequenceRunning = false;
                            this.suspect.Delete();
                            if (this.SuspectInCustody != null)
                            {
                                this.SuspectInCustody();
                            }

                            this.End();
                        }
                    }

                    return;
                }

                if (cop2 != null && cop2.Exists())
                {
                    if (!cop2.IsInVehicle && !cop2.IsInGroup)
                    {
                        if (suspect.Exists())
                        {
                            if (!suspect.IsInVehicle)
                            {
                                cop2.Task.ClearAll();

                                if (cop2.Model == "M_M_FATCOP_01")
                                {
                                    cop2.SayAmbientSpeech("ARREST_PLAYER");
                                }
                                else
                                {
                                    cop2.SayAmbientSpeech("ARREST_PED");
                                }

                                if (Common.GetRandomBool(0, 2, 1))
                                {
                                    DelayedCaller.Call(delegate { if (suspect != null && suspect.Exists()) suspect.SayAmbientSpeech("INTIMIDATE_RESP"); }, 3000);
                                }

                                Group group = new Group(suspect);
                                group.FormationSpacing = 3.0f;
                                group.AddMember(cop2);
                            }
                            else
                            {
                                if (this.cop2.Position.DistanceTo2D(cop2Position) < 0.75f)
                                {
                                    if (!this.cop2.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord))
                                    {
                                        cop2.Task.ClearAll();
                                        cop2.Task.TurnTo(this.suspect);
                                    }
                                }
                            }
                        }
                    }
                }

                // When door is open, make cop step back and make suspect exit
                if (!this.suspect.IsInVehicle || this.suspect.CurrentVehicle.Door(door).isOpen)
                {
                    if (this.suspect.IsInVehicle && !this.suspect.IsGettingOutOfAVehicle && !this.leavingVehicle)
                    {
                        Log.Debug("Suspect still in vehicle", this);

                        Vector3 position = this.suspect.GetOffsetPosition(new Vector3(4, 0, 0));
                        if (leftRear)
                        {
                            position = this.suspect.GetOffsetPosition(new Vector3(-4, 0, 0));
                        }

                        GTA.TaskSequence taskSequence = new GTA.TaskSequence();
                        taskSequence.AddTask.StandStill(300);
                        taskSequence.AddTask.GoTo(position);
                        taskSequence.AddTask.TurnTo(this.suspect);
                        taskSequence.AddTask.StandStill(1000);
                        taskSequence.AddTask.GoTo(Main.PoliceDepartmentManager.ClosestPoliceDepartment.Position, false);
                        taskSequence.Perform(this.cop);
                        taskSequence.Dispose();

                        // Leave vehicle and go
                        this.suspect.CurrentVehicle.Door(door).Open();
                        taskSequence = new GTA.TaskSequence();
                        taskSequence.AddTask.StandStill(800);
                        taskSequence.AddTask.LeaveVehicle(this.suspect.CurrentVehicle, false);
                        taskSequence.AddTask.StandStill(1500);
                        taskSequence.AddTask.GoTo(Main.PoliceDepartmentManager.ClosestPoliceDepartment.Position, true);
                        taskSequence.Perform(this.suspect);
                        taskSequence.Dispose();
                        this.leavingVehicle = true;
                    }
                    else if (!this.suspect.IsInVehicle && !this.suspect.IsGettingOutOfAVehicle)
                    {
                        bool isInPd = Main.PoliceDepartmentManager.ClosestPoliceDepartment.Position.DistanceTo(this.suspect.Position) < 1.5f;

                        if (!this.suspect.IsOnScreen || this.timeoutTimer.CanExecute(true) || isInPd)
                        {
                            if (this.timeoutTimer.CanExecute(true) || isInPd)
                            {
                                Log.Debug("Not on screen or timed out", this);
                                Game.FadeScreenOut(3000, true);

                                this.isSequenceRunning = false;
                                if (this.suspect.LastVehicle != null && this.suspect.LastVehicle.Exists())
                                {
                                    this.suspect.LastVehicle.CloseAllDoors();
                                }

                                this.suspect.Delete();

                                if (this.SuspectInCustody != null)
                                {
                                    this.SuspectInCustody();
                                }

                                this.End();

                                // Game.FadeScreenIn(3000, false);
                            }
                        }
                    }
                    else if (this.suspect.IsInVehicle && !this.suspect.IsGettingOutOfAVehicle && this.leavingVehicle)
                    {
                        // They are meant to be leaving the vehicle but aren't for some reason.
                        if (!this.suspect.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                        {
                            this.timeElapsed++;

                            if (this.timeElapsed > 250)
                            {
                                this.suspect.Task.LeaveVehicle(this.suspect.CurrentVehicle, false);
                                forcedToLeaveVehicle = true;
                            }
                        }
                    }

                    if (forcedToLeaveVehicle)
                    {
                        // If we had to force the suspect to leave the vehicle, we'll need to reapply the sequence.

                        if (!this.suspect.IsInVehicle)
                        {
                            // Once they are out of the vehicle, reapply the sequence.
                            forcedToLeaveVehicle = false;
                            timeElapsed = 0;

                            GTA.TaskSequence taskSequence;
                            taskSequence = new GTA.TaskSequence();
                            taskSequence.AddTask.StandStill(1500);
                            taskSequence.AddTask.GoTo(Main.PoliceDepartmentManager.ClosestPoliceDepartment.Position, true);
                            taskSequence.Perform(this.suspect);
                            taskSequence.Dispose();
                        }
                    }
                }
                else
                {
                    if (!this.suspect.CurrentVehicle.Door(door).isOpen)
                    {
                        if (this.timeoutOpenDoorTimer.CanExecute(true))
                        {
                            Log.Warning("Process: Cop failed to reach prisoner's door", this);
                            Game.FadeScreenOut(3000, true);

                            this.isSequenceRunning = false;
                            if (this.suspect.LastVehicle != null && this.suspect.LastVehicle.Exists())
                            {
                                this.suspect.LastVehicle.CloseAllDoors();
                            }

                            this.suspect.Delete();

                            if (this.SuspectInCustody != null)
                            {
                                this.SuspectInCustody();
                            }

                            this.End();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            if (this.movementTimer != null)
            {
                this.movementTimer.Stop();
            }

            if (this.checkpoint != null)
            {
                this.checkpoint.Delete();
            }

            // End introduction
            if (this.camera != null && this.camera.Exists())
            {
                this.camera.Deactivate();
                CPlayer.LocalPlayer.Ped.PlaceCamBehind();
                CPlayer.LocalPlayer.CanControlCharacter = true;
            }

            if (this.cop != null && this.cop.Exists())
            {
                this.cop.Delete();
            }

            if (this.cop2 != null && this.cop2.Exists())
            {
                this.cop2.Delete();
            }

            if (this.coffee != null && this.coffee.Exists())
            {
                this.coffee.Delete();
            }

            if (GTA.Native.Function.Call<bool>("IS_SCREEN_FADING") || GTA.Native.Function.Call<bool>("IS_SCREEN_FADED_OUT"))
            {
                Game.FadeScreenIn(3000, true);
            }

            Main.PoliceDepartmentManager.PlayerCloseToPD -= this.PoliceDepartmentManager_PlayerCloseToPD;
            instances--;
            this.Finished = true;

            base.End();
        }

        /// <summary>
        /// Called when the player is inside the checkpoint.
        /// </summary>
        private void InCheckpointCallback()
        {
            // Suspect has to be close too
            bool isClose = Main.PoliceDepartmentManager.ClosestPoliceDepartment.VehiclePosition.DistanceTo(this.suspect.Position) < 5f;
            if (isClose)
            {
                if (this.suspect.IsGrabbed)
                {
                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_STOP_GRABBING"), false);
                    this.checkpoint.ResetDistanceCheck();
                }
                else
                {
                    this.StartSequence();
                }
            }
            else
            {
                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_TAKE_TO_PD_CLOSER"), false);
                this.checkpoint.ResetDistanceCheck();
            }
        }

        /// <summary>
        /// Starts the "taking in custody" sequence.
        /// </summary>
        private void StartSequence()
        {
            // If multiple instances are running, only use this sequence once
            if (instances > 1)
            {
                // If control is already deactivated, another script is showing the cutscene
                if (!CPlayer.LocalPlayer.CanControlCharacter)
                {
                    // Delete checkpoint already
                    if (this.checkpoint != null)
                    {
                        this.checkpoint.Delete();
                    }

                    this.isSequenceRunning = true;
                    this.passiveScript = true;
                    return;
                }
            }

            // Disable player control and stop vehicle
            this.isSequenceRunning = true;

            CPlayer.LocalPlayer.CanControlCharacter = false;

            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                CPlayer.LocalPlayer.Ped.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 2000);
                CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed = 0;
            }

            // Fade screen to black
            Game.FadeScreenOut(3000, true);

            // Delete checkpoint already
            if (this.checkpoint != null)
            {
                this.checkpoint.Delete();
            }

            if (Settings.DisableSuspectTransportCutscene)
            {
                this.isSequenceRunning = false;
                this.suspect.Delete();

                if (this.SuspectInCustody != null)
                {
                    this.SuspectInCustody();
                }

                CPlayer.LocalPlayer.CanControlCharacter = true;

                this.End();
                TextHelper.ClearHelpbox();
                return;
            }

            // Create camera a little behind the vehicle looking downwards a little bit
            this.camera = new Camera();
            Vector3 position = this.suspect.GetOffsetPosition(new Vector3(0, -3.5f, 2f));
            this.camera.Position = position;
            this.camera.Heading = this.suspect.Heading;
            this.camera.Rotation = new Vector3(this.camera.Rotation.X - 8, this.camera.Rotation.Y, this.camera.Rotation.Z);
            this.camera.Activate();

            // If suspect is in right rear seat, spawn cop to the right. If in left rear, spawn cop to the left
            Vector3 copPosition = this.suspect.GetOffsetPosition(new Vector3(2, -2.5f, 0));
            int seat = 2;
            if (this.suspect.IsInVehicle && this.suspect.GetSeatInVehicle() == VehicleSeat.LeftRear)
            {
                copPosition = this.suspect.GetOffsetPosition(new Vector3(-2, -2.5f, 0));
                seat = 1;
            }

            // Create cop
            if (this.suspect.IsInVehicle)
            {
                this.cop = new CPed("M_M_SECURITYMAN", copPosition, EPedGroup.MissionPed);
                int attempts = 0;
                while (!this.cop.Exists())
                {
                    Game.WaitInCurrentScript(50);
                    this.cop = new CPed("M_M_SECURITYMAN", copPosition, EPedGroup.MissionPed);
                    attempts++;

                    if (attempts > 100)
                    {
                        // Too many attempts, simply delete and exit
                        this.isSequenceRunning = false;
                        this.suspect.Delete();

                        if (this.SuspectInCustody != null)
                        {
                            this.SuspectInCustody();
                        }

                        this.End();

                        // Fade in and make cop open the door
                        TextHelper.ClearHelpbox();
                        return;
                    }
                }

                this.cop.FixCopClothing();
                this.cop.Task.AlwaysKeepTask = true;

                this.cop2 = new CPed(CModel.CurrentCopModel, this.cop.GetSafePositionAlternate().Around(1.0f), EPedGroup.MissionPed);

                if (this.cop2 != null && this.cop2.Exists())
                {
                    this.cop2.FixCopClothing();

                    if (this.cop2.Model == "M_Y_COP")
                    {
                        if (Game.CurrentEpisode == GameEpisode.GTAIV)
                        {
                            cop2.Skin.Component.Head.ChangeIfValid(1, 0);
                        }
                        else
                        {
                            cop2.Skin.Component.Head.ChangeIfValid(0, 0);
                        }

                        cop2.Voice = "M_Y_COP_WHITE_02";
                    }
                    else if (this.cop2.Model == "M_M_FATCOP_01")
                    {
                        cop2.Skin.Component.Head.ChangeIfValid(1, 0);
                        cop2.Skin.Component.UpperBody.ChangeIfValid(0, 1);
                        cop2.Voice = "M_M_FATCOP_01_BLACK";
                    }
                    else if (this.cop2.Model == "M_Y_STROOPER")
                    {
                        cop2.Voice = "M_M_STROOPER_WHITE_01";
                    }

                    this.cop2.BecomeMissionCharacter();
                    if (this.suspect.GetSeatInVehicle() == VehicleSeat.LeftRear)
                    {
                        cop2Position = this.suspect.GetOffsetPosition(new Vector3(-1.75f, 1.0f, 0));
                        this.cop2.Task.GoTo(cop2Position, EPedMoveState.Walk);
                    }
                    else
                    {
                        cop2Position = this.suspect.GetOffsetPosition(new Vector3(1.75f, 1.0f, 0));
                        this.cop2.Task.GoTo(cop2Position, EPedMoveState.Walk);
                    }

                    this.coffee = World.CreateObject("amb_clipboard", this.cop2.GetOffsetPosition(new Vector3(0f, 0f, 10f)));
                    if (this.coffee != null && this.coffee.Exists())
                    {
                        this.coffee.AttachToPed(this.cop2, Bone.RightHand, new Vector3(0.085f, -0.050f, -0.13f), new Vector3(-2.86f, 3.78f, 4.58f));
                    }
                }
            }

            // If not in vehicle, delete already
            if (!this.suspect.IsInVehicle)
            {
                this.isSequenceRunning = false;
                this.suspect.Delete();

                if (this.SuspectInCustody != null)
                {
                    this.SuspectInCustody();
                }

                this.End();
                TextHelper.ClearHelpbox();
                return;
            }

            // Fade in and make cop open the door
            Game.FadeScreenIn(3000, true);
            TextHelper.ClearHelpbox();
            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("ARREST_SUSPECT_BEING_REMOVED"), true); }, 500);

            // Suspect could be deleted at this point already if not in vehicle
            if (this.suspect.Exists() && this.suspect.IsInVehicle)
            {
                this.suspect.CurrentVehicle.DoorLock = DoorLock.None;
                this.cop.SetNextDesiredMoveState(EPedMoveState.Walk);
                this.cop.Task.OpenPassengerDoor(this.suspect.CurrentVehicle, seat);
            }
        }

        /// <summary>
        /// Called when the player is close to <paramref name="policeDepartment"/>.
        /// </summary>
        /// <param name="policeDepartment">The police department.</param>
        /// <returns>True if player can enter the pd, false if not.</returns>
        private bool PoliceDepartmentManager_PlayerCloseToPD(PoliceDepartment policeDepartment)
        {
            return false;
        }

        /// <summary>
        /// The movement timer's callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void MovementTimerCallback(object[] parameter)
        {
            if (this.suspect.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 3 && !this.suspect.IsGettingIntoAVehicle && !this.isInVehicle)
            {
                this.suspect.Task.RunTo(CPlayer.LocalPlayer.Ped.Position.Around(1f));
                this.suspect.Task.PlayAnimSecondaryUpperBody("idle", "move_m@h_cuffed", 4.0f, true, 0, 0, 0, -1);
            }
        }
    }
}