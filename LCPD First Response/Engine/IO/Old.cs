namespace LCPD_First_Response.Engine.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using GTA;

    using LCPD_First_Response.LCPDFR;

    /// <summary>
    /// Contains old IO code from LCPDFR 0.95 which has not yet ported.
    /// </summary>
    internal class Legacy
    {
        public class FileParser
        {
            public static DataFile ParseDataFile(string resource)
            {
                return new DataFile(resource);
            }

            /// <summary>
            /// Parses the given resource string to a string array
            /// Supports comments (#)
            /// </summary>
            /// <param name="resource"></param>
            /// <returns></returns>
            public static string[] ParseString(string resource)
            {
                // To store the return array
                List<string> parsedStrings = new List<string>();

                // Split by newline
                Regex regex = new Regex(Environment.NewLine);
                string[] tempArray = regex.Split(resource);

                foreach (string s in tempArray)
                {
                    // If line only contains whitespaces
                    if (s.Trim().Length == 0) continue;

                    // Lines that start with a # are comments and are ignored
                    if (s[0] == '#') continue;

                    string line = s;
                    // If there is a comment in the line cut string there
                    if (line.Contains('#'))
                    {
                        line = line.Substring(0, line.IndexOf('#'));
                    }
                    parsedStrings.Add(line);
                }
                return parsedStrings.ToArray();
            }

            /// <summary>
            /// Parses a string and returns a vector3
            /// String must be of format: X,Y,Z
            /// </summary>
            /// <param name="vector"></param>
            /// <returns></returns>
            public static GTA.Vector3 ParseVector3(string vector)
            {
                // Kill all whitespaces
                vector = vector.Replace(" ", string.Empty);

                // Split by ,
                Regex regex = new Regex(",");
                string[] values = regex.Split(vector);

                float f = -1;
                float.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out f);

                float f2 = -1;
                float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out f2);

                float f3 = -1;
                float.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out f3);

                // Return vector
                return new GTA.Vector3(f, f2, f3);
            }

            /// <summary>
            /// Parses a string and returns a vector4. Heading is stored in Vector4.W
            /// String must be of format: X,Y,Z,Heading
            /// </summary>
            /// <param name="vector"></param>
            /// <returns></returns>
            public static SpawnPoint ParseVector3WithHeading(string vector)
            {
                // Kill all whitespaces and ;
                vector = vector.Replace(" ", string.Empty);
                vector = vector.Replace(";", string.Empty);

                // Split by ,
                Regex regex = new Regex(",");
                string[] values = regex.Split(vector);

                float f = -1;
                float.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out f);

                float f2 = -1;
                float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out f2);

                float f3 = -1;
                float.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out f3);

                float f4 = -1;
                float.TryParse(values[3], NumberStyles.Any, CultureInfo.InvariantCulture, out f4);

                return new SpawnPoint(f4, new Vector3(f, f2, f3));
                //// Return vector
                //return new Vector4(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]), Convert.ToSingle(values[3]));
            }
        }

        // Generic classes
        public class DataFile
        {
            public const string DataSetAttributeSeparator = ";";
            public const string DataSetAttributeValue = "=";
            /// <summary>
            /// Ends a dataset line
            /// </summary>
            public const string DataSetEnd = ">";
            /// <summary>
            /// Ends a dataset
            /// </summary>
            public const string DataSetFinish = "</";
            /// <summary>
            /// Separates a tag start from the first attribute
            /// </summary>
            public const string DataSetSeparator = " ";
            /// <summary>
            /// Starts a dataset
            /// </summary>
            public const string DataSetStart = "<";
            /// <summary>
            /// Ends a tag
            /// </summary>
            public const string DataSetTagFinish = "/>";

            public DataSet[] DataSets { get; private set; }

            public DataFile(string resource)
            {
                DataSets = ParseDataFile(FileParser.ParseString(resource));
            }

            private static DataSet[] ParseDataFile(string[] content)
            {
                List<DataSet> dataSets = new List<DataSet>();

                List<string> dataSetContent = new List<string>();
                string dataSetName = string.Empty;
                bool inDataSet = false;

                foreach (string s in content)
                {
                    if (inDataSet)
                    {
                        if (s.StartsWith(DataSetFinish + dataSetName + DataSetEnd))
                        {
                            // Data set has finished
                            dataSets.Add(new DataSet(dataSetName, dataSetContent.ToArray()));
                            inDataSet = false;
                            // Erase old data
                            dataSetContent.Clear();
                        }
                        else
                        {
                            // Not yet ended, add content
                            dataSetContent.Add(s);
                        }
                    }
                    else
                    {
                        // Check if a new data set starts
                        if (s.StartsWith(DataSetStart) && s.EndsWith(DataSetEnd))
                        {
                            // Cut the starting "<" and the ending ">"
                            dataSetName = s.Substring(1, s.Length - 2);
                            // So we know we have a dataset open
                            inDataSet = true;
                        }
                    }
                }
                return dataSets.ToArray();
            }
        }

        public class DataSet
        {
            public string Name { get; private set; }
            public Tag[] Tags { get; private set; }

            public DataSet(string name, string[] content)
            {
                Name = name;

                Tags = ParseDataSet(content);
            }

            private static Tag[] ParseDataSet(string[] content)
            {
                List<Tag> tags = new List<Tag>();

                foreach (string s in content)
                {
                    // Check for tag start
                    if (s.StartsWith(DataFile.DataSetStart))
                    {
                        // Tag name will be from start + 1 to first whitespace
                        string name = s.Substring(1, s.IndexOf(DataFile.DataSetSeparator) - 1);

                        // Now cut out both, the start and ending tag and create a new tag instance using the line content
                        string line = s.Substring(s.IndexOf(DataFile.DataSetSeparator));
                        line = line.Substring(0, line.IndexOf(DataFile.DataSetTagFinish) - 1);

                        tags.Add(new Tag(name, line));
                    }
                }
                return tags.ToArray();
            }
        }

        public class Tag
        {
            public Attribute[] Attributes { get; private set; }
            public string Name { get; private set; }

            public Tag(string name, string lineContent)
            {
                Name = name;

                Attributes = ParseTags(lineContent);
            }

            /// <summary>
            /// Returns the attribute with the given name. Returns null if name not found
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public Attribute GetAttributeByName(string name)
            {
                foreach (Attribute attribute in Attributes)
                {
                    if (attribute.Name == name) return attribute;
                }
                return null;
            }

            public T GetAttributesValueByName<T>(string name)
            {
                Attribute attribute = GetAttributeByName(name);
                if (attribute != null)
                {
                    return (T)Convert.ChangeType(attribute.Value, typeof(T));
                }
                return default(T);
            }

            private static Attribute[] ParseTags(string content)
            {
                // Split by ;
                Regex regex = new Regex(DataFile.DataSetAttributeSeparator);

                List<Attribute> attributes = new List<Attribute>();
                foreach (string s in regex.Split(content))
                {
                    string name = s.Substring(1, s.IndexOf(DataFile.DataSetAttributeValue) - 1);
                    string value = s.Substring(s.IndexOf(DataFile.DataSetAttributeValue) + 1);

                    attributes.Add(new Attribute(name, value));
                }
                return attributes.ToArray();
            }
        }

        public class Attribute
        {
            public string Name { get; private set; }
            public string Value { get; private set; }

            public Attribute(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}
