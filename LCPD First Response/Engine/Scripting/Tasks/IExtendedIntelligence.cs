namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    interface IExtendedIntelligence
    {
        void HasBeenKilled();

        void Initialize();

        void Process();

        void Shutdown();
    }
}