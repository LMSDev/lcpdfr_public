namespace LCPD_First_Response.Engine.Input
{
    using System;
    using System.Reflection;

    using GTA;

    /// <summary>
    /// Iterates through all functions in this assembly and looks for <see cref="ConsoleCommandAttribute"/>.
    /// </summary>
    internal static class ConsoleCommandAssigner
    {
        /// <summary>
        ///  Initializes static members of the <see cref="ConsoleCommandAssigner"/> class.
        /// </summary>
        public static void Initialize()
        {
            // Get current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Iterate through all types and looks for ConsoleCommandAttribute
            foreach (Type type in assembly.GetTypes())
            {
                RegisterConsoleCommandsOfType(type, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null);
            }
        }

        /// <summary>
        /// Registers all console commands for <paramref name="t"/> and binds them to <paramref name="instance"/>. Does not support static functions.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <param name="instance">The object instance.</param>
        public static void RegisterConsoleCommandsForInstance(Type t, object instance)
        {
            RegisterConsoleCommandsOfType(t, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, instance);
        }

        /// <summary>
        /// Registers all console commands of <paramref name="t"/> using <paramref name="bindingFlags"/> and binds them to <paramref name="instance"/>, if available.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <param name="bindingFlags">The binding flags.</param>
        /// <param name="instance">The instance. Null for static functions.</param>
        private static void RegisterConsoleCommandsOfType(Type t, BindingFlags bindingFlags, object instance)
        {
            MethodInfo[] methodInfo = t.GetMethods(bindingFlags);
            foreach (MethodInfo info in methodInfo)
            {
                foreach (ConsoleCommandAttribute consoleCommandAttribute in info.GetCustomAttributes(typeof(ConsoleCommandAttribute), true))
                {
                    // If command is dev only and debug mode is not active
                    if (consoleCommandAttribute.DevOnly && !Main.DEBUG_MODE)
                    {
                        continue;
                    }

                    // Get delegate
                    Delegate @delegate = null;
                    if (bindingFlags.HasFlag(BindingFlags.Static))
                    {
                        @delegate = Delegate.CreateDelegate(typeof(ConsoleCommandDelegate), null, info);
                    }
                    else
                    {
                        @delegate = Delegate.CreateDelegate(typeof(ConsoleCommandDelegate), instance, info.Name);
                    }
                    
                    ScriptHelper.BindConsoleCommandS(consoleCommandAttribute.Command, consoleCommandAttribute.Description, (ConsoleCommandDelegate)@delegate);
                }
            }
        }
    }
}