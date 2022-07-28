namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using GTA;

    using LCPD_First_Response.Engine.Input;

    /// <summary>
    /// Player class.
    /// </summary>
    internal class CPlayer : PlayerBase
    {
        /// <summary>
        /// The local player.
        /// </summary>
        private static CPlayer localPlayer;

        /// <summary>
        /// The player ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The voice of the player.
        /// </summary>
        private string voice;

        /// <summary>
        /// The animation group of the player.
        /// </summary>
        private string animGroup;

        /// <summary>
        /// Initializes a new instance of the <see cref="CPlayer"/> class.
        /// </summary>
        public CPlayer()
        {
            this.Player = GTA.Game.LocalPlayer;
        }

        /// <summary>
        /// Gets the local player.
        /// </summary>
        public static CPlayer LocalPlayer
        {
            get
            {
                if (localPlayer == null)
                {
                    localPlayer = new CPlayer();
                }

                return localPlayer;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the camera control is disabled when the player control is disabled.
        /// </summary>
        public bool CameraControlsDisabledWithPlayerControls
        {
            set
            {
                Native.Natives.SetCameraControlsDisabledWithPlayerControls(value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player is ignored by the LCPDFR AI. This does not mean the player is ignored by the game AI, e.g. when shooting!
        /// </summary>
        public bool IgnoredByAI { get; set; }

        /// <summary>
        /// Gets a value indicating whether player is targetting anything.
        /// </summary>
        public bool IsTargettingAnything
        {
            get
            {
                return Native.Natives.IsPlayerTargettingAnything(this.Player);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player targets or aims at a ped.
        /// </summary>
        public bool IsTargettingOrAimingAtPed
        {
            get
            {
                CPed targetedPed = null;
                if (CPlayer.LocalPlayer.Ped.IsAiming)
                {
                    targetedPed = this.Ped.Intelligence.GetTargetedPed();
                }
                else
                {
                    targetedPed = this.GetTargetedPed();
                }

                return targetedPed != null && targetedPed.Exists();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player targets or aims at a ped.
        /// </summary>
        public bool IsTargettingOrAimingAtPedOnFoot
        {
            get
            {
                CPed targetedPed = null;
                if (CPlayer.LocalPlayer.Ped.IsAiming)
                {
                    targetedPed = this.Ped.Intelligence.GetTargetedPed();
                }
                else
                {
                    targetedPed = this.GetTargetedPed();
                }

                return targetedPed != null && targetedPed.Exists() && !targetedPed.IsInVehicle;
            }
        }

        /// <summary>
        /// Gets or sets the peds pulled over last.
        /// </summary>
        public CPed[] LastPedPulledOver { get; set; }

        /// <summary>
        /// Gets or sets the model of the player.
        /// </summary>
        public new CModel Model
        {
            get
            {
                return base.Model;
            }

            set
            {
                // Change model
                base.Model = value;

                // Since some classes may keep a private copy of the player ped and SHDN for fucks sake will update their internal handles on model change, we have to reset their handle
                this.ped.ResetAPed();

                // Free current instance
                this.ped.Delete(true);
                this.ped = null;

                bool test = this.Ped.IsAlive;
                if (test)
                {
                    Log.Debug("CModel.Set: New ped is valid", this);
                }

                // And reset APed
                // this.ped.ResetAPed();
            }
        }

        /// <summary>
        /// Gets the player ped.
        /// </summary>
        public virtual CPed Ped
        {
            get
            {
                if (this.ped == null || !this.ped.Exists())
                {
                    Log.Debug("Ped.Get: Player ped is invalid. Creating new instance", "CPlayer");
                    this.ped = new CPed(true);
                }

                if (this.ped.Handle != Game.LocalPlayer.Character.pHandle)
                {
                    Log.Warning("Ped.Get: Local handle is not player handle. Did someone change the model from remote?", "CPlayer");
                    this.ped.Delete(true);
                    this.ped = null;

                    bool test = this.Ped.IsAlive;
                    if (test)
                    {
                        Log.Info("CPed.Get: New ped is valid", this);
                    }
                    else
                    {
                        Log.Error("CPed.Get: Failed to update ped", this);
                    }
                }

                return this.ped;
            }
        }

        /// <summary>
        /// Gets or sets the voice of the player.
        /// </summary>
        public string Voice
        {
            get
            {
                return this.voice;
            }

            set
            {
                this.voice = value;
                this.Ped.Voice = value;
                Log.Debug("Voice: Setting voice to " + value, this);
            }
        }

        /// <summary>
        /// Gets or sets the movement animation set of the player.
        /// </summary>
        public string AnimGroup
        {
            get
            {
                return this.animGroup;
            }

            set
            {
                this.animGroup = value;
                Native.Natives.SetAnimGroupForChar(this.ped, value);
            }
        }

        /// <summary>
        /// Returns the name of the player as an officer, either the LCPDFR username (if available) or a local random name generated from the persona data.
        /// </summary>
        /// <returns>The name.</returns>
        public string GetOfficerName()
        {
            string name = Main.Authentication.Userdata.Username;

            // If name is empty, use player's persona name
            if (name == string.Empty)
            {
                name = CPlayer.LocalPlayer.Ped.PedData.Persona.FullName;
            }

            return name;
        }

        /// <summary>
        /// Returns the ped the player is targetting. This is refering to the actual target of melee weapons, not the ped aiming at using a weapon!
        /// </summary>
        /// <returns>The ped targetting.</returns>
        public new CPed GetTargetedPed()
        {
            if (this.IsTargettingAnything)
            {
                foreach (CPed ped in Pools.PedPool.GetAll())
                {
                    if (this.IsTargettingChar(ped))
                    {
                        return ped;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the ped the player is aiming at. Note: This is also used in <see cref="Scripting.Tasks.PedIntelligence.GetTargetedPed"/> in addition.
        /// </summary>
        /// <returns>The ped the player is aiming at.</returns>
        public CPed GetPedAimingAt()
        {
            GTA.Ped targetedPed = this.Player.GetTargetedPed();
            if (targetedPed != null && targetedPed.Exists())
            {
                return Pools.PedPool.GetPedFromPool(targetedPed);
            }

            return null;
        }

        /// <summary>
        /// Gets the vehicle the player would enter.
        /// </summary>
        /// <returns>The vehicle.</returns>
        public CVehicle GetVehiclePlayerWouldEnter()
        {
            Vehicle vehicle = Native.Natives.GetVehiclePlayerWouldEnter();
            if (vehicle != null && vehicle.Exists())
            {
                CVehicle newVehicle = Pools.VehiclePool.GetVehicleFromPool(vehicle);
                return newVehicle;
            }

            return null;
        }

        /// <summary>
        /// Returns whether <paramref name="ped"/> can be seen by the player.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>True if can be seen, false if not.</returns>
        public bool HasVisualOnSuspect(CPed ped)
        {
            if (this.IgnoredByAI)
            {
                return false;
            }

            if (ped == this.ped)
            {
                return false;
            }

            if (ped.PedData.CurrentChase != null)
            {
                if (ped.PedData.CurrentChase.OnlyAIVisuals)
                {
                    return false;
                }
            }

            return this.Ped.HasSpottedChar(ped);
        }

        /// <summary>
        /// Returns whether <paramref name="ped"/> can be seen by in front of the player.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>True if can be seen, false if not.</returns>
        public bool HasVisualOnSuspectInFront(CPed ped)
        {
            if (this.IgnoredByAI)
            {
                return false;
            }

            if (ped == this.ped)
            {
                return false;
            }

            if (ped.PedData.CurrentChase != null)
            {
                if (ped.PedData.CurrentChase.OnlyAIVisuals)
                {
                    return false;
                }
            }

            return this.Ped.HasSpottedCharInFront(ped);
        }

        /// <summary>
        /// Returns whether the player is targetting <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>True if targetting, false if not.</returns>
        public bool IsTargettingChar(CPed ped)
        {
            return Native.Natives.IsPlayerTargettingChar(this.Player, ped);
        }

        [ConsoleCommand("notarget", "Makes player invisible to LCPDFR AI")]
        private static void PlayerIgnoredByAIConsoleCommand(GTA.ParameterCollection parameterCollection)
        {
            bool value = false;
            if (parameterCollection.Count > 0)
            {
                bool.TryParse(parameterCollection[0], out value);
            }

            CPlayer.localPlayer.IgnoredByAI = value;

            if (value)
            {
                CPlayer.localPlayer.ped.RelationshipGroup = RelationshipGroup.Civillian_Male;
            }
            else
            {
                CPlayer.localPlayer.ped.RelationshipGroup = RelationshipGroup.Cop;
            }

            Log.Debug("PlayerIgnoredByAIConsoleCommand: Ignored: " + value.ToString(), "CPlayer");
        }
    }
}