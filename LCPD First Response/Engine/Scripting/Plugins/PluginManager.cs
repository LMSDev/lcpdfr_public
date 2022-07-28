namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Manages all classes deriving from <see cref="Plugin"/>.
    /// </summary>
    internal class PluginManager
    {
        /// <summary>
        /// Plugins found in the assembly.
        /// </summary>
        private List<Type> foundPlugins;

        /// <summary>
        /// Loaded plugins.
        /// </summary>
        private List<Plugin> plugins;

        /// <summary>
        /// Initializes all values of an instance of <see cref="PluginManager"/>.
        /// </summary>
        public void Initialize()
        {
            this.foundPlugins = new List<Type>();
            this.plugins = new List<Plugin>();

            // Iterate through all types and check if subclass of Plugin. If so, add to the list
            this.foundPlugins = AssemblyHelper.GetTypesInheritingFromType(typeof(Plugin)).ToList();

            if (!System.IO.File.Exists("LCPD First Response.debug"))
            {
                Log.Info("Initialize: Loading custom plugins", "PluginManager");

                Assembly[] assemblies = AssemblyHelper.GetAllAssembliesInFolder("lcpdfr\\plugins\\");
                if (assemblies != null)
                {
                    foreach (Assembly assembly in assemblies)
                    {
                        this.foundPlugins = this.foundPlugins.Concat(AssemblyHelper.GetTypesInheritingFromTypeInAssembly(typeof(Plugin), assembly)).ToList();
                    }
                }
            }
            else
            {
                Log.Info("Initialize: In debug mode, no custom plugins loaded", "PluginManager");
            }

            // Sort plugins (plugins that have IsEntryPoint set are at the beginning of the list)
            List<Type> sortedPlugins = new List<Type>();
            foreach (Type foundPlugin in this.foundPlugins)
            {
                try
                {
                    PluginInfoAttribute pluginInfoAttribute = AssemblyHelper.GetAttribute<PluginInfoAttribute>(foundPlugin);
                    if (pluginInfoAttribute.IsEntryPoint)
                    {
                        sortedPlugins.Insert(0, foundPlugin);
                    }
                    else
                    {
                        sortedPlugins.Add(foundPlugin);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("PluginManager: Error while reading plugin data: " + foundPlugin.FullName + ". " + ex.Message, "PluginManager");
                }
            }

            // Update reference
            this.foundPlugins = sortedPlugins;

            // Create an instance of all plugins that have AutoCreate set to true
            foreach (Type foundPlugin in this.foundPlugins)
            {
                PluginInfoAttribute pluginInfoAttribute = AssemblyHelper.GetAttribute<PluginInfoAttribute>(foundPlugin);

                if (pluginInfoAttribute.AutoCreate)
                {
                    Log.Debug("Creating plugin: " + pluginInfoAttribute.Name, "PluginManager");

                    try
                    {
                        // Create instance of plugin
                        Plugin plugin = Activator.CreateInstance(foundPlugin) as Plugin;
                        this.plugins.Add(plugin);
                    }
                    catch (Exception exception)
                    {
                        Log.Warning("Error while creating plugin: " + pluginInfoAttribute.Name + ": " + exception.Message + exception.StackTrace, "PluginManager");
                    }
                }
            }

            // All plugins are created, intialize them
            foreach (Plugin plugin in this.plugins)
            {
                try
                {
                    plugin.Initialize();
                }
                catch (Exception exception)
                {
                    Log.Warning("Error while initializing plugin: " + plugin.PluginInfo.Name + ": " + exception.Message + exception.StackTrace, "PluginManager");
                }
            }

            Log.Debug("All plugins initialized", "PluginManager");
        }

        /// <summary>
        /// Creates a plugin instance of <paramref name="name"/> if found. Returns null otherwise.
        /// </summary>
        /// <typeparam name="T">The plugin type.</typeparam>
        /// <param name="name">The plugin name.</param>
        /// <returns>Instance if name was found. Null if not.</returns>
        public T CreatePlugin<T>(string name) where T : Plugin
        {
            foreach (Type foundPlugin in this.foundPlugins)
            {
                PluginInfoAttribute pluginInfoAttribute = AssemblyHelper.GetAttribute<PluginInfoAttribute>(foundPlugin);

                if (pluginInfoAttribute.Name.ToLower() == name.ToLower())
                {
                    Log.Debug("CreatePlugin: Creating plugin: " + pluginInfoAttribute.Name, "PluginManager");

                    // Create instance of plugin
                    Plugin plugin = Activator.CreateInstance(foundPlugin) as Plugin;

                    // Immediately invoke Initialize
                    plugin.Initialize();
                    return (T)plugin;
                }
            }

            Log.Warning("CreatePlugin: Failed to find plugin: " + name, "PluginManager");
            return null;
        }

        /// <summary>
        /// Processes all plugins.
        /// </summary>
        public void Process()
        {
            foreach (Plugin plugin in this.plugins)
            {
                try
                {
                    if (plugin.ExceptionOccured)
                    {
                        continue;
                    }

                    plugin.Process();
                }
                catch (Exception ex)
                {
                    Log.Error("Unhandled exception caught while processing plugin: " + plugin.PluginInfo.Name + ". Plugin will no longer be executed.", "PluginManager");

                    // Pass exception and sender (not the manager but the plugin itself in this case) to the exception handler
                    ExceptionHandler.ExceptionCaught(plugin, ex);

                    // Free all plugin resources
                    plugin.ExceptionOccured = true;
                    plugin.Finally();
                }
            }
        }
    }
}
