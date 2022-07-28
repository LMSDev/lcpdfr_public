/* Defines and manages a wanted ped */

namespace LCPD_First_Response.Engine.Scripting
{
    using System.Collections.Generic;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    class Wanted
    {
        public CPed ArrestedBy { get; set; }

        /// <summary>
        /// Gets or sets the number of cops that have been damaged.
        /// </summary>
        public int CopsDamaged { get; set; }

        /// <summary>
        /// Set to true if ped has been arrested and is in a cop vehicle
        /// </summary>
        public bool HasBeenArrested { get; set; }

        /// <summary>
        /// Gets or sets the number of helicopter units chasing the suspect.
        /// </summary>
        public int HelicoptersChasing { get; set; }

        public bool IsBeingArrested { get; set; }
        public bool IsBeingArrestedByPlayer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped is being frisked.
        /// </summary>
        public bool IsBeingFrisked { get; set; }

        public bool Invisible { get; set; }

        public bool IsCuffed { get; set; }
        /// <summary>
        /// Returns true if suspect is currently deciding to give up
        /// </summary>
        public bool IsDeciding { get; set; }

        public bool IsIdle
        {
            get
            {
                return !this.IsStopped && !this.HasBeenArrested && !this.IsBeingArrested && !this.IsBeingArrestedByPlayer && !this.IsCuffed && !this.IsBeingFrisked;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ped has been ordered to stop by the player.
        /// </summary>
        public bool IsStopped { get; set; }

        public bool LastKnownInVehicle { get; set; }
        public bool LastKnownOnFoot { get; set; }
        public GTA.Vector3 LastKnownPosition { get; set; }
        public CVehicle LastKnownVehicle { get; set; }
        public int MaxUnits { get; set; }
        public int OfficersChasing { get; set; }
        public int OfficersVisual { get; set; }

        /// <summary>
        /// Gets the number of officers chasing a suspect on foot - this only includes officers in ETaskCopChasePedOnFoot state Chase
        /// </summary>
        public int OfficersChasingOnFoot { get; set; }

        /// <summary>
        /// Gets the number of officers trying to Taser a suspect
        /// </summary>
        public int OfficersTasing { get; set; }

        public bool ResistedArrest { get; set; }
        public bool ResistedDropWeapon { get; set; }
        /// <summary>
        /// Indicates whether the ped gave up fleeing and surrendered
        /// </summary>
        public bool Surrendered { get; set; }
        public int TimesArrestResisted { get; set; }
        public bool VisualLost { get; set; }
        public int VisualLostSince { get; set; }
        public bool VisualLostReported { get; set; }
        public bool WeaponSpotted { get; set; }

        /// <summary>
        /// Gets the number of officers trying to search for the suspect on foot
        /// </summary>
        public int OfficersSearchingOnFoot { get; set; }

        /// <summary>
        /// Gets the number of officers trying to search for the suspect in a vehicle
        /// </summary>
        public int OfficersSearchingInAVehicle { get; set; }

        /// <summary>
        /// Returns true if suspect has been ordered to surrender or stop by any cop
        /// </summary>
        public bool HasBeenAskedToSurrender { get; set; }

        /// <summary>
        /// Returns true if suspect has been ordered to stop at taserpoint
        /// </summary>
        public bool HasBeenAskedToSurrenderBeforeTaser { get; set; }

        /// <summary>
        /// Indicates how many officers are chasing in a vehicle.
        /// </summary>
        public int OfficersChasingInVehicle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped has used a weapon. NOTE: Only works for criminals!
        /// </summary>
        public bool WeaponUsed { get; set; }

        private CPed ped;

        public List<PlaceToSearch> PlacesToSearch;

        public Wanted(CPed ped)
        {
            this.ped = ped;
            this.PlacesToSearch = new List<PlaceToSearch>();
        }

        /// <summary>
        /// Removes all places to search.
        /// </summary>
        public void ClearPlacesToSearch()
        {
            if (this.ped.Wanted.PlacesToSearch != null)
            {
                foreach (PlaceToSearch pts in this.ped.Wanted.PlacesToSearch)
                {
                    pts.Remove();
                }

                this.ped.Wanted.PlacesToSearch.Clear();
            }
        }

        /// <summary>
        /// Resets if the ped has resisted arrest, dropping the weapon, if a weapon has been spotted and if a weapon has been fired
        /// </summary>
        public void ResetArrestFlags()
        {
            // Also reset some flags
            this.CopsDamaged = 0;
            this.ResistedArrest = false;
            this.ResistedDropWeapon = false;
            this.WeaponSpotted = false;
            this.WeaponUsed = false;

            this.HasBeenAskedToSurrender = false;
            this.HasBeenAskedToSurrenderBeforeTaser = false;
        }
    }
}
