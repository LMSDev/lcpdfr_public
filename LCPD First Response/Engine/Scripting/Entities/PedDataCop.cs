namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using LCPD_First_Response.Engine.Scripting.Tasks;

    /// <summary>
    /// The cop state.
    /// </summary>
    internal enum ECopState
    {
        /// <summary>
        /// No state.
        /// </summary>
        None,

        /// <summary>
        /// Blocker state, not defined.
        /// </summary>
        Blocker,

        /// <summary>
        /// Cop is chasing.
        /// </summary>
        Chase,

        /// <summary>
        /// Cop is idle.
        /// </summary>
        Idle,

        /// <summary>
        /// Cop is investigating.
        /// </summary>
        Investigating,

        /// <summary>
        /// Cop is at a roadblock.
        /// </summary>
        Roadblock,

        /// <summary>
        /// Cop transports a suspect.
        /// </summary>
        SuspectTransporter,
    }

    /// <summary>
    /// Heavily used in conjunction with Chase, CopManager and TaskCop.
    /// </summary>
    internal class PedDataCop : PedData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PedDataCop"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public PedDataCop(CPed ped) : base(ped)
        {
            this.AIEnabled = true;
            this.CopState = ECopState.Idle;
        }

        /// <summary>
        /// Delegate when ped action was requested.
        /// </summary>
        /// <param name="sender">The cop.</param>
        /// <param name="copState">The requested state.</param>
        /// <param name="controller">The controller.</param>
        /// <returns>True if allowed, false if not.</returns>
        public delegate bool RequestedPedActionEventHandler(object sender, ECopState copState, IPedController controller);

        /// <summary>
        /// The event when a ped action was requested.
        /// </summary>
        public event RequestedPedActionEventHandler RequestedPedAction;

        /// <summary>
        /// Gets or sets a value indicating whether TaskCop can make decisions and alter the cop's behavior.
        /// </summary>
        public bool AIEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cop can see the suspect.
        /// </summary>
        public bool CanSeeSuspect { get; set; }

        /// <summary>
        /// Gets the state of the cop.
        /// </summary>
        public ECopState CopState { get; private set; }

        /// <summary>
        /// Gets or sets the current target of the cop.
        /// </summary>
        public CPed CurrentTarget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cop is forced to kill the suspect.
        /// </summary>
        public bool ForceKilling { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cop needs a vehicle to continue the chase.
        /// </summary>
        public bool NeedsVehicleForChase { get; set; }

        // Cop functions

        /// <summary>
        /// Checks whether the cop could change its state to <paramref name="copState"/>.
        /// </summary>
        /// <param name="copState"></param>
        /// <returns></returns>
        public bool IsFreeForAction(ECopState copState, IPedController pedController)
        {
            if (RequestedPedAction != null)
            {
                foreach (RequestedPedActionEventHandler @delegate in RequestedPedAction.GetInvocationList())
                {
                    if (!@delegate.Invoke(this.Ped, copState, pedController)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Helper function to indicate if the given controller is still allowed to use the ped
        /// </summary>
        /// <param name="pedController"></param>
        /// <returns></returns>
        public bool IsPedStillUseable(IPedController pedController)
        {
            return this.Ped.Intelligence.PedController == pedController;
        }

        /// <summary>
        /// 'Asks' the cop if he is free for the given state. TaskCop handles this
        /// </summary>
        /// <param name="copState"></param>
        /// <param name="pedController"></param>
        /// <returns></returns>
        public bool RequestPedAction(ECopState copState, IPedController pedController)
        {
            if (RequestedPedAction != null)
            {
                foreach (RequestedPedActionEventHandler @delegate in RequestedPedAction.GetInvocationList())
                {
                    if (!@delegate.Invoke(this.Ped, copState, pedController)) return false;
                }
            }

            this.CopState = copState;

            // If not idle, set new action
            if (copState != ECopState.Idle)
            {
                this.Ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, pedController);
            }

            return true;
        }

        /// <summary>
        /// Resets the ped state if the given pedController is the current controller
        /// </summary>
        /// <param name="pedController">The ped controller.</param>
        public void ResetPedAction(IPedController pedController)
        {
            if (pedController == this.Ped.Intelligence.PedController)
            {
                this.Ped.Intelligence.ResetAction(pedController);

                // Let all event listeners know, the state should be idle now
                this.RequestPedAction(ECopState.Idle, null);
            }
        }

        /// <summary>
        /// Resets the ped state if the given pedController is the current controller. DO NOT USE THIS IF YOU DON'T KNOW WHAT YOU ARE DOING.
        /// </summary>
        /// <param name="pedController">The ped controller.</param>
        /// <param name="force">Indicates if the current controller parameter can be ignored.</param>
        public void ResetPedAction(IPedController pedController, bool force)
        {
            if (pedController == this.Ped.Intelligence.PedController || force)
            {
                this.Ped.Intelligence.ResetAction(pedController);

                // Let all event listeners know, the state should be idle now
                this.RequestPedAction(ECopState.Idle, null);
            }
        }
    }
}