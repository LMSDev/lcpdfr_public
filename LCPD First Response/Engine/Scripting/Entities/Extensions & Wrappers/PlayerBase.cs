namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// This is a wrapper class for <see cref="GTA.Player"/>
    /// </summary>
    internal abstract class PlayerBase
    {
        /// <summary>
        /// The internal player instance.
        /// </summary>
        private GTA.Player player;

        /// <summary>
        /// Gets or sets a value indicating whether the player character can be controlled.
        /// </summary>
        public bool CanControlCharacter
        {
            get
            {
                return this.player.CanControlCharacter;
            }

            set
            {
                this.player.CanControlCharacter = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the player can control ragdoll.
        /// </summary>
        public bool CanControlRagdoll
        {
            set
            {
                this.player.CanControlRagdoll = value;
            }
        }

        /// <summary>
        /// Gets the color of the player.
        /// </summary>
        public System.Drawing.Color Color
        {
            get
            {
                return this.player.Color;
            }
        }

        /// <summary>
        /// Gets the group of the player.
        /// </summary>
        public GTA.Group Group
        {
            get
            {
                return this.player.Group;
            }
        }

        /// <summary>
        /// Gets the ID of the player.
        /// </summary>
        public int ID
        {
            get
            {
                return this.player.ID;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the player is ignored by everyone.
        /// </summary>
        public bool IgnoredByEveryone
        {
            set
            {
                this.player.IgnoredByEveryone = value;
            }
        }

        /// <summary>
        /// Gets the player index.
        /// </summary>
        public int Index
        {
            get
            {
                return this.player.Index;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is active.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return this.player.isActive;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is the local player.
        /// </summary>
        public bool IsLocalPlayer
        {
            get
            {
                return this.player.isLocalPlayer;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is on mission.
        /// </summary>
        public bool IsOnMission
        {
            get
            {
                return this.player.isOnMission;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is playing.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return this.player.isPlaying;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is pressing the horn.
        /// </summary>
        public bool IsPressingHorn
        {
            get
            {
                return this.player.isPressingHorn;
            }
        }

        /// <summary>
        /// Gets the last vehicle used by the player.
        /// </summary>
        public CVehicle LastVehicle
        {
            get
            {
                if (this.player.LastVehicle == null)
                {
                    return null;
                }

                return Pools.VehiclePool.GetVehicleFromPool(this.player.LastVehicle);
            }
        }

        /// <summary>
        /// Sets the maximum armor of the player.
        /// </summary>
        public int MaxArmor
        {
            set
            {
                this.player.MaxArmor = value;
            }
        }

        /// <summary>
        /// Sets the maximum health of the player.
        /// </summary>
        public int MaxHealth
        {
            set
            {
                this.player.MaxHealth = value;
            }
        }

        /// <summary>
        /// Gets or sets the model of the player.
        /// </summary>
        public CModel Model
        {
            get
            {
                return new CModel(this.player.Model);
            }

            set
            {
                this.player.Model = value;
            }
        }

        /// <summary>
        /// Gets or sets the current amount of money of the player.
        /// </summary>
        public int Money
        {
            get
            {
                return this.player.Money;
            }

            set
            {
                this.player.Money = value;
            }
        }

        /// <summary>
        /// Gets the name of the player.
        /// </summary>
        public string Name
        {
            get
            {
                return this.player.Name;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the player never gets tired.
        /// </summary>
        public bool NeverGetsTired
        {
            set
            {
                this.player.NeverGetsTired = value;
            }
        }

        /// <summary>
        /// Gets the player skin.
        /// </summary>
        public GTA.value.PlayerSkin Skin
        {
            get
            {
                return this.player.Skin;
            }
        }

        /// <summary>
        /// Gets or sets the player's team.
        /// </summary>
        public GTA.Multiplayer.Team Team
        {
            get
            {
                return this.player.Team;
            }

            set
            {
                this.player.Team = value;
            }
        }

        /// <summary>
        /// Gets or sets the wanted level of the player.
        /// </summary>
        public int WantedLevel
        {
            get
            {
                return this.player.WantedLevel;
            }

            set
            {
                this.player.WantedLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets the internal <see cref="GTA.Player"/> instance.
        /// </summary>
        protected GTA.Player Player
        {
            get
            {
                return this.player;
            }

            set
            {
                this.player = value;
            }
        }

        /// <summary>
        /// Activates the multiplayer skin.
        /// </summary>
        public void ActivateMultiplayerSkin()
        {
            this.player.ActivateMultiplayerSkin();
        }

        /// <summary>
        /// Returns the ped the player is targetting.
        /// </summary>
        /// <returns>The ped.</returns>
        public CPed GetTargetedPed()
        {
            return Pools.PedPool.GetPedFromPool(this.player.GetTargetedPed());
        }

        /// <summary>
        /// Returns whether the player is aiming at <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>True if yes, false otherwise.</returns>
        public bool IsTargetting(CPed ped)
        {
            return this.player.isTargetting(ped);
        }

        /// <summary>
        /// Returns whether the player is aiming at <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>True if yes, false otherwise.</returns>
        public bool IsTargetting(GTA.Object obj)
        {
            return this.player.isTargetting(obj);
        }

        /// <summary>
        /// Sets the visibility of the ped component.
        /// </summary>
        /// <param name="pedComponent">The ped component.</param>
        /// <param name="visible">True if visible, false if not.</param>
        public void SetComponentVisibility(GTA.PedComponent pedComponent, bool visible)
        {
            this.player.SetComponentVisibility(pedComponent, visible);
        }

        /// <summary>
        /// Teleports the player to the position.
        /// </summary>
        /// <param name="position">The position.</param>
        public void TeleportTo(GTA.Vector3 position)
        {
            this.player.TeleportTo(position);
        }

        /// <summary>
        /// Teleports the player to the position.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        public void TeleportTo(float x, float y)
        {
            this.player.TeleportTo(x, y);
        }
    }
}
