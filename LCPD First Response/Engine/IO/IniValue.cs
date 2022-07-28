namespace LCPD_First_Response.Engine.IO
{
    /// <summary>
    /// Ini value containing the name of the value and the value itself
    /// </summary>
    internal class IniValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IniValue"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public IniValue(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets the name of the value
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the value
        /// </summary>
        public string Value { get; private set; }
    }
}
