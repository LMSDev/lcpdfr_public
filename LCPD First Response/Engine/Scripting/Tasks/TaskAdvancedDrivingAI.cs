namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine.DevTools;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// Improves the driving AI by monitoring distance to nearby objects and traffic and braking if appropriate. Sticks around most of the time (so use permament).
    /// </summary>
    internal class TaskAdvancedDrivingAI : PedTask
    {
        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// The maximum allowed speed.
        /// </summary>
        private float maxSpeed;

        /// <summary>
        /// The minimum distance to close objects.
        /// </summary>
        private float minDistance;

        /// <summary>
        /// The ray tracing instance.
        /// </summary>
        private Tracer tracer;

        /// <summary>
        /// The last front position of the vehicle;
        /// </summary>
        private Vector3 oldPositionFront;

        /// <summary>
        /// The last back position of the vehicle;
        /// </summary>
        private Vector3 oldPositionBack;

        /// <summary>
        /// Whether the vehicle is driving backwards.
        /// </summary>
        private bool isReversing;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAdvancedDrivingAI"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="maxSpeed">
        /// The max Speed.
        /// </param>
        /// <param name="minDistance">
        /// The minimum Distance.
        /// </param>
        public TaskAdvancedDrivingAI(CVehicle vehicle, float maxSpeed, float minDistance) : base(ETaskID.AdvancedDrivingAI)
        {
            this.vehicle = vehicle;
            this.maxSpeed = maxSpeed;
            this.minDistance = minDistance;

            this.tracer = new Tracer();
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskAdvancedDrivingAI";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            this.SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            // Set maximum speed
            GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)ped, this.maxSpeed);
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

            // Compare old to new position to get whether vehicle is reversing
            float distanceToOldPosition = this.vehicle.Position.DistanceTo(this.oldPositionFront);
            float distanceToOldPositionBack = this.vehicle.Position.DistanceTo(this.oldPositionBack);

            if (distanceToOldPositionBack 
                < distanceToOldPosition)
            {
                this.isReversing = true;
            }
            else
            {
                this.isReversing = false;
            }

            float length = this.vehicle.Model.GetDimensions().Y;
            this.oldPositionFront = this.vehicle.GetOffsetPosition(new Vector3(0, length / 2, 0));
            this.oldPositionBack = this.vehicle.GetOffsetPosition(new Vector3(0, -length / 2, 0));

            Game.DisplayText(this.vehicle.Speed + " -- " + this.isReversing.ToString());

            // Try to maintain speed
            if (this.vehicle.Speed > this.maxSpeed)
            {
                GTA.Native.Function.Call("SET_DRIVE_TASK_CRUISE_SPEED", (GTA.Ped)ped, this.maxSpeed);

                // If going way too fast, brake
                if (this.vehicle.Speed > this.maxSpeed + 5)
                {
                    // Brake a little
                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                    {
                        Log.Debug("Process: Vehicle is way too fast, started braking", this);
                        ped.Task.CarTempAction(ECarTempActionType.SlowDownSoftly, 250);
                    }
                }
            }

            //if (this.CollisionDetectionObjects())
            //{
            //    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
            //    {
            //        Log.Debug("Process: Vehicle close to object collision, started hard braking", this);
            //        ped.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 250);
            //    }
            //}

            if (this.CollisionDetectionVehicles())
            {
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                {
                    Log.Debug("Process: Vehicle close to vehicle collision, started hard braking", this);
                    ped.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 250);
                }
            }

            if (this.vehicle.IsStuck(5000))
            {
                // TODO: Check if front is clear
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                {
                    ped.Task.CarTempAction(ECarTempActionType.SpeedUpForwardsSoflty, 2500);
                }
            }
        }

        private bool CollisionDetectionVehicles()
        {
            // Get all close vehicles, not possible in real life (scan range depends on speed)
            float distance = 15f;  //this.vehicle.Speed * 2.5f;
            Vector3 maxPosition = this.vehicle.GetOffsetPosition(new Vector3(0, distance, 0));
            GTA.Native.Function.Call("DRAW_CHECKPOINT_WITH_ALPHA", maxPosition.X, maxPosition.Y, maxPosition.Z, 0.2f, 255, 255, 255, 200);

            // Some variables
            Vector3 tempPos = this.vehicle.GetOffsetPosition(new Vector3(0, -50, 0));
            Vector2 ourStartPoint = new Vector2(tempPos.X, tempPos.Y);
            tempPos = this.vehicle.GetOffsetPosition(new Vector3(0, 50, 0));
            Vector2 ourStartPoint2 = new Vector2(tempPos.X, tempPos.Y);

            CVehicle[] vehicles = this.vehicle.Driver.Intelligence.GetVehiclesAround(distance, EVehicleSearchCriteria.All);
            

            foreach (CVehicle closeVehicle in vehicles)
            {
                if (closeVehicle == this.vehicle)
                {
                    continue;
                }

                // Are we actually able to see the car (this includes vehicles after buildings too, but it is a pretty cheap check, so nvm)
                if (closeVehicle.IsOnScreen)
                {
                    // Calculate possibilty of crashing into the vehicle, we can easily grab the vehicles direction here (the advantage of a simulation)
                    Vector3 tempPos2 = closeVehicle.GetOffsetPosition(new Vector3(0, -50, 0));
                    Vector2 vehicleStartPoint = new Vector2(tempPos2.X, tempPos2.Y);
                    tempPos2 = closeVehicle.GetOffsetPosition(new Vector3(0, 50, 0));
                    Vector2 vehicleStartPoint2 = new Vector2(tempPos2.X, tempPos2.Y);
                    Vector2 lineIntersection2DVector = this.LineIntersectionPoint(ourStartPoint, ourStartPoint2, vehicleStartPoint, vehicleStartPoint2);
                    Vector3 lineIntersection3D = new Vector3(lineIntersection2DVector, World.GetGroundZ(new Vector3(lineIntersection2DVector, 0)));

                    // Now that we know where the vehicles will crash, we calculate the time they will need to reach the points
                    float distanceOurVehicle = this.vehicle.Position.DistanceTo2D(lineIntersection3D);
                    float distanceOtherVehicle = closeVehicle.Position.DistanceTo2D(lineIntersection3D);
                    float timeOurVehicle = distanceOurVehicle / Common.EnsureValueIsNotZero<float>(this.vehicle.Speed);
                    float timeOtherVehicle = distanceOtherVehicle / Common.EnsureValueIsNotZero<float>(closeVehicle.Speed);

                    // Check where other vehicle will be when we are at the spot
                    Vector3 otherVehiclePositionWhenWeReached = closeVehicle.GetOffsetPosition(new Vector3(0, timeOurVehicle * closeVehicle.Speed, 0));
                    GTA.Native.Function.Call("DRAW_CHECKPOINT_WITH_ALPHA", otherVehiclePositionWhenWeReached.X, otherVehiclePositionWhenWeReached.Y, otherVehiclePositionWhenWeReached.Z, 1.2f, 100, 100, 255, 200);

                    // Is it realistic we will both be at the spot at the same time? (Verify we are far away enough then)
                    float distanceToCollisionSpot = lineIntersection3D.DistanceTo2D(otherVehiclePositionWhenWeReached);
                    GTA.Game.DisplayText(distanceToCollisionSpot.ToString() + " -- " + closeVehicle.Model.GetDimensions().Y);
                    GTA.Native.Function.Call("DRAW_CHECKPOINT_WITH_ALPHA", lineIntersection3D.X, lineIntersection3D.Y, lineIntersection3D.Z, 1.2f, 255, 255, 165, 255);

                    // Check if vehicle rectangles intersect
                    // Get rectangle in world coordinates of our vehicle
                    Vector3 diffVec = lineIntersection3D - this.vehicle.Position;
                    Vector3 diffVecOther = otherVehiclePositionWhenWeReached - closeVehicle.Position;
                    Vector3[] modelRectangle = this.GetModelRectangle(this.vehicle, diffVec);
                    Vector3[] modelRectangleOther = this.GetModelRectangle(closeVehicle, diffVecOther);


                    foreach (Vector3 vector3 in modelRectangle)
                    {
                        // Move model rectangle points to collosion point
                        GUI.Gui.DrawColouredCylinder(vector3, System.Drawing.Color.Orange);
                    }

                    foreach (Vector3 vector3 in modelRectangleOther)
                    {
                        GUI.Gui.DrawColouredCylinder(vector3, System.Drawing.Color.FromArgb(200, 100, 100, 255));
                    }


                    if (this.DoRectanglesIntersect(modelRectangle, modelRectangleOther))
                    {
                        GTA.Game.DisplayText("COLLISION IN " + timeOurVehicle);
                    }
                }
            }

            return false;
        }

        private bool CollisionDetectionObjects()
        {
            bool ret = false;
            string debugPrint = string.Empty;

            // Check for possible crashes
            float width = this.vehicle.Model.GetDimensions().X;
            float length = this.vehicle.Model.GetDimensions().Y;

            // Ray trace
            Vector3 vehicleFront;
            Vector3 endPos;
            Vector3 hitPos = Vector3.Zero;
            Vector3 hitNormal = Vector3.Zero;
            uint hitEntity = 0;

            if (!this.isReversing)
            {
                vehicleFront = this.vehicle.GetOffsetPosition(new Vector3(0, length / 2, -0.3f));
                endPos = this.vehicle.GetOffsetPosition(new Vector3(0, (length / 2) + 100, -0.3f));

                // Check if we hit something within the current stopping distance
                if (this.tracer.DoTrace(vehicleFront, endPos, ref hitEntity, ref hitPos, ref hitNormal))
                {
                    // If type is game world and vehicle's pitch is too high, don't break (since it probably drives up a  bridge or something)
                    if (this.IsValidHit(hitEntity, hitPos))
                    {
                        // Calculate time to hit
                        BrakeTesting brakeTesting = new BrakeTesting();
                        Vector3 position = brakeTesting.GetStoppingPosition(this.vehicle);
                        float stoppingDistance = position.DistanceTo(this.vehicle.Position);
                        float distanceToObstacle = this.vehicle.Position.DistanceTo(hitPos);

                        if ((stoppingDistance * 1.1) > distanceToObstacle)
                        {
                            ret = true;
                        }

                        debugPrint += "Left: Valid -- ";
                    }

                    GTA.Native.Function.Call("DRAW_CHECKPOINT_WITH_ALPHA", hitPos.X, hitPos.Y, hitPos.Z, 0.2f, 255, 255, 255, 200);
                }
                vehicleFront = this.vehicle.GetOffsetPosition(new Vector3(width / 2, length / 2, -0.3f));
                endPos = this.vehicle.GetOffsetPosition(new Vector3(width / 2, (length / 2) + 100, -0.3f));

                if (this.tracer.DoTrace(vehicleFront, endPos, ref hitEntity, ref hitPos, ref hitNormal))
                {
                    if (this.IsValidHit(hitEntity, hitPos))
                    {
                        // Calculate time to hit
                        BrakeTesting brakeTesting = new BrakeTesting();
                        Vector3 position = brakeTesting.GetStoppingPosition(this.vehicle);
                        float stoppingDistance = position.DistanceTo(this.vehicle.Position);
                        float distanceToObstacle = this.vehicle.Position.DistanceTo(hitPos);

                        if ((stoppingDistance * 1.1) > distanceToObstacle)
                        {
                            ret = true;
                        }

                        debugPrint += "Center: Valid -- ";
                    }

                    GTA.Native.Function.Call("DRAW_CHECKPOINT_WITH_ALPHA", hitPos.X, hitPos.Y, hitPos.Z, 0.2f, 255, 255, 255, 200);
                }

                vehicleFront = this.vehicle.GetOffsetPosition(new Vector3(-width / 2, length / 2, -0.3f));
                endPos = this.vehicle.GetOffsetPosition(new Vector3(-width / 2, (length / 2) + 100, -0.3f));
                if (this.tracer.DoTrace(vehicleFront, endPos, ref hitEntity, ref hitPos, ref hitNormal))
                {
                    if (this.IsValidHit(hitEntity, hitPos))
                    {
                        // Calculate time to hit
                        BrakeTesting brakeTesting = new BrakeTesting();
                        Vector3 position = brakeTesting.GetStoppingPosition(this.vehicle);
                        float stoppingDistance = position.DistanceTo(this.vehicle.Position);
                        float distanceToObstacle = this.vehicle.Position.DistanceTo(hitPos);

                        if ((stoppingDistance * 1.1) > distanceToObstacle)
                        {
                            ret = true;
                        }

                        debugPrint += "Right: Valid";
                    }

                    GTA.Native.Function.Call("DRAW_CHECKPOINT_WITH_ALPHA", hitPos.X, hitPos.Y, hitPos.Z, 0.2f, 255, 255, 255, 200);
                }
            }

            if (!string.IsNullOrEmpty(debugPrint))
            {
                Game.DisplayText(debugPrint);
            }

            return ret;
        }

        /// <summary>
        /// Tries to determine whether we hit an actually valid target (e.g. a building) and not just a steep road.
        /// </summary>
        /// <param name="hitEntity">The hit entity.</param>
        /// <param name="position">The position of the hit.</param>
        /// <returns>True if valid hit, false otherwise.</returns>
        private bool IsValidHit(uint hitEntity, Vector3 position)
        {
            int hitEntityType = 0;

            if (hitEntity != 0)
            {
                hitEntityType = AdvancedHookManaged.AGame.GetTypeOfEntity((int)hitEntity);
            }

            if (hitEntityType == 2)
            {
                // We hit a static object, to ensure it's not a road, we perform a height check
                float z = World.GetGroundZ(position, GroundType.NextAboveCurrent);
                float diff = z - position.Z;

                // No object above, looks like a road
                if (diff <= 0.1f)
                {
                    return false;
                }
            }

            return true;
        }

        Vector2 LineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2,
   Vector2 pe2)
        {
            // Get A,B,C of first line - points : ps1 to pe1
            float A1 = pe1.Y - ps1.Y;
            float B1 = ps1.X - pe1.X;
            float C1 = A1 * ps1.X + B1 * ps1.Y;

            // Get A,B,C of second line - points : ps2 to pe2
            float A2 = pe2.Y - ps2.Y;
            float B2 = ps2.X - pe2.X;
            float C2 = A2 * ps2.X + B2 * ps2.Y;

            // Get delta and check if the lines are parallel
            float delta = A1 * B2 - A2 * B1;
            if (delta == 0)
                throw new System.Exception("Lines are parallel");

            // now return the Vector2 intersection point
            return new Vector2(
                (B2 * C1 - B1 * C2) / delta,
                (A1 * C2 - A2 * C1) / delta
            );
        }

        private bool DoRectanglesIntersect(Vector3[] rectangleA, Vector3[] rectangleB)
        {
            foreach (Vector3 vector3 in rectangleB)
            {
                if (this.IsPointInRectangle(vector3, rectangleA[0], rectangleA[1], rectangleA[2], rectangleA[3]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPointInRectangle(Vector3 point, Vector3 leftCorner, Vector3 rightCorner, Vector3 leftCorner2, Vector3 rightCorner2)
        {
            // Here come's the fun stuff
            Vector3 position = point;
            Vector3 point0 = leftCorner;
            Vector3 point1 = leftCorner2;
            bool line0 = ((point1.X - point0.X) * (position.Y - point0.Y) - (point1.Y - point0.Y) * (position.X - point0.X) < 0);

            point0 = rightCorner;
            point1 = rightCorner2;
            bool line1 = ((point1.X - point0.X) * (position.Y - point0.Y) - (point1.Y - point0.Y) * (position.X - point0.X) >= 0);

            point0 = leftCorner;
            point1 = rightCorner;
            bool line2 = ((point1.X - point0.X) * (position.Y - point0.Y) - (point1.Y - point0.Y) * (position.X - point0.X) >= 0);

            point0 = leftCorner2;
            point1 = rightCorner2;
            bool line3 = ((point1.X - point0.X) * (position.Y - point0.Y) - (point1.Y - point0.Y) * (position.X - point0.X) < 0);
            return line0 && line1 && line2 && line3;
        }

        private Vector3[] GetModelRectangle(CVehicle vehicle, Vector3 offset)
        {
            Vector3 modelDimensions = vehicle.Model.GetDimensions();
            float width = modelDimensions.X;
            float length = modelDimensions.Y;

            Vector3 leftCorner = vehicle.GetOffsetPosition(new Vector3(-width / 2, -length / 2, 0));
            Vector3 rightCorner = vehicle.GetOffsetPosition(new Vector3(width / 2, -length / 2, 0));

            Vector3 leftOutmostCorner = vehicle.GetOffsetPosition(new Vector3(-width / 2, length / 2, 0));
            Vector3 rightOutmostCorner = vehicle.GetOffsetPosition(new Vector3(width / 2, length / 2, 0));
           return new Vector3[] { leftCorner + offset, rightCorner + offset, leftOutmostCorner + offset, rightOutmostCorner + offset }; 
        }
    }
}