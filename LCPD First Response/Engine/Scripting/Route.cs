namespace LCPD_First_Response.Engine.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using GTA;

    /// <summary>
    /// Consists of a number of waypoints.
    /// </summary>
    internal class Route
    {
        /// <summary>
        /// The waypoints.
        /// </summary>
        private SequencedList<Vector3> waypointsList;

        /// <summary>
        /// Initializes a new instance of the <see cref="Route"/> class.
        /// </summary>
        public Route()
        {
            this.waypointsList = new SequencedList<Vector3>();
        }

        /// <summary>
        /// Adds the waypoint to the route.
        /// </summary>
        /// <param name="waypoint">The waypoint.</param>
        public void AddWaypoint(Vector3 waypoint)
        {
            this.waypointsList.Add(waypoint);
        }

        /// <summary>
        /// Clears the list of waypoints.
        /// </summary>
        public void Clear()
        {
            this.waypointsList.Clear();
        }

        /// <summary>
        /// Gets the next waypoint.
        /// </summary>
        /// <returns>The waypoint.</returns>
        public Vector3 GetNextWaypoint()
        {
            if (this.IsWaypointAvailable())
            {
                return this.waypointsList.Next();
            }

            return Vector3.Zero;
        }

        /// <summary>
        /// Returns if there is still a waypoint available.
        /// </summary>
        /// <returns>True if one is available, false if not.</returns>
        public bool IsWaypointAvailable()
        {
            return this.waypointsList.IsNextItemAvailable();
        }

        /// <summary>
        /// Resets the route, so the next waypoint returned will be the first.
        /// </summary>
        public void Reset()
        {
            this.waypointsList.Reset();
        }

        /// <summary>
        /// Reverses the complete route.
        /// </summary>
        public void Reverse()
        {
            this.waypointsList.Reverse();
        }
    }
}