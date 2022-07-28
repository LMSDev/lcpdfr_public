namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;

    /// <summary>
    /// Contains information about a script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ScriptInfoAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptInfoAttribute"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="processInScriptManager">
        /// Whether or not script should be processed in script manager.
        /// </param>
        public ScriptInfoAttribute(string name, bool processInScriptManager)
        {
            this.Name = name;
            this.ProcessInScriptManager = processInScriptManager;
        }

        /// <summary>
        /// Gets the name of the script.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the script should be processed in the script manager, that is that Process is called from the ScriptManager.
        /// If you want to have influence on the calling order, set this to false and call Process yourself.
        /// </summary>
        public bool ProcessInScriptManager { get; private set; }
    }
}
