namespace LCPD_First_Response.Engine.IO
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Parses the content of an ini file into sections and values. Supports comments (#).
    /// </summary>
    internal class IniFile
    {
        /// <summary>
        /// Regex to recognize a section of format [Name].
        /// </summary>
        private const string SectionRegex = @"(\[)(.*?)(\])";

        /// <summary>
        /// All sections found in the content.
        /// </summary>
        private IniSection[] sections;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniFile"/> class.
        /// </summary>
        /// <param name="content">
        /// The content.
        /// </param>
        public IniFile(string content)
        {
            // Parse the content with comments stripped out
            this.sections = this.ParseIniFile(FileParser.ParseString(content));
        }

        /// <summary>
        /// Gets the section <paramref name="name"/>. Returns null if not found.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        /// <returns>Null if not found.</returns>
        public IniSection GetSection(string name)
        {
            foreach (IniSection iniSection in this.sections)
            {
                if (iniSection.Name == name)
                {
                    return iniSection;
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the lines into sections.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <returns>Parsed sections.</returns>
        private IniSection[] ParseIniFile(string[] lines)
        {
            bool inSection = false;
            List<string> sectionContent = new List<string>();
            string sectionName = string.Empty;
            Regex sectionRegex = new Regex(SectionRegex);
            List<IniSection> tempSections = new List<IniSection>();

            foreach (string line in lines)
            {
                // If already in a section 
                if (inSection)
                {
                    // Check if line is a section
                    if (sectionRegex.Match(line).Success)
                    {
                        // New section, so finish old one
                        tempSections.Add(new IniSection(sectionName, sectionContent.ToArray()));

                        // Clear content data
                        sectionContent.Clear();
                        
                        // Get new name
                        sectionName = sectionRegex.Split(line)[2];
                    }
                    else
                    {
                        // Not a new section, so store this line
                        sectionContent.Add(line);
                    }
                }
                else
                {
                    // Check if line is a section
                    if (sectionRegex.Match(line).Success)
                    {
                        // Get section name
                        sectionName = sectionRegex.Split(line)[2];
                        inSection = true;
                    }
                }
            }

            // If in a section, finish
            if (inSection)
            {
                // Finish section
                tempSections.Add(new IniSection(sectionName, sectionContent.ToArray()));
            }

            return tempSections.ToArray();
        }
    }
}
