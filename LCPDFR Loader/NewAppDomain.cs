using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;

namespace LCPDFR_Loader
{
    [Serializable]
    class NewAppDomain
    {
        private AppDomain appDomain;
        private object main;
        private MethodInfo tick;

        public NewAppDomain()
        {
            main = null;

            Load();
        }

        private void Load()
        {
            System.Diagnostics.Debugger.Launch();

            string name = AppDomain.CurrentDomain.FriendlyName;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            Type t = this.GetType();
            bool serializeAble = t.IsSerializable;

            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = Path.Combine(GTA.Game.InstallFolder, "scripts\\");
            setup.PrivateBinPath = Path.Combine(GTA.Game.InstallFolder, "scripts\\");
            appDomain = AppDomain.CreateDomain("NewAppDomain", AppDomain.CurrentDomain.Evidence, setup, AppDomain.CurrentDomain.PermissionSet);
            appDomain.AssemblyResolve += new ResolveEventHandler(appDomain_AssemblyResolve);
            // Load all needed assemblies
            //appDomain.Load(GetAssemblyBytes(GTA.Game.InstallFolder + "\\ScriptHookDotNet.asi"));
            //byte[] bytes = GetAssemblyBytes(Path.Combine(GTA.Game.InstallFolder, "scripts\\") + "LCPDFR Loader.net.dll");
            //appDomain.Load(bytes);

            //m = new NewAppDomain();
            //appDomain.DoCallBack(new CrossAppDomainDelegate(LoadAssembly));

            string path = Path.Combine(GTA.Game.InstallFolder, "scripts\\") + "LCPDFR Loader.net.dll";
            GTA.Game.Console.Print("Location: " + path);

            ObjectHandle objectHandle = Activator.CreateInstanceFrom(appDomain, path, "LCPDFR_Loader.Test");
            Test o = (Test)objectHandle.Unwrap();

            o.LoadAssembly();

            //Assembly a = Assembly.LoadFrom("LCPD First Response.dll");
            //Type t = a.GetType("LCPD_First_Response.Engine.Main");

            //main = Activator.CreateInstance(t, new object[] { });

            //tick = main.GetType().GetMethod("Main_Tick");
        }

        Assembly appDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = "";
            if (args.RequestingAssembly != null) name = args.RequestingAssembly.FullName;
            System.IO.File.AppendAllText("Test.log", args.Name + " - - -" + name);
            if (args.Name.Contains("ScriptHook"))
            {
                return Assembly.LoadFrom(GTA.Game.InstallFolder + "\\ScriptHookDotNet.asi");
            }
            throw new NotImplementedException();
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("LCPDFR Loader"))
            {
                return Assembly.GetExecutingAssembly();
                //return Assembly.LoadFrom(Path.Combine(GTA.Game.InstallFolder, "scripts\\") + "LCPDFR Loader.net.dll");
            }
            return null;
        }

        private byte[] GetAssemblyBytes(string path)
        {
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

        static void Callback()
        {

        }

        public void LoadAssembly()
        {
            GTA.Game.Console.Print("NewAppDomain: LoadAssembly");
            // We are in the new appdomain, so we can use Assembly.Load
            Assembly classLibrary1 = null;
            using (FileStream fs = File.Open(GTA.Game.InstallFolder + "\\LCPD First Response.dll", FileMode.Open))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int read = 0;
                    while ((read = fs.Read(buffer, 0, 1024)) > 0)
                        ms.Write(buffer, 0, read);
                    classLibrary1 = Assembly.Load(ms.ToArray());
                }
            }
            // Create main type
            Type t = classLibrary1.GetType("LCPD_First_Response.Engine.Main");
            main = Activator.CreateInstance(t, new object[] { });

            tick = main.GetType().GetMethod("Main_Tick");
        }

        public void Tick()
        {
            if (tick != null)
            {
                tick.Invoke(main, new object[] { null, null });
            }
        }
    }

    [Serializable]
    class Test
    {
        private object main;
        private MethodInfo tick;

        public Test()
        {
        }

        private byte[] LoadFile(string path)
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

        public void LoadAssembly()
        {
            if (File.Exists(GTA.Game.InstallFolder + "\\LCPD First Response.dll"))
            {
                GTA.Game.Console.Print("NewAppDomain: LoadAssembly");
                // We are in the new appdomain, so we can use Assembly.Load
                string assemblyPath = GTA.Game.InstallFolder + "\\LCPD First Response.dll";
                string symbolsPath = GTA.Game.InstallFolder + "\\LCPD First Response.pdb";

                byte[] assemblyBytes = LoadFile(assemblyPath);
                byte[] symbolsBytes = LoadFile(symbolsPath);

                Assembly assembly = null;
                if (File.Exists(GTA.Game.InstallFolder + "\\LCPD First Response.debug"))
                {
                    assembly = Assembly.Load(assemblyBytes, symbolsBytes);
                }
                else
                {
                    assembly = Assembly.LoadFrom(assemblyPath);
                }

                // Create main type
                Type t = assembly.GetType("LCPD_First_Response.Engine.Main");
                main = Activator.CreateInstance(t, new object[] {});

                tick = main.GetType().GetMethod("Main_Tick");
            }
            else if (File.Exists(GTA.Game.InstallFolder + "\\IV Coop.dll"))
            {
                GTA.Game.Console.Print("NewAppDomain: LoadAssembly");
                // We are in the new appdomain, so we can use Assembly.Load
                string assemblyPath = GTA.Game.InstallFolder + "\\IV Coop.dll";
                string symbolsPath = GTA.Game.InstallFolder + "\\IV Coop.pdb";

                byte[] assemblyBytes = LoadFile(assemblyPath);
                byte[] symbolsBytes = LoadFile(symbolsPath);

                Assembly assembly = Assembly.Load(assemblyBytes, symbolsBytes);

                // Create main type
                Type t = assembly.GetType("IV_Coop.Engine.Main");
                main = Activator.CreateInstance(t, new object[] { });

                tick = main.GetType().GetMethod("Main_Tick");
            }



            //Assembly classLibrary1 = null;
            //using (FileStream fs = File.Open(GTA.Game.InstallFolder + "\\LCPD First Response.dll", FileMode.Open))
            //{
            //    using (MemoryStream ms = new MemoryStream())
            //    {
            //        byte[] buffer = new byte[1024];
            //        int read = 0;
            //        while ((read = fs.Read(buffer, 0, 1024)) > 0)
            //            ms.Write(buffer, 0, read);
            //        classLibrary1 = Assembly.Load(ms.ToArray());
            //    }
            //}
            //// Create main type
            //Type t = classLibrary1.GetType("LCPD_First_Response.Engine.Main");
            //main = Activator.CreateInstance(t, new object[] { });

            //tick = main.GetType().GetMethod("Main_Tick");
        }

        public void Tick(object sender, EventArgs e)
        {
            if (tick != null)
            {
                tick.Invoke(main, new object[] { sender, e });
            }
        }
    }
}
