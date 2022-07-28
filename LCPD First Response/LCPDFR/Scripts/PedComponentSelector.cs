namespace LCPD_First_Response.LCPDFR.Scripts
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// Allows to customize a ped. No camera changes or player control done here.
    /// </summary>
    [ScriptInfo("PedComponentSelector", true)]
    internal class PedComponentSelector : GameScript
    {
        /// <summary>
        /// The current component of the player we are changing.
        /// </summary>
        private PedComponent currentComponent;

        /// <summary>
        /// Timer to delay execution.
        /// </summary>
        private NonAutomaticTimer delayTimer;

        /// <summary>
        /// Whether the user just went one step back during the component selection and is now changing the component before.
        /// </summary>
        private bool justWentBackInComponentOrder;

        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The state of the model selection.
        /// </summary>
        private ESelectionState selectionState;

        /// <summary>
        /// Initializes a new instance of the <see cref="PedComponentSelector"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public PedComponentSelector(CPed ped)
        {
            this.currentComponent = PedComponent.Head;
            this.selectionState = ESelectionState.PedHeadSelection;
            this.ped = ped;
            this.delayTimer = new NonAutomaticTimer(100, ETimerOptions.OneTimeReturnTrue);
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        public delegate void SelectionGenericEventHandler();

        /// <summary>
        /// Invoked when selection has been aborted, so user went back in first step.
        /// </summary>
        public event SelectionGenericEventHandler SelectionAborted;

        /// <summary>
        /// Invoked when selection has finished, so all components have been selected.
        /// </summary>
        public event SelectionGenericEventHandler SelectionFinished;

        /// <summary>
        /// The selecting state.
        /// </summary>
        private enum ESelectionState
        {
            /// <summary>
            /// Selecting the head.
            /// </summary>
            PedHeadSelection,

            /// <summary>
            /// Selecting a hat.
            /// </summary>
            PedHatSelection,

            /// <summary>
            /// Selecting glasses.
            /// </summary>
            PedHeadExtrasSelection,

            /// <summary>
            /// Selecting the torso.
            /// </summary>
            PedBodySelection,

            /// <summary>
            /// Selecting lower body.
            /// </summary>
            PedLowerSelection,

            /// <summary>
            /// Selection has finished.
            /// </summary>
            Finished,
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.delayTimer.CanExecute())
            {
                this.HandlePedComponentSelection();
            }
        }

        /// <summary>
        /// Handles the selection of the various ped components.
        /// </summary>
        private void HandlePedComponentSelection()
        {
            // When only one texture or model available, skip if in either head, upper or lower body selection
            bool noModels = this.ped.Skin.Component[this.currentComponent].AvailableModels == 1;
            bool noTextures = this.ped.Skin.Component[this.currentComponent].AvailableTextures == 1;
            bool skip = noModels && noTextures && (this.selectionState == ESelectionState.PedBodySelection
                || this.selectionState == ESelectionState.PedHeadSelection || this.selectionState == ESelectionState.PedLowerSelection);

            // Get number of hats and glasses available
            int hatsCount = this.ped.Model.ModelInfo.GetNumberOfHats();
            int glassesCount = this.ped.Model.ModelInfo.GetNumberOfGlasses();

            // Random hacks for some models
            if (this.ped.Model == "M_Y_COP")
            {
                // The second lower body model is bugged, so we skip
                if (this.selectionState == ESelectionState.PedLowerSelection)
                {
                    skip = true;
                }
            }

            // If no glasses or hats available, skip
            if ((this.selectionState == ESelectionState.PedHatSelection && hatsCount == 0)
                || (this.selectionState == ESelectionState.PedHeadExtrasSelection && glassesCount == 0))
            {
                skip = true;
            }

            // If the component should be skipped, we don't want to override the just went back flag yet, because we might need it below
            if (!skip)
            {
                this.justWentBackInComponentOrder = false;
            }

            // Get string of component
            string component = string.Empty;
            if (this.selectionState == ESelectionState.PedBodySelection)
            {
                component = "Body";
            }
            else if (this.selectionState == ESelectionState.PedHatSelection)
            {
                component = "Hat";
            }
            else if (this.selectionState == ESelectionState.PedHeadExtrasSelection)
            {
                component = "Glasses";
            }
            else if (this.selectionState == ESelectionState.PedHeadSelection)
            {
                component = "Head";
            }
            else if (this.selectionState == ESelectionState.PedLowerSelection)
            {
                component = "Legs";
            }

            switch (this.selectionState)
            {
                // When changing component, use left /right and up/down to change models/textures
                case ESelectionState.PedBodySelection:
                case ESelectionState.PedHeadSelection:
                case ESelectionState.PedLowerSelection:
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.NextCopModel))
                    {
                        this.ChangePlayerComponentModel(true);
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModel))
                    {
                        this.ChangePlayerComponentModel(false);
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.NextCopModelTexture))
                    {
                        this.ChangePlayerComponentTexture(true);
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModelTexture))
                    {
                        this.ChangePlayerComponentTexture(false);
                    }

                    // Print helpbox
                    TextHelper.PrintFormattedHelpBox(string.Format(CultureHelper.GetText("GOONDUTY_COP_MODEL_COMPONENT"), component), false);
                    break;

                case ESelectionState.PedHatSelection:
                case ESelectionState.PedHeadExtrasSelection:

                    // Print helpbox
                    TextHelper.PrintFormattedHelpBox(string.Format(CultureHelper.GetText("GOONDUTY_COP_MODEL_COMPONENT"), component), false);
                    break;
            }

            switch (this.selectionState)
            {
                case ESelectionState.PedHeadSelection:
                    // When this component should be skipped and we just went back, go back another time
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModelComponent) || (skip && this.justWentBackInComponentOrder))
                    {
                        if (this.SelectionAborted != null)
                        {
                            this.SelectionAborted();
                        }

                        this.End();

                        return;
                    }

                    // Skip if needed
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ConfirmCopModel) || skip)
                    {
                        this.currentComponent = PedComponent.UpperBody;
                        this.selectionState = ESelectionState.PedBodySelection;
                        TextHelper.ClearHelpbox();
                    }

                    break;

                case ESelectionState.PedBodySelection:
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModelComponent) || (skip && this.justWentBackInComponentOrder))
                    {
                        this.currentComponent = PedComponent.Head;
                        this.justWentBackInComponentOrder = true;
                        this.selectionState = ESelectionState.PedHeadSelection;
                        skip = false;
                        TextHelper.ClearHelpbox();
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ConfirmCopModel) || skip)
                    {
                        this.currentComponent = PedComponent.LowerBody;
                        this.selectionState = ESelectionState.PedLowerSelection;
                        TextHelper.ClearHelpbox();
                    }

                    break;

                case ESelectionState.PedLowerSelection:
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModelComponent) || (skip && this.justWentBackInComponentOrder))
                    {
                        this.currentComponent = PedComponent.UpperBody;
                        this.justWentBackInComponentOrder = true;
                        this.selectionState = ESelectionState.PedBodySelection;
                        skip = false;
                        TextHelper.ClearHelpbox();
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ConfirmCopModel) || skip)
                    {
                        this.selectionState = ESelectionState.PedHatSelection;
                        TextHelper.ClearHelpbox();
                    }

                    break;

                case ESelectionState.PedHatSelection:
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModelComponent) || (skip && this.justWentBackInComponentOrder))
                    {
                        this.currentComponent = PedComponent.LowerBody;
                        this.justWentBackInComponentOrder = true;
                        this.selectionState = ESelectionState.PedLowerSelection;
                        skip = false;
                        TextHelper.ClearHelpbox();
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.NextCopModel))
                    {
                        int propIndex = this.ped.Skin.GetPropIndex((PedProp)EPedProp.Hat);
                        if (propIndex == hatsCount)
                        {
                            propIndex = 0;
                        }
                        else
                        {
                            propIndex++;
                        }

                        this.ped.SetProp(EPedProp.Hat, propIndex);
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModel))
                    {
                        int propIndex = this.ped.Skin.GetPropIndex((PedProp)EPedProp.Hat);
                        if (propIndex == 0)
                        {
                            propIndex = hatsCount;
                        }
                        else
                        {
                            propIndex--;
                        }

                        this.ped.SetProp(EPedProp.Hat, propIndex);
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ConfirmCopModel) || skip)
                    {
                        this.selectionState = ESelectionState.PedHeadExtrasSelection;
                        TextHelper.ClearHelpbox();
                    }

                    break;

                case ESelectionState.PedHeadExtrasSelection:
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModelComponent) || (skip && this.justWentBackInComponentOrder))
                    {
                        this.justWentBackInComponentOrder = true;
                        this.selectionState = ESelectionState.PedHatSelection;
                        skip = false;
                        TextHelper.ClearHelpbox();
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.NextCopModel))
                    {
                        int propIndex = this.ped.Skin.GetPropIndex((PedProp)EPedProp.Glasses);
                        if (propIndex == glassesCount)
                        {
                            propIndex = 0;
                        }
                        else
                        {
                            propIndex++;
                        }

                        this.ped.SetProp(EPedProp.Glasses, propIndex);
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.PreviousCopModel))
                    {
                        int propIndex = this.ped.Skin.GetPropIndex((PedProp)EPedProp.Glasses);
                        if (propIndex == 0)
                        {
                            propIndex = glassesCount;
                        }
                        else
                        {
                            propIndex--;
                        }

                        this.ped.SetProp(EPedProp.Glasses, propIndex);
                    }

                    // Finish
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.ConfirmCopModel) || skip)
                    {
                        this.selectionState = ESelectionState.Finished;

                        if (this.SelectionFinished != null)
                        {
                            this.SelectionFinished();
                        }

                        this.End();
                    }

                    break;
            }
        }

        /// <summary>
        /// Changes the current component model index to either the next or previous one depending on <paramref name="next"/>.
        /// </summary>
        /// <param name="next">If true, the component model index is increased, if not, it is decreased.</param>
        private void ChangePlayerComponentModel(bool next)
        {
            GTA.value.PedComponent pedComponent = this.ped.Skin.Component[this.currentComponent];
            int availableModels = pedComponent.AvailableModels;
            int modelIndex = pedComponent.ModelIndex;
            int newModel = modelIndex;

            if (next)
            {
                if (newModel + 1 == availableModels)
                {
                    newModel = 0;
                }
                else
                {
                    newModel++;
                }
            }
            else
            {
                if (newModel == 0)
                {
                    newModel = availableModels - 1;
                }
                else
                {
                    newModel--;
                }
            }

            // Better always use 0 as texture because some compontent models might not have the current texture index available
            pedComponent.ChangeIfValid(newModel, 0);
        }

        /// <summary>
        /// Changes the current component texture index to either the next or previous one depending on <paramref name="next"/>.
        /// </summary>
        /// <param name="next">If true, the component texture index is increased, if not, it is decreased.</param>
        private void ChangePlayerComponentTexture(bool next)
        {
            GTA.value.PedComponent pedComponent = this.ped.Skin.Component[this.currentComponent];

            int availableTextures = pedComponent.AvailableTextures;
            int textureIndex = pedComponent.TextureIndex;
            int modelIndex = pedComponent.ModelIndex;
            int newTexture = textureIndex;

            if (next)
            {
                if (newTexture + 1 == availableTextures)
                {
                    newTexture = 0;
                }
                else
                {
                    newTexture++;
                }
            }
            else
            {
                if (newTexture == 0)
                {
                    newTexture = availableTextures - 1;
                }
                else
                {
                    newTexture--;
                }
            }

            pedComponent.ChangeIfValid(modelIndex, newTexture);
        }
    }
}