namespace LCPD_First_Response.LCPDFR.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;

    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.LCPDFR.Callouts;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Scripts;
    using LCPD_First_Response.LCPDFR.Scripts.Partners;
    using LCPD_First_Response.LCPDFR.Scripts.Scenarios;

    /// <summary>
    /// The sound type.
    /// </summary>
    public enum ESound
    {
        /// <summary>
        /// The sound played by control when a pursuit has been acknowledged.
        /// </summary>
        PursuitAcknowledged,
    }

    /// <summary>
    /// The sound type.
    /// </summary>
    public enum EIntroReportedBy
    {
        /// <summary>
        /// 'Civilians report'
        /// </summary>
        Civilians,
        /// <summary>
        /// 'Units report'
        /// </summary>
        Officers
    }

    /// <summary>
    /// Provides access to LCPDFR functions.
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// The event handler for accepted callouts.
        /// </summary>
        /// <param name="name">The name of the callout. This is not the typename but the internally assigned name from <see cref="ScriptInfoAttribute.Name"/></param>.
        /// <param name="isInternal">Whether or not the callout is internal, that is a default LCPD First Response callout.</param>
        public delegate void OnCalloutAcceptedEventHandler(string name, bool isInternal);

        /// <summary>
        /// The event handler used for newly created peds.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public delegate void NewPedCreatedEventHandler(LPed ped);

        /// <summary>
        /// The event handler used for newly created vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        public delegate void NewVehicleCreatedEventHandler(LVehicle vehicle);

        /// <summary>
        /// The event handler for when the on duty state has changed.
        /// </summary>
        /// <param name="onDuty">The new on duty state.</param>
        public delegate void OnDutyStateChangedEventHandler(bool onDuty);

        /// <summary>
        /// The event handler for when a ped has been looked up in the police computer.
        /// </summary>
        /// <param name="personaData">The persona data.</param>
        public delegate void PedLookedUpInPoliceComputerEventHandler(PersonaData personaData);

        /// <summary>
        /// Event fired when the on duty state has changed.
        /// </summary>
        public static event OnDutyStateChangedEventHandler OnOnDutyStateChanged;

        /// <summary>
        /// Event fired when a ped has been looked up in the police computer.
        /// </summary>
        public static event PedLookedUpInPoliceComputerEventHandler PedLookedUpInPoliceComputer;

        /// <summary>
        /// Attaches a callback, that will be called each timer the player accepts a callout.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public static void AttachCalloutAcceptedHook(OnCalloutAcceptedEventHandler callback)
        {
            Main.CalloutManager.CalloutHasBeenAccepted += (sender, s) => callback.Invoke(s, sender.GetType().Assembly == typeof(Main).Assembly);
        }

        /// <summary>
        /// Attaches a ped creation hook and calls <paramref name="callback"/> when a new ped has been created.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public static void AttachPedCreationHook(NewPedCreatedEventHandler callback)
        {
            EventNewPedCreated.EventRaised += @event => callback.Invoke(new LPed(@event.Ped));
        }

        /// <summary>
        /// Attaches a vehicle creation hook and calls <paramref name="callback"/> when a new vehicle has been created.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public static void AttachVehicleCreationHook(NewVehicleCreatedEventHandler callback)
        {
            EventNewVehicleCreated.EventRaised += @event => callback.Invoke(new LVehicle(@event.Vehicle));
        }

        /// <summary>
        /// Adds <paramref name="ped"/> to <paramref name="pursuit"/>.
        /// </summary>
        /// <param name="pursuit">The pursuit instance.</param>
        /// <param name="ped">The ped.</param>
        public static void AddPedToPursuit(LHandle pursuit, LPed ped)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (ped == null)
            {
                throw new ArgumentNullException("ped");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.AddTarget(ped.Ped);
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the text wall.
        /// </summary>
        /// <param name="text">The text to add.</param>
        public static void AddTextToTextwall(string text)
        {
            TextHelper.AddTextToTextWall(text);
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the text wall in the following format: [<paramref name="reporter"/>] <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="reporter">The reporter.</param>
        public static void AddTextToTextwall(string text, string reporter)
        {
            TextHelper.AddTextToTextWall(text, reporter);
        }


        /// <summary>
        /// Adds <paramref name="ped"/> to the deletion list of <paramref name="gameScript"/>, so the ped will be freed when the script ends.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="gameScript">The script.</param>
        public static void AddToScriptDeletionList(LPed ped, GameScript gameScript)
        {
            gameScript.ContentManager.AddPed(ped.Ped);
        }

        /// <summary>
        /// Adds <paramref name="vehicle"/> to the deletion list of <paramref name="gameScript"/>, so the vehicle will be freed when the script ends.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="gameScript">The script.</param>
        public static void AddToScriptDeletionList(LVehicle vehicle, GameScript gameScript)
        {
            gameScript.ContentManager.AddVehicle(vehicle.Vehicle);
        }

        /// <summary>
        /// Adds a world event to the LCPDFR scenario manager.
        /// </summary>
        /// <param name="type">The type (i.e. class) of the world event.</param>
        /// <param name="name">The name.</param>
        public static void AddWorldEvent(Type type, string name)
        {
            BaseScript[] scripts = Main.ScriptManager.GetRunningScriptInstances("AmbientScenarioManager");
            if (scripts != null && scripts.Length > 0)
            {
                AmbientScenarioManager scenarioManager = (AmbientScenarioManager)scripts[0];
                scenarioManager.RegisterScenario(type, name);
            }
        }

        /// <summary>
        /// Creates a blip for an area at <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>The blip.</returns>
        public static GTA.Blip CreateBlipForArea(GTA.Vector3 position, float radius)
        {
            return AreaBlocker.CreateAreaBlip(position, radius);
        }

        /// <summary>
        /// Creates a pursuit.
        /// </summary>
        /// <returns>The chase handle.</returns>
        public static LHandle CreatePursuit()
        {
            Pursuit pursuit = new Pursuit();

            return new LHandle(pursuit);
        }

        /// <summary>
        /// Creates a random audio intro string which can be as an intro when playing audio from the police scanner.
        /// </summary>
        /// <returns>The audio string.</returns>
        public static string CreateRandomAudioIntroString(EIntroReportedBy reportedBy)
        {
            return AudioHelper.CreateIntroAudioMessage(reportedBy);
        }

        /// <summary>
        /// Forces a pullover to end.
        /// </summary>
        public static void ForceEndPullover()
        {
            if (IsPlayerPerformingPullover())
            {
                BaseScript[] scripts = Main.ScriptManager.GetRunningScriptInstances("Pullover");
                foreach (BaseScript baseScript in scripts)
                {
                    baseScript.End();
                }
            }
        }

        /// <summary>
        /// Ends <paramref name="pursuit"/> if still running.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        public static void ForceEndPursuit(LHandle pursuit)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.EndChase();
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Gets a detailed area name of <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>The area name.</returns>
        public static string GetAreaStringFromPosition(GTA.Vector3 position)
        {
            return AreaHelper.GetAreaNameMeaningful(position);
        }

        /// <summary>
        /// Gets all peds the player arrested and is taking care of, i.e. taking to a PD.
        /// </summary>
        /// <returns>An array of peds.</returns>
        public static LPed[] GetArrestedPeds()
        {
            List<LPed> peds = new List<LPed>();

            BaseScript[] arrestScripts = Main.ScriptManager.GetRunningScriptInstances("Arrest");
            if (arrestScripts != null)
            {
                foreach (BaseScript script in arrestScripts)
                {
                    Arrest arrestScript = (Arrest)script;
                    if (arrestScript.RequiresPlayerAttention && arrestScript.Suspect.Wanted.IsCuffed)
                    {
                        peds.Add(new LPed(arrestScript.Suspect));
                    }
                }
            }

            return peds.ToArray();
        }

        /// <summary>
        /// Gets the name of <paramref name="callout"/>.
        /// </summary>
        /// <param name="callout">The callout.</param>
        /// <returns>The name.</returns>
        public static string GetCalloutName(LHandle callout)
        {
            if (callout == null)
            {
                throw new ArgumentNullException("callout");
            }

            if (callout.Object is Callout)
            {
                Callout p = callout.Object as Callout;
                return p.ScriptInfo.Name;
            }
            else
            {
                throw new ArgumentException("Callout handle is invalid.");
            }
        }

        /// <summary>
        /// Gets the current callout.
        /// </summary>
        /// <returns>The current callout.</returns>
        public static LHandle GetCurrentCallout()
        {
            if (!Main.CalloutManager.IsCalloutRunning)
            {
                return null;
            }

            return new LHandle(Main.CalloutManager.CurrentCallout);
        }

        /// <summary>
        /// Gets the current partner instance. This function can return null if LCPDFR is not yet running.
        /// </summary>
        /// <returns>The partner.</returns>
        public static LHandle GetCurrentPartner()
        {
            return new LHandle(Main.PartnerManager);
        }

        /// <summary>
        /// Gets the current pullover instance, if any.
        /// </summary>
        /// <returns>The pullover instance.</returns>
        public static LHandle GetCurrentPullover()
        {
            if (!Main.PulloverManager.IsPulloverRunning)
            {
                return null;
            }

            BaseScript[] scripts = Main.ScriptManager.GetRunningScriptInstances("Pullover");
            if (scripts != null && scripts.Length > 0)
            {
                if (scripts[0] is Pullover)
                {
                    Pullover pullover = scripts[0] as Pullover;
                    return new LHandle(pullover);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the partner ped. This can be null.
        /// </summary>
        /// <param name="partner">The partner handle.</param>
        /// <returns>The partner ped.</returns>
        public static LPed GetPartnerPed(LHandle partner)
        {
            if (partner == null)
            {
                throw new ArgumentNullException("partner");
            }


            if (partner.Object is PartnerManager)
            {
                PartnerManager p = partner.Object as PartnerManager;
                if (p.Partners.Count == 0)
                {
                    return null;
                }

                return new LPed(p.Partners[0].PartnerPed);
            }
            else
            {
                throw new ArgumentException("Partner handle is invalid.");
            }
        }

        /// <summary>
        /// Gets all peds from <paramref name="partner"/>.
        /// </summary>
        /// <param name="partner">The partner instance.</param>
        /// <returns>The partner peds.</returns>
        public static LPed[] GetPartnerPeds(LHandle partner)
        {
            if (partner == null)
            {
                throw new ArgumentNullException("partner");
            }


            if (partner.Object is PartnerManager)
            {
                PartnerManager p = partner.Object as PartnerManager;
                if (p.Partners.Count == 0)
                {
                    return null;
                }

                List<LPed> peds = p.Partners.Select(part => new LPed(part.PartnerPed)).ToList();
                return peds.ToArray();
            }
            else
            {
                throw new ArgumentException("Partner handle is invalid.");
            }
        }

        /// <summary>
        /// Gets the vehicle pulled over.
        /// </summary>
        /// <param name="pullover">The pullover instance.</param>
        /// <returns>The vehicle.</returns>
        public static LVehicle GetPulloverVehicle(LHandle pullover)
        {
            if (pullover == null)
            {
                throw new ArgumentNullException("pullover");
            }

            if (pullover.Object is Pullover)
            {
                Pullover p = pullover.Object as Pullover;
                if (p.Vehicle == null || !p.Vehicle.Exists())
                {
                    return null;
                }

                return new LVehicle(p.Vehicle);
            }
            else
            {
                throw new ArgumentException("Pullover handle is invalid.");
            }
        }

        /// <summary>
        /// Gets the translated representation for <paramref name="name"/> based on the current language from the language resources file. Returns <paramref name="name"/> if not found.
        /// </summary>
        /// <param name="name">The name of the string.</param>
        /// <returns>The translated string.</returns>
        public static string GetStringFromLanguageFile(string name)
        {
            return CultureHelper.GetText(name);
        }

        /// <summary>
        /// Gets whether a callout is running.
        /// </summary>
        /// <returns>Whether a callout is running.</returns>
        public static bool IsCalloutRunning()
        {
            return Main.CalloutManager.IsCalloutRunning;
        }

        /// <summary>
        /// Returns whether <paramref name="ped"/> has an owner.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>The return value.</returns>
        public static bool DoesPedHaveAnOwner(LPed ped)
        {
            if (ped == null)
            {
                throw new ArgumentNullException("ped");
            }

            return ped.Ped.HasOwner;
        }

        /// <summary>
        /// Returns whether a controller is connected to the PC and used as main input device.
        /// </summary>
        /// <returns>Whether a controller is being used.</returns>
        public static bool IsControllerInUse()
        {
            return Engine.Main.KeyWatchDog.IsUsingController;
        }

        /// <summary>
        /// Returns whether <paramref name="keyboardKey "/> has been pressed since the last tick. Does not return true again if the key has been down the ticket before. For live state use <see cref="IsKeyStillDown"/>.
        /// </summary>
        /// <param name="keyboardKey">The keyboard key.</param>
        /// <returns>The key state.</returns>
        public static bool IsKeyDown(System.Windows.Forms.Keys keyboardKey)
        {
            return Input.KeyHandler.IsKeyboardKeyDown(keyboardKey);
        }

        /// <summary>
        /// Returns whether <paramref name="keyboardKey "/> is held down at the moment.
        /// </summary>
        /// <param name="keyboardKey">The keyboard key.</param>
        /// <returns>The key state.</returns>
        public static bool IsKeyStillDown(System.Windows.Forms.Keys keyboardKey)
        {
            return Input.KeyHandler.IsKeyboardKeyStillDown(keyboardKey);
        }

        /// <summary>
        /// Returns whether <paramref name="button"/> has been pressed since the last tick. Does not return true again if the button has been down the ticket before. For live state use <see cref="IsControllerKeyStillDown"/>.
        /// </summary>
        /// <param name="button">The controller button.</param>
        /// <returns>The button state.</returns>
        public static bool IsControllerKeyDown(SlimDX.XInput.GamepadButtonFlags button)
        {
            return Input.KeyHandler.IsControllerKeyDown(button);
        }

        /// <summary>
        /// Returns whether <paramref name="button "/> is held down at the moment.
        /// </summary>
        /// <param name="button">The controller button.</param>
        /// <returns>The button state.</returns>
        public static bool IsControllerKeyStillDown(SlimDX.XInput.GamepadButtonFlags button)
        {
            return Input.KeyHandler.IsControllerKeyStillDown(button);
        }

        /// <summary>
        /// Returns whether <paramref name="ped"/> is still assigned to <paramref name="gameScript"/> as its main controller.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="gameScript">The script.</param>
        /// <returns>Whether still assigned.</returns>
        public static bool IsStillControlledByScript(LPed ped, GameScript gameScript)
        {
            return ped.Ped.Intelligence.IsStillAssignedToController(gameScript);
        }

        /// <summary>
        /// Returns whether <paramref name="ped"/> is still assigned to <paramref name="worldEvent"/> as its main controller.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="worldEvent">The world event script.</param>
        /// <returns>Whether still assigned.</returns>
        public static bool IsStillControlledByScript(LPed ped, WorldEvent worldEvent)
        {
            return ped.Ped.Intelligence.IsStillAssignedToController(worldEvent);
        }

        /// <summary>
        /// Gets whether LCPDFR or an LCPDFR addon is currently using Text Input (should you ignore key input)
        /// </summary>
        /// <returns>Whether or not LCPDFR, LCPDFR addon or SHDN is using text input</returns>
        public static bool IsTextInputActive()
        {
            if (GTA.Game.Console.isActive || Engine.GUI.Gui.TextInputActive || Globals.IsExternalTextInput)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets whether the player is in a pursuit.
        /// </summary>
        /// <returns>
        /// Whether a pursuit is running.
        /// </returns>
        public static bool IsPlayerInPursuit()
        {
            return CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null;
        }

        /// <summary>
        /// Gets whether the player is performing a pullover.
        /// </summary>
        /// <returns>Whether player is performing a pullover.</returns>
        public static bool IsPlayerPerformingPullover()
        {
            return Main.PulloverManager.IsPulloverRunning;
        }

        /// <summary>
        /// Gets a value whether <paramref name="pursuit"/> is still running.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <returns>The result.</returns>
        public static bool IsPursuitStillRunning(LHandle pursuit)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                return p.IsRunning;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Plays <paramref name="sound"/>.
        /// </summary>
        /// <param name="sound">The sound name.</param>
        /// <param name="scannerIntro">Whether a short beep should be played at the start and end of the audio. Doesn't work when <paramref name="scannerNoise"/> is false.</param>
        /// <param name="scannerNoise">Whether background noise should be added during playback.</param>
        public static void PlaySound(string sound, bool scannerIntro, bool scannerNoise)
        {
            if (scannerIntro && scannerNoise)
            {
                AudioHelper.PlayActionInScanner(sound);
            }
            else if (!scannerIntro)
            {
                AudioHelper.PlayActionSound(sound);
            }
            else
            {
                AudioHelper.PlayActionInScannerNoNoise(sound);
            }
        }

        /// <summary>
        /// Plays <paramref name="sound"/> using <paramref name="position"/>.
        /// </summary>
        /// <param name="sound">The sound ID.</param>
        /// <param name="position">The position used within the sound.</param>
        public static void PlaySoundUsingPosition(ESound sound, GTA.Vector3 position)
        {
            if (sound == ESound.PursuitAcknowledged)
            {
                AudioHelper.PlayDispatchAcknowledgeReportedCrime(position, AudioHelper.EPursuitCallInReason.Pursuit);
            }
        }

        /// <summary>
        /// Plays all actions in <paramref name="sound"/> and replaces POSITION with <paramref name="position"/> while playing.
        /// </summary>
        /// <param name="sound">The string containing all sounds. Use a whitespace to separate.</param>
        /// <param name="position">The position to use.</param>
        public static void PlaySoundUsingPosition(string sound, GTA.Vector3 position)
        {
            AudioHelper.PlayActionInScannerUsingPosition(sound, position);   
        }

        /// <summary>
        /// Prints <paramref name="text"/> in a help box.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void PrintHelp(string text)
        {
            TextHelper.PrintFormattedHelpBox(text);
        }

        /// <summary>
        /// Prints <paramref name="text"/> in the lower center of the screen.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="duration">The duration.</param>
        public static void PrintText(string text, int duration)
        {
            TextHelper.PrintText(text, duration);
        }

        /// <summary>
        /// Registers a callout of type <paramref name="type"/> to the callout manager to make it being processed with all LCPDFR callouts.
        /// </summary>
        /// <param name="type">The type.</param>
        public static void RegisterCallout(Type type)
        {
            Main.CalloutManager.RegisterCallout(type);
        }

        /// <summary>
        /// Removes <paramref name="ped"/> from the deletion list of <paramref name="gameScript"/>, so the ped will no longer be freed when the script ends.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="gameScript">The script.</param>
        public static void RemoveFromDeletionList(LPed ped, GameScript gameScript)
        {
            gameScript.ContentManager.RemovePed(ped.Ped);
        }

        /// <summary>
        /// Removes <paramref name="vehicle"/> from the deletion list of <paramref name="gameScript"/>, so the vehicle will no longer be freed when the script ends.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="gameScript">The script.</param>
        public static void RemoveFromDeletionList(LVehicle vehicle, GameScript gameScript)
        {
            gameScript.ContentManager.RemoveVehicle(vehicle.Vehicle);
        }

        /// <summary>
        /// Requests backup at <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void RequestPoliceBackupAtPosition(GTA.Vector3 position)
        {
            Main.BackupManager.RequestPoliceBackup(position, null);
        }

        /// <summary>
        /// Sets <paramref name="callout"/> as accepted.
        /// </summary>
        /// <param name="callout">The callout.</param>
        public static void SetCalloutAsAccepted(LHandle callout)
        {
            if (callout == null)
            {
                throw new ArgumentNullException("callout");
            }

            if (callout.Object is Callout)
            {
                Callout p = callout.Object as Callout;
                if (p.AcceptanceState == ECalloutAcceptanceState.Pending)
                {
                    Main.CalloutManager.AcceptCallout();
                }
            }
            else
            {
                throw new ArgumentException("Callout handle is invalid.");
            }
        }

        /// <summary>
        /// Sets whether callouts are disabled.
        /// </summary>
        /// <param name="disable">Whether callouts should be disabled.</param>
        public static void SetDisableCallouts(bool disable)
        {
            Main.CalloutManager.AllowRandomCallouts = !disable;
        }

        /// <summary>
        /// Sets whether random scenarios (world events like random pursuits) are disabled.
        /// </summary>
        /// <param name="disable">Whether scenarios are disabled.</param>
        public static void SetDisableRandomScenarios(bool disable)
        {
            AmbientScenarioManager ambientScenarioManager = Main.ScriptManager.GetRunningScriptInstances("AmbientScenarioManager")[0] as AmbientScenarioManager;
            if (ambientScenarioManager != null)
            {
                ambientScenarioManager.AllowAmbientScenarios = !disable;
            }
            else
            {
                throw new Exception("An internal error occured: Scenario Manager not running. Please restart LCPDFR.");
            }
        }

        /// <summary>
        /// Sets whether <paramref name="ped"/> is owned by <paramref name="gameScript"/>. 
        /// This marks the ped as mission ped and makes it unavailable for other scripts.
        /// This action has to be undone in order to free the ped properly and make it available again.
        /// If the ped is in a deletion list, it will be marked as no longer needed, however the availability won't change until you set it to false
        /// using this function.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="gameScript">The game script.</param>
        /// <param name="owned">Whether the ped is owned by the script.</param>
        public static void SetPedIsOwnedByScript(LPed ped, GameScript gameScript, bool owned)
        {
            if (ped == null)
            {
                throw new ArgumentNullException("ped");
            }

            if (gameScript == null)
            {
                throw new ArgumentNullException("gameScript");
            }

            if (owned)
            {
                ped.Ped.RequestOwnership(gameScript);
                ped.Ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, gameScript);
            }
            else
            {
                ped.Ped.ReleaseOwnership(gameScript);
                ped.Ped.Intelligence.ResetAction(gameScript);
            }
        }

        /// <summary>
        /// Sets whether <paramref name="ped"/> is owned by <paramref name="worldEvent"/>. 
        /// This marks the ped as mission ped and makes it unavailable for other scripts.
        /// This action has to be undone in order to free the ped properly and make it available again.
        /// If the ped is in a deletion list, it will be marked as no longer needed, however the availability won't change until you set it to false
        /// using this function.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="worldEvent">The game script.</param>
        /// <param name="owned">Whether the ped is owned by the worldEvent.</param>
        public static void SetPedIsOwnedByScript(LPed ped, WorldEvent worldEvent, bool owned)
        {
            if (ped == null)
            {
                throw new ArgumentNullException("ped");
            }

            if (worldEvent == null)
            {
                throw new ArgumentNullException("worldEvent");
            }

            if (owned)
            {
                ped.Ped.RequestOwnership(worldEvent);
                ped.Ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, worldEvent);
            }
            else
            {
                ped.Ped.ReleaseOwnership(worldEvent);
                ped.Ped.Intelligence.ResetAction(worldEvent);
            }
        }

        /// <summary>
        /// Sets external text input to be active or not (makes LCPDFR ignore all keys)
        /// </summary>
        /// <param name="isActive">Whether or not to toggle this feature on or not</param>
        public static void SetExternalTextInputActive(bool isActive)
        {
            Globals.IsExternalTextInput = isActive;
        }

        /// <summary>
        /// Sets whether the maximum units tolerance is enabled for <paramref name="pursuit"/>. If true, nearby units can join the pursuit even though this would exceed the maximum units number.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="value">Whether maximum unit tolerance is enabled.</param>
        public static void SetPursuitAllowMaxUnitsTolerance(LHandle pursuit, bool value)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.AllowMaxUnitsTolerance = value;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets whether a pursuit should not enable cop blips.
        /// </summary>
        /// <param name="pursuit">
        /// The pursuit.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void SetPursuitDontEnableCopBlips(LHandle pursuit, bool value)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.DontEnableCopBlips = value;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets whether suspects in a pursuits should fight.
        /// </summary>
        /// <param name="pursuit">
        /// The pursuit.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void SetPursuitForceSuspectsToFight(LHandle pursuit, bool value)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.ForceSuspectsToFight = true;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets whether the callout has been called in, so the player no longer has to call it in manually.
        /// </summary>
        /// <param name="pursuit">
        /// The pursuit.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void SetPursuitHasBeenCalledIn(LHandle pursuit, bool value)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.HasBeenCalledIn = value;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Makes the pursuit an active pursuit for the player by setting it as player's chase and letting cops join after the given amounts of time.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="delayToMakePlayerChase">The amount of time before pursuit is set as player's. Use -1 to block.</param>
        /// <param name="delayToMakeCopsJoin">The amount of time before cops can join. Use -1 to block.</param>
        public static void SetPursuitIsActiveDelayed(LHandle pursuit, int delayToMakePlayerChase, int delayToMakeCopsJoin)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.MakeActiveChase(delayToMakePlayerChase, delayToMakeCopsJoin);
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }


        /// <summary>
        /// Sets a value whether <paramref name="pursuit"/> is the active pursuit of the player.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="active">Whether the pursuit is the active player pursuit.</param>
        public static void SetPursuitIsActiveForPlayer(LHandle pursuit, bool active)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                if (active)
                {
                    p.SetAsCurrentPlayerChase();
                }
                else
                {
                    p.ClearAsCurrentPlayerChase();
                }
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets a value whether vehicles are allowed for suspects in <paramref name="pursuit"/>.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="allowVehicles">Whether vehicles are allowed.</param>
        public static void SetPursuitAllowVehiclesForSuspects(LHandle pursuit, bool allowVehicles)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.AllowSuspectVehicles = allowVehicles;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets a value whether weapons are allowed for suspects in <paramref name="pursuit"/>.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="allowWeapons">Whether weapons are allowed.</param>
        public static void SetPursuitAllowWeaponsForSuspects(LHandle pursuit, bool allowWeapons)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.AllowSuspectWeapons = allowWeapons;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets the maximum number of units for <paramref name="pursuit"/>.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="copsInVehicles">Maximum number of cops in vehicles. Default is 5.</param>
        /// <param name="copsOnFoot">Maximum number of cops on foot. Default is 20.</param>
        public static void SetPursuitMaximumUnits(LHandle pursuit, int copsInVehicles, int copsOnFoot)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.MaxCars = copsInVehicles;
                p.MaxUnits = copsOnFoot;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets a value whether <paramref name="pursuit"/> has been called in.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="calledIn">Whether the pursuit has been called in.</param>
        public static void SetPursuitCalledIn(LHandle pursuit, bool calledIn)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.HasBeenCalledIn = calledIn;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets whether cops can join <paramref name="pursuit"/>.
        /// </summary>
        /// <param name="pursuit">
        /// The pursuit.
        /// </param>
        /// <param name="canCopsJoin">
        /// Whether cops can join.
        /// </param>
        public static void SetPursuitCopsCanJoin(LHandle pursuit, bool canCopsJoin)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.CanCopsJoin = canCopsJoin;
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets the tactics of <paramref name="pursuit"/> to either aggressive or passive.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="aggressive">The tactics.</param>
        public static void SetPursuitTactics(LHandle pursuit, bool aggressive)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.ChangeTactics(aggressive ? EChaseTactic.Active : EChaseTactic.Passive);
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Sets the helicopter tactics of <paramref name="pursuit"/> to either aggressive or passive.
        /// </summary>
        /// <param name="pursuit">The pursuit.</param>
        /// <param name="aggressive">The tactics.</param>
        public static void SetPursuitHelicopterTactics(LHandle pursuit, bool aggressive)
        {
            if (pursuit == null)
            {
                throw new ArgumentNullException("pursuit");
            }

            if (pursuit.Object is Pursuit)
            {
                Pursuit p = pursuit.Object as Pursuit;
                p.ChangeHeliTactics(aggressive ? EHeliTactic.Active : EHeliTactic.Passive);
            }
            else
            {
                throw new ArgumentException("Pursuit handle is invalid.");
            }
        }

        /// <summary>
        /// Starts a callout called <paramref name="name"/>. If name is invalid, a random callout is started instead.
        /// </summary>
        /// <param name="name">
        /// The name of the callout.
        /// </param>
        /// <returns>
        /// The <see cref="LHandle"/> for the callout.
        /// </returns>
        public static LHandle StartCallout(string name)
        {
            return new LHandle(Main.CalloutManager.StartCallout(name));
        }

        /// <summary>
        /// Stops the currently running callout. Does nothing when no callout is running.
        /// </summary>
        public static void StopCurrentCallout()
        {
            if (Functions.IsCalloutRunning())
            {
                Main.CalloutManager.StopCallout();
            }
        }

        /// <summary>
        /// Invokes the <see cref="OnOnDutyStateChanged"/> event.
        /// </summary>
        /// <param name="onDuty">The new state</param>
        internal static void InvokeOnDutyStateChanged(bool onDuty)
        {
            if (OnOnDutyStateChanged != null)
            {
                OnOnDutyStateChanged(onDuty);
            }
        }

        /// <summary>
        /// Initializes all necessary data after LCPDFR was started.
        /// </summary>
        internal static void InitializeLCPDFRSpecific()
        {
            Main.PoliceComputer.PedHasBeenLookedUp += delegate(Persona persona) 
            { 
                if (PedLookedUpInPoliceComputer != null)
                {
                    PedLookedUpInPoliceComputer(PersonaData.FromPersona(persona));
                }
            };
        }
    }
}