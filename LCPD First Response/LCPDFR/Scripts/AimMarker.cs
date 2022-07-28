namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Windows.Forms;

    using AdvancedHookManaged;

    using GTA;

    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// The aim marker dot in the middle of the screen, assisting with targeting people and places.
    /// </summary>
    [ScriptInfo("AimMarker", true)]
    internal class AimMarker : GameScript
    {
        /// <summary>
        /// The range of the tracer.
        /// </summary>
        private const float TracerRange = 50f;

        /// <summary>
        /// The last position hit.
        /// </summary>
        private Vector3 lastPosition;

        /// <summary>
        /// The tracing instance.
        /// </summary>
        private Tracer tracer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AimMarker"/> class.
        /// </summary>
        public AimMarker()
        {
            this.tracer = new Tracer();
        }

        /// <summary>
        /// Gets the entity type.
        /// </summary>
        public int EntityType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the player hit a valid target (ped or vehicle).
        /// </summary>
        public bool HasTarget { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the aim marker is currently being drawn.
        /// </summary>
        public bool IsBeingDrawn { get; private set; }

        /// <summary>
        /// Gets the targeted entity. Allows upcasting.
        /// </summary>
        public CEntity TargetedEntity { get; private set; }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            this.IsBeingDrawn = false;
            this.HasTarget = false;

            if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.AimMarker))
            {
                this.IsBeingDrawn = true;

                this.PerformCheck();
                if (this.HasTarget)
                {
                    if (this.EntityType == 3 || this.EntityType == 4)
                    {
                        Engine.GUI.Gui.DrawRectPositionRelative(0.5f, 0.5f, 15, 15, System.Drawing.Color.FromArgb(100, 255, 0, 0));
                    }
                }
                else
                {
                    Engine.GUI.Gui.DrawRectPositionRelative(0.5f, 0.5f, 15, 15, System.Drawing.Color.FromArgb(100, 255, 255, 255));
                }
            }
        }

        /// <summary>
        /// Performs the ray cast and updates all variables.
        /// </summary>
        public void PerformCheck()
        {
            this.HasTarget = false;
            this.EntityType = -1;

            // Calculate positions manually
            Vector3 start = Game.CurrentCamera.Position;
            Vector3 direction = Game.CurrentCamera.Direction;
            direction.Normalize();
            Vector3 startPos = start + (direction * 0.3f);
            Vector3 endPos = start + (direction * TracerRange);

            uint hitEntity = 0;
            Vector3 hitPos = Vector3.Zero, normal = Vector3.Zero;

            // Check for hit
            if (this.tracer.DoTrace(startPos, endPos, ref hitEntity, ref hitPos, ref normal))
            {
                this.lastPosition = hitPos;

                // If entity hit isn't zero, we hit a valid target
                if (hitEntity != 0)
                {
                    // Use advanced hook to get the type of the target
                    int type = AGame.GetTypeOfEntity((int)hitEntity);
                    if (type == 3 || type == 4)
                    {
                        this.HasTarget = true;
                        this.EntityType = type;

                        // Store entity
                        if (type == 3)
                        {
                            this.TargetedEntity = this.GetVehicleFromEntity(hitEntity);
                        }
                        else if (type == 4)
                        {
                            this.TargetedEntity = this.GetPedFromEntity(hitEntity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the last hit position.
        /// </summary>
        /// <returns>The last position.</returns>
        public Vector3 GetLastHitPosition()
        {
            return this.lastPosition;
        }

        /// <summary>
        /// Returns the ped instance for an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The ped.</returns>
        private CPed GetPedFromEntity(uint entity)
        {
            foreach (CPed ped in CPed.GetPedsAround(float.MaxValue, EPedSearchCriteria.All, Vector3.Zero))
            {
                if (ped.Exists() && ((Ped)ped).MemoryAddress == entity)
                {
                    return ped;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the vehicle instance for an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The vehicle.</returns>
        private CVehicle GetVehicleFromEntity(uint entity)
        {
            foreach (CVehicle vehicle in Engine.Pools.VehiclePool.GetAll())
            {
                if (vehicle.Exists() && ((Vehicle)vehicle).MemoryAddress == entity)
                {
                    return vehicle;
                }
            }

            return null;
        }
    }
}