namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages all classes deriving from <see cref="BaseScript"/>. Use this in your plugin for scripts. You can create scripts via their constructor and add them later
    /// or directly create them via the manager. Supports no auto creation of scripts at the moment, this is a design decision.
    /// </summary>
    public class ScriptManager
    {
        /// <summary>
        /// Scripts found.
        /// </summary>
        private List<Type> foundScripts;

        /// <summary>
        /// Scripts running.
        /// </summary>
        private List<BaseScript> scripts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptManager"/> class.
        /// </summary>
        public ScriptManager()
        {
            this.foundScripts = new List<Type>();
            this.scripts = new List<BaseScript>();
        }

        /// <summary>
        /// Searches for scripts in the current assembly and adds them to the internal cache, so they are available for starting.
        /// </summary>
        public void LookForScriptsInCurrentAssembly()
        {
            // Iterate through all types and check if subclass of BaseClass. If so, add to the list
            this.foundScripts = AssemblyHelper.GetTypesInheritingFromType(typeof(BaseScript)).ToList();
        }

        /// <summary>
        /// Gets all registered scripts.
        /// </summary>
        /// <returns>All registered scripts.</returns>
        public Type[] GetAllRegisteredScripts()
        {
            return this.foundScripts.ToArray();
        }

        /// <summary>
        /// Gets all running scripts.
        /// </summary>
        /// <returns>All running scripts.</returns>
        public BaseScript[] GetAllRunningScripts()
        {
            return this.scripts.ToArray();
        }

        /// <summary>
        /// Gets all running script instances of <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The scripts instances.</returns>
        public BaseScript[] GetRunningScriptInstances(string name)
        {
            return this.scripts.Where(baseScript => baseScript.ScriptInfo.Name.ToLower() == name.ToLower()).ToArray();
        }

        /// <summary>
        /// Processes all scripts.
        /// </summary>
        public void Process()
        {
            for (int i = 0; i < this.scripts.Count; i++)
            {
                BaseScript script = this.scripts[i];

                if (script.ScriptInfo.ProcessInScriptManager)
                {
                    try
                    {
                        script.Process();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error while processing script: " + script.ScriptInfo.Name + ": " + ex.Message  + ex.StackTrace, this);
                        ExceptionHandler.ExceptionCaught(script, ex);
                        try
                        {
                            script.End();
                        }
                        catch (Exception)
                        {
                            Log.Warning("Failed to free script properly: " + script.ScriptInfo.Name, this);
                        }

                        this.scripts.Remove(script);
                    }
                }
            }
        }

        /// <summary>
        /// Registers the script to the manager, so it can be started. Useful when the script isn't automatically detected by the manager.
        /// </summary>
        /// <param name="t">The type of the script.</param>
        public void RegisterScript(Type t)
        {
            if (t.IsSubclassOf(typeof(BaseScript)))
            {
                this.foundScripts.Add(t);
            }
            else
            {
                throw new ArgumentException("Invalid type not inheriting from BaseScript.");
            }
        }

        /// <summary>
        /// Registers an already running script instance that has been created by its constructor rather than StartScript to the script manager.
        /// </summary>
        /// <param name="baseScript">
        /// The script.
        /// </param>
        public void RegisterScriptInstance(BaseScript baseScript)
        {
            this.RegisterScriptInstance(baseScript, baseScript.ScriptInfo.ProcessInScriptManager);
        }

        /// <summary>
        /// Registers an already running script instance that has been created by its constructor rather than StartScript to the script manager.
        /// </summary>
        /// <param name="baseScript">
        /// The script.
        /// </param>
        /// <param name="processInScriptManager">
        /// Whether or not the script should be processed in the script manager, overrides the script attribute settings.
        /// </param>
        public void RegisterScriptInstance(BaseScript baseScript, bool processInScriptManager)
        {
            this.AddScript(baseScript);
        }

        /// <summary>
        /// Starts the script called <paramref name="name"/>.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <typeparam name="T">
        /// The type of the script.
        /// </typeparam>
        /// <returns>
        /// The script instance or null.
        /// </returns>
        public T StartScript<T>(string name) where T : BaseScript
        {
            foreach (Type foundScript in this.foundScripts)
            {
                try
                {
                    ScriptInfoAttribute scriptInfoAttribute = AssemblyHelper.GetAttribute<ScriptInfoAttribute>(foundScript);
                    if (scriptInfoAttribute.Name.ToLower() == name.ToLower())
                    {
                        Log.Debug("StartScript: Starting script " + scriptInfoAttribute.Name, "ScriptManager");

                        // Create instance of script
                        BaseScript baseScript = Activator.CreateInstance(foundScript) as BaseScript;
                        this.AddScript(baseScript);

                        return (T)baseScript;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("StartScript: Error while creating script: " + foundScript.FullName + ". " + ex.Message, "ScriptManager");
                }
            }

            Log.Warning("StartScript: Failed to find script: " + name, "ScriptManager");
            return null;
        }

        /// <summary>
        /// Stops all scripts.
        /// </summary>
        public void Shutdown()
        {
            // Reverse loop because scripts are removed while ending them, so count gets smaller
            for (int i = this.scripts.Count - 1; i > 0; i--)
            {
                BaseScript script = this.scripts[i];
                script.End();
            }
        }

        /// <summary>
        /// Stops the script.
        /// </summary>
        /// <param name="name">The name.</param>
        public void StopScript(string name)
        {
            for (int i = 0; i < this.scripts.Count; i++)
            {
                BaseScript script = this.scripts[i];

                if (script.ScriptInfo.Name == name)
                {
                    script.End();
                    return;
                }
            }
        }

        /// <summary>
        /// Stops the script.
        /// </summary>
        /// <param name="script">The script.</param>
        public void StopScript(BaseScript script)
        {
            script.End();
        }

        /// <summary>
        /// Adds the given script to the script manager.
        /// </summary>
        /// <param name="baseScript">The script.</param>
        private void AddScript(BaseScript baseScript)
        {
            baseScript.OnEnd += new BaseScript.OnEndEventHandler(this.BaseScript_OnEnd);
            this.scripts.Add(baseScript);
        }

        /// <summary>
        /// Script on end.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        private void BaseScript_OnEnd(object sender)
        {
            // Remove script
            this.scripts.Remove((BaseScript)sender);
        }
    }
}
