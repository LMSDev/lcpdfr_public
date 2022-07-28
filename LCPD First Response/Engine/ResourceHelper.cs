namespace LCPD_First_Response.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Simplifies usage of resource files.
    /// </summary>
    internal class ResourceHelper
    {
        /// <summary>
        /// Returns the content of the given resource from the given namespace in a byte array.
        /// </summary>
        /// <param name="resourceName">The name of the resource.</param>
        /// <param name="nameSpace">The namespace where the resource is defined.</param>
        /// <returns>Byte array of the resource data.</returns>
        public static byte[] GetResourceBytes(string resourceName, Type nameSpace)
        {
            // Get stream and return empty array if invalid
            Stream stream = GetResourceStream(resourceName, nameSpace);
            if (stream == null || stream.Length == 0)
            {
                return new byte[0];
            }

            // Create bytearray with size of stream and read data into array
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, Convert.ToInt32(stream.Length));
            return data;
        }

        /// <summary>
        /// Returns all resource names from the given namespace.
        /// </summary>
        /// <param name="nameSpace">The namespace where the resource is defined.</param>
        /// <returns>The name of all resources.</returns>
        private static IEnumerable<string> GetResourceNames(Type nameSpace)
        {
            return nameSpace.Assembly.GetManifestResourceNames();
        }

        /// <summary>
        /// Returns a stream of the given resource from the given namespace.
        /// </summary>
        /// <param name="resourceName">The name of the resource.</param>
        /// <param name="nameSpace">The namespace where the resource is defined.</param>
        /// <returns>The resource stream.</returns>
        private static Stream GetResourceStream(string resourceName, Type nameSpace)
        {
            string resourceNameDot = "." + resourceName;
            IEnumerable<string> names = GetResourceNames(nameSpace);

            foreach (string name in names)
            {
                if (name.Equals(resourceName, StringComparison.InvariantCultureIgnoreCase) || name.EndsWith(resourceNameDot, StringComparison.InvariantCultureIgnoreCase))
                {
                    return nameSpace.Assembly.GetManifestResourceStream(name);
                }
            }

            return null;
        }
    }
}