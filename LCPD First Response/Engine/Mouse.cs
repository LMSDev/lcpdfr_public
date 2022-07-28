using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using LCPD_First_Response.Engine.Scripting.Native;
using GTA;

namespace LCPD_First_Response.Engine
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class Mouse
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUT
        {
            [FieldOffset(0)]
            public int type;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        const int INPUT_MOUSE = 0;
        const int INPUT_KEYBOARD = 1;
        const int INPUT_HARDWARE = 2;
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;
        const uint KEYEVENTF_SCANCODE = 0x0008;
        const uint XBUTTON1 = 0x0001;
        const uint XBUTTON2 = 0x0002;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_XDOWN = 0x0080;
        const uint MOUSEEVENTF_XUP = 0x0100;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private static MOUSEINPUT createMouseInput(int x, int y, uint data, uint t, uint flag)
        {
            MOUSEINPUT mi = new MOUSEINPUT();
            mi.dx = x;
            mi.dy = y;
            mi.mouseData = data;
            mi.time = t;
            mi.dwFlags = flag;
            return mi;
        }

        private static KEYBDINPUT createKeybdInput(short wVK, uint flag)
        {
            KEYBDINPUT i = new KEYBDINPUT();
            i.wVk = (ushort)wVK;
            i.wScan = 0;
            i.time = 0;
            i.dwExtraInfo = IntPtr.Zero;
            i.dwFlags = flag;
            return i;
        }

        public static void Test()
        {
            // Move mouse 100pixel on x-axis
            INPUT[] input = new INPUT[1];
            input[0].type = INPUT_MOUSE;
            input[0].mi = createMouseInput(100, 0, 0, 0, MOUSEEVENTF_MOVE);
            SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
        }

        public static void AimAtPosition(GTA.Vector3 position)
        {
            // Vector pointing to the target's head from the camera position
            Vector3 newVector = position - CPlayer.LocalPlayer.Ped.Position;
            newVector.Normalize();

            Vector3 direction = newVector - Game.CurrentCamera.Direction;
            float mouseMove = direction.Z * 192 * -1;
            GTA.Game.DisplayText(direction.Z.ToString());

            while (direction.Z > 0.09)
            {
                direction = newVector - Game.CurrentCamera.Direction;
                // Adjust z axis
                INPUT[] input = new INPUT[1];
                input[0].type = INPUT_MOUSE;
                input[0].mi = createMouseInput(0, -5, 0, 0, MOUSEEVENTF_MOVE);
                SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
                Game.WaitInCurrentScript(10);
            }

            //INPUT[] input;
            //// Cancel aiming
            //input = new INPUT[1];
            //input[0].type = INPUT_MOUSE;
            //input[0].mi = createMouseInput(0, 0, 0, 0, MOUSEEVENTF_RIGHTUP);
            //SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
            //GTA.Game.WaitInCurrentScript(10);


            //// Vector pointing to the target's head from the camera position
            //Vector3 newVector = position - Main.Player.Ped.Position;
            //newVector.Normalize();

            //float finalHeading = GTA.Helper.DirectionToHeading(newVector);
            //Main.Player.Ped.Heading = finalHeading;
            //Natives.PlaceCamBehindPed(Main.Player.Ped);

            //Vector3 direction = newVector - Game.CurrentCamera.Direction;
            //float mouseMove = direction.Z * 192 * -1;
            //// Hold down right mouse button
            //input = new INPUT[1];
            //input[0].type = INPUT_MOUSE;
            //input[0].mi = createMouseInput(0, 0, 0, 0, MOUSEEVENTF_RIGHTDOWN);
            //SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
            //while (mouseMove > 0.3)
            //{
            //    direction = newVector - Game.CurrentCamera.Direction;
            //    mouseMove = direction.Z * 192 * -1;

            //    // Adjust z axis
            //    input = new INPUT[1];
            //    input[0].type = INPUT_MOUSE;
            //    input[0].mi = createMouseInput(0, (int)mouseMove, 0, 0, MOUSEEVENTF_MOVE);
            //    SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
            //    Game.WaitInCurrentScript(10);
            //}
            //// Fire!
            //input = new INPUT[1];
            //input[0].type = INPUT_MOUSE;
            //input[0].mi = createMouseInput(0, 0, 0, 0, MOUSEEVENTF_LEFTDOWN);
            //SendInput(1, input, Marshal.SizeOf(input[0].GetType()));

            //GTA.Game.WaitInCurrentScript(200);

            //input = new INPUT[1];
            //input[0].type = INPUT_MOUSE;
            //input[0].mi = createMouseInput(0, 0, 0, 0, MOUSEEVENTF_LEFTUP);
            //SendInput(1, input, Marshal.SizeOf(input[0].GetType()));

            //input = new INPUT[1];
            //input[0].type = INPUT_MOUSE;
            //input[0].mi = createMouseInput(0, 0, 0, 0, MOUSEEVENTF_RIGHTUP);
            //SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
        }
    }
}
