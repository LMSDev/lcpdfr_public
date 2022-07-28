namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;

    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Scenarios;


    /// <summary>
    /// The entity type.
    /// </summary>
    internal enum EEntityType : byte
    {
        /// <summary>
        /// Ped type.
        /// </summary>
        Ped,

        /// <summary>
        /// Vehicle type.
        /// </summary>
        Vehicle,
    }

    /// <summary>
    /// Base class for all entities.
    /// </summary>
    internal abstract class CEntity : BaseComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CEntity"/> class.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        protected CEntity(EEntityType type)
        {
            this.EntityType = type;
        }

        /// <summary>
        /// Gets the content manager resposible for managing this entity.
        /// </summary>
        public ContentManager ContentManager { get; private set; }

        /// <summary>
        /// Gets the options used for the content manager.
        /// </summary>
        public ContentManagerOptions ContentManagerOptions { get; private set; }

        /// <summary>
        /// Gets the entitiy type.
        /// </summary>
        public EEntityType EntityType { get; private set; }

        /// <summary>
        /// Gets the in-game handle of the entity.
        /// </summary>
        public int Handle { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entity has been created by the game or by us.
        /// </summary>
        public bool HasBeenCreatedByUs { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this entity has an owner.
        /// </summary>
        public bool HasOwner { get; private set; }

        /// <summary>
        /// Gets the owner of the entity.
        /// </summary>
        public ICanOwnEntities Owner { get; private set; }


        /// <summary>
        /// Changes the content manager, that is resposible for the entity. For engine usage only. Use ContentManager.Add to add entites instead!
        /// </summary>
        /// <param name="contentManager">The content Manager.</param>
        /// <param name="contentManagerOptions">The content manager options.</param>
        public void ChangeContentManager(ContentManager contentManager, ContentManagerOptions contentManagerOptions)
        {
            this.ContentManager = contentManager;
            this.ContentManagerOptions = contentManagerOptions;
        }

        /// <summary>
        /// Deletes the entity from the game
        /// </summary>
        public void DeleteEntity()
        {
            if (this.Exists())
            {
                if (this is CPed)
                {
                    ((CPed)this).Delete();
                }

                if (this is CVehicle)
                {
                    ((CVehicle)this).Delete();
                }
            }
        }

        /// <summary>
        /// Returns if the entity still exists in-game.
        /// </summary>
        /// <returns>True if exists, false if not.</returns>
        public bool Exists()
        {
            if (this is CPed)
            {
                return ((CPed)this).Exists();
            }

            if (this is CVehicle)
            {
                return ((CVehicle)this).Exists();
            }

            return false;
        }

        /// <summary>
        /// Marks the entity as no longer needed.
        /// </summary>
        public void Free()
        {
            if (this is CPed)
            {
                ((CPed)this).NoLongerNeeded();
            }

            if (this is CVehicle)
            {
                ((CVehicle)this).NoLongerNeeded();
            }
        }

        /// <summary>
        /// Releases the ownership of the ped, if <paramref name="canOwnEntities"/> is the owner.
        /// </summary>
        /// <param name="canOwnEntities">The owner.</param>
        /// <param name="setAsNoLongerNeeded">Whether or not entity should be marked as no longer needed.</param>
        public void ReleaseOwnership(ICanOwnEntities canOwnEntities, bool setAsNoLongerNeeded = true)
        {
            if (this.Owner == canOwnEntities)
            {
                this.Owner = null;
                this.HasOwner = false;

                // Set available to true
                if (this.Exists())
                {
                    if (this.EntityType == EEntityType.Ped)
                    {
                        CPed ped = this as CPed;
                        ped.PedData.Available = true;
                        if (setAsNoLongerNeeded)
                        {
                            ped.NoLongerNeeded();
                        }
                    }

                    if (this.EntityType == EEntityType.Vehicle)
                    {
                        if (setAsNoLongerNeeded)
                        {
                            ((CVehicle)this).NoLongerNeeded();
                        }
                    }
                }

                // If the owner is a scenario subtype, inform the scenario class about the remove of the object
                if (canOwnEntities is Scenario || canOwnEntities.GetType().IsSubclassOf(typeof(Scenario)))
                {
                    ((Scenario)canOwnEntities).RemoveOwnedEntity(this);
                }
            }
        }

        /// <summary>
        /// Requests ownership for the entity using <paramref name="canOwnEntities"/>. Note: This sets CPed.PedData.Available to false for peds!
        /// </summary>
        /// <param name="canOwnEntities">The owner.</param>
        /// <param name="setAsMissionEntity">Whether or not the entity should be set as mission entity. Default is true.</param>
        public void RequestOwnership(ICanOwnEntities canOwnEntities, bool setAsMissionEntity = true)
        {
            if (!this.HasOwner)
            {
                this.Owner = canOwnEntities;
                this.HasOwner = true;

                // Set available to false
                if (this.Exists())
                {
                    if (this.EntityType == EEntityType.Ped)
                    {
                        CPed ped = this as CPed;
                        ped.PedData.Available = false;
                        if (setAsMissionEntity)
                        {
                            ped.BecomeMissionCharacter();
                            //ped.IsRequiredForMission = true;
                        }
                    }

                    if (this.EntityType == EEntityType.Vehicle)
                    {
                        if (setAsMissionEntity)
                        {
                            ((CVehicle)this).IsRequiredForMission = true;
                        }
                    }
                }


                // If the new owner is a scenario subtype, inform the scenario class about its new object
                if (canOwnEntities is Scenario || canOwnEntities.GetType().IsSubclassOf(typeof(Scenario)))
                {
                    ((Scenario)canOwnEntities).AddOwnedEntity(this);
                }
            }
            else
            {
                string currentName = this.Owner.GetType().FullName;
                string newName = canOwnEntities.GetType().FullName;

                if (this.Owner is BaseComponent)
                {
                    currentName = ((BaseComponent)this.Owner).ComponentName;
                }
                else if (this.Owner is BaseScript)
                {
                    currentName = ((BaseScript)this.Owner).ScriptInfo.Name;
                }

                if (canOwnEntities is BaseComponent)
                {
                    newName = ((BaseComponent)canOwnEntities).ComponentName;
                }
                else if (canOwnEntities is BaseScript)
                {
                    newName = ((BaseScript)canOwnEntities).ScriptInfo.Name;
                }

                throw new Exception("Entity already has an owner! Name: " + currentName + " Requesting owner name: " + newName);
            }
        }

        /// <summary>
        /// Resets the content manager. Only use this when you know what you are doing!
        /// </summary>
        public void ResetContentManager()
        {
            this.ContentManager = null;
            this.ContentManagerOptions = null;
        }

        /// <summary>
        /// Sets the handle of the entity.
        /// </summary>
        /// <param name="handle">The handle.</param>
        protected void SetHandle(int handle)
        {
            this.Handle = handle;
        }
    }
}
