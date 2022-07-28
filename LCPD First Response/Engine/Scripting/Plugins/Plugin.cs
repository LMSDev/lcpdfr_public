namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using LCPD_First_Response.Engine.Input;

    /// <summary>
    /// Plugin class for objects that should run all the time and shouldn't end.
    /// </summary>
    public abstract class Plugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        protected Plugin()
        {
            this.PluginInfo = AssemblyHelper.GetAttribute<PluginInfoAttribute>(this.GetType());
        }

        /// <summary>
        /// Gets a value indicating whether an exception occured in this plugin.
        /// </summary>
        public bool ExceptionOccured { get; internal set; }

        /// <summary>
        /// Gets PluginInfo.
        /// </summary>
        internal PluginInfoAttribute PluginInfo { get; private set; }

        /// <summary>
        /// Called when the plugin has been created successfully.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Called every tick to process all plugin logic.
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Called when the plugin is being disposed, e.g. because an unhandled exception occured in Process. Free all resources here!
        /// </summary>
        public abstract void Finally();

        /// <summary>
        /// Registers all console commands.
        /// </summary>
        protected void RegisterConsoleCommands()
        {
            ConsoleCommandAssigner.RegisterConsoleCommandsForInstance(this.GetType(), this);
        }
    }
}