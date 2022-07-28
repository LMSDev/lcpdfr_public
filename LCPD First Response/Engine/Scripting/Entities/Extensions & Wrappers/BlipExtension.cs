namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using GTA;

    public static class BlipExtension
    {
        public static BlipDisplay GetBlipDisplay(this GTA.Blip blip)
        {
            int ret = GTA.Native.Function.Call<int>("GET_BLIP_INFO_ID_DISPLAY", blip.pHandle);
            return (BlipDisplay)ret;
        }
    }
}