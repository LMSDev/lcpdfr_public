namespace LCPD_First_Response.Engine
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides advanced exception handling functions, such as logging or attaching exception handlers.
    /// </summary>
    internal static class ExceptionHandler
    {
        /// <summary>
        /// Event handler
        /// </summary>
        private static ExceptionCaughtEventHandler eventHandler;

        /// <summary>
        /// Delegate used for the general exception handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="exception">The exception.</param>
        public delegate void ExceptionCaughtEventHandler(object sender, Exception exception);

        /// <summary>
        /// Called when an exception occured in the engine. Logs the exception and passes it to the exception handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="exception">The exception.</param>
        public static void ExceptionCaught(object sender, Exception exception)
        {
            LogCriticalException(exception, sender);
            if (eventHandler == null)
            {
                Log.Warning("ExceptionCaught: No exception handler attached.", "ExceptionHandler");
                return;
            }

            eventHandler.Invoke(sender, exception);
        }

        /// <summary>
        /// Logs <paramref name="exception"/>
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogCriticalException(Exception exception)
        {
            Log.Error(exception.ToString(), string.Empty);
        }

        /// <summary>
        /// Logs <paramref name="exception"/>. If <paramref name="sender"/> is of <see cref="BaseComponent"/> type, debug output and component name is logged as well.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="sender">The sender.</param>
        public static void LogCriticalException(Exception exception, object sender)
        {
            if (sender is BaseComponent)
            {
                ((BaseComponent)sender).PrintDebugOutput();
            }

            // Exception text can be obfuscated, so we try to fix a fix formatting issues here.
            string text = exception.ToString();
            Regex rgx = new Regex(@"[^a-zA-Z0-9-_().:\\ \t\s]");
            text = rgx.Replace(text, string.Empty);
            Log.Error(text, sender);
        }

        /// <summary>
        /// Reports the <paramref name="text"/> to the user in a help box.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public static void ReportException(string text)
        {
            GUI.HelpBox.Print(text);
        }

        /// <summary>
        /// Sets the general exception handler to the given delegate.
        /// </summary>
        /// <param name="exceptionCaughtEventHandler">The delegate.</param>
        public static void SetGeneralExceptionHandler(ExceptionCaughtEventHandler exceptionCaughtEventHandler)
        {
            eventHandler = exceptionCaughtEventHandler;
        }

        internal static string GetStackTraceWithILOffset(Exception ex)
        {
            var stackTrace = new StackTrace(ex, true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            var sb = new StringBuilder();

            var stackFramesLength = new int[stackFrames.Length];
            for (var i = 0; i < stackFramesLength.Length; i++)
            {
                var stacfkFrame = stackFrames[i];
                var method = stacfkFrame.GetMethod();
                var parameters = GetMethodParameters(method);
                var ilOffset = GetILOffset(stacfkFrame.GetILOffset());
                sb.Append(string.Format(" {0}.{1}({2}) {3} \r\n",
                method.ReflectedType.FullName,
                method.Name,
                parameters,
                ilOffset));
            }
            return sb.ToString();
        }

        private static string GetILOffset(int ilOffset)
        {
            // Format to hexadecimal to have a Reflector like IL instruction OffSet
            var ilOffsetHexString = ilOffset.ToString("X").ToLower();

            // Get a Reflector-like ILOffset like "L_018e"
            var sb = new StringBuilder("L_");
            if (ilOffsetHexString.Length < 4)
            {
                sb.Append(new string('0', 4 - ilOffsetHexString.Length));
            }

            sb.Append(ilOffsetHexString);
            return sb.ToString();
        }

        private static string GetMethodParameters(MethodBase method)
        {
            var parameters = method.GetParameters();
            var length = parameters.Length;
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                var parameter = parameters[i];
                sb.Append(parameter.ParameterType.Name);
                sb.Append(" ");
                sb.Append(parameter.Name);
                if (i < length - 1)
                {
                    sb.Append(", ");
                }
            }

            return sb.ToString();
        }
    }
}