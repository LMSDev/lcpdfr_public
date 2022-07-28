namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;

    /// <summary>
    /// Contains information about a plugin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginInfoAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInfoAttribute"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="isEntryPoint">
        /// If plugin is entry point.
        /// </param>
        /// <param name="autoCreate">
        /// Whether an instace of the plugin should be automatically created. This is not recommended, because there is no real reference to the instance for the code.
        /// </param>
        public PluginInfoAttribute(string name, bool isEntryPoint, bool autoCreate)
        {
            this.Name = name;
            this.IsEntryPoint = isEntryPoint;
            this.AutoCreate = autoCreate;
        }

        /// <summary>
        /// Gets a value indicating whether the plugin should be created by the plugin manager.
        /// </summary>
        public bool AutoCreate { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the plugin is marked as entry point and thus should be started first.
        /// </summary>
        public bool IsEntryPoint { get; private set; }

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name { get; private set; }
    }
}
