namespace LCPD_First_Response.Engine.IO
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A .ini file section containing the values.
    /// </summary>
    internal class IniSection
    {
        /// <summary>
        /// Regex to recognize a value of format Name=Value.
        /// </summary>
        private const string ValueRegex = @"(.*?)(=)(.*?)";

        /// <summary>
        /// All values in this section.
        /// </summary>
        private IniValue[] values;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniSection"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="content">
        /// The content.
        /// </param>
        public IniSection(string name, string[] content)
        {
            this.Name = name;

            this.values = this.ParseIniSection(content);
        }

        /// <summary>
        /// Gets the name of this section.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets all values in this section.
        /// </summary>
        public IniValue[] Values
        {
            get
            {
                return this.values;
            }
        }

        /// <summary>
        /// Gets the value <paramref name="name"/>. Returns null if not found.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <returns>Null if not found.</returns>
        public string GetValue(string name)
        {
            foreach (IniValue iniValue in this.values)
            {
                if (iniValue.Name == name)
                {
                    return iniValue.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the lines into values.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <returns>Parsed values.</returns>
        private IniValue[] ParseIniSection(string[] lines)
        {
            Regex valueRegex = new Regex(ValueRegex);
            List<IniValue> tempValues = new List<IniValue>();

            foreach (string line in lines)
            {
                // Check if line is valid
                if (valueRegex.Match(line).Success)
                {
                    // Get name of the value and the value itself
                    string[] split = valueRegex.Split(line);
                    string name = split[1].Trim();
                    string value = split[4].Trim();

                    tempValues.Add(new IniValue(name, value));
                }
            }

            return tempValues.ToArray();
        }
    }
}
