namespace LCPD_First_Response.Engine
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.LCPDFR.Callouts;

    /// <summary>
    /// Logging class providing advanced logging functions.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// First message logged to the file.
        /// </summary>
        private const string InitializeLogMessage = "------------------------";
        
        /// <summary>
        /// Name of the log file.
        /// </summary>
        private const string LogFile = "LCPDFR.log";

        /// <summary>
        /// Whether or not console is beeping
        /// </summary>
        private static bool isBeeping;

        /// <summary>
        /// Lock object for thread safety.
        /// </summary>
        private static object lockObject;

        /// <summary>
        /// The event handler for logged text.
        /// </summary>
        /// <param name="text">The logged text.</param>
        public delegate void TextHasBeenLoggedEventHandler(string text);

        /// <summary>
        /// Fired when text has been logged.
        /// </summary>
        public static event TextHasBeenLoggedEventHandler TextHasBeenLogged;

        /// <summary>
        /// Initializes static members of the <see cref="Log"/> class.
        /// </summary>
        static Log()
        {
            // Initialize log
            lockObject = new object();

            LogText(InitializeLogMessage, false);
            Info("Started", "Log");
        }

        /// <summary>
        /// Gets or sets a value indicating whether there's a console beep if a new message has been logged or not.
        /// </summary>
        public static bool BeepOnNewMessages { get; set; }

        /// <summary>
        /// Logs the caller of this function in the following format: [File.cs: 0] Function().
        /// </summary>
        /// <param name="memberName">The name of the member.</param>
        /// <param name="sourceFilePath">The source file path at compiling time.</param>
        /// <param name="sourceLineNumber">The source file number at compiling time.</param>
        public static void Caller([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string text = memberName;
            Debug(text, sourceFilePath + ": " + sourceLineNumber);
        }

        /// <summary>
        /// Logs a debug message. Only works in debug mode.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Debug(string text, string sender)
        {
            Debug(text, sender, false);
        }

        /// <summary>
        /// Logs a debug message. Only works in debug mode.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Debug(string text, string sender, bool noNewLine)
        {
            LogText(text, "DEBUG", sender, noNewLine);
        }

        /// <summary>
        /// Logs a debug message. Only works in debug mode.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Debug(string text, object sender)
        {
            LogText(text, "DEBUG", sender, false);
        }

        /// <summary>
        /// Logs a debug message. Only works in debug mode.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Debug(string text, object sender, bool noNewLine)
        {
            LogText(text, "DEBUG", sender, noNewLine);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Error(string text, string sender)
        {
            Error(text, sender, false);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Error(string text, string sender, bool noNewLine)
        {
            LogText(text, "ERROR", sender, noNewLine);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Error(string text, object sender)
        {
            LogText(text, "ERROR", sender, false);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Error(string text, object sender, bool noNewLine)
        {
            LogText(text, "ERROR", sender, noNewLine);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Info(string text, string sender)
        {
            Info(text, sender, false);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Info(string text, string sender, bool noNewLine)
        {
            LogText(text, "INFO", sender, noNewLine);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Info(string text, object sender)
        {
            LogText(text, "INFO", sender, false);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Info(string text, object sender, bool noNewLine)
        {
            LogText(text, "INFO", sender, noNewLine);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Warning(string text, string sender)
        {
            Warning(text, sender, false);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Warning(string text, string sender, bool noNewLine)
        {
            LogText(text, "WARNING", sender, noNewLine);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        public static void Warning(string text, object sender)
        {
            LogText(text, "WARNING", sender, false);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        public static void Warning(string text, object sender, bool noNewLine)
        {
            LogText(text, "WARNING", sender, noNewLine);
        }

        /// <summary>
        /// Logs <paramref name="text"/> using <paramref name="level"/> and <paramref name="sender"/>.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="level">Logging level.</param>
        /// <param name="sender">The sender object, supports BaseComponent, BaseScript and Plugin.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        private static void LogText(string text, string level, object sender, bool noNewLine)
        {
            string type = string.Empty;

            if (sender is BaseComponent)
            {
                type = ((BaseComponent)sender).ComponentName;
            }

            if (sender is Plugin)
            {
                type = "Plugin." + ((Plugin)sender).PluginInfo.Name;
            }

            if (sender is BaseScript)
            {
                type = "Script." + ((BaseScript)sender).ScriptInfo.Name;
            }

            LogText(text, level, type, noNewLine);
        }

        /// <summary>
        /// Logs <paramref name="text"/> using <paramref name="level"/> and <paramref name="sender"/>.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="level">Logging level.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        private static void LogText(string text, string level, string sender, bool noNewLine)
        {
            // If level is debug, ensure DEBUG_MODE is true
            if (level == "DEBUG")
            {
                if (!Main.DEBUG_MODE)
                {
                    return;
                }
            }

            DateTime time = DateTime.Now;
            string timeString = time.ToLongTimeString();

            // Create logtext using date text and level
            string logText = "[{0} - {1}] [{2}] {3}";
            logText = string.Format(logText, level, timeString, sender, text);
            LogText(logText, noNewLine);
        }

        /// <summary>
        /// Logs <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text message.</param>
        /// <param name="noNewLine">Whether or not no new line should be logged.</param>
        private static void LogText(string text, bool noNewLine)
        {
            // Deactivate when not called from main thread
            if (Thread.CurrentThread.IsBackground)
            {
                // Queue up execution
                Timers.DelayedCaller.Call(delegate { LogText(text, noNewLine); }, 1);
                return;
            }

            // Threaded console beep to inform user about new output
            if (BeepOnNewMessages)
            {
                Thread thread = new Thread(ConsoleBeep);
                thread.Start();
            }

            // This mustn't be executed by more than one thread at the same time
            lock (lockObject)
            {
                FileStream fileStream = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.None);
                var sw = new StreamWriter(fileStream);
                if (noNewLine)
                {
                    sw.Write(text);
                }
                else
                {
                    sw.WriteLine(text);
                }

                sw.Flush();
                sw.Close();

                TextHasBeenLoggedEventHandler handler = TextHasBeenLogged;
                if (handler != null)
                {
                    handler(text);
                }
            }
        }

        /// <summary>
        /// Beep in console.
        /// </summary>
        private static void ConsoleBeep()
        {
            if (isBeeping)
            {
                return;
            }

            isBeeping = true;
            Console.Beep(2000, 500);
            isBeeping = false;
        }
    }
}
