namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Base class for almost all actions.
    /// </summary>
    public abstract class BaseScript
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseScript"/> class.
        /// </summary>
        protected BaseScript()
        {
            this.ScriptInfo = AssemblyHelper.GetAttribute<ScriptInfoAttribute>(this.GetType());
        }

        /// <summary>
        /// Event handler for OnEnd.
        /// </summary>
        /// <param name="sender">The sender.</param>
        public delegate void OnEndEventHandler(object sender);

        /// <summary>
        /// Invoked when the script has ended.
        /// </summary>
        public event OnEndEventHandler OnEnd;

        /// <summary>
        /// Gets the script info.
        /// </summary>
        public ScriptInfoAttribute ScriptInfo { get; private set; }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public virtual void End()
        {
            DelayedCaller.ClearAllRunningCalls(false, this);

            if (this.OnEnd != null)
            {
                this.OnEnd(this);
            }
        }

        /// <summary>
        /// Called every tick to process all script logic.
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Registers all console commands.
        /// </summary>
        protected void RegisterConsoleCommands()
        {
            ConsoleCommandAssigner.RegisterConsoleCommandsForInstance(this.GetType(), this);
        }
    }
}