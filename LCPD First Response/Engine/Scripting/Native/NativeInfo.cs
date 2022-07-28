using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LCPD_First_Response.Engine.Scripting.Native
{
    class NativeInfo
    {
        public const int ArgSize = 4;

        private List<NativeData> nativeData;

        public NativeInfo()
        {
            nativeData = new List<NativeData>();
            // Populate DB
            nativeData.Add(new NativeData("ADD_BLIP_FOR_COORD", 3, 0xA, 4, EArgumentType.Float, EArgumentType.Float, EArgumentType.Float, EArgumentType.UInt32));
            nativeData.Add(new NativeData("START_CUTSCENE_NOW", 1, 0xD, 1, EArgumentType.String));
            nativeData.Add(new NativeData("SET_MISSION_FLAG", 2, 0xA, 1, EArgumentType.Bool));
        }

        public NativeData GetNativeInfo(string name)
        {
            // Check if name is known
            foreach (NativeData data in nativeData)
            {
                if (data.Name == name) return data;
            }
            return null;
        }

        public NativeData GetNativeInfo(int id)
        {
            // Check if name is known
            foreach (NativeData data in nativeData)
            {
                if (data.ID == id) return data;
            }
            return null;
        }   
    }

    class NativeData
    {
        public int ArgumentCount { get; private set; }
        public NativeArgument Arguments { get; private set; }
        public int BytesToOverwrite { get; private set; }
        public int ID { get; private set; }
        public string Name { get; private set; }

        public NativeData(string name, int id, int bytesToOverwrite, int argumentCount, params EArgumentType[] types)
        {
            this.Name = name;
            this.ID = id;
            this.BytesToOverwrite = bytesToOverwrite;
            this.ArgumentCount = argumentCount;
            this.Arguments = new NativeArgument(types);
        }
    }

    class NativeArgument
    {
        public List<EArgumentType> Types { get; private set; }

        public NativeArgument(params EArgumentType[] types)
        {
            if (types == null) return;
            this.Types = new List<EArgumentType>();
            foreach (EArgumentType eArgumentType in types)
            {
                this.Types.Add(eArgumentType);
            }   
        }

        public unsafe object[] Parse(AdvancedHookManaged.ANativeCallContextSimple callContextSimple)
        {
            List<object> arguments = new List<object>();

            // All types have a size of 4, so we create an int array and put all args in it
            int[] tempArgs = new int[callContextSimple.m_nArgCount];
            for (int i = 0; i < callContextSimple.m_nArgCount; i++)
            {
                IntPtr ptr = new IntPtr(callContextSimple.m_pArgs);
                IntPtr ptr2 = new IntPtr(ptr.ToInt32() + i * NativeInfo.ArgSize);
                tempArgs[i] = ptr2.ToInt32();
            }

            for (int i = 0; i < tempArgs.Length; i++)
            {
                // Get argument and argument type
                int tempArg = tempArgs[i];
                EArgumentType type = this.Types[i];

                // Handle types
                switch (type)
                {
                    case EArgumentType.Bool:
                        bool bArg = *(bool*) tempArg;
                        arguments.Add(bArg);
                        break;
                    case EArgumentType.Float:
                        float fArg = *(float*) tempArg;
                        arguments.Add(fArg);
                        break;
                    case EArgumentType.Int32:
                        int i32Arg = *(int*) tempArg;
                        arguments.Add(i32Arg);
                        break;
                    case EArgumentType.String:
                        int sArg = *(int*) tempArg;
                        string s = Marshal.PtrToStringAnsi(new IntPtr(sArg));
                        // Warning: Freeing will crash for strings pushed by the SHDN since these will be deleted by the SHDN. This only works for native gta scripts
                        //AdvancedHookManaged.AGame.Free((void*)tempArg);
                        arguments.Add(s);
                        break;
                    case EArgumentType.UInt32:
                        uint u32Arg = *(uint*) tempArg;
                        arguments.Add(u32Arg);
                        break;
                }
            }

            //for (int i = 0; i < callContextSimple.m_nArgCount; i++)
            //{
            //    // Get argument type
            //    EArgumentType type = this.Types[i];
            //    void* args = callContextSimple.m_pArgs;

            //    // Read argument
            //    switch (type)
            //    {
            //        case EArgumentType.Bool:
            //            bool boArg = *(bool*) args;
            //            arguments.Add(boArg);
            //            break;
            //        case EArgumentType.Float:
            //            float fArg = *(float*) args;
            //            arguments.Add(fArg);
            //            break;
            //        case EArgumentType.Int32:
            //            int iArg = *(int*)args;
            //            arguments.Add(iArg);
            //            break;
            //        case EArgumentType.String:
            //            int sArg = *(int*) args;
            //            string s = Marshal.PtrToStringAnsi(new IntPtr(sArg));
            //            //AdvancedHookManaged.AGame.FreeArray((void*)sArg);
            //            //Marshal.FreeHGlobal(new IntPtr(sArg));
            //            arguments.Add(s);
            //            break;
            //    }

            //    // Get argument 
            //    //int iArgs = *(int*)(callContextSimple.m_pArgs)i;
            //    //string s = Marshal.PtrToStringAnsi(new IntPtr(iArgs));


            //    //GTA.Game.DisplayText(s);
            //    //GTA.Game.Console.Print(s);
            //}
            // Free mem
            //Marshal.FreeHGlobal(new IntPtr(callContextSimple.m_pArgs));
            //AdvancedHookManaged.AGame.Free(callContextSimple.m_pArgs);

            // Free context
            AdvancedHookManaged.AGame.Free(callContextSimple.NativeContext);

            return arguments.ToArray();
        }

        public int[] ToIntArray()
        {
            int[] iTypes = new int[this.Types.Count];

            for (int index = 0; index < this.Types.Count; index++)
            {
                iTypes[index] = (int)this.Types[index];

            }
            return iTypes.ToArray();
        }
    }

    enum EArgumentType
    {
        Bool = 1,
        Float = 2,
        Int32 = 3,
        String = 4,
        UInt32 = 5,
    }
}
