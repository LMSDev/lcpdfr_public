namespace LCPD_First_Response.LCPDFR.Input
{
    using System;
    using System.Reflection;
    using System.Windows.Forms;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Entities;

    using SlimDX.XInput;

    using Controller = LCPD_First_Response.Engine.Input.Controller;
    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// Defines the LCPDFR keys.
    /// </summary>
    internal enum ELCPDFRKeys
    {
        /// <summary>
        /// Key to abort the suspect transporter cutscene.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.Space, GamepadButtonFlags.None, "AbortSuspectTransportCutscene")]
        AbortSuspectTransportCutscene,

        /// <summary>
        /// Accept callout key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.Y, Keys.None, GamepadButtonFlags.A, GamepadButtonFlags.LeftShoulder, "AcceptCallout")]
        AcceptCallout,

        /// <summary>
        /// Accept partner in pd key.
        /// </summary>
        [KeyAttribute(EKeyType.InPD | EKeyType.OnFoot, Keys.E, GamepadButtonFlags.LeftShoulder, "AcceptPartner")]
        AcceptPartner,

        /// <summary>
        /// The aim marker key.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.LMenu, GamepadButtonFlags.None, "AimMarker")]
        AimMarker,

        /// <summary>
        /// Arrest key.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.E, GamepadButtonFlags.LeftShoulder, "Arrest")]
        Arrest,

        /// <summary>
        /// Call a transporter during arrest key.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.N, GamepadButtonFlags.X, "ArrestCallTransporter")]
        ArrestCallTransporter,

        /// <summary>
        /// Drive suspect to pd during arrest key.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.Y, GamepadButtonFlags.B, "ArrestDriveToPD")]
        ArrestDriveToPD,

        /// <summary>
        /// The block area key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.M, GamepadButtonFlags.None, "BlockArea")]
        BlockArea,

        /// <summary>
        /// The key to trigger rotating during a checkpoint control setup.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.LControlKey, GamepadButtonFlags.None, "CheckpointControlRotate")]
        CheckpointControlRotate,

        /// <summary>
        /// The key used to start a checkpoint control at the current location.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.B, Keys.LControlKey, GamepadButtonFlags.None, GamepadButtonFlags.None, "CheckpointControlStart")]
        CheckpointControlStart,

        /// <summary>
        /// The key used to move up during checkpoint control setup.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.Up, GamepadButtonFlags.DPadUp, "CheckpointControlUp")]
        CheckpointControlUp,

        /// <summary>
        /// The key used to move down during checkpoint control setup.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.Down, GamepadButtonFlags.DPadDown, "CheckpointControlDown")]
        CheckpointControlDown,

        /// <summary>
        /// The key used to move left during checkpoint control setup.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.Left, GamepadButtonFlags.DPadLeft, "CheckpointControlLeft")]
        CheckpointControlLeft,

        /// <summary>
        /// The key used to move right during checkpoint control setup.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.Right, GamepadButtonFlags.DPadRight, "CheckpointControlRight")]
        CheckpointControlRight,

        /// <summary>
        /// The key used to confirm during checkpoint control setup.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.Enter, GamepadButtonFlags.A, "CheckpointControlConfirm")]
        CheckpointControlConfirm,


        /// <summary>
        /// Confirm key in model selection.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.Enter, GamepadButtonFlags.A, "ConfirmCopModel")]
        ConfirmCopModel,

        /// <summary>
        /// Confirm key in model selection.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.Enter, GamepadButtonFlags.A, "ConfirmCopCarModel")]
        ConfirmCopCarModel,

        /// <summary>
        /// The key for reason 1 in a dialogue.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.F9, GamepadButtonFlags.DPadLeft, "Dialog1")]
        Dialog1,

        /// <summary>
        /// The key for reason 2 in a dialogue.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.F10, GamepadButtonFlags.DPadRight, "Dialog2")]
        Dialog2,

        /// <summary>
        /// The key for reason 3 in a dialogue.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.F11, GamepadButtonFlags.DPadDown, "Dialog3")]
        Dialog3,

        /// <summary>
        /// The key for reason 4 in a dialogue.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.F12, GamepadButtonFlags.DPadUp, "Dialog4")]
        Dialog4,

        /// <summary>
        /// Deny callout key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.N, Keys.None, GamepadButtonFlags.B, GamepadButtonFlags.LeftShoulder, "DenyCallout")]
        DenyCallout,

        /// <summary>
        /// The drink coffee key.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.O, Keys.None, GamepadButtonFlags.None, GamepadButtonFlags.None, "DrinkCoffee")]
        DrinkCoffee,

        /// <summary>
        /// The key to grab a suspect.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.E, GamepadButtonFlags.LeftShoulder, "GrabSuspect")]
        GrabSuspect,

        /// <summary>
        /// The key used to holster the taser.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.T, GamepadButtonFlags.RightThumb, "HolsterTaser")]
        HolsterTaser,

        /// <summary>
        /// The key used to walk faster.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.LMenu, GamepadButtonFlags.None, "WalkFaster")]
        WalkFaster,

        /// <summary>
        /// The key used to issue a parking ticket.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.E, GamepadButtonFlags.LeftShoulder, "IssueParkingTicket")]
        IssueParkingTicket,

        /// <summary>
        /// Join pursuit key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.E, GamepadButtonFlags.None, "JoinPursuit")]
        JoinPursuit,

        /// <summary>
        /// Next model key in the model selection when going on duty.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.Right, GamepadButtonFlags.DPadRight, "NextCopModel")]
        NextCopModel,

        /// <summary>
        /// Next model texture key in the model selection when going on duty.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.Up, GamepadButtonFlags.DPadUp, "NextCopModelTexture")]
        NextCopModelTexture,

        /// <summary>
        /// Next model key in the model selection when going on duty.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.Right, GamepadButtonFlags.DPadRight, "NextCopCarModel")]
        NextCopCarModel,

        /// <summary>
        /// The key used to open the trunk of a vehicle.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.E, GamepadButtonFlags.LeftShoulder, "OpenTrunk")]
        OpenTrunk,

        /// <summary>
        /// The key to make the partner arrest.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.G, GamepadButtonFlags.None, "PartnerArrest")]
        PartnerArrest,

        /// <summary>
        /// The key to make the partner regroup.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.R, GamepadButtonFlags.None, "PartnerRegroup")]
        PartnerRegroup,

        /// <summary>
        /// The key used to start the police computer.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle, Keys.E, GamepadButtonFlags.LeftShoulder, "PoliceComputer")]
        PoliceComputer,

        /// <summary>
        /// The next model key in the model selection when going on duty.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.Left, GamepadButtonFlags.DPadLeft, "PreviousCopModel")]
        PreviousCopModel,

        /// <summary>
        /// The previous cop model component, so essentially going back to the component changed before.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.Back, GamepadButtonFlags.LeftThumb, "PreviousCopModelComponent")]
        PreviousCopModelComponent,

        /// <summary>
        /// Next model texture key in the model selection when going on duty.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.Down, GamepadButtonFlags.DPadDown, "PreviousCopModelTexture")]
        PreviousCopModelTexture,

        /// <summary>
        /// The next model key in the model selection when going on duty.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.Left, GamepadButtonFlags.DPadLeft, "PreviousCopCarModel")]
        PreviousCopCarModel,

        /// <summary>
        /// The key used to start license checking.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.F9, GamepadButtonFlags.DPadLeft, "PulloverLicenseYes")]
        PulloverLicenseYes,

        /// <summary>
        /// The key used to skip license checking.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.F10, GamepadButtonFlags.DPadRight, "PulloverLicenseNo")]
        PulloverLicenseNo,

        /// <summary>
        /// The start pullover key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle, Keys.ShiftKey, GamepadButtonFlags.X, "PulloverStart")]
        PulloverStart,

        /// <summary>
        /// The show pursuit tactics menu key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.CapsLock, GamepadButtonFlags.LeftThumb, "ShowQuickActionMenu")]
        QuickActionMenuShow,

        /// <summary>
        /// The cycle quick action menu group left key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.None, GamepadButtonFlags.LeftShoulder, "QuickActionMenuGroupCycleLeft")]
        QuickActionMenuCycleLeft,

        /// <summary>
        /// The cycle quick action menu group left key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.Tab, GamepadButtonFlags.RightShoulder, "QuickActionMenuGroupCycleRight")]
        QuickActionMenuCycleRight,

        /// <summary>
        /// The cycle quick action menu group option key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.MButton, GamepadButtonFlags.RightThumb, "QuickActionMenuCycleOption")]
        QuickActionMenuCycleOption,

        /// <summary>
        /// The key used to randomize the cop outfit while selecting.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.Down, GamepadButtonFlags.DPadDown, "RandomizeCopModel")]
        RandomizeCopModel,

        /// <summary>
        /// The key used to recruit a partner in the street.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.G, GamepadButtonFlags.X, "RecruitPartner")]
        RecruitPartner,

        /// <summary>
        /// Request backup key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.B, Keys.Menu, GamepadButtonFlags.DPadDown, GamepadButtonFlags.A, "RequestBackup")]
        RequestBackup,

        /// <summary>
        /// Request ambulance key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.M, Keys.ControlKey, GamepadButtonFlags.None, GamepadButtonFlags.None,  "RequestAmbulance")]
        RequestAmbulance,

        /// <summary>
        /// Request firefighter key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.F, Keys.ControlKey, GamepadButtonFlags.None, GamepadButtonFlags.None,  "RequestFirefighter")]
        RequestFirefighter,

        /// <summary>
        /// Request helicopter backup key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.N, Keys.ControlKey, GamepadButtonFlags.None, GamepadButtonFlags.None, "RequestHelicopterBackup")]
        RequestHelicopterBackup,

        /// <summary>
        /// Request noose backup key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.N, Keys.Menu, GamepadButtonFlags.B, GamepadButtonFlags.DPadDown, "RequestNooseBackup")]
        RequestNooseBackup,

        /// <summary>
        /// Request roadblock key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.None, GamepadButtonFlags.None,  "RequestRoadblock")]
        RequestRoadblock,

        /// <summary>
        /// The show pursuit tactics menu key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.F8, GamepadButtonFlags.None, "ShowPursuitTacticsMenu")]
        ShowPursuitTacticsMenu,

        /// <summary>
        /// Start key.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.P, Keys.Menu, GamepadButtonFlags.None, GamepadButtonFlags.None, "Start")]
        Start,

        /// <summary>
        /// The modifier key used in conjunction with the arrest key to start a new chase for the targeted ped.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.LMenu, GamepadButtonFlags.None, "StartChaseOnPed")]
        StartChaseOnPed,

        /// <summary>
        /// The toggle busy key to enable or disable random callouts.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.F7, GamepadButtonFlags.DPadDown, "ToggleBusy")]
        ToggleBusy,

        /// <summary>
        /// Toggles the player's hat.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot, Keys.U, Keys.Menu, GamepadButtonFlags.None, GamepadButtonFlags.None,  "ToggleHat")]
        ToggleHat,

        /// <summary>
        /// The toggle siren mode key for switching through the siren modes.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle, Keys.Divide, GamepadButtonFlags.None, "ToggleSirenMode")]
        ToggleSirenMode,

        /// <summary>
        /// The toggle siren sound key to toggle the sound of the siren in a police vehicle.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle, Keys.Multiply,  GamepadButtonFlags.None, "ToggleSirenSound")]
        ToggleSirenSound,

        /// <summary>
        /// The key to start the tutorial.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.Y, GamepadButtonFlags.X, "TutorialStart")]
        TutorialStart,

        /// <summary>
        /// The key to start the tutorial in the police department.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle | EKeyType.InPD, Keys.E, GamepadButtonFlags.X, "TutorialStartPoliceDepartment")]
        TutorialStartPoliceDepartment,

        /// <summary>
        /// The key used to update the callout status.
        /// </summary>
        [KeyAttribute(EKeyType.InVehicle | EKeyType.OnFoot, Keys.E, Keys.Menu, GamepadButtonFlags.None, GamepadButtonFlags.None, "UpdateCalloutState")]
        UpdateCalloutState,

        /// <summary>
        /// View introduction key.
        /// </summary>
        [KeyAttribute(EKeyType.InPD | EKeyType.OnFoot, Keys.E, GamepadButtonFlags.LeftShoulder, "ViewIntroduction")]
        ViewIntroduction,


        /// <summary>
        /// The key to call in a pursuit.
        /// </summary>
        [KeyAttribute(EKeyType.OnFoot | EKeyType.InVehicle, Keys.E, Keys.Menu, GamepadButtonFlags.X, GamepadButtonFlags.DPadDown, "CallInPursuit")]
        CallInPursuit,
    }

    /// <summary>
    /// Represents a lcpdfr key.
    /// </summary>
    internal class LCPDFRKey
    {
        /// <summary>
        /// The key attribute data.
        /// </summary>
        private KeyAttribute keyAttribute;

        /// <summary>
        /// Initializes a new instance of the <see cref="LCPDFRKey"/> class.
        /// </summary>
        /// <param name="keys">
        /// The keys.
        /// </param>
        public LCPDFRKey(ELCPDFRKeys keys)
        {
            this.KeyIdentifier = keys;

            // Get attribute
            Type t = this.KeyIdentifier.GetType();
            MemberInfo[] memberInfos = t.GetMember(keys.ToString());
            foreach (object attribute in memberInfos[0].GetCustomAttributes(false))
            {
                if (attribute is KeyAttribute)
                {
                    this.keyAttribute = (KeyAttribute)attribute;
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the assigned controller key.
        /// </summary>
        public GamepadButtonFlags ControllerKey { get; private set; }

        /// <summary>
        /// Gets the assigned controller modifier key.
        /// </summary>
        public GamepadButtonFlags ControllerModifierKey { get; private set; }

        /// <summary>
        /// Gets the assigned game key.
        /// </summary>
        public EGameKey GameKey { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the key uses modifier keys.
        /// </summary>
        public bool HasModifierKeys
        {
            get
            {
                return this.KeyModifierKey != Keys.None;
            }
        }

        /// <summary>
        /// Gets the assigned key.
        /// </summary>
        public Keys Key { get; private set; }

        public Keys KeyModifierKey { get; private set; }

        /// <summary>
        /// Gets the key attribute.
        /// </summary>
        public KeyAttribute KeyAttribute
        {
            get
            {
                return this.keyAttribute;
            }
        }

        /// <summary>
        /// Gets the key this instance represents.
        /// </summary>
        public ELCPDFRKeys KeyIdentifier { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the required modifier keys are down (if any). Only used for keyboard input.
        /// </summary>
        public bool ModifierKeysDown
        {
            get
            {
                if (this.HasModifierKeys)
                {
                    return KeyHandler.IsKeyboardKeyDown(this.KeyModifierKey);
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the required modifier keys are still down (if any). Only used for keyboard input.
        /// </summary>
        public bool ModifierKeysStillDown
        {
            get
            {
                return KeyHandler.IsKeyboardKeyStillDown(this.KeyModifierKey);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the key has been pressed since the last check.
        /// </summary>
        public bool IsDown
        {
            get
            {
                if (this.keyAttribute == null)
                {
                    throw new NullReferenceException("KeyAttribute is null for identifier: " + this.KeyIdentifier);
                }

                // Check if key can be down due to the type
                if (this.keyAttribute.KeyType.HasFlag(EKeyType.InPD))
                {
                    // If not in pd, return
                    if (!Main.PoliceDepartmentManager.IsPlayerInPoliceDepartment)
                    {
                        return false;
                    }
                }
                else
                {
                    // If in pd, return
                    if (Main.PoliceDepartmentManager.IsPlayerInPoliceDepartment)
                    {
                        return false;
                    }
                }

                if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                {
                    if (!this.keyAttribute.KeyType.HasFlag(EKeyType.InVehicle))
                    {
                        return false;
                    }
                    else
                    {
                        // If the player is getting out of a car, this should return false as well.
                        if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(Engine.Scripting.Tasks.EInternalTaskID.CTaskSimpleCarGetOut))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (!this.keyAttribute.KeyType.HasFlag(EKeyType.OnFoot))
                    {
                        return false;
                    }
                }

                // Now that all checks are passed, peform the actual key down check
                if (Engine.Main.KeyWatchDog.IsUsingController)
                {
                    if (this.ControllerKey == GamepadButtonFlags.None)
                    {
                        return false;
                    }

                    // Check if there is a second button assigned
                    if (this.ControllerModifierKey != GamepadButtonFlags.None)
                    {
                        // Perform key down check if modifier has been pressed first
                        if (Controller.IsKeyDown(this.ControllerKey))
                        {
                            return Controller.IsKeyStillDown(this.ControllerModifierKey);
                        }
                        else
                        {
                            // Modifier might be pressed after the key, so we check that
                            if (Controller.IsKeyDown(this.ControllerModifierKey))
                            {
                                return Controller.IsKeyStillDown(this.ControllerKey);
                            }
                        }
                    }

                    return Controller.IsKeyDown(this.ControllerKey);
                }

                // If we have modifier keys, things get a little bit difficult, since we want to allow Modifier+Key and Key+Modifier in both orders
                if (this.HasModifierKeys)
                {
                    // So we now first check if the key is down and if so check whether the modifier is still down, so that is has been down before the check already and still is
                    if (Engine.Main.KeyWatchDog.IsKeyDown(this.Key))
                    {
                        return this.ModifierKeysStillDown;
                    }
                }

                if (this.ModifierKeysDown)
                {
                    // Game key not down, so perform keyboard key check
                    // If using modifier keys, we have to use the real key state
                    if (this.HasModifierKeys)
                    {
                        return Engine.Main.KeyWatchDog.IsKeyStillDown(this.Key);
                    }

                    return Engine.Main.KeyWatchDog.IsKeyDown(this.Key);
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the key is still down.
        /// </summary>
        public bool IsStillDown
        {
            get
            {
                if (this.keyAttribute == null)
                {
                    throw new NullReferenceException("KeyAttribute is null for identifier: " + this.KeyIdentifier);
                }

                // Check if key can be down due to the type
                if (this.keyAttribute.KeyType.HasFlag(EKeyType.InPD))
                {
                    // If not in pd, return
                    if (!Main.PoliceDepartmentManager.IsPlayerInPoliceDepartment)
                    {
                        return false;
                    }
                }
                else
                {
                    // If in pd, return
                    if (Main.PoliceDepartmentManager.IsPlayerInPoliceDepartment)
                    {
                        return false;
                    }
                }

                if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                {
                    if (!this.keyAttribute.KeyType.HasFlag(EKeyType.InVehicle))
                    {
                        return false;
                    }
                    else
                    {
                        // If the player is getting out of a car, this should return false as well.
                        if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(Engine.Scripting.Tasks.EInternalTaskID.CTaskSimpleCarGetOut))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (!this.keyAttribute.KeyType.HasFlag(EKeyType.OnFoot))
                    {
                        return false;
                    }
                }

                // Now that all checks are passed, peform the actual key down check
                if (Engine.Main.KeyWatchDog.IsUsingController)
                {
                    if (this.ControllerKey == GamepadButtonFlags.None)
                    {
                        return false;
                    }

                    // Check if there is a second button assigned
                    if (this.ControllerModifierKey != GamepadButtonFlags.None)
                    {
                        return Controller.IsKeyStillDown(this.ControllerModifierKey) && Controller.IsKeyStillDown(this.ControllerKey);
                    }

                    return Controller.IsKeyStillDown(this.ControllerKey);
                }

                if (this.HasModifierKeys)
                {
                    return this.ModifierKeysStillDown && Engine.Main.KeyWatchDog.IsKeyStillDown(this.Key);
                }

                if (this.ModifierKeysDown)
                {
                    // Game key not down, so perform keyboard key check
                    return Engine.Main.KeyWatchDog.IsKeyStillDown(this.Key);
                }

                return false;
            }
        }

        /// <summary>
        /// Reads the settings file and changes the associated keys for the lcpdfrkey if different keys are given in the settings file.
        /// </summary>
        public void ReadAssignedKeys()
        {
            this.ControllerKey = Settings.GetControllerKey(this.keyAttribute.SettingsName, this.keyAttribute.ControllerKey);
            this.ControllerModifierKey = Settings.GetControllerKey(this.keyAttribute.SettingsName + "ModifierKey", this.keyAttribute.ControllerModifierKey);
            this.Key = Settings.GetKey(this.keyAttribute.SettingsName, this.keyAttribute.Key);
            this.KeyModifierKey = Settings.GetKey(this.keyAttribute.SettingsName + "ModifierKey", this.keyAttribute.KeyModifierKey);

            // Verify keys
            if ((int)this.Key > 255)
            {
                Log.Warning("ReadAssignedKeys: Invalid key found: " + this.Key.ToString() + " (" + this.keyAttribute.SettingsName + "). Please refer to the official key documentation for all valid key codes.", "Keys");
                this.Key = this.keyAttribute.Key;
            }

            if ((int)this.KeyModifierKey > 255)
            {
                Log.Warning("ReadAssignedKeys: Invalid key found: " + this.KeyModifierKey.ToString() + " (" + this.keyAttribute.SettingsName + "ModifierKey). Please refer to the official key documentation for all valid key codes.", "Keys");
                this.KeyModifierKey = this.keyAttribute.KeyModifierKey;
            }
        }
    }
}