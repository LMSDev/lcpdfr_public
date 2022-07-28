namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.LCPDFR.API;

    /// <summary>
    /// Advanced BaseScript with specific functions when designing scripts that will make use of in-game entities, e.g. provides a content manager, that ensures
    /// all entities created by the script will be properly released when the script is shutdown.
    /// </summary>
    [ScriptInfo("GameScript", false)]
    public abstract class GameScript : BaseScript, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameScript"/> class.
        /// </summary>
        protected GameScript()
        {
            this.ContentManager = new ContentManager();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="GameScript"/> class. 
        /// </summary>
        ~GameScript()
        {
            // Release all entities associated
            this.ContentManager.ReleaseAll();
        }

        /// <summary>
        /// Gets the content manager.
        /// </summary>
        internal ContentManager ContentManager { get; private set; }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            // Release all entities associated
            this.ContentManager.ReleaseAll();
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            this.ContentManager.Process();
        }

        // Bad cross reference to API in LCPDFR part of the code which actually shouldn't belong into the core engine.
        // However I didn't want to create yet another script class for LCPDFR API where this would fit better, maybe we should consider
        // doing so in the future, though.

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        public virtual void PedLeftScript(LPed ped)
        {
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// We inherit explicitly here, because we don't want this to be public since that would require setting <see cref="CPed"/> to be public as well.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        void IPedController.PedHasLeft(CPed ped)
        {
            // Invoke function
            this.PedLeftScript(ped);
            this.PedLeftScript(new LPed(ped));
        }

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        internal virtual void PedLeftScript(CPed ped)
        {
        }
    }
}