namespace LCPD_First_Response.Engine.Networking
{
    using global::LCPDFR.Networking;

    public static class DynamicDataExtension
    {
        public static void Write(this DynamicData dynamicData, GTA.Vector2 value)
        {
            dynamicData.Write(value.X);
            dynamicData.Write(value.Y);
        }

        public static void Write(this DynamicData dynamicData, GTA.Vector3 value)
        {
            dynamicData.Write(value.X);
            dynamicData.Write(value.Y);
            dynamicData.Write(value.Z);
        }

        public static void Write(this DynamicData dynamicData, SpawnPoint value)
        {
            dynamicData.Write(value.Heading);
            dynamicData.Write(value.Position);
        }
    }
}