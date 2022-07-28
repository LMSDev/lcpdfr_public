namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System.Diagnostics;
    using System.Drawing;
    using GTA;
    using GTA.value;

    /// <summary>
    /// This class is a wrapper class for <see cref="GTA.Ped"/> to make it able to inherit, because GTA.Ped
    /// is sealed. This class does not contain any new features, see CPed.cs instead.
    /// CeasedToExist was not wrapped!
    /// Get/Has/SetMetadata was not wrapped!
    /// Some names have been adjusted aswell as some properties and functions were set to protected to avoid returning of GTA objects (GTA.Ped and GTA.Vehicle)
    /// </summary>
    abstract class PedBase : CEntity
    {
        protected GTA.Ped ped;

        protected PedBase() : base(EEntityType.Ped) {}
        #region Propertys
        public int Accuracy
        {
            // Get not available using a native, memory?
            set
            {
                this.ped.Accuracy = value;
            }
        }
        public bool AlwaysDiesOnLowHealth
        {
            set
            {
                this.ped.AlwaysDiesOnLowHealth = value;
            }
        }
        /// <summary>
        /// Provides control over ped animations.
        /// </summary>
        public PedAnimation Animation
        {
            get
            {
                return this.ped.Animation;
            }
        }
        /// <summary>
        /// Gets or sets the current health of the ped. 0 to 100 is the normal range.
        /// </summary>
        public int Armor
        {
            get
            {
                return this.ped.Armor;
            }
            set
            {
                this.ped.Armor = value;
            }
        }
        public bool BlockGestures
        {
            set
            {
                this.ped.BlockGestures = value;
            }
        }
        /// <summary>
        /// Sets whether permanent events - like responding to an aggressor - that may abort currently assigned tasks should be blocked.
        /// </summary>
        public bool BlockPermanentEvents
        {
            set
            {
                this.ped.BlockPermanentEvents = value;
            }
        }
        public bool BlockWeaponSwitching
        {
            set
            {
                this.ped.BlockWeaponSwitching = value;
            }
        }
        public bool CanBeDraggedOutOfVehicle
        {
            set
            {
                this.ped.CanBeDraggedOutOfVehicle = value;
            }
        }
        public bool CanBeKnockedOffBike
        {
            set
            {
                this.ped.CanBeKnockedOffBike = value;
            }
        }
        public bool CanSwitchWeapons
        {
            set
            {
                this.ped.CanSwitchWeapons = value;
            }
        }
        public bool CowerInsteadOfFleeing
        {
            set
            {
                this.ped.CowerInsteadOfFleeing = value;
            }
        }
        /// <summary>
        /// Gets or sets the interior room the ped currently is at. It needs to be set correctly for the ped to be visible inside the room.
        /// </summary>
        public Room CurrentRoom
        {
            get
            {
                return this.ped.CurrentRoom;
            }
            set
            {
                this.ped.CurrentRoom = value;
            }
        }
        /// <summary>
        /// Gets the currently used vehicle of the ped. Returns Nothing when no vehicle is used
        /// </summary>
        //protected Vehicle CurrentVehicle
        //{
        //    get
        //    {
        //        return this.ped.CurrentVehicle;
        //    }
        //}
        /// <summary>
        /// Gets the currently used vehicle of the ped. Returns Nothing when no vehicle is used
        /// </summary>
        public CVehicle CurrentVehicle
        {
            get
            {
                if (!this.IsInVehicle()) return null;
                return Pools.VehiclePool.GetVehicleFromPool(this.ped.CurrentVehicle);
            }
        }
        public Vector3 Direction
        {
            get
            {
                return this.ped.Direction;
            }
        }
        public bool DuckWhenAimedAtByGroupMember
        {
            set
            {
                this.ped.DuckWhenAimedAtByGroupMember = value;
            }
        }
        /// <summary>
        /// Sets whether the ped should be considered an enemy of the player.
        /// </summary>
        public bool Enemy
        {
            set
            {
                this.ped.Enemy = value;
            }
        }
        /// <summary>
        /// Provides some easy to use Euphoria/NaturalMotion objects.
        /// </summary>
        public Euphoria Euphoria
        {
            get
            {
                return this.ped.Euphoria;
            }
        }
        public float FireDamageMultiplier
        {
            set
            {
                this.ped.FireDamageMultiplier = value;
            }
        }
        public bool FreezePosition
        {
            set
            {
                this.ped.FreezePosition = value;
            }
        }
        public Gender Gender
        {
            get
            {
                return this.ped.Gender;
            }
        }
        public float GravityMultiplier
        {
            set
            {
                this.ped.GravityMultiplier = value;
            }
        }
        public float Heading
        {
            get
            {
                return this.ped.Heading;
            }
            set
            {
                this.ped.Heading = value;
            }
        }
        /// <summary>
        /// Gets or sets the current health of the ped. 1 to 100 is the normal range. 
        /// Can be greater than 100 for Peds with incresed MaxHealth.
        /// -99 to 0 is injured. -100 is completely dead.
        /// </summary>
        public int Health
        {
            get
            {
                return this.ped.Health;
            }
            set
            {
                this.ped.Health = value;
            }
        }
        public float HeightAboveGround
        {
            get
            {
                return this.ped.HeightAboveGround;
            }
        }
        public bool Invincible
        {
            set
            {
                this.ped.Invincible = value;
            }
        }
        public bool IsAlive
        {
            get
            {
                return this.ped.isAlive;
            }
        }
        public bool IsAliveAndWell
        {
            get
            {
                return this.ped.isAliveAndWell;
            }
        }
        public bool IsDead
        {
            get
            {
                return this.ped.isDead;
            }
        }
        public bool IsGettingIntoAVehicle
        {
            get
            {
                return this.ped.isGettingIntoAVehicle;
            }
        }
        public bool IsGettingUp
        {
            get
            {
                return this.ped.isGettingUp;
            }
        }
        /// <summary>
        /// Gets whether the ped is currently available for idle animations. Has some overhead, don't call it every frame.
        /// </summary>
        public bool IsIdle
        {
            get
            {
                return this.ped.isIdle;
            }
        }
        public bool IsInAir
        {
            get
            {
                return this.ped.isInAir;
            }
        }
        public bool IsInCombat
        {
            get
            {
                return this.ped.isInCombat;
            }
        }
        public bool IsInGroup
        {
            get
            {
                return this.ped.isInGroup;
            }
        }
        public bool IsInjured
        {
            get
            {
                return this.ped.isInjured;
            }
        }
        public bool IsInMeleeCombat
        {
            get
            {
                return this.ped.isInMeleeCombat;
            }
        }
        public bool IsInWater
        {
            get
            {
                return this.ped.isInWater;
            }
        }
        // It's obsolete, so we remove it
        //[Obsolete("Please use the property isRequiredForMission instead!")]
        //public bool IsMissionCharacter
        //{
        //    get
        //    {
        //        // DO NOT REMOVE THIS ALTOUGH IT'S MARKED AS OBSOLETE!
        //        return this.ped.isMissionCharacter;
        //    }
        //}
        public bool IsOnFire
        {
            get
            {
                return this.ped.isOnFire;
            }
            set
            {
                this.ped.isOnFire = value;
            }
        }
        public bool IsOnScreen
        {
            get
            {
                return this.ped.isOnScreen;
            }
        }
        public bool IsRagdoll
        {
            get
            {
                return this.ped.isRagdoll;
            }
            set
            {
                this.ped.isRagdoll = value;
            }
        }
        public bool IsRequiredForMission
        {
            get
            {
                return this.ped.isRequiredForMission;
            }
            set
            {
                this.ped.isRequiredForMission = value;
            }
        }
        public bool IsShooting
        {
            get
            {
                return this.ped.isShooting;
            }
        }
        public bool IsSwimming
        {
            get
            {
                return this.ped.isSwimming;
            }
        }
        /// <summary>
        /// Gets or sets the maximum health for NPCs. Default is 100. [Note by LMS: Get is added in current svn version, but not in current release!] [Note by LMS: No idea what I was talking about :D]
        /// </summary>
        public int MaxHealth
        {
            set
            {
                this.ped.MaxHealth = value;
            }
        }
        // No metadata
        ///// <summary>
        ///// Allows you to store Metadata on an object. Metadata can be read back as long as the object exists. 
        ///// Usage: object.Metadata.MyValueName = MyData
        ///// </summary>
        ///// <param name="ValueName">Name of the stored Metadata.</param>
        //public object Metadata
        //{
        //    get
        //    {
        //        return this.ped.Metadata;
        //    }
        //}
        public CModel Model
        {
            get
            {
                return new CModel(this.ped.Model);
            }
        }
        public int Money
        {
            get
            {
                return this.ped.Money;
            }
            set
            {
                this.ped.Money = value;
            }
        }
        public PedType PedType
        {
            get
            {
                return this.ped.PedType;
            }
        }
        public Vector3 Position
        {
            get
            {
                return this.ped.Position;
            }
            set
            {
                this.ped.Position = value;
            }
        }
        public bool PreventRagdoll
        {
            set
            {
                this.ped.PreventRagdoll = value;
            }
        }
        public bool PriorityTargetForEnemies
        {
            set
            {
                this.ped.PriorityTargetForEnemies = value;
            }
        }
        public RelationshipGroup RelationshipGroup
        {
            get
            {
                return this.ped.RelationshipGroup;
            }
            set
            {
                this.ped.RelationshipGroup = value;
            }
        }
        public float SenseRange
        {
            set
            {
                this.ped.SenseRange = value;
            }
        }
        public PedSkin Skin
        {
            get
            {
                return this.ped.Skin;
            }
        }
        /// <summary>
        /// Tasks control the behaviour of peds. 
        /// </summary>
        public PedTasks Task
        {
            get
            {
                return this.ped.Task;
            }
        }
        public Vector3 Velocity
        {
            get
            {
                return this.ped.Velocity;
            }
            set
            {
                this.ped.Velocity = value;
            }
        }
        public bool Visible
        {
            set
            {
                this.ped.Visible = value;
            }
        }
        public string Voice
        {
            set
            {
                this.ped.Voice = value;
            }
        }
        public bool WantedByPolice
        {
            set
            {
                this.ped.WantedByPolice = value;
            }
        }
        /// <summary>
        /// Contains several classes to access weapon functionality. 
        /// </summary>
        public WeaponCollection Weapons
        {
            get
            {
                return this.ped.Weapons;
            }
        }
        public bool WillDoDrivebys
        {
            set
            {
                this.ped.WillDoDrivebys = value;
            }
        }
        public bool WillFlyThroughWindscreen
        {
            set
            {
                this.ped.WillFlyThroughWindscreen = value;
            }
        }
        public bool WillUseCarsInCombat
        {
            set
            {
                this.ped.WillUseCarsInCombat = value;
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// Applies a force given in world coordinates to the ped.
        /// </summary>
        /// <param name="Direction"></param>
        public void ApplyForce(Vector3 Direction)
        {
            this.ped.ApplyForce(Direction);
        }

        /// <summary>
        /// Applies a force given in world coordinates to the ped.
        /// </summary>
        /// <param name="Direction"></param>
        /// <param name="Rotation"></param>
        public void ApplyForce(Vector3 Direction, Vector3 Rotation)
        {
            this.ped.ApplyForce(Direction, Rotation);
        }

        /// <summary>
        /// Applies a force that is relative to the current orientation of the ped.
        /// Directions: positive X is right, positive Y is front, positive Z is Top 
        /// </summary>
        /// <param name="Direction"></param>
        public void ApplyForceRelative(Vector3 Direction)
        {
            this.ped.ApplyForceRelative(Direction);
        }

        /// <summary>
        /// Applies a force that is relative to the current orientation of the ped.
        /// Directions: positive X is right, positive Y is front, positive Z is Top
        /// </summary>
        /// <param name="Direction"></param>
        /// <param name="Rotation"></param>
        public void ApplyForceRelative(Vector3 Direction, Vector3 Rotation)
        {
            this.ped.ApplyForceRelative(Direction, Rotation);
        }

        protected Blip AttachBlip()
        {
            return this.ped.AttachBlip();
        }

        ///// <summary>
        ///// Attaches the ped to a vehicle WITHOUT physics!
        ///// </summary>
        ///// <param name="vehicle"></param>
        ///// <param name="Offset"></param>
        //protected void AttachTo(GTA.Vehicle vehicle, Vector3 Offset)
        //{
        //    this.ped.AttachTo(vehicle, Offset);
        //}
        /// <summary>
        /// Attaches the ped to a vehicle WITHOUT physics!
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="offset"></param>
        public void AttachTo(CVehicle vehicle, GTA.Vector3 offset)
        {
            this.ped.AttachTo(vehicle, offset);
        }

        public void BecomeMissionCharacter()
        {
            this.ped.BecomeMissionCharacter();
        }

        public void CancelAmbientSpeech()
        {
            this.ped.CancelAmbientSpeech();
        }

        public void CantBeDamagedByRelationshipGroup(RelationshipGroup group, bool value)
        {
            this.ped.CantBeDamagedByRelationshipGroup(group, value);
        }

        public void ChangeRelationship(RelationshipGroup group, Relationship level)
        {
            this.ped.ChangeRelationship(group, level);
        }

        public new void Delete()
        {
            this.ped.Delete();
            base.Delete();
        }

        public void Detach()
        {
            this.ped.Detach();
        }

        public void Die()
        {
            this.ped.Die();
        }

        public void DropCurrentWeapon()
        {
            this.ped.DropCurrentWeapon();
        }

        public bool Equals(GTA.@base.HandleObject obj)
        {
            return this.ped.Equals(obj);
        }

        public bool Equals(GTA.@base.iHandleObject obj)
        {
            return this.ped.Equals(obj);
        }

        ///// <summary>
        ///// Returns whether the object still exists in game. Other calls to non-existing objects will result in an error. This function is FAST and is better checked too much rather than too less.
        ///// </summary>
        //public bool Exists()
        //{
        //    return this.ped.Exists();
        //}
        public new bool Exists()
        {
            try
            {
                //StackFrame stackFrame = new StackFrame(1);
                //string file = stackFrame.GetMethod().DeclaringType.FullName;
                //string method = stackFrame.GetMethod().Name;
                //Log.Info(file + ": " + method, "");
                return this.ped != null && this.ped.Exists();
            }
            catch (System.StackOverflowException)
            {
                StackFrame stackFrame = new StackFrame(1);
                string file = stackFrame.GetFileName();
                int line = stackFrame.GetFileLineNumber();
                string method = stackFrame.GetMethod().Name;
                Log.Info(method, "");
                throw;
            }
        }


        //protected void FleeByVehicle(GTA.Vehicle Vehicle)
        //{
        //    this.ped.FleeByVehicle(Vehicle);
        //}
        public void FleeByVehicle(CVehicle vehicle)
        {
            this.ped.FleeByVehicle(vehicle);
        }

        public void ForceHelmet(bool enable)
        {
            this.ped.ForceHelmet(enable);
        }

        public void ForceRagdoll(int Duration, bool TryToStayUpright)
        {
            this.ped.ForceRagdoll(Duration, TryToStayUpright);
        }

        public Vector3 GetBonePosition(Bone Bone)
        {
            return this.ped.GetBonePosition(Bone);
        }

        public Player GetControllingPlayer()
        {
            return this.ped.GetControllingPlayer();
        }

        public new int GetHashCode()
        {
            return this.ped.GetHashCode();
        }

        /// <summary>
        /// Returns the given offset in world coordinates.
        /// Directions: positive X is right, positive Y is front, positive Z is Top 
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        public Vector3 GetOffsetPosition(Vector3 Offset)
        {
            return this.ped.GetOffsetPosition(Offset);
        }

        public void GiveFakeNetworkName(string Name, Color Color)
        {
            this.ped.GiveFakeNetworkName(Name, Color);
        }

        //protected bool HasBeenDamagedBy(GTA.Ped ped)
        //{
        //    return this.ped.HasBeenDamagedBy(ped);
        //}

        public bool HasBeenDamagedBy(CPed ped)
        {
            return this.ped.HasBeenDamagedBy(ped);
        }

        public bool HasBeenDamagedBy(CVehicle vehicle)
        {
            return this.ped.HasBeenDamagedBy(vehicle);
        }

        public bool HasBeenDamagedBy(GTA.Weapon weapon)
        {
            return this.ped.HasBeenDamagedBy(weapon);
        }

        //protected bool HasBeenDamagedBy(GTA.Vehicle vehicle)
        //{
        //    return this.ped.HasBeenDamagedBy(vehicle);
        //}

        public bool IsAttachedToVehicle()
        {
            return this.ped.isAttachedToVehicle();
        }

        public bool IsInArea(Vector3 Corner1, Vector3 Corner2, bool IgnoreHeight)
        {
            return this.ped.isInArea(Corner1, Corner2, IgnoreHeight);
        }

        public bool IsInVehicle()
        {
            return this.ped.isInVehicle();
        }

        //protected bool IsInVehicle(GTA.Vehicle vehicle)
        //{
        //    return this.ped.isInVehicle(vehicle);
        //}
        public bool IsInVehicle(CVehicle vehicle)
        {
            if (!vehicle.Exists()) return false;
            return this.ped.isInVehicle(vehicle);
        }

        public bool IsSittingInVehicle()
        {
            return this.ped.isSittingInVehicle();
        }

        //protected bool IsSittingInVehicle(GTA.Vehicle vehicle)
        //{
        //    return this.ped.isSittingInVehicle(vehicle);
        //}
        public bool IsSittingInVehicle(CVehicle vehicle)
        {
            return this.ped.isInVehicle(vehicle);
        }

        // @ because 'object' normally is System.Object, but we it as param name here
        protected bool IsTouching(GTA.Object @object)
        {
            return this.ped.isTouching(@object);
        }

        //protected bool IsTouching(GTA.Ped ped)
        //{
        //    return this.ped.isTouching(ped);
        //}

        //protected bool IsTouching(GTA.Vehicle vehicle)
        //{
        //    return this.ped.isTouching(vehicle);
        //}
        public bool IsTouching(CPed ped)
        {
            return this.ped.isTouching(ped);
        }

        public bool IsTouching(CVehicle vehicle)
        {
            return this.ped.isTouching(vehicle);
        }

        public void LeaveGroup()
        {
            this.ped.LeaveGroup();
        }

        public void LeaveVehicle()
        {
            this.ped.LeaveVehicle();
        }

        public void MakeProofTo(bool Bullets, bool Fire, bool Explosions, bool FallingDamage, bool MeeleAttacks)
        {
            this.ped.MakeProofTo(Bullets, Fire, Explosions, FallingDamage, MeeleAttacks);
        }

        public void NoLongerNeeded()
        {
            this.ped.NoLongerNeeded();
        }

        public void RandomizeOutfit()
        {
            this.ped.RandomizeOutfit();
        }

        public void RemoveFakeNetworkName()
        {
            this.ped.RemoveFakeNetworkName();
        }

        public void SayAmbientSpeech(string PhraseID)
        {
            this.ped.SayAmbientSpeech(PhraseID);
        }

        public void SetDefaultVoice()
        {
            this.ped.SetDefaultVoice();
        }

        public void SetDefensiveArea(Vector3 Positon, float Radius)
        {
            this.ped.SetDefensiveArea(Positon, Radius);
        }

        public void SetPathfinding(bool AllowClimbovers, bool AllowLadders, bool AllowDropFromHeight)
        {
            this.ped.SetPathfinding(AllowClimbovers, AllowLadders, AllowDropFromHeight);
        }

        public void ShootAt(Vector3 Position)
        {
            this.ped.ShootAt(Position);
        }

        public void StartKillingSpree(bool alsoAttackPlayer)
        {
            this.ped.StartKillingSpree(alsoAttackPlayer);
        }

        //protected void WarpIntoVehicle(GTA.Vehicle Vehicle, VehicleSeat Seat)
        //{
        //    this.ped.WarpIntoVehicle(Vehicle, Seat);
        //}
        public void WarpIntoVehicle(CVehicle vehicle, GTA.VehicleSeat seat)
        {
            this.ped.WarpIntoVehicle(vehicle, seat);
        }
        #endregion

        public static implicit operator GTA.Ped(PedBase ped)
        {
            return ped.ped;
        }

        //public static implicit operator BasePed(Player source)
        //{
        //    // As long as Player isn't wrapped we have to convert the baseped
        //    return new BasePed(source.Character);
        //}
    }
}