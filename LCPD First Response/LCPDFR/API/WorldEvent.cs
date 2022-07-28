namespace LCPD_First_Response.LCPDFR.API
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.LCPDFR.Scripts.Scenarios;

    internal class ScenarioHelper : Scenario, IAmbientScenario
    {
        private readonly WorldEvent worldEvent;

        public ScenarioHelper(WorldEvent worldEvent)
        {
            this.worldEvent = worldEvent;
        }

        public override string ComponentName
        {
            get
            {
                return this.worldEvent.Name;
            }
        }

        public override void Initialize()
        {
            this.worldEvent.Initialize();
        }

        public override void Process()
        {
            this.worldEvent.Process();
        }

        public bool CanScenarioStart(Vector3 position)
        {
            return this.worldEvent.CanStart(position);
        }

        public bool CanBeDisposedNow()
        {
            bool result = this.worldEvent.CanBeDisposedNow();
            if (result)
            {
                this.worldEvent.End();
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a random world event that happens during game play.
    /// </summary>
    public abstract class WorldEvent : ICanOwnEntities, IPedController
    {
        /// <summary>
        /// The scenario.
        /// </summary>
        private Scenario scenario;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldEvent"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        protected WorldEvent(string name)
        {
            this.Name = name;
            this.scenario = new ScenarioHelper(this);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Checks whether the world event can start at the position depending on available peds.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <returns>
        /// True if yes, otherwise false.
        /// </returns>
        public abstract bool CanStart(Vector3 position);

        /// <summary>
        /// Checks whether the world event can be disposed now, most likely because player got too far away.
        /// </summary>
        /// <returns>True if yes, false otherwise.</returns>
        public abstract bool CanBeDisposedNow();

        /// <summary>
        /// Called right after a world event was started.
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Processes the main logic.
        /// </summary>
        public virtual void Process()
        {        
        }

        /// <summary>
        /// Called when a world event should be disposed. This is also called when <see cref="CanBeDisposedNow"/> returns false.
        /// </summary>
        public virtual void End()
        {
            this.scenario.MakeAbortable();
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        void IPedController.PedHasLeft(CPed ped)
        {
            this.PedLeftScript(new LPed(ped));
        }

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        public virtual void PedLeftScript(LPed ped)
        {
        }

        /// <summary>
        /// Returns the internal <see cref="Scenario"/>.
        /// </summary>
        /// <returns>The scenario.</returns>
        internal Scenario GetScenario()
        {
            return this.scenario;
        }
    }
}