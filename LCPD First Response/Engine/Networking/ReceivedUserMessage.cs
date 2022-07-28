namespace LCPD_First_Response.Engine.Networking
{
    using global::LCPDFR.Networking.User;

    using GTA;

    public static class ReceivedUserMessageExtension
    {
        public static Vector2 ReadVector2(this ReceivedUserMessage userMessage)
        {
            float x = userMessage.ReadFloat();
            float y = userMessage.ReadFloat();
            return new Vector2(x, y);
        }

        public static Vector3 ReadVector3(this ReceivedUserMessage userMessage)
        {
            float x = userMessage.ReadFloat();
            float y = userMessage.ReadFloat();
            float z = userMessage.ReadFloat();
            return new Vector3(x, y, z);
        }

        public static SpawnPoint ReadSpawnPoint(this ReceivedUserMessage userMessage)
        {
            float heading = userMessage.ReadFloat();
            Vector3 vector3 = userMessage.ReadVector3();
            return new SpawnPoint(heading, vector3);
        }
    }
}