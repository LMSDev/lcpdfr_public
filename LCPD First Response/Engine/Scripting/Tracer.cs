/* 
 * Tracer.cs
 * 2013-01-22
 * Written thaCURSEDpie. Based largely on research by MulleDK19 and lms.
 * Heavily edited by lms.
 */

namespace LCPD_First_Response.Engine.Scripting
{
    using System;
    using System.Runtime.InteropServices;

    using GTA;

    /// <summary>
    /// A Vector 3d.
    /// </summary>
    [StructLayout(LayoutKind.Sequential), Serializable]
    internal class VECTOR3
    {
        /// <summary>
        /// The X.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y.
        /// </summary>
        public float Y;

        /// <summary>
        /// The Z.
        /// </summary>
        public float Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="VECTOR3"/> class.
        /// </summary>
        public VECTOR3()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VECTOR3"/> class.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <param name="z">
        /// The z.
        /// </param>
        public VECTOR3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VECTOR3"/> class.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public VECTOR3(Vector3 value)
        {
            this.X = value.X;
            this.Y = value.Y;
            this.Z = value.Z;
        }
    }

    /// <summary>
    /// A Vector 2d.
    /// </summary>
    [StructLayout(LayoutKind.Sequential), Serializable]
    internal class VECTOR2
    {
        /// <summary>
        /// The X.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y.
        /// </summary>
        public float Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="VECTOR2"/> class.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        public VECTOR2(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VECTOR2"/> class.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public VECTOR2(Vector2 value)
        {
            this.X = value.X;
            this.Y = value.Y;
        }
    }

    /// <summary>
    /// Arguments for tracing.
    /// </summary>
    [StructLayout(LayoutKind.Sequential), Serializable]
    internal class TraceArgs
    {
        public UInt32 StartEntity;
        public VECTOR2 WorldDirection;
        public VECTOR3 StartPos;
        public VECTOR3 EndPos;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 80)]
        UInt32[] padding;

        public TraceArgs(Vector2 worldDir, Vector3 startPos, Vector3 endPos)
        {
            this.StartEntity = 0;
            this.WorldDirection = new VECTOR2(worldDir);
            this.StartPos = new VECTOR3(startPos);
            this.EndPos = new VECTOR3(endPos);

            this.padding = new UInt32[80];
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    internal class HitInfo
    {
        public UInt32 val1;
        public UInt32 val2;
        public UInt32 val3;
        public UInt32 HitEntity;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 200)] // Excessive padding, but it works :-P
        UInt32[] padding;

