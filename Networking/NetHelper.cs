namespace LCPDFR.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Net helper class for common functions.
    /// </summary>
    public class NetHelper
    {
        /// <summary>
        /// Gets the version of the current assembly and its build date (if specified).
        /// </summary>
        /// <returns>The version.</returns>
        public static string GetVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            string s = v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision;
            var buildDateTime = new DateTime(2000, 1, 1).Add(new TimeSpan((TimeSpan.TicksPerDay * v.Build) + (TimeSpan.TicksPerSecond * 2 * v.Revision)));
            s += " (" + buildDateTime.ToShortDateString() + " " + buildDateTime.ToLongTimeString() + ")";

            return s;
        }

        /// <summary>
        /// Gets the local Windows user name.
        /// </summary>
        /// <returns>The user name.</returns>
        public static string GetUserName()
        {
            return string.Format("{0} ({1})", Environment.UserName, Environment.MachineName);
        }

        //public static void AddUserHandlerFromAssembly(Assembly assembly, BaseNetPeer netPeer)
        //{
        //    List<MethodInfo> methods = new List<MethodInfo>();

        //    foreach (Type t in assembly.GetTypes())
        //    {
        //        foreach (var method in t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
        //        {
        //            methods.AddRange(from attributeData in method.CustomAttributes where attributeData.AttributeType == typeof(ServerMessageReceivedAttribute) select method);
        //        }
        //    }
        //}
    }
}