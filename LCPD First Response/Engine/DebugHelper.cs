namespace LCPD_First_Response.Engine
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class DebugHelper
    {
        public static string BooleanValueToString(string name, bool value)
        {
            if (value)
            {
                return name + ": True";
            }
            return name + ": False";
        }

        /// <summary>
        /// Returns a string saying if a certain object is null. E.g.: "Name: Null" or "Name: Exists"
        /// Supports CEntity
        /// </summary>
        /// <returns></returns>
        public static string ObjectNullToString(string name, object obj)
        {
            if (obj != null)
            {
                if (obj is CEntity)
                {
                    if ((obj as CEntity).Exists())
                    {
                        return name + ": Exists ingame";
                    }
                }

                return name + ": Exists";
            }
            return name + ": Null";
        }
    }
}
