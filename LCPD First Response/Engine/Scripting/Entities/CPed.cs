namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AdvancedHookManaged;

    using global::LCPDFR.Networking;

    using GTA;
    using LCPD_First_Response.Engine.Networking;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using Lidgren.Network;
    using System.Drawing;

    /// <summary>
    /// Ped group
    /// </summary>
    internal enum EPedGroup
    {
        Unknown,
        Cop,
        Criminal,
        MissionPed,
        Pedestrian,
        Player,
        Testing,
    }

    /// <summary>
    /// Ped sub group, providing advanced information
    /// </summary>
    internal enum EPedSubGroup
    {
        Unknown,
        FBI,
        Noose,
        NormalCop,
        TrafficCop,
    }

    /// <summary>
    /// The ped props.
    /// </summary>
    internal enum EPedProp
    {
        /// <summary>
        /// The hat prop.
        /// </summary>
        Hat,

        /// <summary>
        /// The glasses prop.
        /// </summary>
        Glasses,
    }

    /// <summary>
    /// The network messages for peds.
    /// </summary>
    internal enum EPedNetworkMessages
    {
        AttachBlip,
        SetRequiredForMissin,
        RemoveBlip,
        SetBlipMode,
    }

#pragma warning disable 660,661
    internal class CPed : PedBase
