namespace LCPD_First_Response.Engine.IO
{
    using System;
    using System.Globalization;
    using System.IO;

    using LCPD_First_Response.Engine.Input;

    using SlimDX.XInput;

    /// <summary>
    /// Provides functions to read/write from/to an ini file.
    /// </summary>
    internal class SettingsFile
    {
        /// <summary>
        /// The underlying ini content containg all sections and values.
        /// </summary>
        private IniFile iniFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsFile"/> class.
        /// </summary>
        /// <param name="path">
        /// The file path.
        /// </param>
        public SettingsFile(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// Gets the settings file path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Returns whether the file exists or not.
        /// </summary>
        /// <returns>True on existance, otherwise false.</returns>
        public bool Exists()
        {
            return File.Exists(this.Path);
        }

        /// <summary>
        /// Gets the value in <paramref name="sectionName"/> called <paramref name="valueName"/> and returns it as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to return the value in.</typeparam>
        /// <param name="sectionName">The section the value is in.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <param name="defaultValue">Optional: Default value</param>
        /// <returns>The value with the given name.</returns>
        public T GetValue<T>(string sectionName, string valueName, T defaultValue = default(T))
        {
            if (!this.Exists())
            {
                return defaultValue;
            }

            // Get the section
            IniSection iniSection = this.iniFile.GetSection(sectionName);
            if (iniSection == null)
            {
                Log.Debug("GetValue: Section not found: " + sectionName, "SettingsFile");
                return defaultValue;
            }

            // Get the value
            string iniValue = iniSection.GetValue(valueName);
            if (iniValue == null)
            {
                Log.Debug("GetValue: Value not found: " + valueName + " (" + sectionName + ")", "SettingsFile");
                return defaultValue;
            }

            try
            {
                // Convert string value to T
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)iniValue;
                }

                if (typeof(T) == typeof(int))
                {
                    return (T)(object)Convert.ToInt32(iniValue);
                }

                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)Convert.ToBoolean(iniValue);
                }

                if (typeof(T) == typeof(float))
                {
                    float fVal = -1;
                    if (float.TryParse(iniValue, NumberStyles.Any, CultureInfo.InvariantCulture, out fVal))
                    {
                        return (T)(object)fVal;
                    }
                }

                if (typeof(T) == typeof(double))
                {
                    return (T)(object)Convert.ToDouble(iniValue);
                }

                if (typeof(T) == typeof(System.Windows.Forms.Keys))
                {
                    return (T)Enum.Parse(typeof(System.Windows.Forms.Keys), iniValue);
                }

                if (typeof(T) == typeof(EGameKey))
                {
                    return (T)Enum.Parse(typeof(EGameKey), iniValue);
                }

                if (typeof(T) == typeof(GamepadButtonFlags))
                {
                    return (T)Enum.Parse(typeof(GamepadButtonFlags), iniValue);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("GetValue: Failed to convert value: " + iniValue + " in "  + valueName + " (" + sectionName + ")", "SettingsFile");
            }

            Log.Warning("GetValue: Type not supported: " + typeof(T).Name, "SettingsFile");
            return defaultValue;
        }

        /// <summary>
        /// Writes <paramref name="value"/> in <paramref name="sectionName"/>. Adds if non-existent.
        /// </summary>
        /// <param name="sectionName">The section the value is in.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if value has been written successfully.</returns>
        public bool WriteValue(string sectionName, string valueName, string value)
        {
            if (!this.Exists())
            {
                return false;
            }

            // Get the section
            IniSection iniSection = this.iniFile.GetSection(sectionName);
            if (iniSection == null)
            {
                // Add section and value to file
                File.AppendAllText(this.Path, Environment.NewLine + "[" + sectionName + "]");
                File.AppendAllText(this.Path, Environment.NewLine + valueName + "=" + value);
            }
            else
            {
                // Section exists, check if value exists too
                if (iniSection.GetValue(valueName) == null)
                {
                    // Add value below last section entry
                    IniValue lastValue = iniSection.Values[iniSection.Values.Length - 1];

                    // Read content
                    StreamReader streamReader = new StreamReader(this.Path);
                    string content = streamReader.ReadToEnd();
                    content = content.Replace(" ", string.Empty);
                    streamReader.Close();

                    // Insert
                    int index = content.IndexOf("[" + iniSection.Name + "]");
                    int lastValueIndex = content.IndexOf(lastValue.Name, index);
                    int lineBreak = content.IndexOf(Environment.NewLine, lastValueIndex);
                    if (lineBreak == -1)
                    {
                        lineBreak = content.IndexOf("=" + lastValue.Value, lastValueIndex) + lastValue.Value.Length + 1;
                    }

                    content = content.Insert(lineBreak, Environment.NewLine + valueName + "=" + value);

                    StreamWriter streamWriter = new StreamWriter(this.Path);
                    streamWriter.Write(content);
                    streamWriter.Close();
                }
                else
                {
                    // Read content
                    StreamReader streamReader = new StreamReader(this.Path);
                    string content = streamReader.ReadToEnd();
                    streamReader.Close();

                    // Replace value
                    content = content.Replace(valueName + "=" + iniSection.GetValue(valueName), valueName + "=" + value);
                    StreamWriter streamWriter = new StreamWriter(this.Path);
                    streamWriter.Write(content);
                    streamWriter.Close();
                }
            }

            this.Read();

            return true;
        }

        /// <summary>
        /// Reads the settings file into memory.
        /// </summary>
        /// <returns>True on success. False otherwise.</returns>
        public bool Read()
        {
            if (!this.Exists())
            {
                throw new FileNotFoundException("SettingsFile: Read: Attempt to read non existant file: " + this.Path);
            }

            StreamReader streamReader = new StreamReader(this.Path);
            string content = streamReader.ReadToEnd();
            streamReader.Close();

            this.iniFile = new IniFile(content);
            return true;
        }
    }
}