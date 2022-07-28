namespace LCPD_First_Response.Engine
{
    using System;

    /// <summary>
    /// Defines a spawnpoint, consisting of a <see cref="GTA.Vector3"/> as position and a <see cref="System.Single"/> as heading.
    /// </summary>
#pragma warning disable 660,661
    public struct SpawnPoint
#pragma warning restore 660,661
    {
        /// <summary>
        /// The heading.
        /// </summary>
        private readonly float heading;

        /// <summary>
        /// The position.
        /// </summary>
        private readonly GTA.Vector3 position;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpawnPoint"/> struct.
        /// </summary>
        /// <param name="heading">
        /// The heading.
        /// </param>
        /// <param name="position">
        /// The position.
        /// </param>
        public SpawnPoint(float heading, GTA.Vector3 position)
        {
            this.heading = heading;
            this.position = position;
        }


        /// <summary>
        /// Gets a zero spawnpoint.
        /// </summary>
        public static SpawnPoint Zero
        {
            get
            {
                return new SpawnPoint(0, GTA.Vector3.Zero);
            }
        }

        /// <summary>
        /// Gets the heading.
        /// </summary>
        public float Heading
        {
            get
            {
                return this.heading;
            }
        }

        /// <summary>
        /// Gets the position.
        /// </summary>
        public GTA.Vector3 Position
        {
            get
            {
                return this.position;
            }
        }

        /// <summary>
        /// Compares the two spawn point instances.
        /// </summary>
        /// <param name="spawnPoint">
        /// The spawn point.
        /// </param>
        /// <param name="spawnPoint2">
        /// The spawn point 2.
        /// </param>
        /// <returns>
        /// True if equal, false if not.
        /// </returns>
        public static bool operator ==(SpawnPoint spawnPoint, SpawnPoint spawnPoint2)
        {
            return spawnPoint.position == spawnPoint2.position && spawnPoint.heading == spawnPoint2.heading;
        }

        /// <summary>
        /// Compares the two spawn point instances.
        /// </summary>
        /// <param name="spawnPoint">
        /// The spawn point.
        /// </param>
        /// <param name="spawnPoint2">
        /// The spawn point 2.
        /// </param>
        /// <returns>
        /// False if equal, true if not.
        /// </returns>
        public static bool operator !=(SpawnPoint spawnPoint, SpawnPoint spawnPoint2)
        {
            return !(spawnPoint == spawnPoint2);
        }
    }


    /// <summary>
    /// Contains often used functions, that don't fit into a category.
    /// </summary>
    public class Common
    {
        /// <summary>
        /// Static instance used to calculate random numbers
        /// </summary>
        private static Random random;

        /// <summary>
        /// The direction of a heading.
        /// </summary>
        public enum EDirection
        {
            /// <summary>
            /// No direction.
            /// </summary>
            None,

            /// <summary>
            /// North, top (0/360°).
            /// </summary>
            North,

            /// <summary>
            /// North-West, top left (0.1°-89.9°).
            /// </summary>
            NorthWest,

            /// <summary>
            /// West, left (90°).
            /// </summary>
            West,

            /// <summary>
            /// South-West, bottom left (90.1°-179.9°).
            /// </summary>
            SouthWest,

            /// <summary>
            /// South, bottom (180°).
            /// </summary>
            South,

            /// <summary>
            /// South-East, bottom right (180.1°-269.9°).
            /// </summary>
            SouthEast,

            /// <summary>
            /// East, right (270°).
            /// </summary>
            East,

            /// <summary>
            /// North-East, top right (270.1°-359.9°).
            /// </summary>
            NorthEast,
        }

        /// <summary>
        /// Returns if the point can be seen from point2 using the given direction and float value (-1 to 1)
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="value">The value. 1 is directly ahead, -1 directly behind, 0 is to the side.</param>
        /// <returns>True if can be seen, false if not.</returns>
        public static bool CanPointBeSeenFromPoint(GTA.Vector3 point, GTA.Vector3 point2, GTA.Vector3 direction, float value)
        {
            // Check if vehicle can be seen using 30 as fov so we only see vehicles straight infront of the ped
            var test = point - point2;
            test.Normalize();
            var dir = direction;
            dir.Normalize();

            float dot = GTA.Vector3.Dot(test, dir);
            return dot > value;
        }

        /// <summary>
        /// Converts <paramref name="heading"/> to a world-point direction.
        /// </summary>
        /// <param name="heading">The heading.</param>
        /// <returns>The direction.</returns>
        public static EDirection ConvertHeadingToDirection(float heading)
        {
            if (heading == 0 || heading == 360)
            {
                return EDirection.North;
            }

            if (heading > 0 && heading < 90)
            {
                return EDirection.NorthWest;
            }

            if (heading == 90)
            {
                return EDirection.West;
            }

            if (heading > 90 && heading < 180)
            {
                return EDirection.SouthWest;
            }

            if (heading == 180)
            {
                return EDirection.South;
            }

            if (heading > 180 && heading < 270)
            {
                return EDirection.SouthEast;
            }

            if (heading == 270)
            {
                return EDirection.East;
            }

            if (heading > 270 && heading < 360)
            {
                return EDirection.NorthEast;
            }

            return EDirection.None;
        }

        /// <summary>
        /// Ensures <paramref name="value"/> is not zero by returning 1 if it is. Works on any type implementing <see cref="IComparable"/>.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="value">The value to be checked.</param>
        /// <returns>A non-null value.</returns>
        public static T EnsureValueIsNotZero<T>(IComparable value) where T : IComparable<T>
        {
            T defaultZero = default(T);

            // If value is equal to default, which is always zero, CompareTo will result 0, so we return a 1
            if (value.CompareTo(defaultZero) == 0)
            {
                return (T)Convert.ChangeType(1, typeof(T));
            }

            return (T)value;
        }

        /// <summary>
        /// Ensures <paramref name="value"/> is positive. Works on any type implementing <see cref="IComparable"/>.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="value">The value to be checked.</param>
        /// <returns>A non-null value.</returns>
        public static T EnsureValueIsPositive<T>(T value) where T : IComparable<T>
        {
            dynamic d = Convert.ChangeType(value, typeof(T));
            Type t = d.GetType();

            if (d < 0)
            {
                T minusOne = (T)Convert.ChangeType(-1, t);
                dynamic result = d * minusOne;

                return (T)Convert.ChangeType(result, typeof(T));
            }

            return value;
        }

        /// <summary>
        /// Calculates the distance between <paramref name="point0"/> amd <paramref name="point1"/>.
        /// </summary>
        /// <param name="point0">The first point.</param>
        /// <param name="point1">The second point.</param>
        /// <returns>The distance.</returns>
        public static double GetDistanceBetweenTwoPoints(GTA.Vector3 point0, GTA.Vector3 point1)
        {
            double distanceStep1 = Math.Pow(point0.X - point1.X, 2);
            double distanceStep2 = Math.Pow(point0.Y - point1.Y, 2);
            double distanceStep3 = Math.Pow(point0.Z - point1.Z, 2);

            double distanceFinal = Math.Sqrt(distanceStep1 + distanceStep2 + distanceStep3);
            return distanceFinal;
        }

        /// <summary>
        /// Returns true if the random value between min and max is trueValue. Returns false if not.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="trueValue"></param>
        /// <returns></returns>
        public static bool GetRandomBool(int min, int max, int trueValue)
        {
            if (GetRandomValue(min, max) == trueValue) return true;
            return false;
        }

        public static T GetRandomCollectionValue<T>(Array collection)
        {
            int random = GetRandomValue(0, collection.Length);
            return (T)collection.GetValue(random);
        }

        public static Enum GetRandomEnumValue(Type enumType)
        {
            int random = GetRandomValue(0, Enum.GetValues(enumType).Length);
            return Enum.ToObject(enumType, random) as Enum;
        }

        /// <summary>
        /// Gets a random letter.
        /// </summary>
        /// <param name="upperCase">True if letter should be uppercase.</param>
        /// <returns>The letter.</returns>
        public static char GetRandomLetter(bool upperCase)
        {
            int num = GetRandomValue(0, 26);
            char start = 'a';
            if (upperCase)
            {
                start = 'A';
            }

            return (char)(start + num);
        }

        /// <summary>
        /// Gets a random value between <paramref name="min"/> and <paramref name="max"/>. <paramref name="min"/> can occur, <paramref name="max"/> can't.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The minimum value (can't occur).</param>
        /// <returns>The random value.</returns>
        public static int GetRandomValue(int min, int max)
        {
            if (Common.random == null)
            {
                Common.random = new Random(Environment.TickCount);
            }
            return Common.random.Next(min, max);
        }

        public static bool IsNumberInRange(int number, int range, int rangeDown, int rangeUp)
        {
            if (number > range + rangeUp) return false;
            if (number < range - rangeDown) return false;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="range"></param>
        /// <param name="rangeDown"></param>
        /// <param name="rangeUp"></param>
        /// <param name="end">The max value that can be used before it will start from 0 again. Useful to compare heading, e.g: 0 and 359 and then pass 360 to end</param>
        /// <returns></returns>
        public static bool IsNumberInRange(float number, float range, float rangeDown, float rangeUp, float end = float.MaxValue)
        {
            if (number + rangeUp > end) number = 0 + (end - number);
            if (range + rangeUp > end) range = 0 + (end - range);
            if (number > range + rangeUp) return false;
            if (number < range - rangeDown) return false;
            return true;
        }

        public static float GetLowestDifference(float value, float compareValue, float maximum)
        {
            float difference = value - compareValue;
            if  (difference < 0)
            {
                difference = difference * -1;
            }

            float difference2 = value - compareValue - maximum;
            if (difference2 < 0)
            {
                difference2 = difference2 * -1;
            }

            if (difference < difference2)
            {
                return difference;
            }

            return difference2;
        }
    }
}
