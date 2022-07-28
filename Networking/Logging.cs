namespace LCPDFR.Networking
{
    using System;

    /// <summary>
    /// The logging class.
    /// </summary>
    public class Logging
    {
        /// <summary>
        /// The delegate for received logging events.
        /// </summary>
        /// <param name="text">The text.</param>
        public delegate void MessageReceivedEventHandler(string text);

        /// <summary>
        /// The event fired whenever a message has been received from the network engine.
        /// </summary>
        public static event MessageReceivedEventHandler MessageReceived;

        internal static void Debug(string text, string origin)
        {
            InvokeMessageReceived(BuildLoggingMessage(text, "DEBUG", origin));
        }

        internal static void Debug(string text, ILoggable origin)
        {
            InvokeMessageReceived(BuildLoggingMessage(text, "DEBUG", origin.ComponentName));
        }

        internal static void Info(string text, ILoggable origin)
        {
            InvokeMessageReceived(BuildLoggingMessage(text, "INFO", origin.ComponentName));
        }

        internal static void Error(string text, ILoggable origin)
        {
            InvokeMessageReceived(BuildLoggingMessage(text, "ERROR", origin.ComponentName));
        }

        internal static void Warning(string text, ILoggable origin)
        {
            InvokeMessageReceived(BuildLoggingMessage(text, "WARNING", origin.ComponentName));
        }

        private static string BuildLoggingMessage(string text, string level, string origin)
        {
            DateTime time = DateTime.Now;
            string timeString = time.ToLongTimeString();

            // Create logtext using date text and level
            const string LogText = "[{0} - {1}] [{2}] {3}";
            return string.Format(LogText, level, timeString, origin, text);
        }

        private static void InvokeMessageReceived(string text)
        {
            MessageReceivedEventHandler handler = MessageReceived;
            if (handler != null)
            {
                handler(text);
            }
        }
    }
}