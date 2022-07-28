namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// Responsible for going on/off duty and handles the ped and vehicle model selection when going on duty.
    /// </summary>
    [ScriptInfo("GoOnDuty", true)]
    internal class GoOnDuty : GameScript
    {
        /// <summary>
        /// List of cop models.
        /// </summary>
        private SequencedList<CModel> copModels;

        /// <summary>
        /// List of cop car models.
        /// </summary>
        private SequencedList<CModel> copCarModels;

        /// <summary>
        /// The old model the player had before going on duty.
        /// </summary>
        private CModel oldModel;

        /// <summary>
        /// The old skin the player had before going on duty.
        /// </summary>
        private SkinTemplate oldSkin;

        /// <summary>
        /// The vehicle selected.
        /// </summary>
        private CVehicle selectedVehicle;

        /// <summary>
        /// The state of the model selection.
        /// </summary>
        private ESelectionState selectionState;
        
        /// <summary>
        /// The spawn position of the vehicle.
        /// </summary>
        private SpawnPoint vehicleSpawnPosition;

        /// <summary>
        /// The ped component selector for the player and partner model.
        /// </summary>
        private PedComponentSelector pedComponentSelector;

        /// <summary>
        /// The spawn position of the ped model.
        /// </summary>
        private Vector3 pedModelPosition; 

        /// <summary>
        /// Initializes a new instance of the <see cref="GoOnDuty"/> class.
        /// </summary>
        public GoOnDuty()
        {
            Main.PoliceDepartmentManager.PlayerEnteredLeftPD += new PoliceDepartment.PlayerEnteredLeftPDEventHandler(this.PoliceDepartmentManager_PlayerEnteredLeftPD);
        }

        /// <summary>
        /// Invoked when the ped model selection has been finished.
        /// </summary>
        public event Action PedModelSelectionFinished;

        /// <summary>
        /// Invoked when the player went on duty.
        /// </summary>
        public event Action PlayerWentOnDuty;

        /// <summary>
        /// Invoked when the player went off duty.
        /// </summary>
        public event Action PlayerWentOffDuty;

        /// <summary>
        /// The state of the model selection.
        /// </summary>
        private enum ESelectionState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None,

            /// <summary>
            /// Selecting a ped model.
            /// </summary>
            PedModel,

            /// <summary>
            /// Selecting the ped components.
            /// </summary>
            Component,

            /// <summary>
            /// Ped model has been selected, now the script is waiting for the vehicle model selection to be started.
            /// </summary>
            PedModelSelected,

            /// <summary>
            /// Selecting a vehicle model.
            /// </summary>
            VehicleModel,
        }

        /// <summary>
        /// Gets a value indicating whether the on duty script has focus, that is the model selection is active and listens for keys.
        /// </summary>
        public bool HasFocus { get; private set; }

        /// <summary>
        /// Gets a value indicating whether going on duty is in progress (e.g. selecting a model at the moment).
        /// </summary>
        public bool IsInProgress { get; private set; }

        /// <summary>
        /// Starts the on duty process, that is the model selection.
        /// </summary>
        /// <param name="vehicleSpawnPosition">The spawn position of the vehicle.</param>
        /// <param name="pedModelPosition">The spawn position of the ped model.</param>
        public void Start(SpawnPoint vehicleSpawnPosition, Vector3 pedModelPosition)
        {
            if (this.IsInProgress)
            {
                Log.Warning("Start: Already in progress", this);
                return;
            }

            this.vehicleSpawnPosition = vehicleSpawnPosition;
            this.pedModelPosition = pedModelPosition;
            this.HasFocus = true;
            this.IsInProgress = true;

            // Fade out, teleport player and setup everything
            Game.FadeScreenOut(3000, true);
            World.LoadEnvironmentNow(pedModelPosition);
            CPlayer.LocalPlayer.TeleportTo(pedModelPosition);
            CPlayer.LocalPlayer.Ped.Heading = 300;
            CPlayer.LocalPlayer.Ped.ClearProps();
            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = false;
            CPlayer.LocalPlayer.CanControlCharacter = false;
            
            // Save data
            this.oldModel = CPlayer.LocalPlayer.Ped.Model;
            this.oldSkin = CPlayer.LocalPlayer.Ped.Skin;

            // Prepare model selection
            this.selectionState = ESelectionState.PedModel; 
            this.copModels = new SequencedList<CModel>();
            CModelInfo[] modelInfos = Engine.Main.ModelManager.GetModelInfosByFlags(EModelFlags.IsCop);
            foreach (CModelInfo modelInfo in modelInfos)
            {
                this.copModels.Add(new CModel(modelInfo.Name));
            }

            this.copCarModels = new SequencedList<CModel>();
            modelInfos = Engine.Main.ModelManager.GetModelInfosByFlags(EModelFlags.IsCopCar);

            // Hardcoded order
            string[] forcedModelOrder = { "POLICE", "POLICE2", "POLICE3", "POLICE4", "POLICEB", "FBI", "NOOSE", "POLPATRIOT", "PSTOCKADE", "NSTOCKADE" };
            foreach (string s in forcedModelOrder)
            {
                foreach (CModelInfo modelInfo in modelInfos)
                {
                    // Add model by string
                    if (s == modelInfo.Name && !modelInfo.ModelFlags.HasFlag(EModelFlags.IsHelicopter))
                    {
                        this.copCarModels.Add(new CModel(modelInfo.Name));
                    }
                }
            }

            // Change to first model and then fade in
            this.ChangePlayerModel(this.copModels.Next());

            // Fading in might prevent the helpbox in ChangePlayerModel to pop up, so we call it here again, but delayed
            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("GOONDUTY_COP_MODEL")); }, 3200);
            Game.CurrentCamera.Heading = 120;
            Game.FadeScreenIn(3000, true);
            Game.CurrentCamera.Heading = 120;
        }

        /// <summary>
        /// Goes off duty.
        /// </summary>
        /// <param name="noFading">
        /// Whether the screen shouldn't be faded out.
        /// </param>
        public void GoOffDuty(bool noFading = false)
        {
            if (!noFading)
            {
                Game.FadeScreenOut(3000, true);
            }

            if (this.oldModel != null && this.oldSkin != null)
            {
                CPlayer.LocalPlayer.Model = this.oldModel;
                CPlayer.LocalPlayer.Skin.Template = this.oldSkin;
            }

            Globals.IsOnDuty = false;
            if (this.selectedVehicle != null && this.selectedVehicle.Exists())
            {
                this.selectedVehicle.NoLongerNeeded();
            }

            if (!noFading)
            {
                Game.FadeScreenIn(3000, true);
            }

            TextHelper.PrintText(CultureHelper.GetText("GOONDUTY_END"), 5000);
            if (this.PlayerWentOffDuty != null)
            {
                this.PlayerWentOffDuty();
            }
        }

        /// <summary>
        /// Forces on duty.
        /// </summary>
        public void ForceOnDuty()
        {
            if (this.PedModelSelectionFinished != null)
            {
                this.PedModelSelectionFinished.Invoke();
            }

            // Save data
            this.oldModel = CPlayer.LocalPlayer.Ped.Model;
            this.oldSkin = CPlayer.LocalPlayer.Ped.Skin;

            CPlayer.LocalPlayer.Ped.Armor = 100;
            CPlayer.LocalPlayer.Ped.Weapons.Glock.Ammo = 170;
            CPlayer.LocalPlayer.Ped.Weapons.BaseballBat.Ammo = 1;
            CPlayer.LocalPlayer.Ped.Weapons.Select(Weapon.Unarmed);

            // Player model in multiplayer is a bad idea.
            if (Engine.Main.NetworkManager.IsNetworkSession)
            {
                Log.Debug("ForceOnDuty: Using multiplayer model", this);
                CPlayer.LocalPlayer.Model = "M_Y_MULTIPLAYER";
            }
            else
            {
                CPlayer.LocalPlayer.Model = "PLAYER";
            }

            if (CModel.IsCurrentCopModelAlderneyModel)
            {
                CPlayer.LocalPlayer.Model = "M_Y_STROOPER";
            }
            else
            {
                CPlayer.LocalPlayer.Model = "M_Y_COP";
                if (Game.CurrentEpisode == GameEpisode.TBOGT) CPlayer.LocalPlayer.Skin.Component.Head.ChangeIfValid(2, 0);
                CPlayer.LocalPlayer.Skin.Component.UpperBody.ChangeIfValid(0, 0);
                CPlayer.LocalPlayer.Skin.Component.LowerBody.ChangeIfValid(0, 0);
            }

            AdjustPlayerModel();

            // Make friend with cops
            CPlayer.LocalPlayer.Ped.ChangeRelationship(RelationshipGroup.Cop, Relationship.Companion);
            CPlayer.LocalPlayer.Ped.RelationshipGroup = RelationshipGroup.Cop;

            // Unlock all islands so the player doesn't get a wanted level
            World.UnlockAllIslands();

            Globals.IsOnDuty = true;
            if (this.PlayerWentOnDuty != null)
            {
                this.PlayerWentOnDuty();
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            switch (this.selectionState)
            {
                case ESelectionState.PedModel:
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.NextCopModel))
                    {
                        this.ChangePlayerModel(this.copModels.Next());
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModel))
                    {
                        this.ChangePlayerModel(this.copModels.Previous());
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.RandomizeCopModel))
                    {
                        CPlayer.LocalPlayer.Ped.RandomizeOutfit();
                    }

                    // Confirm model and start selection of components
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ConfirmCopModel))
                    {
                        TextHelper.ClearHelpbox();

                        this.pedComponentSelector = new PedComponentSelector(CPlayer.LocalPlayer.Ped);
                        this.pedComponentSelector.SelectionAborted += this.pedComponentSelector_SelectionAborted;
                        this.pedComponentSelector.SelectionFinished += this.pedComponentSelector_SelectionFinished;
                        Main.ScriptManager.RegisterScriptInstance(this.pedComponentSelector);
                        this.selectionState = ESelectionState.Component;
                    }

                    break;

                case ESelectionState.VehicleModel:
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.NextCopCarModel))
                    {
                        this.ChangeVehicleModel(this.copCarModels.Next());
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopCarModel))
                    {
                        this.ChangeVehicleModel(this.copCarModels.Previous());
                    }

                    // Confirm model
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ConfirmCopCarModel))
                    {
                        TextHelper.ClearHelpbox();
                        CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = true;
                        CPlayer.LocalPlayer.CanControlCharacter = true;
                        this.selectionState = ESelectionState.None;
                        this.HasFocus = false;
                        this.IsInProgress = false;

                        // Set global onduty flag to true, player is ready now!
                        Globals.IsOnDuty = true;
                        if (this.PlayerWentOnDuty != null)
                        {
                            this.PlayerWentOnDuty();
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Called when the selection has been aborted.
        /// </summary>
        private void pedComponentSelector_SelectionAborted()
        {
            this.selectionState = ESelectionState.PedModel;
            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("GOONDUTY_COP_MODEL"));

            this.pedComponentSelector.SelectionAborted -= this.pedComponentSelector_SelectionAborted;
            this.pedComponentSelector.SelectionFinished -= this.pedComponentSelector_SelectionFinished;
        }

        /// <summary>
        /// Called when the selection has finished.
        /// </summary>
        private void pedComponentSelector_SelectionFinished()
        {
            this.pedComponentSelector.SelectionAborted -= this.pedComponentSelector_SelectionAborted;
            this.pedComponentSelector.SelectionFinished -= this.pedComponentSelector_SelectionFinished;

            this.HasFocus = false;
            this.selectionState = ESelectionState.PedModelSelected;
            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = true;

            CPlayer.LocalPlayer.Ped.Armor = 100;
            CPlayer.LocalPlayer.Ped.Weapons.Glock.Ammo = 170;
            CPlayer.LocalPlayer.Ped.Weapons.BaseballBat.Ammo = 1;
            CPlayer.LocalPlayer.Ped.Weapons.Select(Weapon.Unarmed);

            // Make friend with cops
            CPlayer.LocalPlayer.Ped.ChangeRelationship(RelationshipGroup.Cop, Relationship.Companion);
            CPlayer.LocalPlayer.Ped.RelationshipGroup = RelationshipGroup.Cop;

            // Add text to news scrollbar
            TextHelper.AddStringToNewsScrollbar(string.Format(CultureHelper.GetText("GOONDUTY_NEWS_SCROLLBAR_WELCOME"), Main.Version));
            TextHelper.AddStringToNewsScrollbar(CultureHelper.GetText("LCPDMAIN_DEVELOPED_BY") + " -- ");
            TextHelper.AddStringToNewsScrollbar(CultureHelper.GetText("GOONDUTY_NEWS_SCROLLBAR_WEBSITE") + " -- ");
            TextHelper.ClearHelpbox();

            if (this.PedModelSelectionFinished != null)
            {
                this.PedModelSelectionFinished.Invoke();
            }
        }

        /// <summary>
        /// Changes the player model to <paramref name="model"/>.
        /// </summary>
        /// <param name="model">The model.</param>
        private void ChangePlayerModel(CModel model)
        {
            CPlayer.LocalPlayer.Model = model;
            CPlayer.LocalPlayer.Ped.RandomizeOutfit();
            AdjustPlayerModel();
            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("GOONDUTY_COP_MODEL"));

            CPlayer.LocalPlayer.Ped.Animation.Play(new AnimationSet("clothing"), "brushoff_suit_stand", 4.0f);
        }

        /// <summary>
        ///  Depending on the player model, some things such as voice or component variation are changed.
        /// </summary>
        public static void AdjustPlayerModel()
        {
            // M_Y_Clubfit needs specific jacket to look good
            if (CPlayer.LocalPlayer.Ped.Model == "M_Y_CLUBFIT")
            {
                CPlayer.LocalPlayer.Ped.SetComponentVariation(1, 1, 0);

                // Needs cop voice too
                int modelIndex = CPlayer.LocalPlayer.Ped.Skin.Component.Head.ModelIndex;
                string voiceString = modelIndex == 0 ? "M_Y_COP_BLACK" : "M_Y_COP_HISPANIC";

                CPlayer.LocalPlayer.Voice = voiceString;
                CPlayer.LocalPlayer.AnimGroup = "move_m@bness_b";
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_Y_Nhelipilot")
            {
                // Needs swat voice
                CPlayer.LocalPlayer.Voice = "M_Y_SWAT_WHITE";
                CPlayer.LocalPlayer.AnimGroup = "move_m@bness_a";
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_Y_SWAT")
            {
                // Needs swat voice
                CPlayer.LocalPlayer.Voice = "M_Y_SWAT_WHITE";
                CPlayer.LocalPlayer.AnimGroup = "move_m@swat";
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_Y_STROOPER")
            {
                // Needs state trooper voice
                int voiceRandom = Common.GetRandomValue(0, 2);

                if (voiceRandom == 0)
                {
                    CPlayer.LocalPlayer.Voice = "M_Y_STROOPER_WHITE_01";
                }
                else
                {
                    CPlayer.LocalPlayer.Voice = "M_Y_COP_WHITE";
                }

                CPlayer.LocalPlayer.AnimGroup = "move_cop";
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_Y_COP_TRAFFIC")
            {
                int voiceRandom = Common.GetRandomValue(0, 3);
                string voiceString;
                if (voiceRandom == 0)
                {
                    voiceString = "M_Y_COP_TRAFFIC_HISPANIC";
                }
                else if (voiceRandom == 1)
                {
                    voiceString = "M_Y_COP_TRAFFIC_WHITE";
                }
                else
                {
                    voiceString = "M_Y_COP_TRAFFIC_BLACK";
                }

                CPlayer.LocalPlayer.AnimGroup = "move_cop";
                CPlayer.LocalPlayer.Voice = voiceString;
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_M_FATCOP_01")
            {
                int modelIndex = CPlayer.LocalPlayer.Ped.Skin.Component.Head.ModelIndex;
                string voiceString = modelIndex == 0 ? "M_M_FATCOP_01_WHITE" : "M_M_FATCOP_01_BLACK";
                CPlayer.LocalPlayer.AnimGroup = "move_m@fat";
                CPlayer.LocalPlayer.Voice = voiceString;
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_Y_COP")
            {
                int modelIndex = CPlayer.LocalPlayer.Ped.Skin.Component.Head.ModelIndex;
                string voiceString;
                if (modelIndex == 0)
                {
                    voiceString = Engine.Main.IsTbogt ? "M_Y_COP_HISPANIC" : "M_Y_COP_BLACK";
                }
                else if (modelIndex == 1)
                {
                    voiceString = Engine.Main.IsTbogt ? "M_Y_COP_TRAFFIC_HISPANIC" : "M_Y_COP_HISPANIC";
                }
                else if (modelIndex == 2)
                {
                    int voiceRandom = Common.GetRandomValue(0, 2);
                    voiceString = voiceRandom == 0 ? "M_Y_COP_WHITE" : "M_Y_COP_WHITE_02";
                }
                else
                {
                    int voiceRandom = Common.GetRandomValue(0, 2);
                    voiceString = voiceRandom == 0 ? "M_Y_COP_TRAFFIC_WHITE" : "M_Y_COP_BLACK_02";
                }

                CPlayer.LocalPlayer.Voice = voiceString;
                CPlayer.LocalPlayer.AnimGroup = "move_cop";
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_M_FBI" || CPlayer.LocalPlayer.Ped.Model == "M_Y_CIADLC_01" || CPlayer.LocalPlayer.Ped.Model == "M_Y_CIADLC_02")
            {
                CPlayer.LocalPlayer.Voice = "M_Y_FIB";
                CPlayer.LocalPlayer.AnimGroup = "move_m@eddie";
            }
            else if (CPlayer.LocalPlayer.Ped.Model == "M_M_ARMOURED")
            {
                int voiceRandom = Common.GetRandomValue(0, 2);
                string voiceString = voiceRandom == 0 ? "M_Y_COP_TRAFFIC_HISPANIC" : "M_Y_COP_TRAFFIC_BLACK";
                CPlayer.LocalPlayer.Voice = voiceString;
                CPlayer.LocalPlayer.AnimGroup = "move_cop";
            }

            CPlayer.LocalPlayer.Ped.FixCopClothing();
            CPlayer.LocalPlayer.Ped.VoiceData.Reset();
        }

        /// <summary>
        /// Changes the vehicle model to <paramref name="model"/>.
        /// </summary>
        /// <param name="model">The vehicle model.</param>
        private void ChangeVehicleModel(CModel model)
        {
            if (this.selectedVehicle != null && this.selectedVehicle.Exists())
            {
                this.selectedVehicle.Delete();
            }

            // Load model
            model.LoadIntoMemory(false);
            this.selectedVehicle = new CVehicle(model, this.vehicleSpawnPosition.Position, EVehicleGroup.Normal);

            if (this.selectedVehicle.Exists())
            {
                this.selectedVehicle.PlaceOnGroundProperly();
                this.selectedVehicle.Heading = this.vehicleSpawnPosition.Heading;
                CPlayer.LocalPlayer.Ped.WarpIntoVehicle(this.selectedVehicle, VehicleSeat.Driver);
                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("GOONDUTY_CAR_MODEL"));
            }
        }

        /// <summary>
        /// Called when the player has either entered or left a pd.
        /// </summary>
        /// <param name="policeDepartment">The police department.</param>
        /// <param name="entered">True if entered, false if not.</param>
        private void PoliceDepartmentManager_PlayerEnteredLeftPD(PoliceDepartment policeDepartment, bool entered)
        {
            // If we haven't finished yet, start vehicle selection
            if (this.IsInProgress)
            {
                CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = false;
                CPlayer.LocalPlayer.CanControlCharacter = false;
                this.selectionState = ESelectionState.VehicleModel;
                this.HasFocus = true;

                // Change to first model
                this.ChangeVehicleModel(this.copCarModels.Next());
            }
        }
    }
}