#pragma warning restore 660,661
    {
        public byte Alpha
        {
            set
            {
                Natives.SetPedAlpha(this, value);
            }
        }

        /// <summary>
        /// Gets or sets whether the ped will be marked as no longer needed on death
        /// </summary>
        public bool AlwaysFreeOnDeath { get; set; }

        private string animGroup;

        /// <summary>
        /// Gets or sets the movement animation set of the ped.
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


        public APed APed { get; private set; }
        public GTA.Blip Blip { get; private set; }

        /// <summary>
        /// Sets a value indicating whether the ped has collision.
        /// </summary>
        public bool Collision
        {
            set
            {
                Natives.SetCharCollision(this.ped, value);
            }
        }

        /// <summary>
        /// Gets the currently used vehicle of the ped. Returns null when no vehicle is used.
        /// </summary>
        public new CVehicle CurrentVehicle
        {
            get
            {
                CVehicle currentVehicle = base.CurrentVehicle;
                if (currentVehicle != null)
                {
                    this.lastVehicle = currentVehicle;
                }

                return currentVehicle;
            }
        }

        /// <summary>
        /// Gets or sets a string that can contain any type of debug data, most likely changed every tick
        /// </summary>
        public string Debug { get; set; }

        /// <summary>
        /// Sets a value indicating whether ragdoll can start from player impact.
        /// </summary>
        public bool DontActivateRagdollFromPlayerImpact
        {
            set
            {
                Natives.SetDontActivateRagdollFromPlayerImpact(this, value);
            }
        }

        public bool DontRemoveBlipOnDeath { get; set; }
        public bool DontRemoveBlipWhenBusted { get; set; }
        public PedIntelligence Intelligence { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the char is currently ducking.
        /// </summary>
        public bool IsDucking
        {
            get
            {
                return Natives.IsCharDucking(this);
            }
        }

        public bool IsDriver
        {
            get
            {
                if (!this.IsInVehicle()) return false;

                CVehicle currentVehicle = base.CurrentVehicle;
                CPed driver = currentVehicle.GetPedOnSeat(GTA.VehicleSeat.Driver);
                if (driver == null || !driver.Exists()) return false;
                int handle = driver.Handle;

                return driver.Handle == base.Handle;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is aiming.
        /// </summary>
        public bool IsAiming
        {
            get
            {
                return this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is playing an ambient speech.
        /// </summary>
        public bool IsAmbientSpeechPlaying
        {
            get
            {
                return Natives.IsAmbientSpeechPlaying(this.ped);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is controlled by us in a network session.
        /// </summary>
        public bool IsControlledByUs
        {
            get
            {
                return Natives.HasControlOfNetworkID(this.NetworkID);
            }
        }

        public bool IsEnteringVehicle
        {
            get
            {
                return CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is fleeing, either use the normal fleeing task or the retreat task.
        /// </summary>
        public bool IsFleeing
        {
            get
            {
                return this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSmartFleeEntity) || this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is in combat but not fleeing, but actively attacking.
        /// </summary>
        public bool IsInCombatNotFleeing
        {
            get
            {
                return this.IsInCombat && !this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask);
            }
        }

        /// <summary>
        /// Hides parameterless overload of PedBase.IsInVehicle() for easier use
        /// </summary>
        public new bool IsInVehicle
        {
            get { return base.IsInVehicle(); }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is getting out of a vehicle
        /// </summary>
        public bool IsGettingOutOfAVehicle
        {
            get
            {
                return this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleCarGetOut);
            }
        }

        public bool IsLeavingVehicle
        {
            get
            {
                return this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is not in combat or if, only fleeing, so not actively attacking.
        /// </summary>
        public bool IsNotInCombatOrOnlyFleeing
        {
            get
            {
                return !this.IsInCombat || this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is a player.
        /// </summary>
        public bool IsPlayer
        {
            get
            {
                return this.PedGroup == EPedGroup.Player;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is standing still.
        /// </summary>
        public bool IsStandingStill
        {
            get
            {
                Vector3 velocity = this.Velocity;
                return Math.Round(velocity.X, 1) == 0 && Math.Round(velocity.Y, 1) == 0 && Math.Round(velocity.Z, 1) == 0;
            }
        }

        public bool HasBlip
        {
            get { return this.Blip != null && this.Blip.Exists(); }
        }

        /// <summary>
        /// Gets the last vehicle used by the ped. If in a vehicle, returns the current one. Only works when <see cref="CurrentVehicle"/> has been used while the ped was
        /// inside the vehicle.
        /// </summary>
        public CVehicle LastVehicle
        {
            get
            {
                if (this.IsInVehicle)
                {
                    this.lastVehicle = this.CurrentVehicle;
                }

                return this.lastVehicle;
            }
        }

        public int NetworkID
        {
            get { return Natives.GetNetworkIDFromPed(this); }
        }
        public PedData PedData { get;  set; }
        public SearchArea SearchArea { get; set; }
        public EPedGroup PedGroup { get; set; }
        public EPedSubGroup PedSubGroup { get; private set; }

        /// <summary>
        /// Sets a value indicating whether the ped is ready to be executed, that is when aiming at the ped the health circle will flash.
        /// </summary>
        public bool ReadyToBeExecuted
        {
            set
            {
                Natives.SetCharReadyToBeExecuted(this.ped, value);
            }
        }

        /// <summary>
        /// Sets a value indicating whether the ped is ready to be stunned, that is when targetting the ped and getting really close the health circle will change and hitting the ped will result in a special move.
        /// </summary>
        public bool ReadyToBeStunned
        {
            set
            {
                Natives.SetCharReadyToBeStunned(this.ped, value);
            }
        }

        /// <summary>
        /// Gets the speed of the char.
        /// </summary>
        public float Speed
        {
            get
            {
                return Natives.GetCharSpeed(this);
            }
        }

        /// <summary>
        /// Gets the voice of the ped.
        /// </summary>
        public Voice VoiceData { get; private set; }

        public Wanted Wanted { get; private set; }
        public bool WillLeaveCarInCombat
        {
            set
            {
                Natives.SetCharWillLeaveCarInCombat(this, value);
            }
        }

        /// <summary>
        /// The last vehicle used.
        /// </summary>
        private CVehicle lastVehicle;

        public CPed(CModel model, GTA.Vector3 position, EPedGroup type)
        {
            // First ensure that actual ped is created
            // Note: Calling without the relationgroup set makes SHDN read the gender of the model afterwards which appears to crash under certain circumstances
            // So for now, we stick with civ male
            base.ped = GTA.World.CreatePed(model, position, RelationshipGroup.Civillian_Male);
            if (Exists())
            {
                this.HasBeenCreatedByUs = true;
                this.PedGroup = type;
                Initialize();
            }
        }

        public CPed(CModel model, GTA.Vector3 position, EPedGroup type, bool lanOrHostOnly)
        {
            if (lanOrHostOnly)
            {
                if (!Main.IsSinglePlayerOrHost) return;
            }

            // First ensure that actual ped is created
            // Note: Calling without the relationgroup set makes SHDN read the gender of the model afterwards which appears to crash under certain circumstances
            // So for now, we stick with civ male
            base.ped = GTA.World.CreatePed(model, position, RelationshipGroup.Civillian_Male);
            if (Exists())
            {
                this.HasBeenCreatedByUs = true;
                this.PedGroup = type;
                Initialize();
            }
        }

        public CPed(int handle)
        {
            //Create GTA.Ped instance based on handle
            base.ped = new GTA.Ped(handle);
            if (Exists())
            {
                if (this.ped.pHandle == GTA.Game.LocalPlayer.Character.pHandle)
                {
                    this.ped = Game.LocalPlayer.Character;
                    this.PedGroup = EPedGroup.Player;
                    this.Initialize();
                    return;
                }

                // The creation doesn't add the ped to shdn's contentcache which makes the ped not available for all functions that retun a ped such as GTA.Vehicle.GetPedOnSeat()
                GTA.ContentCache.AddPed(base.ped);

                this.PedGroup = EPedGroup.Unknown;
                Initialize();
            }
        }

        public CPed(bool isPlayer)
        {
            if (isPlayer)
            {
                base.ped = GTA.Game.LocalPlayer.Character;
                this.PedGroup = EPedGroup.Player;
                Initialize();
            }
        }

        //public CPed(CPed ped)
        //{
        //    this.SetHandle(ped.Handle);
        //    this.ped = ped.ped;
        //    this.APed = ped.APed;
        //    this.Blip = ped.Blip;
        //    this.DontRemoveBlipOnDeath = ped.DontRemoveBlipOnDeath;
        //    this.DontRemoveBlipWhenBusted = ped.DontRemoveBlipWhenBusted;
        //    this.Intelligence = ped.Intelligence;
        //    this.PedData = ped.PedData;
        //    this.PedGroup = ped.PedGroup;
        //    this.PedSubGroup = ped.PedSubGroup;
        //    this.Wanted = ped.Wanted;
        //}

        private void Initialize()
        {
            // CEntity.SetHandle
            this.SetHandle(this.ped.pHandle);

            // Process different ped types
            if (this.PedGroup == EPedGroup.Unknown)
            {
                // TODO: Use AdvancedHook to check if ped is cop (CPedBase.m_dwPedType - ProcessCopBlip)
                //string model = base.Model.ToString();
                //if (base.Model == "M_Y_COP" || base.Model == "M_M_FATCOP_01")

                //string internalName = ((GTA.Model) base.Model).ToString();
                //GTA.Game.Console.Print("Name: " + internalName);
                if (base.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCop))
                {
                    this.PedGroup = EPedGroup.Cop;
                    this.PedSubGroup = EPedSubGroup.NormalCop;
                }
                else
                {
                    this.PedGroup = EPedGroup.Pedestrian;

                    // In MP, check if ped is other player.
                    if (Main.NetworkManager.IsNetworkSession)
                    {
                        foreach (Player player in Game.PlayerList)
                        {
                            if (player.Character.pHandle == this.Handle)
                            {
                                this.PedGroup = EPedGroup.Player;
                            }
                        }
                    }
                }
            }

            // Initialize objects
            if (this.PedGroup == EPedGroup.Cop)
            {
                this.PedData = new PedDataCop(this);

                if (Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsNoose))
                {
                    this.PedSubGroup = EPedSubGroup.Noose;
                    this.ped.Skin.SetPropIndex(PedProp.UNKNOWN_0, Common.GetRandomValue(0, 3));  // Helmet
                }
                else if (Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsTrafficCop))
                {
                    this.PedSubGroup = EPedSubGroup.TrafficCop;
                }
                else if (Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsFBI))
                {
                    this.PedSubGroup = EPedSubGroup.FBI;
                }
            }
            else
            {
                this.PedData = new PedData(this);
            }

            this.Intelligence = new PedIntelligence(this); // This fires and registers events
            this.APed = new APed(this.Handle); 
            this.Wanted = new Wanted(this);

            // Assign anim group (In memory only, no need to actually set it with the native)
            this.animGroup = this.Model.ModelInfo.AnimGroup;
            // Game.Console.Print("Set " + this.Model.ModelInfo.Name + "'s anim group to " + this.AnimGroup);

            // Add ref
            Pools.PedPool.Add(this);

            // Now that all objects are created, we can fire and listen to some events
            this.Intelligence.SetupDefaultSettingsForPedGroup();

            // Network sync
            //CREATE_CHAR(Var3, Var2[Var9(1)], Var4._f0, Var4._f4, Var4._f8, &Var10, 1);
            //SET_CHAR_HEADING(Var10, Var7);
            //Var1 = GetNetworkIDFromPed(Var10);
            //SET_NETWORK_ID_EXISTS_ON_ALL_MACHINES(Var1, 1);
            if (Main.NetworkManager.IsNetworkSession && this.HasBeenCreatedByUs && Main.NetworkManager.IsHost)
            {
                int networkID = this.NetworkID;
                Natives.SetNetworkIDCanMigrate(networkID, false);
                Natives.SetNetworkIDExistsOnAllMachines(networkID, true);
                Log.Debug("Initialize: Set " + networkID + " to exist on all machines", this);
            }

            if (Main.NetworkManager.IsNetworkSession && this.HasBeenCreatedByUs)
            {
                // Let all clients know this one is important
                this.BecomeMissionCharacter();
            }

            // Set a default content manager for entities created by us
            if (this.HasBeenCreatedByUs)
            {
                Plugins.ContentManager.DefaultContentManager.AddPed(this);
            }

            // Store vehicle, if any
            if (this.IsInVehicle)
            {
                this.lastVehicle = this.CurrentVehicle;
            }

            this.VoiceData = new Voice(this);

            // Don't allow dummy peds
            GTA.Native.Function.Call("SET_PED_WITH_BRAIN_CAN_BE_CONVERTED_TO_DUMMY_PED", (GTA.Ped)this, false);

            new EventNewPedCreated(this);
        }

        /// <summary>
        /// Returns whether enemies are around the ped.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <returns>Whether or not peds are around.</returns>
        public bool AreEnemiesAround(float radius)
        {
            return Natives.AreEnemyPedsInArea(this.ped, radius);
        }

        public GTA.Blip AttachBlip(bool removeBlipOnDeath = true, bool sync = true, bool forceBlip = false)
        {
            if (this.HasBlip && !forceBlip) return this.Blip;
            this.Blip = base.AttachBlip();
            if (sync)
            {
                // If host, sync with all clients
                if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.IsHost && Main.NetworkManager.CanSendData)
                {
                    Main.NetworkManager.SendMessageWithNetworkID("CPed", EPedNetworkMessages.AttachBlip, this.NetworkID);
                }
            }
            this.DontRemoveBlipOnDeath = !removeBlipOnDeath;
            return this.Blip;
        }

        public void BecomeMissionCharacter(bool sync = true)
        {
            base.BecomeMissionCharacter();

            if (sync && Main.NetworkManager.IsNetworkSession)
            {
                if (this.RequestNetworkControl(false))
                {
                    Natives.SetNetworkIDExistsOnAllMachines(this.NetworkID, true);
                    Natives.SetNetworkIDCanMigrate(this.NetworkID, false);

                    Log.Debug("BecomeMissionCharacter: Set " + this.NetworkID + " to exist on all machines", this);

                    if (Main.NetworkManager.CanSendData)
                    {
                        Main.NetworkManager.SendMessageWithNetworkID("Networking", EGenericNetworkMessages.NewNetworkEntityIDUsed, this.NetworkID);

                        Log.Debug("BecomeMissionCharacter: Synced", this);
                    }
                    else
                    {
                        Log.Debug("BecomeMissionCharacter: Not connected to host", this);
                    }
                }
                else
                {
                    Log.Warning("BecomeMissionCharacter: Failed to obtain network control", this);
                }
            }
        }

        public new bool IsRequiredForMission 
        {
            get
            {
                return base.IsRequiredForMission;
            }

            set
            {
                if (value)
                {
                    this.BecomeMissionCharacter();
                }
                else
                {
                    this.NoLongerNeeded();
                }
            }
        }

        public void NoLongerNeeded(bool sync = true)
        {
            base.NoLongerNeeded();

            if (sync && Main.NetworkManager.IsNetworkSession)
            {
                if (this.RequestNetworkControl(false))
                {
                    Natives.SetNetworkIDExistsOnAllMachines(this.NetworkID, false);
                    Natives.SetNetworkIDCanMigrate(this.NetworkID, true);

                    if (Main.NetworkManager.CanSendData)
                    {
                        //DynamicData dynamicData = new DynamicData(Main.NetworkManager.ActivePeer);
                        //dynamicData.Write(this.NetworkID);
                        //dynamicData.Write(false);
                        //Main.NetworkManager.ActivePeer.Send("CPed", EPedNetworkMessages.SetRequiredForMissin, dynamicData);
                        //Log.Debug("NoLongerNeeded: Synced", this);
                    }
                    else
                    {
                        Log.Debug("NoLongerNeeded: Not connected to host", this);
                    }
                }
                else
                {
                    Log.Warning("NoLongerNeeded: Failed to obtain network control", this);
                }
            }
        }

        /// <summary>
        /// Returns whether the ped can be seen by the current camera.
        /// </summary>
        /// <returns>Whether ped can be seen.</returns>
        public bool CanBeSeenByCamera()
        {
            return Game.CurrentCamera.isSphereVisible(ped.Position, 0f) && this.CanBeSeenFromPosition(Game.CurrentCamera.Position);
        }

        /// <summary>
        /// Returns whether the ped can be seen from <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>Whether ped can be seen.</returns>
        public bool CanBeSeenFromPosition(Vector3 position)
        {
            Bone[] array = new Bone[]
	                        {
		                        Bone.LeftHand,
		                        Bone.RightHand,
		                        Bone.Head,
		                        Bone.LeftFoot,
		                        Bone.RightFoot,
		                        Bone.Pelvis
	                        };

            Tracer tracer = new Tracer();
            int num = 0;
            if (0 < array.Length)
            {
                do
                {
                    Vector3 bonePosition = ped.GetBonePosition(array[num]);
                    uint hitEntity = 0;
                    Vector3 hitPos = Vector3.Zero, normal = Vector3.Zero;

                    // Check for hit
                    if (!tracer.DoTrace(position, bonePosition, ref hitEntity, ref hitPos, ref normal))
                    {
                        return true;
                    }
                    else
                    {
                        if (hitEntity != 0)
                        {
                            if (AGame.GetTypeOfEntity((int)hitEntity) == 4)
                            {
                                return true;
                            }
                        }
                    }
                    num++;
                }
                while (num < array.Length);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the ped can enter <paramref name="vehicle"/> depending on the distance and possible objects blocking the door. Note: This ends the current
        /// active task of the ped.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="vehicleSeat">The vehicle seat.</param>
        /// <returns>True if vehicle can entered, false if not.</returns>
        public bool CanEnterVehicle(CVehicle vehicle, VehicleSeat vehicleSeat)
        {
            if (vehicle != null && vehicle.Exists() && this.ped.Exists())
            {
                this.ped.Task.EnterVehicle(vehicle, vehicleSeat);

                // To allow the task to be added properly
                Game.WaitInCurrentScript(1);

                // Caution: Since this allows the game to process, our ped can be deleted
                if (!this.ped.Exists())
                {
                    Log.Warning("CanEnterVehicle: Ped has been deleted while still being in use", this);
                    return false;
                }

                // Check if task is running
                if (this.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle))
                {
                    this.Intelligence.TaskManager.AbortInternalTask(EInternalTaskID.CTaskComplexNewGetInVehicle);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the task of all peds in combat with this ped.
        /// </summary>
        public void ClearCombat()
        {
            // Get close peds and delete their fight task if they are fighting against the ped
            foreach (var closePed in this.Intelligence.GetPedsAround(1000f, EPedSearchCriteria.All))
            {
                // Check if fighting against the ped
                if (closePed.IsInCombat && closePed.GetEnemyInCombat() == this)
                {
                    closePed.Task.ClearAll();
                }
            }
        }

        /// <summary>
        /// Clears the last vehicle flag.
        /// </summary>
        public void ClearLastUsedVehicle()
        {
            this.lastVehicle = null;
        }

        /// <summary>
        /// Clears the prop at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The prop index.</param>
        public void ClearProp(int index)
        {
            Natives.ClearCharProp(this.ped, index);
        }

        /// <summary>
        /// Clears all props of the ped.
        /// </summary>
        public void ClearProps()
        {
            Natives.ClearCharProps(this.ped);
        }

        /// <summary>
        /// Changes the idle sit animation of the ped.
        /// </summary>
        /// <param name="animationSet">The animation set.</param>
        /// <param name="animationName">The animation name.</param>
        public void ChangeCharSitIdleAnim(string animationSet, string animationName)
        {
            Natives.RequestAnims(animationSet);
            Natives.ChangeCharSitIdleAnim(this.ped, animationSet, animationName);
        }

        public new void Delete()
        {
            DeleteBlip();
            this.Intelligence.Delete();
            base.Delete();
            Pools.PedPool.Remove(this);
        }

        public new void Delete(bool keepActualEntity)
        {
            DeleteBlip();
            this.Intelligence.Delete();
            if (!keepActualEntity)
            {
                base.Delete();
            }

            Pools.PedPool.Remove(this);
        }

        public void DeleteBlip(bool sync = true)
        {
            if (this.HasBlip)
            {
                this.Blip.Delete();

                if (sync)
                {
                    // If host, sync with all clients
                    if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.IsHost && Main.NetworkManager.CanSendData)
                    {
                        Main.NetworkManager.SendMessageWithNetworkID("CPed", EPedNetworkMessages.RemoveBlip, this.NetworkID);
                    }
                }
            }
        }

        public void EnsurePedHasWeapon()
        {
            // If there is a weapon set in ped data, use it
            Weapon weapon = this.PedData.DefaultWeapon;
            if (weapon != Weapon.None)
            {
                EquipWeapon(weapon); return;
            }

            // Select a model based on the model info. Use hardcoded approach if no weapon found
            if (this.Model.ModelInfo != null)
            {
                if (this.Model.ModelInfo.DefaultWeapon != Weapon.None)
                {
                    EquipWeapon(this.Model.ModelInfo.DefaultWeapon);
                    if (this.PedGroup == EPedGroup.Criminal)
                    {
                        new EventArmedCriminal(this);
                    }
                    return;
                }
            }

            if (this.PedGroup == EPedGroup.Criminal)
            {
                EquipWeapon(Weapon.Handgun_Glock);
                new EventArmedCriminal(this);
            }
            else if (this.PedSubGroup == EPedSubGroup.NormalCop)
            {
                EquipWeapon(Weapon.Handgun_Glock);
            }
            else
            {
                this.Weapons.Glock.Ammo = 9999;
                EquipWeapon(Weapon.Handgun_Glock);
            }
        }

        /// <summary>
        /// Will remove ALL weapons
        /// </summary>
        public void EnsurePedHasNoWeapon()
        {
            ped.DropCurrentWeapon();
            Natives.RemoveAllCharWeapons(this);
            ped.Weapons.Select(Weapon.Unarmed);
        }

        /// <summary>
        /// Will remove ALL weapons
        /// </summary>
        public void EnsurePedHasNoWeapon(bool dontDropCurrent)
        {
            if (!dontDropCurrent)
            {
                ped.DropCurrentWeapon();
            }

            Natives.RemoveAllCharWeapons(this);
            ped.Weapons.Select(Weapon.Unarmed);
        }

        /// <summary>
        /// Ensures the ped is not in a building by teleporting around <paramref name="position"/> if necessary. Returns true on success.
        /// </summary>
        /// <param name="position">
        /// The position used as center.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool EnsurePedIsNotInBuilding(Vector3 position)
        {
            int attempts = 0;
            float distance = 1f;
            while (this.IsInBuilding() && attempts < 100)
            {
                Log.Debug("EnsurePedIsNotInBuilding: Adjust position. Attempt: #" + attempts, this);
                this.Position = position.Around(distance);
                distance += 0.2f;
                attempts++;
            }

            return !this.IsInBuilding();
        }

        /// <summary>
        /// Flashes the blip.
        /// </summary>
        /// <param name="value">
        /// Whether the blip should flash or not.
        /// </param>
        public void FlashBlip(bool value)
        {
            Natives.FlashBlip(this.Blip, value);
        }


        /// <summary>
        /// For M_Y_COP and M_M_FATCOP_01 peds, sets them to not spawn with trenchcoats.
        /// </summary>
        public void FixCopClothing()
        {
            if (ped.Model == "M_Y_COP")
            {
                ped.Skin.Component.UpperBody.ChangeIfValid(Common.GetRandomValue(0, 3), 0);
                ped.Skin.Component.LowerBody.ChangeIfValid(0, 0);
            }
            else if (ped.Model == "M_M_FATCOP_01")
            {
                ped.Skin.Component.UpperBody.ChangeIfValid(1, 0);
            }
        }

        /// <summary>
        /// Helper for easier access to PedDataCop
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetPedData<T>() where T : PedData
        {
            return (T) this.PedData;
        }


        /// <summary>
        /// Gets the current enemy in a combat.
        /// </summary>
        /// <returns>The enemy.</returns>
        public CPed GetEnemyInCombat()
        {
            APed enemy = this.APed.GetPedPedIsInCombatWith();

            if (enemy != null && enemy.Get() != 0)
            {
                return Pools.PedPool.GetPedFromPool(enemy);
            }

            return null;
        }

        public GTA.VehicleSeat GetSeatInVehicle()
        {
            if (!this.IsInVehicle()) return GTA.VehicleSeat.None;

            CVehicle vehicle = this.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists()) return GTA.VehicleSeat.None;
            foreach (GTA.VehicleSeat seat in Enum.GetValues(typeof(GTA.VehicleSeat)))
            {
                if (seat == VehicleSeat.AnyPassengerSeat || seat == VehicleSeat.None)
                {
                    continue;
                }

                if (vehicle.GetPedOnSeat(seat) == this) return seat;
            }
            return GTA.VehicleSeat.None;
        }

        public bool HasSpottedChar(CPed ped)
        {
            if (ped.Wanted.Invisible) return false;

            if (this.Exists() && ped.Exists())
            {
                //Log.Debug(ped.APed.Get().ToString() + " this.handle: " + this.Handle + " this.ped.pHandle: " + this.ped.pHandle, this);
                //bool exists = GTA.Native.Function.Call<bool>("DOES_CHAR_EXIST", ped.APed.Get());
                //Log.Debug("bool exists = GTA.Native.Function.Call<bool>(\"DOES_CHAR_EXIST\", ped.APed.Get()); result is: " + exists.ToString(), this);
                return this.APed.HasSpottedChar(ped.APed);
            }

            return false;
        }

        public bool HasSpottedCharInFront(CPed ped)
        {
            if (ped.Wanted.Invisible) return false;

            if (this.Exists() && ped.Exists())
            {
                //Log.Debug(ped.APed.Get().ToString() + " this.handle: " + this.Handle + " this.ped.pHandle: " + this.ped.pHandle, this);
                //bool exists = GTA.Native.Function.Call<bool>("DOES_CHAR_EXIST", ped.APed.Get());
                //Log.Debug("bool exists = GTA.Native.Function.Call<bool>(\"DOES_CHAR_EXIST\", ped.APed.Get()); result is: " + exists.ToString(), this);
                return this.APed.HasSpottedCharInFront(ped.APed);
            }

            return false;
        }

        public bool IsFacingChar(CPed ped)
        {
            return Natives.IsCharFacingChar(this, ped);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ped is being grabbed
        /// </summary>
        public bool IsGrabbed { get; set; }

        /// <summary>
        /// Returns whether the ped is on a street. Not very reliable.
        /// </summary>
        /// <returns></returns>
        public bool IsOnStreet()
        {
            Vector3 positionOnStreet = World.GetNextPositionOnStreet(this.ped.Position);
            return positionOnStreet.DistanceTo(this.ped.Position) < 7;
        }

        /// <summary>
        /// Returns whether the ped is in a building by checking ground z position. Not very reliable.
        /// </summary>
        /// <returns>True if on building, false otherwise.</returns>
        public bool IsInBuilding()
        {
            float z = World.GetGroundZ(this.ped.Position, GroundType.NextAboveCurrent);
            float diff = z - this.ped.Position.Z;
            return diff > 0.1;
        }

        /// <summary>
        /// Checks if the ped is saying ambient speech
        /// </summary>
        /// <returns>Whether the ped is speaking or not</returns>
        public bool IsSayingAmbientSpeech()
        {
            return Natives.IsAmbientSpeechPlaying(this.ped);
        }

        /// <summary>
        /// Jumps while standing if standing still or to the front if moving.
        /// </summary>
        public void Jump()
        {
            if (this.IsStandingStill)
            {
                this.Task.Jump(EJumpType.Stand);
            }
            else
            {
                this.Task.Jump(EJumpType.Front);
            }
        }

        /// <summary>
        /// Changes the relationship with cops to be friends, so the ped is no longer considered an enemy. Also clears the task of cops that are fighting against the ped.
        /// </summary>
        /// <param name="friends">
        /// Whether friends or not.
        /// </param>
        public void MakeFriendsWithCops(bool friends)
        {
            if (friends)
            {
                ped.RelationshipGroup = RelationshipGroup.Cop;
                ped.ChangeRelationship(RelationshipGroup.Cop, Relationship.Companion);

                // Get close cops and delete their fight task if they are fighting against the ped
                foreach (var closePed in this.Intelligence.GetPedsAround(50f, EPedSearchCriteria.CopsOnly | EPedSearchCriteria.NotAvailable))
                {
                    // Check if fighting against the ped
                    if (closePed.IsInCombat && closePed.GetEnemyInCombat() == this)
                    {
                        closePed.Task.ClearAll();
                    }
                }
            }
            else
            {
                ped.RelationshipGroup = RelationshipGroup.Civillian_Male;
                ped.ChangeRelationship(RelationshipGroup.Cop, Relationship.Neutral);
            }
        }

        public void PlaceCamBehind()
        {
            Natives.PlaceCamBehindPed(this.ped);
        }

        public void PlaceCamInFront()
        {
            Natives.PlaceCamInFrontOfPed(this.ped);
        }

        /// <summary>
        /// Puts a hat on or off.
        /// </summary>
        /// <param name="on">Whether a hat should be put on or off.</param>
        public void PutHatOn(bool on)
        {
            if (on)
            {
                ped.Skin.SetPropIndex(0, 0);
            }
            else
            {
                this.ClearProp(0);
            }
        }

        public bool RequestNetworkControl(bool returnImmediately)
        {
            int networkID = this.NetworkID;
            int attempts = 0;
            Natives.SetNetworkIDCanMigrate(networkID, true);
            while (!Natives.HasControlOfNetworkID(networkID) && attempts < 100)
            {
                Natives.RequestControlOfNetworkID(networkID);
                attempts++;
                Game.WaitInCurrentScript(10);
            }

            Natives.SetNetworkIDCanMigrate(networkID, false);
            return Natives.RequestControlOfNetworkID(networkID);
        }

        /// <summary>
        /// Resets the APed property and creates a new instance. Used when player model is changed to get a valid APed instance again.
        /// </summary>
        public void ResetAPed()
        {
            if (this.PedGroup == EPedGroup.Player)
            {
                this.APed = new APed(Game.LocalPlayer.Character.pHandle);
            }
            else
            {
                this.APed = new APed(this.ped.pHandle);
            }
        }

        public void SetComponentVariation(int a0, int a1, int a2)
        {
            Natives.SetCharComponentVariation(this.ped, a0, a1, a2);   
        }

        public void SetFlashlight(bool enabled, bool onlyAtNight, bool force, float coneRange = 6.0f, Color colour = default(Color), float diffusion = 10.0f, float intensity = 20.0f, float envRange = 20f, float envIntensity = 30.92f, float envDiffusion = 25.0f)
        {
            if (enabled)
            {
                // BAD: Move property to engine.
                if (onlyAtNight && !LCPDFR.Globals.IsNightTime)
                {
                    if (!force)
                    {
                        return;
                    }
                }

                if (!this.Intelligence.TaskManager.IsTaskActive(ETaskID.Flashlight))
                {
                    if (colour == default(Color)) colour = System.Drawing.Color.White;
                    TaskFlashlight taskFlashlight = new TaskFlashlight(coneRange, colour, diffusion, intensity, envRange, envDiffusion, envIntensity);
                    taskFlashlight.AssignTo(this, ETaskPriority.SubTask);
                }
            }
            else
            {
                if (this.Intelligence.TaskManager.IsTaskActive(ETaskID.Flashlight))
                {
                    this.Intelligence.TaskManager.Abort(this.Intelligence.TaskManager.FindTaskWithID(ETaskID.Flashlight));
                }
            }
        }

        public void SetTaserLight(bool enabled, bool onlyAtNight, bool force)
        {
            SetFlashlight(enabled, onlyAtNight, force, 5.0f, System.Drawing.Color.Red, 1.0f, 30.0f, 10.0f, 10000.0f, 1.0f);
        }

        

        /// <summary>
        /// Sets the ped's position to <paramref name="position"/> and doesn't warp the gang.
        /// </summary>
        /// <param name="position"></param>
        public void SetPositionDontWarpGang(Vector3 position)
        {
            Natives.SetCharCoordinatesDontWarpGang(this.ped, position);
        }

        /// <summary>
        /// Sets <paramref name="prop"/> to <paramref name="index"/>.
        /// </summary>
        /// <param name="prop">The prop.</param>
        /// <param name="index">The index.</param>
        public void SetProp(EPedProp prop, int index)
        {
            ped.Skin.SetPropIndex((PedProp)prop, index);
        }

        public void SetWaterFlags(bool drownsInVehicle, bool drowns, bool triesToLeaveWater)
        {
            Natives.SetCharDrownsInSinkingVehicle(this.ped, drownsInVehicle);
            Natives.SetCharDrownsInWater(this.ped, drowns);
            Natives.SetCharWillTryToLeaveWater(this.ped, triesToLeaveWater);
        }

        /// <summary>
        /// Sets the weapon of the ped. This forces the weapon without the switching task.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        public void SetWeapon(GTA.Weapon weapon)
        {
            Natives.SetCurrentCharWeapon(this.ped, weapon, true);
        }

        /// <summary>
        /// Teleports the player out of a car to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public void WarpFromCar(GTA.Vector3 position)
        {
            Natives.WarpCharFromCar(this.ped, position);
        }

        private void EquipWeapon(GTA.Weapon weapon)
        {
            this.Weapons[weapon].Ammo = 9999;
            this.Weapons[weapon].Select();
        }

        public override string ComponentName
        {
            get { return "CPed"; }
        }

        // Operators
        public static bool operator ==(CPed ped, CPed ped2)
        {
            // If both are null, return true
            if ((object)ped == null && (object)ped2 == null) return true;
            // If one of them is null, return false
            if ((object)ped == null) return false;
            if ((object)ped2 == null) return false;
            
            // Compare using handle
            if (ped.Handle == ped2.Handle) return true;
            return false;
        }

        public static bool operator !=(CPed ped, CPed ped2)
        {
            return !(ped == ped2);
        }

        public static implicit operator APed(CPed ped)
        {
            return ped.APed;
        }

        // Statics
        public static CPed FromNetworkID(int networkID)
        {
            GTA.Ped ped = null;
            Natives.GetPedFromNetworkID(networkID, ref ped);
            if (ped != null && ped.Exists())
            {
                return Pools.PedPool.GetPedFromPool(ped);
            }
            return null;
        }

        /// <summary>
        /// Gets the closest peds using the <paramref name="pedSearchCriteria"/>.
        /// </summary>
        /// <param name="pedSearchCriteria">The search criteria.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="position">The position.</param>
        /// <returns>The closest vehicle.</returns>
        public static CPed GetClosestPed(EPedSearchCriteria pedSearchCriteria, float distance, Vector3 position)
        {
            CPed closestPed = null;
            float closestDistance = float.MaxValue;
            foreach (CPed ped in CPed.GetPedsAround(distance, pedSearchCriteria, position))
            {
                if (!ped.Exists())
                {
                    continue;
                }
                float distanceTo = ped.Position.DistanceTo(position);
                if (distanceTo < closestDistance)
                {
                    closestPed = ped;
                    closestDistance = distanceTo;
                }
            }
            return closestPed;
        }

        /// <summary>
        /// Gets the number of dead peds in the array.
        /// </summary>
        /// <param name="peds">The ped array to check.</param>
        /// <param name="performExists">Whether an existance check should be performed.</param>
        /// <returns>The number of peds dead.</returns>
        public static int GetNumberOfDeadPeds(CPed[] peds, bool performExists)
        {
            int deadPeds = 0;
            foreach (CPed ped in peds)
            {
                if (performExists)
                {
                    if (!ped.Exists())
                    {
                        continue;
                    }
                }

                if (!ped.IsAliveAndWell)
                {
                    deadPeds++;
                }
            }

            return deadPeds;
        }

        /// <summary>
        /// Gets all peds around <paramref name="position"/> using the <paramref name="pedSearchCriteria"/>.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <param name="pedSearchCriteria">The search criteria.</param>
        /// <param name="position">The position.</param>
        /// <returns>Peds array.</returns>
        public static CPed[] GetPedsAround(float distance, EPedSearchCriteria pedSearchCriteria, Vector3 position)
        {
            List<CPed> peds = new List<CPed>();
            foreach (CPed ped in Pools.PedPool.GetAll())
            {
                if (ped.Exists())
                {
                    if (ped.Position.DistanceTo(position) < distance)
                    {
                        // Process flags
                        if (IsPedValid(pedSearchCriteria, ped))
                        {
                            peds.Add(ped);
                        }
                    }
                }
            }

            // Sort by distance
            var lengths = from element in peds
                          orderby element.Position.DistanceTo2D(position)
                          select element;

            return lengths.ToArray();
        }

        /// <summary>
        /// Checks if there is at least on ped in combat at <paramref name="position"/> in the given range. This excludes the retreat task, but is only for real fighting.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="range">The range.</param>
        /// <returns>True if in combat, false if not</returns>
        public static bool IsPedInCombatInArea(Vector3 position, float range)
        {
            CPed[] pedsInArea = SortByDistanceToPosition(Pools.PedPool.GetAll(), position);

            foreach (CPed ped in pedsInArea)
            {
                if (ped.Position.DistanceTo(position) < range)
                {
                    if (ped.IsInCombat || ped.IsInMeleeCombat)
                    {
                        // Not if using the CopChasePedOnFoot task is active
                        if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopChasePedOnFoot))
                        {
                            continue;
                        }

                        // No retreat task
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsPedValid(EPedSearchCriteria pedSearchCriteria, CPed ped)
        {
            if (pedSearchCriteria.HasFlag(EPedSearchCriteria.All)) return true;

            // Don't allow available peds, unless the flag is set explicitly
            if (!ped.PedData.Available)
            {
                if (!pedSearchCriteria.HasFlag(EPedSearchCriteria.NotAvailable)) return false;
            }
            if (ped.HasOwner)
            {
                if (!pedSearchCriteria.HasFlag(EPedSearchCriteria.HaveOwner)) return false;
            }

            // Either detect cop via ped group, or if player and not ignored by AI
            bool isCop = ped.PedGroup == EPedGroup.Cop;
            if (!isCop)
            {
                isCop = ped.PedGroup == EPedGroup.Player && !CPlayer.LocalPlayer.IgnoredByAI && pedSearchCriteria.HasFlag(EPedSearchCriteria.Player);
            }

            if (isCop && !pedSearchCriteria.HasFlag(EPedSearchCriteria.Cops) && !pedSearchCriteria.HasFlag(EPedSearchCriteria.CopsOnly)) return false;
            if (!isCop && pedSearchCriteria.HasFlag(EPedSearchCriteria.CopsOnly)) return false;

            if (ped.IsInVehicle() && ped.CurrentVehicle.IsSuspectTransporter)
            {
                if (!pedSearchCriteria.HasFlag(EPedSearchCriteria.SuspectTransporter)) return false;
            }
            if (ped.PedGroup == EPedGroup.Player && !pedSearchCriteria.HasFlag(EPedSearchCriteria.Player)) return false;

            if (ped.IsInVehicle)
            {
                if (pedSearchCriteria.HasFlag(EPedSearchCriteria.NotInVehicle))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Sorts all peds by their distance to <paramref name="position"/>. Peforms exists check for <paramref name="peds"/>.
        /// </summary>
        /// <param name="peds">The peds.</param>
        /// <param name="position">The position.</param>
        /// <returns>Peds sorted by distance.</returns>
        public static CPed[] SortByDistanceToPosition(CPed[] peds, Vector3 position)
        {
            // LINQ expression to sort vehicles by distance
            var lengths = from element in peds
                          where element.Exists()
                          orderby element.Position.DistanceTo2D(position)
                          select element;
            return lengths.ToArray();
        }
    }
}