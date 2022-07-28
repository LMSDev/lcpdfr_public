namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides helper functions for searching types and attributes.
    /// </summary>
    internal static class AssemblyHelper
    {
        /// <summary>
        /// Finds and returns all assemblies in <paramref name="folder"/>.
        /// </summary>
        /// <param name="folder">The folder</param>
        /// <returns>All found assemblies.</returns>
        public static Assembly[] GetAllAssembliesInFolder(string folder)
        {
            List<Assembly> assemblies = new List<Assembly>();
            DirectoryInfo directoryInfo = new DirectoryInfo(folder);

            if (!Directory.Exists(folder))
            {
                return null;
            }

            // Get all files
            foreach (FileInfo fileInfo in directoryInfo.GetFiles("*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string symbolsPath = Path.ChangeExtension(fileInfo.FullName, ".pdb");
                    byte[] assemblyBytes = LoadFile(fileInfo.FullName);
                    byte[] symbolsBytes = LoadFile(symbolsPath);

                    if (assemblyBytes != null && symbolsBytes != null)
                    {
                        Assembly assembly = Assembly.Load(assemblyBytes, symbolsBytes);
                        assemblies.Add(assembly);
                    }
                    else if (assemblyBytes != null)
                    {
                        Assembly assembly = Assembly.Load(assemblyBytes);
                        assemblies.Add(assembly);
                    }
                }
                catch (FileNotFoundException)
                {
                }
                catch (BadImageFormatException)
                {
                }
                catch (FileLoadException)
                {
                }
            }

            return assemblies.ToArray();
        }

        /// <summary>
        /// Returns all types inheriting from <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The types.
        /// </returns>
        public static Type[] GetTypesInheritingFromType(Type type)
        {
            // Get current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            return GetTypesInheritingFromTypeInAssembly(type, assembly);
        }

        /// <summary>
        /// Returns all types inheriting from <paramref name="type"/> in <paramref name="assembly"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The types.</returns>
        public static Type[] GetTypesInheritingFromTypeInAssembly(Type type, Assembly assembly)
        {
            Type[] foundTypes = null;

            try
            {

                foundTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(type)).ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // now look at ex.LoaderExceptions - this is an Exception[], so:
                foreach (Exception inner in ex.LoaderExceptions)
                {
                    // write details of "inner", in particular inner.Message
                    Log.Error("GetTypesInheritingFromTypeInAssembly: Inner exception: " + inner.Message, "AssemblyHelper");
                }
            }

            return foundTypes;
        }

        /// <summary>
        /// Returns all types inheriting from <paramref name="name"/> in <paramref name="assembly"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The types.</returns>
        public static Type[] GetTypesInheritingFromTypeInAssembly(string name, Assembly assembly)
        {
            Type[] foundTypes = null;

            try
            {
                foundTypes = assembly.GetTypes().Where(t => t.BaseType != null && t.BaseType.FullName == name).ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // now look at ex.LoaderExceptions - this is an Exception[], so:
                foreach (Exception inner in ex.LoaderExceptions)
                {
                    // write details of "inner", in particular inner.Message
                    Log.Error("GetTypesInheritingFromTypeInAssemblyS: Inner exception: " + inner.Message, "AssemblyHelper");
                }
            }

            return foundTypes;
        }

        /// <summary>
        /// Returns the attribute <typeparamref name="T"/> of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <typeparam name="T">
        /// Attribute type.
        /// </typeparam>
        /// <returns>
        /// The attribute.
        /// </returns>
        /// <exception cref="Exception">
        /// If attribute not found, throws exception
        /// </exception>
        public static T GetAttribute<T>(Type type) where T : Attribute
        {
            foreach (Attribute attribute in type.GetCustomAttributes(typeof(T), true))
            {
                return (T)attribute;
            }

            throw new ArgumentException("No attribute of type " + type.Name + " found.");
        }

        /// <summary>
        /// Returns the attribute <typeparamref name="T"/> of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="attributeName">
        /// The attribute name
        /// </param>
        /// <typeparam name="T">
        /// Attribute type.
        /// </typeparam>
        /// <returns>
        /// The attribute.
        /// </returns>
        /// <exception cref="Exception">
        /// If attribute not found, throws exception
        /// </exception>
        public static CustomAttributeData GetAttributeData(Type type, string attributeName)
        {
            for (int i = 0; i < type.GetCustomAttributesData().Count; i++)
            {
                CustomAttributeData customAttributeData = type.GetCustomAttributesData()[i];
                if (customAttributeData.ToString().Contains(attributeName))
                {
                    return customAttributeData;
                }
            }

            throw new ArgumentException("No attribute of type " + type.Name + " found.");
        }

        private static byte[] LoadFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int read = 0;
                    while ((read = fs.Read(buffer, 0, 1024)) > 0)
                        ms.Write(buffer, 0, read);
                    return ms.ToArray();
                }
            }
        }
    }
}
