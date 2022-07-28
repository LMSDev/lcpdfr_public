namespace LCPD_First_Response.LCPDFR.API
{
    using System.Collections.Generic;

    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// The LCPDFR player class.
    /// </summary>
    public class LPlayer : LPlayerBase
    {
        /// <summary>
        /// The local player.
        /// </summary>
        private static LPlayer localPlayer;

        /// <summary>
        /// The player ped.
        /// </summary>
        private LPed ped;

        /// <summary>
        /// Initializes a new instance of the <see cref="LPlayer"/> class.
        /// </summary>
        public LPlayer()
        {
            this.Player = LCPDFRPlayer.LocalPlayer;
        }

        /// <summary>
        /// Gets the local player.
        /// </summary>
        public static LPlayer LocalPlayer
        {
            get
            {
                if (localPlayer == null)
                {
                    localPlayer = new LPlayer();
                }

                return localPlayer;
            }
        }

        /// <summary>
        /// Gets the player ped.
        /// </summary>
        public LPed Ped
        {
            get
            {
                if (this.ped == null || !this.ped.Exists())
                {
                    this.ped = new LPed(CPlayer.LocalPlayer.Ped);
                }

                return this.ped;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is busy. This includes arresting, frisking, pursuits, pullovers, callouts and using the police computer.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return this.Player.IsBusy;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is in the police department.
        /// </summary>
        public bool IsInPoliceDepartment
        {
            get
            {
                return this.Player.IsInPoliceDepartment;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is on duty.
        /// </summary>
        public bool IsOnDuty
        {
            get
            {
                return Globals.IsOnDuty;
            }
        }

        /// <summary>
        /// Gets one or more peds whose IDs have been checked recently.
        /// </summary>
        public LPed[] RecentlyCheckedPeds
        {
            get
            {
                if (CPlayer.LocalPlayer.LastPedPulledOver == null)
                {
                    return new LPed[0];
                }

                return new List<CPed>(CPlayer.LocalPlayer.LastPedPulledOver).ConvertAll(p => new LPed(p)).ToArray();
            }
        }

        /// <summary>
        /// Gets the username of the player. This returns the LCPDFR forum member name if available, or a random generated officer name if not.
        /// </summary>
        public string Username
        {
            get
            {
                return this.Player.GetOfficerName();
            }
        }
    }
}