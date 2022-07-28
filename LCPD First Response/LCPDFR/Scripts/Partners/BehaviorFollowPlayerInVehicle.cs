namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using GTA;
    using GTA.@base;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    class BehaviorFollowPlayerInVehicle : Behavior
    {
        private bool softBraking;

        private TaskGetInVehicle getInVehicleTask;

        private readonly Partner partner;

        private readonly CVehicle vehicle;

        private bool wasInVehicle;

        private bool isSwitchingSiren;

        private string[] argueStrings = new string[] { "Treat it well", "I have a job interview", "I should have stayed in bed", "Enjoy the ride officer", 
            "I knew I should have applied for LCPD", "I need it for my business", "It's rented" };

        public BehaviorFollowPlayerInVehicle(Partner partner, CVehicle vehicle)
        {
            this.partner = partner;
            this.vehicle = vehicle;
        }

        public override void OnAbort()
        {
            if (this.getInVehicleTask != null && this.getInVehicleTask.Active)
            {
                this.getInVehicleTask.MakeAbortable(this.partner.PartnerPed);
            }

            if (this.partner.IsAlive)
            {
                if (this.partner.PartnerPed.IsInVehicle)
                {
                    this.partner.PartnerPed.LeaveVehicle();
                }
            }
        }

        public override EBehaviorState Run()
        {
            if (!wasInVehicle)
            {
                if (!this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                {
                    if (this.partner.PartnerPed.IsInVehicle(this.vehicle))
                    {
                        this.wasInVehicle = true;
                        return this.BehaviorState;
                    }

                    // If another vehicle, exit it first.
                    if (this.partner.PartnerPed.IsInVehicle)
                    {
                        if (!this.partner.PartnerPed.IsLeavingVehicle)
                        {
                            this.partner.PartnerPed.LeaveVehicle();
                        }

                        return this.BehaviorState;
                    }

                    if (this.partner.PartnerPed.Intelligence.IsVehicleBlacklisted(this.vehicle))
                    {
                        Log.Debug("Vehicle is blacklisted for partner", "BehaviorFollowPlayerInVehicle");
                        return this.BehaviorState;
                    }

                    if (this.vehicle.HasDriver)
                    {
                        CPed driver = this.vehicle.Driver;
                        if (driver != null && driver.Exists())
                        {
                            if (driver.PedGroup == EPedGroup.Pedestrian && driver.PedData.Available)
                            {
                                if (!driver.PedData.AskedToLeaveVehicle)
                                {
                                    this.partner.PartnerPed.Task.RunTo(this.vehicle.GetOffsetPosition(new Vector3(-2, 0, 0)));
                                    DelayedCaller.Call(parameter => this.partner.PartnerPed.SayAmbientSpeech("COMMANDEER_VEHICLE"), this, 100);
                                    DelayedCaller.Call(parameter => driver.Task.LeaveVehicle(), this, 1200);
                                    DelayedCaller.Call(parameter => driver.Task.GoTo(this.vehicle.GetOffsetPosition(new Vector3(-3, 0, 0))), this, 2200);
                                    DelayedCaller.Call(
                                        delegate(object[] parameter)
                                        {
                                            if (driver.Exists())
                                            {
                                                TaskArgue taskArgue = new TaskArgue(this.partner.PartnerPed, 5500);
                                                taskArgue.AddLine(Common.GetRandomCollectionValue<string>(this.argueStrings));
                                                taskArgue.AssignTo(driver, ETaskPriority.MainTask);
                                            }
                                        },
                                        this,
                                        5000);

                                    driver.PedData.AskedToLeaveVehicle = true;
                                }

                                return this.BehaviorState;
                            }
                        }
                    }

                    // Find out where we can enter.
                    VehicleSeat seat = this.partner.Intelligence.FindSeatForVehicle(this.vehicle, true);
                    if (seat == VehicleSeat.None)
                    {
                        this.partner.PartnerPed.Intelligence.AddVehicleToBlacklist(this.vehicle, 5000);
                        Log.Debug("Process: Failed to find seat", "BehaviorFollowPlayerInVehicle");
                        return this.BehaviorState;
                    }

                    this.partner.Intelligence.SetIsInPlayerGroup(false);
                    Log.Debug("Removed partner from player group", "BehaviorFollowPlayerInVehicle");

                    this.partner.PartnerPed.Task.ClearAll();
                    this.getInVehicleTask = new TaskGetInVehicle(vehicle, seat, seat, false, true);
                    this.getInVehicleTask.EnterStyle = EPedMoveState.Run;
                    this.getInVehicleTask.AssignTo(this.partner.PartnerPed, ETaskPriority.MainTask);
                    Log.Debug("Process: Made partner enter vehicle", "BehaviorFollowPlayerInVehicle");
                }
            }
            else
            {
                // Check if ped is still in vehicle.
                if (this.partner.PartnerPed.IsInVehicle(this.vehicle))
                {
                    if (this.partner.PartnerPed.IsDriver)
                    {
                        bool followTaskRunning = this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveMission);
                        if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                        {
                            bool ourSiren = this.vehicle.SirenActive;
                            bool playerSiren = CPlayer.LocalPlayer.Ped.CurrentVehicle.SirenActive;
                            if (ourSiren != playerSiren)
                            {
                                if (!this.isSwitchingSiren)
                                { 
                                    DelayedCaller.Call(delegate { this.vehicle.SirenActive = playerSiren; this.isSwitchingSiren = false; }, this, Common.GetRandomValue(250, 750));
                                }

                                this.isSwitchingSiren = true;
                            }

                            this.ProcessFollowingPlayer(followTaskRunning);
                        }
                        else
                        {
                            if (!followTaskRunning)
                            {
                                this.partner.PartnerPed.Task.DriveTo(CPlayer.LocalPlayer.Ped, 15f, false, true);
                            }
                        }
                    }
                }
                else
                {
                    // No longer in vehicle.
                }
            }

            return this.BehaviorState;
        }

        private void ProcessFollowingPlayer(bool followTaskRunning)
        {
            bool performingAction = this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction);
            bool lowSpeedTolerance = CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed < 8f;
            float speedTolerance = 6;

            if (!followTaskRunning && !performingAction)
            { 
                GTA.Native.Function.Call("MARK_CAR_AS_CONVOY_CAR", (GTA.Vehicle)CPlayer.LocalPlayer.Ped.CurrentVehicle, 1);
                GTA.Native.Function.Call("MARK_CAR_AS_CONVOY_CAR", (GTA.Vehicle)this.partner.PartnerPed.CurrentVehicle, 1);
                GTA.Native.Function.Call("TASK_CAR_MISSION", (GTA.Ped)this.partner.PartnerPed, (GTA.Vehicle)this.partner.PartnerPed.CurrentVehicle, (GTA.Vehicle)CPlayer.LocalPlayer.Ped.CurrentVehicle, 12, 65.0f, 3, 4, 20);
            }


            float distanceToPlayer = this.partner.PartnerPed.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position);
            float speedDifference = this.partner.PartnerPed.CurrentVehicle.Speed - CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed;

            //GTA.Game.DisplayText("Distance: " + distanceToPlayer + " -- Speed diff: " + speedDifference);

            // If player vehicle is going really slow (or even standing still) allow us to catch up quickly.
            if (lowSpeedTolerance)
            {
                speedTolerance = 12;
            }

            // If close to player, ensure partner isn't too fast.
            if (distanceToPlayer < 20)
            {
                speedTolerance = 4;
                if (lowSpeedTolerance)
                {
                    speedTolerance = 8;
                }

                // Brake a little.
                if (speedDifference > speedTolerance)
                {
                    this.partner.PartnerPed.Task.CarTempAction(ECarTempActionType.SlowDownSoftly, 500);
                    this.softBraking = true;
                }

                // If getting closer and still going too fast, brake harder.
                if (distanceToPlayer < 15)
                {
                    if (speedDifference > 4 && speedDifference < 8 && !lowSpeedTolerance)
                    {
                        if (!performingAction || this.softBraking)
                        {
                            this.softBraking = false;
                            this.partner.PartnerPed.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 500);
                        }
                    }
                    else if (speedDifference > 8 && this.partner.PartnerPed.CurrentVehicle.IsOnAllWheels)
                    {
                        // Last resort: Fake braking by lowering speed directly, but only if on all wheels to prevent weird results in air.
                        this.partner.PartnerPed.CurrentVehicle.Speed--;
                    }
                }
            }

            // We don't want the vehicle to go too fast.
            GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)this.partner.PartnerPed, CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed + speedTolerance);
        }
    }
}