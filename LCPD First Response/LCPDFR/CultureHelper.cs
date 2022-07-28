namespace LCPD_First_Response.LCPDFR
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Resources;
    using System.Threading;

    using LCPD_First_Response.Engine;

    /// <summary>
    /// Responsible for loading and managing different cultures and languages.
    /// </summary>
    internal static class CultureHelper
    {
        /// <summary>
        /// The current language code.
        /// </summary>
        private static string language = "en-US";

        /// <summary>
        /// The resource manager used
        /// </summary>
        private static ResourceManager rm;

        /// <summary>
        /// Initializes static members of the <see cref="CultureHelper"/> class.
        /// </summary>
        static CultureHelper()
        {
            rm = new ResourceManager("LCPD_First_Response.Resources.Translations", Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Gets the translation for <paramref name="name"/>. Returns <paramref name="name"/> if not found.
        /// </summary>
        /// <param name="name">The name of the string.</param>
        /// <returns>The translated string.</returns>
        public static string GetText(string name)
        {
            // Enforce language
            SetLanguage(language, true);

            try
            {
                return rm.GetString(name);
            }
            catch (Exception)
            {
                Log.Warning("GetText: Missing translation for: " + name + ". Language: " + language, "CultureHelper");
            }

            if (!Engine.Main.DEBUG_MODE)
            {
                try
                {
                    string tempLanguage = language;
                    SetLanguage("en-US", true);
                    string ret = rm.GetString(name);
                    SetLanguage(tempLanguage, true);
                    return ret;
                }
                catch (Exception)
                {
                    Log.Warning("GetText: Fallback translation missing: " + name + ". Language: " + language, "CultureHelper");
                }
            }

            return name;
        }

        /// <summary>
        /// Sets the language to <paramref name="languageCode"/>.
        /// </summary>
        /// <param name="languageCode">The language code.</param>
        public static void SetLanguage(string languageCode)
        {
            SetLanguage(languageCode, false);
        }

        /// <summary>
        /// Logs all missing translations.
        /// </summary>
        /// <param name="parameterCollection">
        /// The parameter Collection.
        /// </param>
        [Engine.Input.ConsoleCommand("LogMissingTranslations")]
        internal static void LogMissingTranslations(GTA.ParameterCollection parameterCollection)
        {
            Log.Debug("LogMissingTranslations: Checking current language for missing entries: " + language, "CultureHelper");

            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (Type type in assembly.GetTypes())
            {
                MethodInfo[] methodInfo = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (MethodInfo info in methodInfo)
                {
                    MethodBody body = info.GetMethodBody();

                    if (body == null)
                    {
                        continue;
                    }

                    var instructions = MethodBodyReader.GetInstructions((MethodBase)info);
                    Instruction instructionBefore = null;

                    foreach (Instruction instruction in instructions)
                    {
                        MethodInfo operand = instruction.Operand as MethodInfo;
                        if (operand != null)
                        {
                            Type declaringType = operand.DeclaringType;
                            ParameterInfo[] parameters = operand.GetParameters();
                            string methodName = declaringType.FullName + "." + operand.Name;

                            // Check if call is made to our function
                            if (methodName == "LCPD_First_Response.LCPDFR.CultureHelper.GetText")
                            {
                                // Check if instructionBefore contains our string
                                object op = instructionBefore.Operand;
                                if (instructionBefore.OpCode.OperandType == OperandType.InlineString)
                                {
                                    string parameter = op as string;
                                    try
                                    {
                                        // If call succeeds, no exception will be thrown and thus the translations exists. If an exception is thrown, it doesn't exist
                                        rm.GetString(parameter);
                                    }
                                    catch (Exception)
                                    {
                                        Log.Debug("Missing translation: " + parameter, "CultureHelper");
                                    }
                                }
                            }
                        }

                        // Save instruction before
                        instructionBefore = instruction;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the language to <paramref name="languageCode"/>.
        /// </summary>
        /// <param name="languageCode">
        /// The language code.
        /// </param>
        /// <param name="noLog">
        /// Whether or not the language change should be logged.
        /// </param>
        private static void SetLanguage(string languageCode, bool noLog)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(languageCode);
            }
            catch (CultureNotFoundException)
            {
                Log.Error("SetLanguage: Failed to set language: Invalid culture name", "CultureHelper");
                return;
            }

            language = languageCode;
            if (!noLog)
            {
                Log.Debug("SetLanguage: Language changed to " + language, "CultureHelper");
            }
        }
    }
}