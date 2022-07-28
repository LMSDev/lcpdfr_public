using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AdvancedHookManaged;
using LCPD_First_Response.Engine.Scripting.Native;

namespace LCPD_First_Response.Engine.Scripting.Native
{
    class NativeHooker : BaseComponent, ICoreTickable
    {
        public delegate void NativeInvokedCallback(ANativeCallContextSimple callContext);
        public delegate void NativeInvokedCallbackDynamic(params object[] arguments);

        private List<NativeHooked> hookedNatives;
        private NativeInfo nativeInfo;


        public NativeHooker()
        {
            this.hookedNatives = new List<NativeHooked>();
            this.nativeInfo = new NativeInfo();
        }

        public bool Hook(string nativeName, NativeInvokedCallbackDynamic callback)
        {
            // Check if we know this native
            NativeData data = this.nativeInfo.GetNativeInfo(nativeName);
            if (data == null) return false;

            // Hook
            AGame.HookNative(data.Name, data.ID, data.Arguments.ToIntArray(), data.BytesToOverwrite);
            // Add callback
            AddHookedNative(data, callback, ENativeCallbackType.Params);
            return true;
        }

        public bool HookUnknown(string nativename, int id, int bytesToOVerwrite, NativeInvokedCallback callback)
        {
            NativeData data = new NativeData(nativename, id, bytesToOVerwrite, 0);
            AddHookedNative(data, callback, ENativeCallbackType.Contenxt);
            return true;
        }

        public void Process()
        {
            // Get all called natives
            ANativeCallContextSimple[] calledNatives = AGame.HookNativeGetCalledNativesList();
            if (calledNatives != null)
            {
                foreach (ANativeCallContextSimple callContextSimple in calledNatives)
                {
                    // Check if ID is hooked
                    foreach (NativeHooked hookedNative in hookedNatives)
                    {
                        if (hookedNative.ID == callContextSimple.ID)
                        {
                            // If type is context, simply pass the context
                            if (hookedNative.CallbackType == ENativeCallbackType.Contenxt)
                            {
                                Delegate[] delegates = hookedNative.Callbacks.ToArray();
                                foreach (NativeInvokedCallback @delegate in delegates)
                                {
                                    @delegate.Invoke(callContextSimple);
                                }
                            }
                            // If type is params, cast all arguments and invoke
                            else if (hookedNative.CallbackType == ENativeCallbackType.Params)
                            {
                                Delegate[] delegates = hookedNative.Callbacks.ToArray();
                                NativeData nativeData = this.nativeInfo.GetNativeInfo(hookedNative.ID);
                                // Parse all arguments
                                object[] arguments = nativeData.Arguments.Parse(callContextSimple);

                                foreach (NativeInvokedCallbackDynamic @delegate in delegates)
                                {
                                    @delegate.Invoke(arguments);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddHookedNative(NativeData data, Delegate callback, ENativeCallbackType callbackType)
        {
            // Check if id is already in dictionary
            if (!this.hookedNatives.ContainsID(data.ID))
            {
                this.hookedNatives.Add(new NativeHooked(data.ID, callback, callbackType));
            }
            else
            {
                foreach (NativeHooked hookedNative in this.hookedNatives)
                {
                    if (hookedNative.ID == data.ID)
                    {
                        hookedNative.AddCallback(callback);
                    }
                }
            }
        }

        public override string ComponentName
        {
            get { return "NativeHooker"; }
        }
    }

    class NativeHooked
    {
        public List<Delegate> Callbacks { get; private set; }
        public ENativeCallbackType CallbackType { get; private set; }
        public int ID { get; private set; }

        public NativeHooked(int id, Delegate callback, ENativeCallbackType callbackType)
        {
            this.ID = id;
            this.Callbacks = new List<Delegate> {callback};
            this.CallbackType = callbackType;
        }

        public void AddCallback(Delegate callback)
        {
            this.Callbacks.Add(callback);
        }
    }

    static class NativeHookedExtension
    {
        public static bool ContainsID(this List<NativeHooked> list, int id)
        {
            foreach (NativeHooked nativeHooked in list)
            {
                if (nativeHooked.ID == id) return true;
            }
            return false;
        }
    }

    enum ENativeCallbackType
    {
        Contenxt,
        Params,
    }
}
