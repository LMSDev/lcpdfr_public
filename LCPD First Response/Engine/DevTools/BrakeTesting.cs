namespace LCPD_First_Response.Engine.DevTools
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts.Scenarios;

    class BrakeTesting : GameScript
    {
        private float initialSpeed;

        private Vector3 initialPosition;

        private float currentSpeed;

        private float initialBreakDistance;

        private float distanceCovered;

        private bool stopped;

        private bool isRunning;

        private GTA.Checkpoint stopMarker;

        private GTA.Checkpoint stopMarker2;

        private DateTime initialStartTime;

        private TimeSpan timePassed;

        private float estimatedTimeToStop;

        private bool firstHit;

        public unsafe Vector3 GetStoppingPosition(CVehicle vehicle)
        {
            IntPtr vehiclePointer = new IntPtr(((GTA.Vehicle)vehicle).MemoryAddress);
            IntPtr handlingPointer = new IntPtr(vehiclePointer.ToInt32() + 0xE18);
            IntPtr handling = new IntPtr(*(int*)handlingPointer.ToPointer());
            IntPtr massPtr = new IntPtr(handling.ToInt32() + 0x10);
            IntPtr brakeForcePtr = new IntPtr(handling.ToInt32() + 0x74);
            float brakeForce = *(float*)brakeForcePtr.ToPointer();

            // When is speed zero?
            double stoppingDistance = this.GetStoppingDistance(vehicle.Speed, brakeForce);
            double vehicleLength = vehicle.Model.GetDimensions().Y / 2;

            return vehicle.GetOffsetPosition(new Vector3(0, (float)(stoppingDistance + vehicleLength), 0));
        }

        public double GetTimeToStop(float speed)
        {
            double time = 0;
            while (true)
            {
                // POLICE2, mass: 1700, breakforce: 0.24 (measured at 27.89 m/s)
                //double tempSpeed = (-0.008967861883050 * time) + speed;

                // POLICE2, mass: 1700, breakforce: 0.48 (measured at 27.89 m/s)
                //double tempSpeed = (-0.0117812782743528 * time) + speed;

                // POLICE2, mass: 1700, breakforce: 0.24 (measured at 41.6 m/s (top speed))
                //double tempSpeed = 0.000000282519277 * Math.Pow(time, 2) - 0.010576900701666 * time + speed;
                double firstCoefficient = 0.000000006386515 * speed - 0.000000017527706;
                double secondCoefficient = 0.000064603294756 * speed + 0.007838781356384;
                double tempSpeed = firstCoefficient * Math.Pow(time, 2) - secondCoefficient * time + speed;

                if (tempSpeed < 1)
                {
                    break;
                }

                time += 1;
            }

            return time;
        }

        public double GetTimeToStop(float speed, float brakeForce)
        {
            double brakeForceDeviation = 6655.978009259180000 * Math.Pow(brakeForce, 2) - 8588.605416666630000 * brakeForce + 1677.880966666660000;

            return this.GetTimeToStop(speed) + brakeForceDeviation;
        }

        public double GetStoppingDistance(float speed, float brakeForce)
        {
            double time = this.GetTimeToStop(speed);

            // POLICE2, mass: 1700, breakforce: 0.24 (measured at 27.89 m/s)
            //double stoppingDistance = -0.000004483762315 * Math.Pow(time, 2) + speed / 1000 * time + 0.01922881771884;

            // POLICE2, mass: 1700, breakforce: 0.48
            //double stoppingDistance = (-0.000005545834738 * Math.Pow(time, 2)) + ((speed / 1000) * time);

            // POLICE2, mass: 1700, breakforce: 0.24 (measured at 41.6 m/s (top speed))

            //double stoppingDistance = -0.000004740635938 * Math.Pow(time, 2) + (speed / 1013.3475443417132536151867303996) * time;


            double firstCoefficient = -0.000000016303865 * speed - 0.000004054335405;
            double secondCoefficient = 0.649164215612445 * speed + 986.37223917621;
            double brakeForceDeviation = 135.782638888887000 * Math.Pow(brakeForce, 2) - 172.652249999999000 * brakeForce + 33.615459999999900;
            double stoppingDistance = firstCoefficient * Math.Pow(time, 2) + (speed / secondCoefficient) * time;

            return stoppingDistance + brakeForceDeviation;
        }

        Tracer tracer = new Tracer();

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public unsafe override void Process()
        {
            base.Process();

            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                // Reset scenarios
                AmbientScenarioManager ambientScenarioManager = LCPDFR.Main.ScriptManager.GetRunningScriptInstances("AmbientScenarioManager")[0] as AmbientScenarioManager;
                if (ambientScenarioManager != null) ambientScenarioManager.AllowAmbientScenarios = false;
                LCPDFR.Main.CalloutManager.AllowRandomCallouts = false;
                AreaHelper.ClearArea(CPlayer.LocalPlayer.Ped.Position, 200f, true, true);

                if (this.stopMarker != null)
                {
                    this.stopMarker.Disable();
                    this.stopMarker = null;
                }

                Vector3 breakPosition = this.GetStoppingPosition(CPlayer.LocalPlayer.Ped.CurrentVehicle);
                this.stopMarker = new Checkpoint(breakPosition, Color.Red, 0.5f);

                if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed < 0.2f)
                {
                    if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                    {
                        CPlayer.LocalPlayer.Ped.Task.ClearAll();
                    }
                }

                if (KeyHandler.IsKeyboardKeyStillDown(Keys.LControlKey))
                {
                    float length = CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.GetDimensions().Y;

                    // For safety reasons, we use a position even one metres before
                    breakPosition = breakPosition + (CPlayer.LocalPlayer.Ped.CurrentVehicle.Direction * (length / 2));

                    Vector3 vehicleFront = CPlayer.LocalPlayer.Ped.CurrentVehicle.GetOffsetPosition(new Vector3(0, length / 2, -0.3f));
                    Vector3 endPos = CPlayer.LocalPlayer.Ped.CurrentVehicle.GetOffsetPosition(new Vector3(0, length / 2 + 100, -0.3f));

                    // Ray trace
                    Vector3 hitPos = Vector3.Zero;
                    Vector3 hitNormal = Vector3.Zero;
                    uint hitEntity = 0;

                    // Check if we hit something within the current stopping distance
                    if (this.tracer.DoTrace(vehicleFront, endPos, ref hitEntity, ref hitPos, ref hitNormal))
                    {
                        int hitEntityType = -1;
                        if (hitEntity != 0)
                        {
                            hitEntityType = AdvancedHookManaged.AGame.GetTypeOfEntity((int)hitEntity);
                        }

                        GTA.Game.DisplayText("Hit Entity mem: " + hitEntity.ToString("X") + " -- Type: " + hitEntityType);


                        if (hitEntityType == 2)
                        {
                            // We hit a static object, to ensure it's not a road, we perform a height check
                            float z = World.GetGroundZ(hitPos, GroundType.NextAboveCurrent);
                            float diff = z - hitPos.Z;

                            GTA.Game.DisplayText(diff.ToString());

                            // No object above, looks like a road
                            if (diff <= 0.1f)
                            {
                                // Have a look at the object by going 0.3 metres in its direction
                                Vector3 position2 = hitPos - (hitNormal * 0.1f);
                                z = World.GetGroundZ(position2, GroundType.NextAboveCurrent);
                                diff = z - hitPos.Z;
                                if (diff <= 0.1f)
                                {
                                    if (this.stopMarker2 != null)
                                    {
                                        this.stopMarker2.Disable();
                                        this.stopMarker2 = null;
                                    }

                                    this.stopMarker2 = new Checkpoint(hitPos, Color.Green, 0.3f);

                                    if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                                    {
                                        CPlayer.LocalPlayer.Ped.Task.ClearAll();
                                    }
                                }

                                return;
                            }
                        }

                        // Draw hit position
                        if (this.stopMarker2 != null)
                        {
                            this.stopMarker2.Disable();
                            this.stopMarker2 = null;
                        }

                        this.stopMarker2 = new Checkpoint(hitPos, Color.Red, 0.3f);

                        // Check whether we will hit the object based on the current stopping distance
                        float distanceToCrash = vehicleFront.DistanceTo(hitPos);
                        float distanceToBreak = vehicleFront.DistanceTo(breakPosition);
                        if (distanceToCrash < distanceToBreak)
                        {
                            // Looks like we're going to crash, so trigger brake
                            if (!CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                            {
                                CPlayer.LocalPlayer.Ped.Task.CarTempAction(ECarTempActionType.SlowDownHard4, 15000);
                            }

                            // Calculate how worse the crash will be to decrease speed manually even more
                            if (distanceToCrash - 2 < distanceToBreak)
                            {
                                //CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed -= 1;
                            }
                        }
                    }
                    else
                    {
                        if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                        {
                            CPlayer.LocalPlayer.Ped.Task.ClearAll();
                        }
                    }
                }

                if (KeyHandler.IsKeyboardKeyDown(Keys.B))
                {
                    if (this.stopMarker2 != null)
                    {
                        this.stopMarker2.Disable();
                        this.stopMarker2 = null;
                    }

                    IntPtr vehiclePointer = new IntPtr(((GTA.Vehicle)CPlayer.LocalPlayer.Ped.CurrentVehicle).MemoryAddress);
                    IntPtr handlingPointer = new IntPtr(vehiclePointer.ToInt32() + 0xE18);
                    IntPtr handling = new IntPtr(*(int*)handlingPointer.ToPointer());
                    IntPtr massPtr = new IntPtr(handling.ToInt32() + 0x10);
                    float mass = *(float*)massPtr.ToPointer();
                    IntPtr brakeForcePtr = new IntPtr(handling.ToInt32() + 0x74);
                    float brakeForce = *(float*)brakeForcePtr.ToPointer();

                    if (KeyHandler.IsKeyboardKeyStillDown(Keys.LShiftKey))
                    {
                        CPlayer.LocalPlayer.Ped.CurrentVehicle.Position = new Vector3(-343.14f, 1164.55f, 14.49f);
                        CPlayer.LocalPlayer.Ped.CurrentVehicle.Heading = 270f;

                        // Read top speed
                        IntPtr topSpeedPtr = new IntPtr(handling.ToInt32() + 0x50);
                        CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed = *(float*)topSpeedPtr.ToPointer();
                    }
                    //*(float*)brakeForcePtr.ToPointer() = 0.48f;

                    Log.Info("MASS: " + mass, this);
                    Log.Info("BRAKEFORCE: " + brakeForce, this);

                    this.initialSpeed = CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed;
                    this.initialPosition = CPlayer.LocalPlayer.Ped.CurrentVehicle.Position;
                    CPlayer.LocalPlayer.Ped.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 15000);
                    this.initialStartTime = DateTime.Now;
                    this.isRunning = true;

                    // Calc things
                    double timeNeeded = this.GetTimeToStop(CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed, brakeForce);
                    this.estimatedTimeToStop = (float)timeNeeded;
                    double stoppingDistance = this.GetStoppingDistance(CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed, brakeForce);
                    this.initialBreakDistance = (float)stoppingDistance;

                    Vector3 pos = this.GetStoppingPosition(CPlayer.LocalPlayer.Ped.CurrentVehicle);
                    this.stopMarker2 = new Checkpoint(pos, Color.Orange, 0.5f);

                    this.firstHit = false;
                    this.stopped = false;
                }

                if (this.isRunning)
                {
                    this.currentSpeed = CPlayer.LocalPlayer.Ped.CurrentVehicle.Speed;
                    this.distanceCovered = CPlayer.LocalPlayer.Ped.CurrentVehicle.Position.DistanceTo2D(this.initialPosition);
                    this.stopped = CPlayer.LocalPlayer.Ped.CurrentVehicle.IsStopped;
                    this.timePassed = DateTime.Now - this.initialStartTime;

                    if (this.currentSpeed < 1 && !this.firstHit)
                    {
                        TextHelper.PrintText("Below 1m/s after " + this.timePassed.TotalMilliseconds + " and " + this.distanceCovered + "metres", 5000);
                        this.firstHit = true;
                        this.isRunning = false;
                    }

                    Log.Info("Time passed: " + this.timePassed.TotalMilliseconds + " -- Distance covered: " + this.distanceCovered + " -- Current speed: " + this.currentSpeed, this);
                    Game.DisplayText("start speed: " + this.initialSpeed + " -- Break distance: " + this.initialBreakDistance + " Curr speed: " + this.currentSpeed + " -- D. covered: " + this.distanceCovered + " -- Stopped: " + this.stopped + " -- Deviation: " + (this.initialBreakDistance - this.distanceCovered) + " -- ETA: " + this.estimatedTimeToStop + " -- Time: " + this.timePassed.TotalMilliseconds, 15000);
                }

                if (this.stopped)
                {
                    this.isRunning = false;
                }
            }
        }
    }
}
