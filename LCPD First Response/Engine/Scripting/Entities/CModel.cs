namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;

    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// Describes an in-game model, like a vehicle or ped model.
    /// </summary>
#pragma warning disable 660,661
    public class CModel
#pragma warning restore 660,661
    {
        /// <summary>
        /// The internal model.
        /// </summary>
        private GTA.Model internalModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="CModel"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public CModel(string name)
        {
            // Retrieve model info instance
            this.ModelInfo = Main.ModelManager.GetModelInfoByName(name);

            if (this.ModelInfo == null)
            {
                Log.Debug("CModel: Couldn't find model name in model info cache: " + name + ". Keep in mind that model flags don't work for models not registered in the model info cache.", "CModel");

                this.internalModel = new GTA.Model(name);
                this.ModelInfo = CModelInfo.None;
            }
            else
            {
                this.internalModel = new GTA.Model(this.ModelInfo.Hash);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CModel"/> class. This is only called by CModel(GTA.Model) and thus only used by the createPed and createVehicle callbacks.
        /// </summary>
        /// <param name="hash">
        /// The hash.
        /// </param>
        public CModel(uint hash)
        {
            // Retrieve model info instance
            this.ModelInfo = Main.ModelManager.GetModelInfoByHash(hash);

            // Model isn't registered, use default instance
            if (this.ModelInfo == null)
            {
                this.internalModel = new GTA.Model(hash);
                this.ModelInfo = CModelInfo.None;
            }
            else
            {
                this.internalModel = new GTA.Model(this.ModelInfo.Hash);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CModel"/> class.
        /// </summary>
        /// <param name="modelInfo">
        /// The model info.
        /// </param>
        public CModel(CModelInfo modelInfo)
        {
            this.ModelInfo = modelInfo;
            this.internalModel = new GTA.Model(this.ModelInfo.Hash);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CModel"/> class. The GTA.Model class is poorly designed and doesn't take care of values bigger than Int32.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        public CModel(GTA.Model model) : this(model.Handle)
        {
        }

        /// <summary>
        /// Gets the basic cop car model.
        /// </summary>
        public static CModel BasicCopCarModel
        {
            get { return new CModel(GTA.Model.BasicPoliceCarModel); }
        }

        /// <summary>
        /// Gets the basic cop model.
        /// </summary>
        public static CModel BasicCopModel
        {
            get { return new CModel(GTA.Model.BasicCopModel); }
        }

        /// <summary>
        /// Gets the current cop car model.
        /// </summary>
        public static CModel CurrentCopCarModel
        {
            get { return new CModel(GTA.Model.CurrentPoliceCarModel); }
        }

        /// <summary>
        /// Gets the current cop model.
        /// </summary>
        public static CModel CurrentCopModel
        {
            get { return new CModel(GTA.Model.CurrentCopModel); }
        }

        /// <summary>
        /// Gets a value indicating whether the current cop model is the alderney one.
        /// </summary>
        public static bool IsCurrentCopModelAlderneyModel
        {
            get
            {
                return GTA.Model.CurrentCopModel == 0xFAAD5B99;
            }
        }

        /// <summary>
        /// Gets the default cop helicopter model.
        /// </summary>
        public static CModel DefaultCopHelicopterModel
        {
            get { return new CModel("POLMAV"); }
        }

        /// <summary>
        /// Gets the default NOOSE helicopter model.
        /// </summary>
        public static CModel DefaultNooseHelicopterModel
        {
            get { return new CModel("ANNIHILATOR"); }
        }

        /// <summary>
        /// Gets a value indicating whether the model is a bike.
        /// </summary>
        public bool IsBike
        {
            get
            {
                return this.internalModel.isBike;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the model is a boat.
        /// </summary>
        public bool IsBoat
        {
            get
            {
                return this.internalModel.isBoat;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the model is a helicopter.
        /// </summary>
        public bool IsHelicopter
        {
            get
            {
                return this.internalModel.isHelicopter;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the model is loaded into memory.
        /// </summary>
        public bool IsInMemory
        {
            get { return Natives.HasModelLoaded(this.internalModel.Hash); }
        }

        /// <summary>
        /// Gets the model info.
        /// </summary>
        public CModelInfo ModelInfo { get; private set; }

        /// <summary>
        /// Gets a random cop car model.
        /// </summary>
        /// <returns>The model.</returns>
        public static CModel GetRandomCopCarModel()
        {
            // Get all models that are cop models and can be used for random dispatching
            var modelInfos = Main.ModelManager.GetModelInfosByFlags(EModelFlags.IsCopCar);

            return new CModel(Common.GetRandomCollectionValue<CModelInfo>(modelInfos));
        }

        /// <summary>
        /// Gets a random cop car model for the unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns>The model.</returns>
        public static CModel GetRandomCopCarModel(EUnitType unitType)
        {
            EModelFlags flags = EModelFlags.IsCopCar;
            if (unitType == EUnitType.Boat || unitType == EUnitType.NooseBoat)
            {
                flags = EModelFlags.IsCopBoat;
            }

            flags = SetUnitTypeFlags(flags, unitType);

            // Get all models that are cop models and can be used for random dispatching
            var modelInfos = Main.ModelManager.GetModelInfosByFlags(flags);

            return new CModel(Common.GetRandomCollectionValue<CModelInfo>(modelInfos));
        }

        /// <summary>
        /// Gets a random cop model.
        /// </summary>
        /// <returns>The model.</returns>
        public static CModel GetRandomCopModel()
        {
            // Get all models that are cop models and can be used for random dispatching
            var modelInfos = Main.ModelManager.GetModelInfosByFlags(EModelFlags.IsCop);

            return new CModel(Common.GetRandomCollectionValue<CModelInfo>(modelInfos));
        }

        /// <summary>
        /// Gets a random cop model that can be used for the unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns>The model.</returns>
        public static CModel GetRandomCopModel(EUnitType unitType)
        {
            EModelFlags flags = EModelFlags.IsCop;
            flags = SetUnitTypeFlags(flags, unitType);

            // Get all models that are cop models and can be used for random dispatching
            var modelInfos = Main.ModelManager.GetModelInfosByFlags(flags);

            return new CModel(Common.GetRandomCollectionValue<CModelInfo>(modelInfos));
        }

        /// <summary>
        /// Gets a random model based on <paramref name="modelFlags"/>.
        /// </summary>
        /// <param name="modelFlags">The model flags.</param>
        /// <returns>The model.</returns>
        public static CModel GetRandomModel(EModelFlags modelFlags)
        {
            var modelInfos = Main.ModelManager.GetModelInfosByFlags(modelFlags);
            return new CModel(Common.GetRandomCollectionValue<CModelInfo>(modelInfos));
        }


        /// <summary>
        /// Gets a random model based on models without <paramref name="modelFlags"/>.
        /// </summary>
        /// <param name="modelFlags">The model flags.</param>
        /// <returns>The model.</returns>
        public static CModel GetRandomModelExclude(EModelFlags modelFlags)
        {
            var modelInfos = Main.ModelManager.GetModelInfosByFlagsExclude(modelFlags);
            return new CModel(Common.GetRandomCollectionValue<CModelInfo>(modelInfos));
        }

        /// <summary>
        /// String to CModel.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// </returns>
        public static implicit operator CModel(string name)
        {
            return new CModel(name);
        }

        /// <summary>
        /// GTA.Model to CModel.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <returns>
        /// </returns>
        public static implicit operator GTA.Model(CModel model)
        {
            return model.internalModel;
        }

        /// <summary>
        /// Compares a model to a model name.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="model2">The second model.</param>
        /// <returns>True if equal, false if not.</returns>
        public static bool operator ==(CModel model, CModel model2)
        {
            if (model.Equals(null))
            {
                if (model2.Equals(null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (model2.Equals(null))
            {
                if (model.Equals(null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return model.internalModel == model2.internalModel;
        }

        /// <summary>
        /// Compares a model to a model name.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="model2">The second model.</param>
        /// <returns>False if equal, true if not.</returns>
        public static bool operator !=(CModel model, CModel model2)
        {
            return !(model == model2);
        }

        /// <summary>
        /// Compares a model to a model name.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="modelName">The model name.</param>
        /// <returns>True if equal, false if not.</returns>
        public static bool operator ==(CModel model, string modelName)
        {
            if (ReferenceEquals(model, null))
            {
                if (ReferenceEquals(modelName, null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (ReferenceEquals(modelName, null))
            {
                if (ReferenceEquals(model, null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return model.internalModel == modelName;
        }

        /// <summary>
        /// Compares a model to a model name.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="modelName">The model name.</param>
        /// <returns>False if equal, true if not.</returns>
        public static bool operator !=(CModel model, string modelName)
        {
            return !(model == modelName);
        }

        /// <summary>
        /// Gets the dimensions of the model.
        /// </summary>
        /// <returns>The dimension vector.</returns>
        public GTA.Vector3 GetDimensions()
        {
            return this.internalModel.GetDimensions();
        }

        /// <summary>
        /// Requests the model.
        /// </summary>
        /// <param name="instantReturn">If this is true, the function returns immediately without waiting for HAS_MODEL_LOADED to return true.</param>
        public void LoadIntoMemory(bool instantReturn)
        {
            Natives.RequestModel(this.internalModel.Hash);
            if (instantReturn)
            {
                return;
            }

            while (!this.IsInMemory)
            {
                GTA.Game.WaitInCurrentScript(0);
            }
        }

        /// <summary>
        /// Marks the model as no longer needed.
        /// </summary>
        public void NoLongerNeeded()
        {
            Natives.MarkModelAsNoLongerNeeded(this.internalModel.Hash);
        }

        /// <summary>
        /// Sets the model flags according to the unit type.
        /// </summary>
        /// <param name="flags">The flags.</param>
        /// <param name="unitType">The unit type.</param>
        /// <returns>Changed flags.</returns>
        private static EModelFlags SetUnitTypeFlags(EModelFlags flags, EUnitType unitType)
        {
            if (unitType == EUnitType.Helicopter)
            {
                flags = flags | EModelFlags.IsHelicopter;
            }

            if (unitType == EUnitType.Noose)
            {
                flags = flags | EModelFlags.IsNoose;
            }

            if (unitType == EUnitType.Police)
            {
                flags = flags | EModelFlags.IsPolice;
            }

            if (unitType == EUnitType.Boat)
            {
                flags = flags | EModelFlags.IsPolice;
            }

            if (unitType == EUnitType.NooseBoat)
            {
                flags = flags | EModelFlags.IsNoose;
            }
            return flags;
        }
    }
}
