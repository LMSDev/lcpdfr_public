namespace LCPD_First_Response.Engine.IO
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides common functions for parsing files.
    /// </summary>
    internal static class FileParser
    {
        /// <summary>
        /// Parses the given string to a string array.
        /// Supports comments (#).
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>The string data as array.</returns>
        public static string[] ParseString(string data)
        {
            // To store the return array
            List<string> parsedStrings = new List<string>();

            // Split by newline
            Regex regex = new Regex(Environment.NewLine);
            string[] tempArray = regex.Split(data);

            foreach (string s in tempArray)
            {
                // If line only contains whitespaces
                if (s.Trim().Length == 0)
                {
                    continue;
                }

                // Lines that start with a # are comments and are ignored
                if (s[0] == '#')
                {
                    continue;
                }

                // Same for //
                if (s[0] == '/' && s[1] == '/')
                {
                    continue;
                }

                string line = s;

                // If there is a comment in the line, cut string there
                if (line.Contains("#"))
                {
                    line = line.Substring(0, line.IndexOf('#'));
                }

                // If there is a comment in the line, cut string there
                if (line.Contains("//"))
                {
                    line = line.Substring(0, line.IndexOf("//", StringComparison.Ordinal) - 1);
                }

                parsedStrings.Add(line);
            }

            return parsedStrings.ToArray();
        }

        /// <summary>
        /// Parses the given string data separated by , to a string array.
        /// Supports comments (#).
        /// </summary>
        /// <param name="data">The data,</param>
        /// <returns>Parsed data.</returns>
        public static string[] ParseStringData(string data)
        {
            // To store the return array
            List<string> parsedStrings = new List<string>();

            // Split by ,
            Regex regex = new Regex(",");
            string[] tempArray = regex.Split(data);

            foreach (string s in tempArray)
            {
                // If line only contains whitespaces
                if (s.Trim().Length == 0)
                {
                    continue;
                }

                // Lines that start with a # are comments and are ignored
                if (s[0] == '#')
                {
                    continue;
                }

                string line = s;

                // If there is a comment in the line, cut string there
                if (line.Contains("#"))
                {
                    line = line.Substring(0, line.IndexOf('#'));
                }

                parsedStrings.Add(line);
            }

            return parsedStrings.ToArray();
        }
    }
}
