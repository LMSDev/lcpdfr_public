namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR;
    using LCPD_First_Response.LCPDFR.GUI;
    using System.Collections.Generic;

    /// <summary>
    /// The cop helicopter task to chase suspects. Based on the seat, the task will either make the ped follow the ped as a driver, or return fire as a passenger.
    /// </summary>
    internal class TaskCopHelicopter : PedTask
    {
        /// <summary>
        /// The target.
        /// </summary>
        private CPed target;

        /// <summary>
        /// Designated attack helicopter which will try to kill the suspect
        /// </summary>
        private static CVehicle attackHelicopter;

        /// <summary>
        /// Whether this ped is driver.
        /// </summary>
        private bool isDriver;

        /// <summary>
        /// Whether the heli task has been assigned
        /// </summary>
        private bool taskAssigned;

        /// <summary>
        /// Every heli has a different mininum height to avoid clashes
        /// </summary>
        private int minHeight;

        /// <summary>
        /// Whether a HelicopterDownEvent has been fired for this helicopter
        /// </summary>
        private bool downEventFired;

        /// <summary>
        /// If an air support unit has reported spotting the suspect
        /// </summary>
        private bool suspectSpotted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopHelicopter"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public TaskCopHelicopter(CPed target) : base(ETaskID.CopHelicopter)
        {
            this.target = target;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskCopHelicopter";
            }
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);
            this.isDriver = ped.IsDriver;

            if (this.isDriver)
            {
                this.target.Wanted.HelicoptersChasing++;
            }

            // Generate a random minium height value so that the helicopters don't hover at the same height
            this.minHeight = Common.GetRandomValue(115, 150);

            // If no attack helicopter is assigned, see if we can assign this one.
            if (attackHelicopter == null)
            {
                if (ped.CurrentVehicle.Model == "ANNIHILATOR")
                {
                    attackHelicopter = ped.CurrentVehicle;
                }
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            // If heli searchlight is still on, turn off
            if (ped.Exists() && ped.IsInVehicle && ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsHelicopter))
            {
                ped.CurrentVehicle.HeliSearchlightOn = false;
                if (ped.CurrentVehicle == attackHelicopter) attackHelicopter = null;
            }

            // If cop is driver, assign FlyOff task
            if (ped.Exists() && ped.IsInVehicle && ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsHelicopter))
            {
                if (this.isDriver)
                {
                    Log.Debug("MakeAbortable: Making heli fly off", this);
                    TaskHeliFlyOff taskHeliFlyOff = new TaskHeliFlyOff();
                    taskHeliFlyOff.AssignTo(ped, ETaskPriority.MainTask);

                    // Also, check if the helicopter is dead, if so, fire an event
                    if (!downEventFired)
                    {
                        if (!ped.CurrentVehicle.IsDriveable || !ped.CurrentVehicle.IsAlive || !ped.IsAliveAndWell || ped.CurrentVehicle.IsOnFire)
                        {
                            if (ped.CurrentVehicle.IsAlive)
                            {
                                AudioHelper.PlayActionInScanner("HELICOPTER_DOWN");
                            }

                            new EventHelicopterDown(ped.CurrentVehicle);
                            downEventFired = true;
                        }
                    }
                }
                else
                {
                    ped.Task.AlwaysKeepTask = true;
                    ped.Task.Wait(int.MaxValue);
                }
            }

            if (this.isDriver)
            {
                this.target.Wanted.HelicoptersChasing--;
            }

            // If the search area is active on the target and is still the bigger helicopter one, set it to normal.
            if (this.target != null && this.target.Exists())
            {
                if (this.target.SearchArea != null)
                {
                    if (this.target.SearchArea.Size != 200)
                    {
                        this.target.SearchArea.Size = 200;
                    }
                }
            }

            this.SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!ped.IsInVehicle)
            {
                this.MakeAbortable(ped);
                return;
            }

            if (!this.target.Exists())
            {
                this.MakeAbortable(ped);
                return;
            }

            // Driver logic
            if (this.isDriver)
            {
                this.ProcessDriver(ped);
            }
            else
            {
                this.ProcessPassenger(ped);
            }
        }

        /// <summary>
        /// Processes the driver logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        private void ProcessDriver(CPed ped)
        {
            // If visual is lost, deactivate searchlight
            if (this.target.Wanted.VisualLost)
            {
                ped.CurrentVehicle.HeliSearchlightOn = false;
            }
            else
            {
                // At night, turn on searchlight as driver
                if (World.CurrentDayTime.Hours > 20 || World.CurrentDayTime.Hours < 6)
                {
                    ped.CurrentVehicle.HeliSearchlightOn = true;
                }
                else
                {
                    ped.CurrentVehicle.HeliSearchlightOn = false;
                }
            }

            if (!suspectSpotted)
            {
                if (!ped.Wanted.Invisible && ped.HasSpottedChar(this.target) && ped.Position.DistanceTo(this.target.Position) < 150.0f)
                {
                    AudioHelper.PlayActionInScanner("HELICOPTER_SEES_SUSPECT");
                    suspectSpotted = true;
                    taskAssigned = false;
                }
            }

            // Follow target, even when visual is lost (just with deactivated spotlight)
            if (!taskAssigned)
            {
                if (this.target.Wanted.VisualLost)
                {
                    Vector3 searchPos = this.target.Wanted.LastKnownPosition.Around(100.0f);

                    searchPos.Z = World.GetGroundZ(searchPos, GroundType.Highest) + 15;

                    if (searchPos.Z < 80)
                    {
                        searchPos.Z = 105;
                    }

                    Native.Natives.TaskHeliMission(ped, ped.CurrentVehicle, 0, 0, searchPos, 4, 500.0f, 0, -1, 175, this.minHeight);

                    suspectSpotted = false;
                    taskAssigned = true;

                    DelayedCaller.Call(delegate { taskAssigned = false; }, 30000);
                }
                else
                {
                    if (this.target.PedData.CurrentChase.HeliTactic == EHeliTactic.Active)
                    {
                        if (ped.CurrentVehicle.Model == "ANNIHILATOR")
                        {
                            // New task by Sam - makes the helicopter follow the suspect a little bit lower, sometimes within shooting range.  Prone to crashing tall buildings however.
                            Native.Natives.TaskHeliMission(ped, ped.CurrentVehicle, 0, this.target.Handle, new Vector3(this.target.Position.X, this.target.Position.Y, this.target.Position.Z + 100), 10, 500.0f, 0, -1, 175, this.minHeight);
                        }
                        else
                        {
                            // New task by Sam - makes the helicopter stay very high above the suspect - great for the searchlight and keeping a visual.
                            Native.Natives.TaskHeliMission(ped, ped.CurrentVehicle, 0, 0, new Vector3(this.target.Position.X, this.target.Position.Y, this.target.Position.Z + 60), 4, 500.0f, 0, -1, 175, this.minHeight);
                        }
                    }
                    else
                    {
                        // New task by Sam - makes the helicopter stay very high above the suspect - great for the searchlight and keeping a visual.
                        Native.Natives.TaskHeliMission(ped, ped.CurrentVehicle, 0, 0, new Vector3(this.target.Position.X, this.target.Position.Y, this.target.Position.Z + 60), 4, 500.0f, 0, -1, 175, this.minHeight);
                    }

                    CVehicle.SetHeliSearchlightTarget(this.target);

                    // Set taskAssigned to true and make it update every second
                    taskAssigned = true;
                    DelayedCaller.Call(delegate { taskAssigned = false; }, 1000);

                    // Also, we can add the old speech from the helicopter back in manually as well...
                    ped.Voice = "M_Y_HELI_COP";
                    if (!ped.IsAmbientSpeechPlaying)
                    {
                        if (Common.GetRandomBool(0, 12, 1))
                        {
                            if (!this.target.IsInVehicle && this.target.IsArmed() || this.target.IsShooting)
                            {
                                ped.SayAmbientSpeech("COP_HELI_MEGAPHONE_WEAPON");
                            }
                            else
                            {
                                ped.SayAmbientSpeech("COP_HELI_MEGAPHONE");
                            }
                        }
                    }
                }
            }

            // Old logic
            /* 
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexHelicopterStrafe))
                {
                    // Ensure spotlight is on. TODO: Spotlight for more than one suspect at the same time. Probably requires a hook and check for vehicle handle and return the 
                    // assigned ped then. Also add exists check for safety so targets can be deleted before heli target was cleared and the game won't crash
                    ped.APed.TaskHelicopterStrafe(this.target.APed);
                    ped.APed.TaskCombatPersueInCarSubtask(this.target.APed);

                    // Update target
                    CVehicle.SetHeliSearchlightTarget(this.target);
                }
            */
        }

        /// <summary>
        /// Processes the passenger logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        private void ProcessPassenger(CPed ped)
        {
            // HANDLED IN TaskCopChasePed Kill

            //if (ped.CurrentVehicle.Exists())
            //{
            //    if (attackHelicopter != null && attackHelicopter.Exists() && ped.CurrentVehicle == attackHelicopter)
            //    {
            //        // If this ped is in the attack helicopter, make it engage the suspect
            //        if (ped.GetSeatInVehicle() != VehicleSeat.RightFront)
            //        {
            //            // Ensure ped has weapon + ammo
            //            ped.Weapons.AssaultRifle_M4.Ammo = 999;

            //            if (ped.Weapons.Current != Weapon.Rifle_M4)
            //            {
            //                Native.Natives.SetCurrentCharWeapon(ped, Weapon.Rifle_M4, true);
            //            }

            //            // If not shooting, apply driveby task
            //            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGangDriveby) || !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleNewGangDriveBy))
            //            {
            //                if (ped.GetSeatInVehicle() == VehicleSeat.LeftRear)
            //                {
            //                    ped.Task.DriveBy(this.target, 0, 0.0f, 0.0f, 0.0f, 500.0f, 8, 0, 250);
            //                }
            //                else
            //                {
            //                    ped.Task.DriveBy(this.target, 0, 0.0f, 0.0f, 0.0f, 500.0f, 8, 1, 250);
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        ped.Task.Wait(int.MaxValue);
            //    }
            //}
        }
    }
}