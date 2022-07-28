namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    enum EBehaviorState
    {
        None,
        Success,
        Failed,
        Running,
    }

    interface IInternalBehavior
    {
        EBehaviorState InternalRun();
    }

    abstract class Behavior : IInternalBehavior
    {
        protected Behavior()
        {
            this.BehaviorState = EBehaviorState.Running;
        }

        public EBehaviorState BehaviorState { get; private set; }

        public void Abort()
        {
            this.BehaviorState = EBehaviorState.Failed;
            this.OnAbort();
        }

        public abstract void OnAbort();

        public abstract EBehaviorState Run();

        EBehaviorState IInternalBehavior.InternalRun()
        {
            this.BehaviorState = this.Run();
            return this.BehaviorState;
        }
    }
}