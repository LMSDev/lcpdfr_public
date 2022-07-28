namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;

    /// <summary>
    /// Task that processes a scenario. Is an anonymous task
    /// </summary>
    internal class TaskScenario : TaskAnonymous
    {
        /// <summary>
        /// The scenario.
        /// </summary>
        private Scenario scenario;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskScenario"/> class.
        /// </summary>
        /// <param name="scenario">
        /// The scenario.
        /// </param>
        /// <param name="autoAssign">
        /// The auto assign.
        /// </param>
        public TaskScenario(Scenario scenario, bool autoAssign = true) : base(ETaskID.Scenario)
        {
            this.scenario = scenario;

            // Don't forward auto assign to base constructor but call here to ensure this.scenario is set when Initialize is called
            if (autoAssign)
            {
                AssignTo();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the scenario is still running.
        /// </summary>
        public bool IsScenarioActive
        {
            get
            {
                return this.scenario.Active;
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            // Abort scenario aswell
            this.scenario.MakeAbortable();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            this.scenario.Initialize();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!this.scenario.Active)
            {
                SetTaskAsDone();
                return;
            }
            this.scenario.InternalProcess();
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskScenario"; }
        }
    }
}
