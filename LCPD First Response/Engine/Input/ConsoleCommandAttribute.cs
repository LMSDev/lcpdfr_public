namespace LCPD_First_Response.Engine.Input
{
    using System;

    /// <summary>
    /// Attribute to specify the console command to call a certain function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleCommandAttribute"/> class.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        /// <param name="devOnly">
        /// Whether or not the command is developer build only. Default set to true.
        /// </param>
        public ConsoleCommandAttribute(string command, bool devOnly = true)
        {
            this.Command = command;
            this.Description = string.Empty;
            this.DevOnly = devOnly;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleCommandAttribute"/> class.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        /// <param name="description">
        /// The description.
        /// </param>
        /// <param name="devOnly">
        /// Whether or not the command is developer build only. Default set to true.
        /// </param>
        public ConsoleCommandAttribute(string command, string description, bool devOnly = true)
        {
            this.Command = command;
            this.Description = description;
            this.DevOnly = devOnly;
        }

        /// <summary>
        /// Gets the command.
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the command is only available in developer build
        /// </summary>
        public bool DevOnly { get; private set; }
    }
}