        public HitInfo()
        {
            this.val1 = 0;
            this.val2 = 0;
            this.val3 = 0;
            this.HitEntity = 0;

            this.padding = new UInt32[200];
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    internal class TraceInfo
    {
        public IntPtr hitInfo;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] paddingBuf1;

        public Vector3 HitPosition;
        UInt32 padding1;
        public Vector3 HitNormal1;
        UInt32 padding2;
        public Vector3 HitNormal2;
        UInt32 padding3;
        public Vector3 HitNormal3;
        UInt32 padding4;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 200)] // Excessive padding, but it works :-P
        UInt32[] padding;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceInfo"/> class.
        /// </summary>
        public TraceInfo()
        {
            this.hitInfo = new IntPtr();
            this.HitPosition = new Vector3();
            this.HitNormal1 = new Vector3();
            this.HitNormal2 = new Vector3();
            this.HitNormal3 = new Vector3();

            this.paddingBuf1 = new char[12];

            this.padding1 = 0;
            this.padding2 = 0;
            this.padding3 = 0;
            this.padding4 = 0;

            this.padding = new UInt32[200];
        }
    }

    /// <summary>
    /// Class to provide functions for ray tracing.
    /// </summary>
    internal class Tracer
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate bool TraceDel(ref Vector3 StartPos,
                                    ref Vector3 EndPos,
                                    ref Vector3 StartPos2,
                                    ref TraceArgs traceArgs,
                                    int i1_1,
                                    IntPtr traceInfo,
                                    int i2_40,
                                    int i3_1,
                                    int i4_0,
                                    int i5_462,
                                    int traceFlags, // should be 257
                                    int i6_8,
                                    int i7_0);

        /// <summary>
        /// The base address.
        /// </summary>
        private int baseAddress;

        /// <summary>
        /// The tracer delegate.
        /// </summary>
        private TraceDel Trace;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class.
        /// </summary>
        public Tracer()
        {
            this.baseAddress = GetModuleHandle(null).ToInt32();

            // Offset based on patch
            if (Game.Version == GameVersion.v1040)
            {
                this.Trace = (TraceDel)Marshal.GetDelegateForFunctionPointer(new IntPtr(this.baseAddress + 0x4EF2C0), typeof(TraceDel));
            }
            else if (Game.Version == GameVersion.v1070)
            {
                this.Trace = (TraceDel)Marshal.GetDelegateForFunctionPointer(new IntPtr(this.baseAddress + 0x59A380), typeof(TraceDel));
            }
            else if (Game.Version == GameVersion.v1120)
            {
                this.Trace = (TraceDel)Marshal.GetDelegateForFunctionPointer(new IntPtr(this.baseAddress + 0x5CC9D0), typeof(TraceDel));
            }
            else
            {
                Log.Warning("Tracer: Game version not supported", "RayTrace");
            }
        }

        /// <summary>
        /// Returns whether an object was hit when tracing from <paramref name="startPos"/> to <paramref name="endPos"/>.
        /// </summary>
        /// <param name="startPos">The start position.</param>
        /// <param name="endPos">The end position.</param>
        /// <param name="collisionPos">The collision position, if found.</param>
        /// <returns>True on collision, false otherwise.</returns>
        public bool DoesHitAnything(Vector3 startPos, Vector3 endPos, ref Vector3 collisionPos)
        {
            Vector3 position = startPos;
            Vector3 inFrontPos = endPos;
            uint hitEntity = 0;
            Vector3 hitPos = Vector3.Zero;
            Vector3 hitNormal = Vector3.Zero;

            if (this.DoTrace(position, inFrontPos, ref hitEntity, ref hitPos, ref hitNormal))
            {
                collisionPos = hitPos;
                return true;
            }

            return false;
        }
        
        public bool DoTrace(Vector3 startPos, Vector3 endPos, ref UInt32 HitEntity, ref Vector3 HitPos, ref Vector3 HitNormal)
        {
            Vector3 start = startPos;
            Vector3 start2 = startPos;

            Vector3 end = endPos;
            TraceArgs args = new TraceArgs(new Vector2(0, 0), startPos, endPos);
            TraceInfo info = new TraceInfo();

            bool ret = false;

            IntPtr info_pointer = new IntPtr();
            IntPtr hitinfo_pointer = new IntPtr();

            try
            {
                info_pointer = Marshal.AllocHGlobal(Marshal.SizeOf(info));
                hitinfo_pointer = Marshal.AllocHGlobal(Marshal.SizeOf(new HitInfo()));

                Marshal.StructureToPtr(new HitInfo(), hitinfo_pointer, true);
                info.hitInfo = hitinfo_pointer;

                Marshal.StructureToPtr(info, info_pointer, true);

                ret = this.Trace(ref start,
                               ref end,
                               ref start2,
                               ref args,
                               1,
                               info_pointer,
                               40,
                               1,
                               0,
                               462,
                               257,
                               8,
                               0);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString(), "Tracer");
                return false;
            }

            info = (TraceInfo)Marshal.PtrToStructure(info_pointer, typeof(TraceInfo));
            HitInfo hit_information = (HitInfo)Marshal.PtrToStructure(info.hitInfo, typeof(HitInfo));
            HitEntity = hit_information.HitEntity;

            HitPos = info.HitPosition; 

            HitNormal = info.HitNormal2;

            Marshal.FreeHGlobal(info_pointer);
            Marshal.FreeHGlobal(hitinfo_pointer);

            return ret;
        }
    }
}