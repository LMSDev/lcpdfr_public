namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using System;

    class AnonymousBehavior : Behavior
    {
        private readonly Func<EBehaviorState> action;

        private readonly Action abort;

        public AnonymousBehavior(Func<EBehaviorState> action)
        {
            this.action = action;
        }

        public AnonymousBehavior(Func<EBehaviorState> action, Action abort)
        {
            this.action = action;
            this.abort = abort;
        }

        public override void OnAbort()
        {
            if (this.abort != null)
            {
                this.abort.Invoke();
            }
        }

        public override EBehaviorState Run()
        {
            return this.action();
        }
    }
}