namespace LCPD_First_Response.LCPDFR.Scripts
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;

    static class LightHelper
    {
        // IV calls:
        //DrawLight(0, 0x2, 0x504, vector1, vector2, vector3, vector4, 30.92f, 4139616000.0f, 9, 20.0f, 1.0f, 25.0f, 0.0f, 0.0f, -1, 0x3F, ((GTA.Ped)CPlayer.LocalPlayer.Ped).MemoryAddress);
        //DrawLight(0, 0x2, 0x114, vector1, vector2, vector3, vector4, 140.0f, 1, 1, 16.0f, 1.0f, 45.0f, 10.0f, 1.0f, -1, -1, 0);
        // 0, 0x2, 0x504, vec, vec, vec, vec, 25.92, unknown, 9, 75.0, 20.0 50.0, 0.0, 0.0, -1, 0x3F, 0x0
        //0, 2, 0x114, { 0.71, 0.70, -0.07 }, { 0.0, -0.10, -0.99 }, { -542, 1224, 40 }, { 1.0, 1.0, 1.0 }, 14.38f, 0, 0, 16.0, 0, 45.0, 0.0, 0.0, -1, -1, 0

        public static void DrawLightCone(Vector3 source, Vector3 direction, Vector3 color, float range, float diffusion, float intensity)
        {
            Vector3 vector2 = new Vector3 { X = 0.0f, Y = -0.92f, Z = -0.39f };
            LightHelper.DrawLight(0, 0x2, 0x108, direction, vector2, source, color, 0.0f, 0, 9, range, 1.0f, diffusion, intensity, 1.0f, -1, -1, 0);
        }

        public static void DrawLight(Vector3 source, Vector3 direction, Vector3 color, float intensity, float range, float diffusion, bool dynamicShadows)
        {
            // 4139616000.0f = Increases intensity a little
            Vector3 vector2 = new Vector3(0.0f, 0f, -0.34f);
            int dynamicShadowSource = 0;
            if (dynamicShadows)
            {
                dynamicShadowSource = (((GTA.Ped)CPlayer.LocalPlayer.Ped).MemoryAddress);
            }

            LightHelper.DrawLight(0, 0x2, 0x114, direction, vector2, source, color, intensity, 4139616000.0f, 9, range, 1.0f, diffusion, 0.0f, 0.0f, -1, 0x3F, dynamicShadowSource);
        }

        public static void DrawLight(int a1, int a2, int a3, Vector3 a4, Vector3 a5, Vector3 a6, Vector3 a7, float a8, float a9, int a10, float range, float a12, float diffusion, float intensity, float a15, int a16, int a17, int a18)
        {
            AdvancedHookManaged.ManagedVector3 vector1 = new AdvancedHookManaged.ManagedVector3 { X = a4.X, Y = a4.Y, Z = a4.Z };
            AdvancedHookManaged.ManagedVector3 vector2 = new AdvancedHookManaged.ManagedVector3 { X = a5.X, Y = a5.Y, Z = a5.Z };
            AdvancedHookManaged.ManagedVector3 vector3 = new AdvancedHookManaged.ManagedVector3 { X = a6.X, Y = a6.Y, Z = a6.Z };
            AdvancedHookManaged.ManagedVector3 vector4 = new AdvancedHookManaged.ManagedVector3 { X = a7.X, Y = a7.Y, Z = a7.Z };
            AdvancedHookManaged.AGame.DrawLight(a1, a2, a3, vector1, vector2, vector3, vector4, a8, a9, a10, range, a12, diffusion, intensity, a15, a16, a17, a18);
        }
    }
}