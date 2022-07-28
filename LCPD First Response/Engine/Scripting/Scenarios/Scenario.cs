namespace LCPD_First_Response.Engine.Scripting.Scenarios
{
    using System.Collections.Generic;
    using LCPD_First_Response.Engine.Scripting.Entities;

    internal abstract class Scenario : BaseComponent
    {
        public bool Active { get; private set; }
        protected List<CEntity> OwnedEntities { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scenario"/> class.
        /// </summary>
        protected Scenario()
        {
            this.OwnedEntities = new List<CEntity>();
            this.Active = true;
        }

        /// <summary>
        /// Processes the scenario.
        /// </summary>
        public void InternalProcess()
        {
            this.Process();
        }

        /// <summary>
        /// Adds the entity to the owned list
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void AddOwnedEntity(CEntity entity)
        {
            this.OwnedEntities.Add(entity);
        }

        /// <summary>
        /// Deletes the entity from the game
        /// </summary>
        public void DeleteAllOwnedEntities()
        {
            foreach (CEntity ownedEntity in this.OwnedEntities)
            {
                ownedEntity.DeleteEntity();
            }   
        }

        /// <summary>
        /// Frees all entities.
        /// </summary>
        public void FreeAllOwnedEntities()
        {
            foreach (CEntity ownedEntity in this.OwnedEntities.ToArray())
            {
                ownedEntity.ReleaseOwnership(ownedEntity.Owner);
            }
        }

        /// <summary>
        /// Removes the entity from the owned list
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void RemoveOwnedEntity(CEntity entity)
        {
            this.OwnedEntities.Remove(entity);
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public virtual void MakeAbortable()
        {
            this.FreeAllOwnedEntities();
            this.Active = false;
            this.Delete();
        }

        /// <summary>
        /// Doesn't delete all owned entities nor their ownership, but all other references. Use this all the time you don't want your entities to be delete ingame
        /// </summary>
        /// <param name="noCleanup"></param>
        public virtual void MakeAbortable(bool noCleanup)
        {
            this.Active = false;

            this.Delete();
        }

        /// <summary>
        /// This is called immediately before the scenario is executed the first time.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public abstract void Process();
    }
}