using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Windows.Forms;

namespace LCPDFR_Loader
{
    [Serializable]
    class Loader : GTA.Script
    {
        private AppDomain appDomain;
        private bool initialized;
        private Test test;

        public Loader()
        {
            this.KeyDown += new GTA.KeyEventHandler(Loader_KeyDown);
            this.Tick += new EventHandler(Loader_Tick);

            this.Interval = 10;
        }

        void Loader_KeyDown(object sender, GTA.KeyEventArgs e)
        {
            // ALT+F11: Unload
            // ALT+F12: Reload

            //if (e.Key == Keys.F11)
            //{
            //    Unload();
            //    GTA.Game.DisplayText("Unloaded");
            //}
            //if (e.Key == Keys.F12)
            //{
            //    Unload();
            //    Load();
            //    GTA.Game.DisplayText("Reloaded");
            //}
        }

        void Loader_Tick(object sender, EventArgs e)
        {
            if (!this.initialized)
            {
                Load();
                this.initialized = true;
            }

            // Call tick
            if (this.test != null)
            {
                this.test.Tick(sender, e);
            }
        }

        private void Load()
        {
            // To allow our current domain (SHDN_ScriptDomain) loading this assembly
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            // Default path is GTA Path\Scripts
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = Path.Combine(GTA.Game.InstallFolder, "scripts\\");
            setup.PrivateBinPath = Path.Combine(GTA.Game.InstallFolder, "scripts\\");
            // Create domain using the setup and the settings of the current appdomain
            this.appDomain = AppDomain.CreateDomain("NewAppDomain", AppDomain.CurrentDomain.Evidence, setup, AppDomain.CurrentDomain.PermissionSet);

            // Create the instance in the appdomain and unwrap the handle to an actual object
            string path = Path.Combine(GTA.Game.InstallFolder, "scripts\\") + "LCPDFR Loader.net.dll";
            ObjectHandle objectHandle = Activator.CreateInstanceFrom(appDomain, path, "LCPDFR_Loader.Test");
            this.test = (Test)objectHandle.Unwrap();

            // Load the LCPD First Response assembly into memory
            this.test.LoadAssembly();
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("LCPDFR Loader"))
            {
                return Assembly.GetExecutingAssembly();
            }
            return null;
        }

        private void Unload()
        {
            // Unload appdomain and destroy the loaded assembly instance
            if (this.appDomain != null)
            {
                // Unload and null all pointers
                AppDomain.Unload(this.appDomain);
                this.test = null;
                this.appDomain = null;
                // Now free all 'nulled' resources
                GC.Collect();
            }
        }
    }
}
