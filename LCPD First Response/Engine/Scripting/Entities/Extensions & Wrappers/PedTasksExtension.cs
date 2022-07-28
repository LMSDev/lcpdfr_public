namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using LCPD_First_Response.Engine.Scripting.Native;

    static class PedTasksExtension
    {

        public enum AnimationFlags
        {
            None = 0,
            Unknown01 = 2,
            FreezePosition = 4,
            Unknown03 = 8,
            Unknown04 = 16,
            Loop = 32,
            NeverEnd = 64,
            Unknown07 = 128,
            Unknown08 = 256,
            BalanceUpperBody = 512,
            Unknown10 = 1024,
            AllowMovement = 2048,
            CanBeAborted = 4096
        }

        public static void AchieveHeading(this GTA.value.PedTasks pedTasks, float heading)
        {
            Natives.TaskAchieveHeading(pedTasks.ped, heading);
        }

        public static void ArrestChar(this GTA.value.PedTasks pedTasks, GTA.Ped pedToArrest)
        {
            Natives.TaskCharArrestChar(pedTasks.ped, pedToArrest);
        }

        public static void CarMission(this GTA.value.PedTasks pedTasks, GTA.Vehicle vehicle, int unknown, EVehicleDrivingStyle drivingStyle, float speed, int unknown2, int unknown3, int unknown4)
        {
            Natives.TaskCarMission(pedTasks.ped, vehicle, unknown, drivingStyle, speed, unknown2, unknown3, unknown4);
        }

        public static void CarTempAction(this GTA.value.PedTasks pedTasks, ECarTempActionType type, int duration)
        {
            Natives.TaskCarTempAction(pedTasks.ped, pedTasks.ped.CurrentVehicle, type, duration);
        }

        public static void CarTempAction(this GTA.value.PedTasks pedTasks, GTA.Vehicle vehicle, ECarTempActionType type, int duration)
        {
            Natives.TaskCarTempAction(pedTasks.ped, vehicle, type, duration);
        }

        public static void Climb(this GTA.value.PedTasks pedTasks, EJumpType jumpType)
        {
            Natives.TaskClimb(pedTasks.ped, (int)jumpType);
        }

        public static void Cower(this GTA.value.PedTasks pedTasks)
        {
            Natives.TaskCower(pedTasks.ped);
        }

        public static void DriveBy(this GTA.value.PedTasks pedTasks, GTA.Ped target, int unknown, float unknown2, float unknown3, float unknown4, float unknown5Distance, int unknown6, int unknown7, int unknown8)
        {
            Natives.TaskDriveBy(pedTasks.ped, target, unknown, unknown2, unknown3, unknown4, unknown5Distance, unknown6, unknown7, unknown8);
        }

        public static void DriveFastTo(this GTA.value.PedTasks pedTasks, GTA.Vehicle vehicle, GTA.Vector3 position)
        {
            Natives.TaskCarMission(pedTasks.ped, vehicle, position, 4, 30.0f, 2, 10, 10);
        }

        public static void DriveSlowlyTo(this GTA.value.PedTasks pedTasks, GTA.Vehicle vehicle, GTA.Vector3 position)
        {
            Natives.TaskCarMission(pedTasks.ped, vehicle, position, 4, 10.0f, 2, 10, 10);
        }

        public static void FleeFromCharAnyMeans(this GTA.value.PedTasks pedTasks, GTA.Ped pedToFleeFrom, float unknown, int unknown2, int unknown3, int unknown4, int unknown5, float unknown6)
        {
            Natives.TaskFleeCharAnyMeans(pedTasks.ped, pedToFleeFrom, unknown, unknown2, unknown3, unknown4, unknown5, unknown6);
        }

        public static void GoTo(this GTA.value.PedTasks pedTasks, CPed ped, EPedMoveState moveState)
        {
            Natives.SetNextDesiredMoveState(moveState);
            pedTasks.GoTo(ped);
        }

        public static void GoTo(this GTA.value.PedTasks pedTasks, GTA.Vector3 position, EPedMoveState moveState)
        {
            Natives.SetNextDesiredMoveState(moveState);
            pedTasks.GoTo(position);
        }

        public static void GoToCharAiming(this GTA.value.PedTasks pedTasks, GTA.Ped pedToAimAt, float stopAndStartAimingDistance, float startAimingWhileRunningDistance)
        {
            Natives.TaskGotoCharAiming(pedTasks.ped, pedToAimAt, stopAndStartAimingDistance, startAimingWhileRunningDistance);
        }

        public static void GoToCoordAiming(this GTA.value.PedTasks pedTasks, GTA.Vector3 position, EPedMoveState pedMoveState, GTA.Vector3 aimingPosition)
        {
            Natives.TaskGoToCoordWhileAiming(pedTasks.ped, position, pedMoveState, 0, 0, 0, aimingPosition, null);
        }

        public static void Jump(this GTA.value.PedTasks pedTasks, EJumpType type)
        {
            Natives.TaskJump(pedTasks.ped, (int)type);
        }

        public static void HeliMission(this GTA.value.PedTasks pedTasks, GTA.Vehicle vehicle, int unknown, int unknown2, GTA.Vector3 position, int unknown4, float speed, int unknown5, float unknown6, int unknown7, int maxHeight)
        {
            Natives.TaskHeliMission(pedTasks.ped, vehicle, unknown, unknown2, position, unknown4, speed, unknown5, unknown6, unknown7, maxHeight);
        }

        public static void LookAtChar(this GTA.value.PedTasks pedTasks, GTA.Ped pedToLookAt, int duration, EPedLookType lookType)
        {
            Natives.TaskLookAtChar(pedTasks.ped, pedToLookAt, duration, lookType);
        }

        public static void LookAtCoord(this GTA.value.PedTasks pedTasks, GTA.Vector3 position, int duration, EPedLookType lookType)
        {
            Natives.TaskLookAtCoord(pedTasks.ped, position, duration, lookType);
        }

        /// <summary>
        /// Opens a passenger door.
        /// </summary>
        /// <param name="pedTasks">The ped task instance.</param>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="seatNumber">0 = right front, 1 = left rear, 2 = right rear.</param>
        public static void OpenPassengerDoor(this GTA.value.PedTasks pedTasks, GTA.Vehicle vehicle, int seatNumber)
        {
            Natives.TaskOpenPassengerDoor(pedTasks.ped, vehicle, GTA.VehicleSeat.Driver, seatNumber);
        }

        /// <summary>
        /// Plays an animation with flags
        /// </summary>
        /// <param name="pedTasks">The ped task instance.</param>
        /// <param name="animationSet">The animation set.</param>
        /// <param name="animationName">The animation name.</param>
        /// <param name="flags">Flag documentation: (Keep in mind that flags can behave different on different animations)</param>
        public static void PlayAnimationWithFlags(this GTA.value.PedTasks pedTasks, GTA.AnimationSet animationSet, string animationName, float speed, AnimationFlags flags)
        {
            pedTasks.ped.Task.PlayAnimation(animationSet, animationName, speed, (GTA.AnimationFlags)flags);
        }

        /// <summary>
        /// Plays an animation with flags
        /// </summary>
        /// <param name="pedTasks">The ped task instance.</param>
        /// <param name="animation">The animation name.</param>
        /// <param name="animationSet">The animation set.</param>
        /// <param name="flags">Flag documentation: (Keep in mind that flags can behave different on different animations)</param>
        public static void PlayAnimationWithFlags(this GTA.value.PedTasks pedTasks, string animation, string animationSet, float speed, AnimationFlags flags)
        {
            pedTasks.ped.Task.PlayAnimation(new GTA.AnimationSet(animationSet), animation, speed, (GTA.AnimationFlags)flags);
        }

        /// <summary>
        /// Plays animation upper body only. Requests the animation before automatically.
        /// </summary>
        /// <param name="pedTasks">The ped task instance.</param>
        /// <param name="animation">The animation name.</param>
        /// <param name="animationSet">The animation set name.</param>
        /// <param name="loop">Loop the animation.</param>
        public static void PlayAnimSecondaryUpperBody(this GTA.value.PedTasks pedTasks, string animation, string animationSet, float unknown1, bool loop, int unknown2=0, int unknown3=0, int unknown4=0, int unknown5=-1)
        {
            Natives.RequestAnims(animationSet);
            Natives.TaskPlayAnimSecondaryUpperBody(pedTasks.ped, animation, animationSet, unknown1, loop, unknown2, unknown3, unknown4, unknown5);
        }

        /// <summary>
        /// Makes the ped slide over to the adjacent seat. A char will be kicked out of the vehicle if in the seat.
        /// </summary>
        /// <param name="pedTasks"></param>
        /// <param name="vehicle"></param>
        public static void ShuffleToNextCarSeat(this GTA.value.PedTasks pedTasks, GTA.Vehicle vehicle)
        {
            Natives.TaskShuffleToNextCarSeat(pedTasks.ped, vehicle);
        }

        public static void SlideToCoord(this GTA.value.PedTasks pedTasks, GTA.Vector3 position, float heading, float speed)
        {
            Natives.TaskCharSlideToCoord(pedTasks.ped, position, heading, speed);
        }
    }
}
