namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Stores the options for an entity assigned to a content manager.
    /// </summary>
    internal class ContentManagerOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentManagerOptions"/> class.
        /// </summary>
        /// <param name="distanceCheck">
        /// Whether the distance should be checked.
        /// </param>
        /// <param name="distance">
        /// The distance.
        /// </param>
        /// <param name="entityToCheckDistanceFrom">
        /// The entity to check the distance from.
        /// </param>
        /// <param name="delete">
        /// Whether the entity should be deleted instead of freed.
        /// </param>
        /// <param name="dontDeleteBlip">
        /// Whether the blip shouldn't be deleted.
        /// </param>
        /// <param name="kill">
        /// Whether the entity should be killed before freed.
        /// </param>
        public ContentManagerOptions(bool distanceCheck, float distance, CEntity entityToCheckDistanceFrom, bool delete, bool dontDeleteBlip, bool kill)
        {
            this.DistanceCheck = distanceCheck;
            this.Distance = distance;
            this.EntityToCheckDistanceFrom = entityToCheckDistanceFrom;
            this.Delete = delete;
            this.DontDeleteBlip = dontDeleteBlip;
            this.Kill = kill;
        }

        /// <summary>
        /// Gets a value indicating whether the distance should be checked.
        /// </summary>
        public bool DistanceCheck { get; private set; }

        /// <summary>
        /// Gets the distance.
        /// </summary>
        public float Distance { get; private set; }

        /// <summary>
        /// Gets the entity to check the distance from.
        /// </summary>
        public CEntity EntityToCheckDistanceFrom { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the entity should be deleted instead of freed.
        /// </summary>
        public bool Delete { get; private set; }

        /// <summary>
        /// Gets value indicating whether the blip shouldn't be deleted.
        /// </summary>
        public bool DontDeleteBlip { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the entity should be killed before freed.
        /// </summary>
        public bool Kill { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been processed.
        /// </summary>
        public bool HasBeenProcessed { get; set; }

    }
}
