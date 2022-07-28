namespace LCPD_First_Response.Engine.Scripting.Plugins
{
    using System;
    using System.Collections.Generic;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;

    /// <summary>
    /// Content manager options.
    /// </summary>
    [Flags]
    internal enum EContentManagerOptions
    {
        /// <summary>
        /// No options.
        /// </summary>
        None = 0x1,

        /// <summary>
        /// Deletes the entity instead of freeing it.
        /// </summary>
        DeleteInsteadOfFree = 0x2,

        /// <summary>
        /// Doesn't delete the blip when freeing.
        /// </summary>
        DontDeleteBlip = 0x4,

        /// <summary>
        /// Kills the entity before freeing it.
        /// </summary>
        KillBeforeFree = 0x8,
    }

    /// <summary>
    /// Manages in-game entities.
    /// </summary>
    internal class ContentManager
    {
        /// <summary>
        /// All assigned entities.
        /// </summary>
        private List<CEntity> entities;

        /// <summary>
        /// All loaded models.
        /// </summary>
        private List<CModel> loadedModels;

        /// <summary>
        /// All started scenarios.
        /// </summary>
        private List<Scenario> scenarios;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentManager"/> class.
        /// </summary>
        public ContentManager()
        {
            this.entities = new List<CEntity>();
            this.loadedModels = new List<CModel>();
            this.scenarios = new List<Scenario>();
        }

        /// <summary>
        /// The delegate for entities being disposed.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="onEnd">True, if the content manager is finishing, false if the entity has been disposed due to its settings.</param>
        public delegate void OnEntityBeingDisposedEventHandler(CEntity entity, bool onEnd);

        /// <summary>
        /// Fired when an entity is being disposed.
        /// </summary>
        public event OnEntityBeingDisposedEventHandler OnEntityBeingDisposed;

        /// <summary>
        /// Gets the default content manager, that will keep track of all objects, that have no content manager to prevent leaking objects on script reload.
        /// </summary>
        public static ContentManager DefaultContentManager { get; private set; }

        /// <summary>
        /// Adds <paramref name="ped"/> to the content manager.
        /// </summary>
        /// <param name="ped">The ped</param>
        public void AddPed(CPed ped)
        {
            this.AddPed(ped, -1, EContentManagerOptions.None);
        }

        /// <summary>
        /// Adds <paramref name="ped"/> to the content manager.
        /// </summary>
        /// <param name="ped">The ped</param>
        /// <param name="distanceToFree">The distance to free.</param>
        public void AddPed(CPed ped, float distanceToFree)
        {
            this.AddPed(ped, distanceToFree, EContentManagerOptions.None);
        }

        /// <summary>
        /// Adds <paramref name="ped"/> to the content manager.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="contentManagerOptions">The content manager options.</param>
        public void AddPed(CPed ped, EContentManagerOptions contentManagerOptions)
        {
            this.AddPed(ped, -1, contentManagerOptions);
        }

        /// <summary> 
        /// Adds <paramref name="ped"/> to the content manager.
        /// </summary><param name="ped">The ped.</param>
        /// <param name="distanceToFree">The distance to free.</param>
        /// <param name="contentManagerOptions">The content manager options.</param>
        public void AddPed(CPed ped, float distanceToFree, EContentManagerOptions contentManagerOptions)
        {
            // If the entity already has a content manager, remove it
            if (ped.ContentManager != null)
            {
                if (ped.ContentManager != DefaultContentManager)
                {
                    if (ped.ContentManager == this)
                    {
                        Log.Warning("AddPed: Ped has been added to the same content manager twice", "ContentManager");
                    }
                    else
                    {
                        Log.Warning("AddPed: Ped already has a non-default content manager. It's recommended to properly reset the content manager, before changing", "ContentManager");
                    }
                }

                ped.ContentManager.RemovePed(ped);
            }

            bool distance = true;
            if (distanceToFree == -1)
            {
                distance = false;
            }

            // Change content manager with proper settings
            ped.ChangeContentManager(
                this, 
                new ContentManagerOptions(
                    distance, 
                    distanceToFree,
                    null,
                    contentManagerOptions.HasFlag(EContentManagerOptions.DeleteInsteadOfFree),
                    contentManagerOptions.HasFlag(EContentManagerOptions.DontDeleteBlip),
                    contentManagerOptions.HasFlag(EContentManagerOptions.KillBeforeFree)));
            this.entities.Add(ped);
        }

        /// <summary>
        /// Adds <paramref name="scenario"/> to the content manager to ensure it is properly aborted on end.
        /// </summary>
        /// <param name="scenario">The scenario.</param>
        public void AddScenario(Scenario scenario)
        {
            this.scenarios.Add(scenario);
        }

        /// <summary>
        /// Adds <paramref name="vehicle"/> to the content manager.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        public void AddVehicle(CVehicle vehicle)
        {
            this.AddVehicle(vehicle, -1, EContentManagerOptions.None);
        }

        /// <summary>
        /// Adds <paramref name="vehicle"/> to the content manager.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="distanceToFree">The distance to free.</param>
        public void AddVehicle(CVehicle vehicle, float distanceToFree)
        {
            this.AddVehicle(vehicle, distanceToFree, EContentManagerOptions.None);
        }

        /// <summary>
        /// Adds <paramref name="vehicle"/> to the content manager.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="contentManagerOptions">The content manager options.</param>
        public void AddVehicle(CVehicle vehicle, EContentManagerOptions contentManagerOptions)
        {
            this.AddVehicle(vehicle, -1, contentManagerOptions);
        }

        /// <summary>
        /// Adds <paramref name="vehicle"/> to the content manager.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="distanceToFree">The distance to free.</param>
        /// <param name="contentManagerOptions">The content manager options.</param>
        public void AddVehicle(CVehicle vehicle, float distanceToFree, EContentManagerOptions contentManagerOptions)
        {
            // If the entity already has a content manager, remove it
            if (vehicle.ContentManager != null)
            {
                if (vehicle.ContentManager != DefaultContentManager)
                {
                    if (vehicle.ContentManager == this)
                    {
                        Log.Warning("AddVehicle: Vehicle has been added to the same content manager twice", "ContentManager");
                    }
                    else
                    {
                        Log.Warning("AddVehicle: Vehicle already has a non-default content manager. It's recommended to properly reset the content manager, before changing", "ContentManager");
                    }
                }

                vehicle.ContentManager.RemoveVehicle(vehicle);
            }

            bool distance = true;
            if (distanceToFree == -1)
            {
                distance = false;
            }

            // Change content manager with proper settings
            vehicle.ChangeContentManager(
                this, 
                new ContentManagerOptions(
                    distance, 
                    distanceToFree,
                    null,
                    contentManagerOptions.HasFlag(EContentManagerOptions.DeleteInsteadOfFree),
                    contentManagerOptions.HasFlag(EContentManagerOptions.DontDeleteBlip),
                    contentManagerOptions.HasFlag(EContentManagerOptions.KillBeforeFree)));
            this.entities.Add(vehicle);
        }

        /// <summary>
        /// Preloads the given model.
        /// </summary>
        /// <param name="model">Model to preload.</param>
        public void PreloadModel(CModel model)
        {
            model.LoadIntoMemory(true);
            this.loadedModels.Add(model);
        }

        /// <summary>
        /// Preloads the given model.
        /// </summary>
        /// <param name="model">The model to preload.</param>
        /// <param name="waitUntilLoaded">If true, will wait until model has been loaded.</param>
        public void PreloadModel(CModel model, bool waitUntilLoaded)
        {
            model.LoadIntoMemory(!waitUntilLoaded);
            this.loadedModels.Add(model);
        }

        /// <summary>
        /// Processes all content manager logic.
        /// </summary>
        public void Process()
        {
            foreach (CEntity entity in this.entities)
            {
                if (entity.ContentManagerOptions == null)
                {
                    Log.Warning("Process: Found entity without options set", "ContentManager");
                    return;
                }

                if (!entity.Exists())
                {
                    entity.ContentManagerOptions.HasBeenProcessed = true;
                    return;
                }

                if (entity.ContentManagerOptions.DistanceCheck && !entity.ContentManagerOptions.HasBeenProcessed)
                {
                    float distanceToPlayer = float.MaxValue;

                    if (entity.EntityType == EEntityType.Ped)
                    {
                        distanceToPlayer = ((CPed)entity).Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position);
                    }

                    if (entity.EntityType == EEntityType.Vehicle)
                    {
                        distanceToPlayer = ((CVehicle)entity).Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position);
                    }

                    if (distanceToPlayer > entity.ContentManagerOptions.Distance)
                    {
                        this.ProcessEntity(entity, true);
                    }
                }
            }
        }

        /// <summary>
        /// Releases all in-game entities.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (CEntity entity in this.entities)
            {
                if (entity.Exists())
                {
                    this.ProcessEntity(entity, false);
                    entity.ResetContentManager();
                }
            }

            this.entities.Clear();

            // Abort all scenarios
            foreach (Scenario scenario in this.scenarios)
            {
                scenario.MakeAbortable();
            }

            this.scenarios.Clear();

            // Free models
            foreach (CModel loadedModel in this.loadedModels)
            {
                loadedModel.NoLongerNeeded();
            }
        }

        /// <summary>
        /// Releases all in-game entities.
        /// </summary>
        /// <param name="delete">If entities should be deleted instead of being marked as no longer needed. Overrides the content manager options.</param>
        public void ReleaseAll(bool delete)
        {
            foreach (CEntity entity in this.entities)
            {
                if (entity.Exists())
                {
                    if (delete)
                    {
                        entity.DeleteEntity();
                    }
                    else
                    {
                        entity.Free();
                    }
                }
            }

            this.entities.Clear();

            // Abort all scenarios
            foreach (Scenario scenario in this.scenarios)
            {
                scenario.MakeAbortable();
            }

            this.scenarios.Clear();

            // Free models
            foreach (CModel loadedModel in this.loadedModels)
            {
                loadedModel.NoLongerNeeded();
            }
        }

        /// <summary>
        /// Removes the ped from the content manager.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public void RemovePed(CPed ped)
        {
            if (this.entities.Contains(ped))
            {
                this.entities.Remove(ped);
                ped.ChangeContentManager(null, null);
            }
        }

        /// <summary>
        /// Removes <paramref name="scenario"/> from the content manager.
        /// </summary>
        /// <param name="scenario">The scenario.</param>
        public void RemoveScenario(Scenario scenario)
        {
            this.scenarios.Remove(scenario);
        }

        /// <summary>
        /// Removes the vehicle from the content manager.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        public void RemoveVehicle(CVehicle vehicle)
        {
            if (this.entities.Contains(vehicle))
            {
                this.entities.Remove(vehicle);
                vehicle.ChangeContentManager(null, null);
            }
        }

        /// <summary>
        /// Processes the entity, depending on the options, so free, delete or kill it.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="calledFromProcessLoop">Whether the function was called from the process loop.</param>
        private void ProcessEntity(CEntity entity, bool calledFromProcessLoop)
        {
            if (entity.ContentManagerOptions.HasBeenProcessed)
            {
                return;
            }

            if (this.OnEntityBeingDisposed != null)
            {
                this.OnEntityBeingDisposed(entity, calledFromProcessLoop);
            }

            if (!entity.ContentManagerOptions.DontDeleteBlip)
            {
                if (entity.EntityType == EEntityType.Ped)
                {
                    if (((CPed)entity).HasBlip)
                    {
                        ((CPed)entity).DeleteBlip();
                    }
                }

                if (entity.EntityType == EEntityType.Vehicle)
                {
                    if (((CVehicle)entity).HasBlip)
                    {
                        ((CVehicle)entity).DeleteBlip();
                    }
                }
            }

            if (entity.ContentManagerOptions.Delete)
            {
                entity.DeleteEntity();
            }
            else if (entity.ContentManagerOptions.Kill)
            {
                if (entity.EntityType == EEntityType.Ped)
                {
                    ((CPed)entity).Die();
                }

                if (entity.EntityType == EEntityType.Vehicle)
                {
                    ((CVehicle)entity).Explode();
                }

                entity.Free();
            }
            else
            {
                entity.Free();
            }

            // Also clear all tasks for peds
            if (entity.EntityType == EEntityType.Ped)
            {
                ((CPed)entity).Intelligence.TaskManager.ClearTasks();
            }

            entity.ContentManagerOptions.HasBeenProcessed = true;
        }

        /// <summary>
        /// Initializes static members of the <see cref="ContentManager"/> class.
        /// </summary>
        internal static void Initialize()
        {
            DefaultContentManager = new ContentManager();
        }
    }
}
