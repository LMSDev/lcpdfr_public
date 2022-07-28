namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.Engine.IO;

    /// <summary>
    /// Responsible for some ambient things, like unlocking police cars.
    /// </summary>
    [ScriptInfo("Ambient", true)]
    internal class Ambient : GameScript, ICanOwnEntities
    {
        /// <summary>
        /// The coffee mug object.
        /// </summary>
        private GTA.Object coffeeMug;

        /// <summary>
        /// The timer used for the key checks.
        /// </summary>
        private Engine.Timers.Timer keyTimer;

        /// <summary>
        /// The last vehicle of the player.
        /// </summary>
        private CVehicle lastPlayerVehicle;

        /// <summary>
        /// Whether or not the drinking sound was played
        /// </summary>
        private bool soundPlayed;

        /// <summary>
        /// Currently spawned ambient cop boats.
        /// </summary>
        private List<KeyValuePair<CVehicle, Blip>> copBoats;

        /// <summary>
        /// Spawn points for ambient cop boats.
        /// </summary>
        private SpawnPoint[] copBoatSpawnPoints;

        /// <summary>
        /// Timer for cop boats spawning.
        /// </summary>
        private NonAutomaticTimer copBoatTimer;

        /// <summary>
        /// Timer for triggering autosave.
        /// </summary>
        private Engine.Timers.Timer autoSaveTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ambient"/> class.
        /// </summary>
        public Ambient()
        {
            this.ContentManager.OnEntityBeingDisposed += this.ContentManager_OnEntityBeingDisposed;
            Main.KeyWatchDog.AlwaysUseKeyboardInput = Settings.AlwaysForceKeyboardInput;
            this.copBoats = new List<KeyValuePair<CVehicle, Blip>>();
            this.copBoatTimer = new NonAutomaticTimer(4000, ETimerOptions.Default);

            if (Settings.AutoSaveEnabled)
            {
                this.autoSaveTimer = new Engine.Timers.Timer(
                    30000,
                    delegate(object[] parameter)
                    {
                        Log.Info("Saving game", this);
                        Savegame.SaveCurrentGameToFile();
                        //TextHelper.PrintFormattedHelpBox("Autosaved.");
                    });
                this.autoSaveTimer.Start();
            }
            else
            {
                Log.Info("Autosave is disabled", this);
                AdvancedHookManaged.AGame.SetDontLaunchErrorReporter(true);
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            this.ProcessCuffeCopLogic();
            this.ProcessAmbientCopBoatLogic();

            // If player is getting into a police car, ensure he won't break into
            if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle))
            {
                CVehicle vehicle = CPlayer.LocalPlayer.GetVehiclePlayerWouldEnter();
                if (vehicle != null && vehicle.Exists())
                {
                    if (vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsPolice))
                    {
                        if (!vehicle.HasDriver)
                        {
                            // 7 is the doorlock vehicles have, that are locked and player can break in
                            if ((int)vehicle.DoorLock == 7)
                            {
                                vehicle.DoorLock = (DoorLock)1;
                                vehicle.NeedsToBeHotwired = false;
                            }
                        }
                    }
                }
            }

            // If player is in a vehicle, require it for mission
            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                if (CPlayer.LocalPlayer.Ped.CurrentVehicle.ContentManager == null && !CPlayer.LocalPlayer.Ped.CurrentVehicle.HasOwner)
                {
                    CPlayer.LocalPlayer.Ped.CurrentVehicle.RequestOwnership(this);
                    this.ContentManager.AddVehicle(CPlayer.LocalPlayer.Ped.CurrentVehicle, 80f);
                }

                // Remove old blip when player is in new vehicle
                if (this.lastPlayerVehicle != CPlayer.LocalPlayer.Ped.CurrentVehicle)
                {
                    // Remove old blip
                    if (this.lastPlayerVehicle != null && this.lastPlayerVehicle.Exists())
                    {
                        this.lastPlayerVehicle.DeleteBlip();
                        this.ContentManager.RemoveVehicle(this.lastPlayerVehicle);
                        if (this.lastPlayerVehicle.Owner == this)
                        {
                            this.lastPlayerVehicle.ReleaseOwnership(this);
                        }
                    }

                    this.lastPlayerVehicle = CPlayer.LocalPlayer.Ped.CurrentVehicle;
                }
                else
                {
                    // Attach car blip to player's last vehicle
                    if (this.lastPlayerVehicle != null && this.lastPlayerVehicle.Exists())
                    {
                        if (this.lastPlayerVehicle.HasBlip)
                        {
                            this.lastPlayerVehicle.Blip.Display = BlipDisplay.Hidden;
                        }
                    }
                }

                // If in cop vehicle, random chance to play radio chatter
                if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar) 
                    && Common.GetRandomBool(0, 1000, 1) && !LCPDFR.Main.PoliceComputer.IsActive && !AudioHelper.IsBusy && !Settings.DisableRandomPoliceChatter)
                {
                    AudioHelper.PlayActionSound("RANDOMCHAT");
                }
            }
            else
            {
                // If not in vehicle, show blip for last vehicle
                if (this.lastPlayerVehicle != null && this.lastPlayerVehicle.Exists())
                {
                    if (!this.lastPlayerVehicle.HasBlip)
                    {
                        this.lastPlayerVehicle.AttachBlip(sync: false).Icon = BlipIcon.Misc_TaxiRank;
                        this.lastPlayerVehicle.Blip.Scale = 0.5f;
                        this.lastPlayerVehicle.Blip.Display = BlipDisplay.MapOnly;
                        this.lastPlayerVehicle.Blip.Name = "Your car";
                    }
                    else
                    {
                        this.lastPlayerVehicle.Blip.Display = BlipDisplay.MapOnly;
                    }
                }
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            foreach (KeyValuePair<CVehicle, Blip> keyValuePair in this.copBoats)
            {
                CVehicle boat = keyValuePair.Key;
                if (boat.Exists())
                {
                    boat.NoLongerNeeded();
                }

                Blip blip = keyValuePair.Value;
                if (blip.Exists())
                {
                    blip.Delete();
                }
            }

            if (this.autoSaveTimer != null)
            {
                this.autoSaveTimer.Stop();
            }
        }

        /// <summary>
        /// Processes the coffee cup logic.
        /// </summary>
        private void ProcessCuffeCopLogic()
        {
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.DrinkCoffee))
            {
                // Detect computer of jam justin to prevent coffee cup access
                bool isJam = false;
                bool spammingHell = false;

                //if (Authentication.GetHardwareID() == "EE47D9E46C20AC71F243B4520E432FEF413B5A56")
                //{
                //    isJam = true;
                //}

                //if (!string.IsNullOrEmpty(Main.Authentication.Userdata.Username))
                //{
                //    if (Main.Authentication.Userdata.Username.Contains("jam")
                //        || Main.Authentication.Userdata.Username.Contains("justin"))
                //    {
                //        isJam = true;
                //    }
                //}

                //if (Environment.MachineName.Contains("justin"))
                //{
                //    isJam = true;
                //}

                //if (isJam && KeyHandler.IsKeyboardKeyStillDown(Keys.LShiftKey))
                //{
                //    TextHelper.PrintFormattedHelpBox("Fair enough...");
                //    isJam = false;
                //    spammingHell = true;
                //}

                //if (isJam && KeyHandler.IsKeyboardKeyStillDown(Keys.LControlKey))
                //{
                //    isJam = false;
                //}


                //if (isJam)
                //{
                //    string message = "Unfortunately, the coffee cup feature is not available in your state due to a copyright complaint by The Bean Machine Inc.";
                //    TextHelper.PrintFormattedHelpBox(message);
                //    return;
                //}

                if (this.keyTimer == null)
                {
                    this.keyTimer = new Engine.Timers.Timer(50, this.PlayerPressedKeyTimer, DateTime.Now, spammingHell);
                    this.keyTimer.Start();
                }
            }
        }

        /// <summary>
        /// Called when the player has pressed the cuffee mug key.
        /// </summary>
        /// <param name="parameter">The time the key has been pressed.</param>
        private void PlayerPressedKeyTimer(params object[] parameter)
        {
            TimeSpan timeElasped = DateTime.Now - (DateTime)parameter[0];
            bool spammingHell = (bool)parameter[1];

            if (spammingHell)
            {
                this.coffeeMug = World.CreateObject("amb_coffee", CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0, 0, 5).Around((float)new Random().NextDouble() * 2)));
                if (this.coffeeMug != null && this.coffeeMug.Exists())
                {
                    // Widespread them
                    Vector3 distance = this.coffeeMug.Position - CPlayer.LocalPlayer.Ped.Position;

                    this.coffeeMug.NoLongerNeeded();
                    this.coffeeMug.ApplyForceRelative(distance, Vector3.Zero);
                }


                return;
            }

            if (!KeyHandler.IsKeyStillDown(ELCPDFRKeys.DrinkCoffee))
            {
                // If key is no longer held down, either spawn or delete mug
                this.keyTimer.Stop();
                this.keyTimer = null;

                // If held down for less than 300 milliseconds
                if (timeElasped.TotalMilliseconds < 300)
                {
                    if (this.coffeeMug == null || !this.coffeeMug.Exists())
                    {
                        this.coffeeMug = World.CreateObject("amb_coffee", CPlayer.LocalPlayer.Ped.Position);
                        if (this.coffeeMug != null && this.coffeeMug.Exists())
                        {
                            this.coffeeMug.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.RightHand, Vector3.Zero, Vector3.Zero);
                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("hold_coffee", "amb@coffee_hold", 7.0f, true);
                        }
                    }
                    else
                    {
                        CPlayer.LocalPlayer.Ped.Task.ClearAll();
                        if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                        {
                            this.coffeeMug.Delete();
                        }
                        else
                        {
                            this.coffeeMug.NoLongerNeeded();
                            this.coffeeMug.Detach();
                        }

                        this.coffeeMug = null;
                    }
                }
                else
                {
                    // Reset animation if any
                    if (CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("amb@coffee_idle_m"), "drink_a"))
                    {
                        CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("hold_coffee", "amb@coffee_hold", 7.0f, true);
                    }
                }

                return;
            }

            if (timeElasped.TotalMilliseconds > 300)
            {
                // Make player drink
                if (this.coffeeMug != null && this.coffeeMug.Exists())
                {
                    if (!CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("amb@coffee_idle_m"), "drink_a"))
                    {
                        CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("drink_a", "amb@coffee_idle_m", 7.0f, true);
                        soundPlayed = false;
                    }
                    else
                    {
                        if (!soundPlayed && CPlayer.LocalPlayer.Ped.Animation.GetCurrentAnimationTime(new AnimationSet("amb@coffee_idle_m"), "drink_a") > 0.4f)
                        {
                            SoundEngine.PlaySoundFromObject(this.coffeeMug, "ANIM_CAN_MACHINE_DRINK");
                            soundPlayed = true;
                        }
                        else
                        {
                            if (CPlayer.LocalPlayer.Ped.Animation.GetCurrentAnimationTime(new AnimationSet("amb@coffee_idle_m"), "drink_a") > 0.99f)
                            {
                                soundPlayed = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes all logic related to spawning ambient cop boats and their blips, which player can enter for water callouts.
        /// </summary>
        private void ProcessAmbientCopBoatLogic()
        {
            // Hide blip if player entered boat.
            foreach (KeyValuePair<CVehicle, Blip> keyValuePair in this.copBoats)
            {
                if (keyValuePair.Key.Exists() && CPlayer.LocalPlayer.Ped.IsInVehicle(keyValuePair.Key))
                {
                    if (keyValuePair.Value.Exists())
                    {
                        keyValuePair.Value.Display = BlipDisplay.Hidden;
                    }
                }
            }

            if (!this.copBoatTimer.CanExecute())
            {
                return;
            }

            if (this.copBoatSpawnPoints == null)
            {
                List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
                string content = Properties.Resources.boatpos;
                Legacy.DataFile dataFile = new Legacy.DataFile(content);
                Legacy.DataSet dataSet = dataFile.DataSets[1];

                foreach (Legacy.Tag tag in dataSet.Tags)
                {
                    if (tag.Name != "BOATAMBIENT")
                    {
                        continue;
                    }

                    Vector3 carPos = Legacy.FileParser.ParseVector3(tag.GetAttributeByName("CARPOS").Value);
                    float heading = -1;
                    if (!float.TryParse(tag.GetAttributeByName("CARHEADING").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out heading))
                    {
                        Log.Warning("Failed to parse value: " + tag.GetAttributeByName("CARHEADING").Value, "Ambient");
                        continue;
                    }

                    SpawnPoint spawnPoint = new SpawnPoint(heading, carPos);
                    spawnPoints.Add(spawnPoint);
                }

                this.copBoatSpawnPoints = spawnPoints.ToArray();
            }

            // Draw 2 closest boats.
            var closestBoats = (from element in this.copBoatSpawnPoints
                                orderby element.Position.DistanceTo2D(CPlayer.LocalPlayer.Ped.Position)
                                select element).ToArray();

            // Free current boats.
            List<KeyValuePair<CVehicle,Blip>> itemsToRemove = new List<KeyValuePair<CVehicle, Blip>>();
            foreach (KeyValuePair<CVehicle, Blip> keyValuePair in this.copBoats)
            {
                // Invalidate spawn points that point to boats already existing.
                if (keyValuePair.Value.Exists())
                {
                    bool invalid = false;
                    for (int i = 0; i < 2; i++)
                    {
                        SpawnPoint closestBoat = closestBoats[i];
                        if (closestBoat.Position.DistanceTo(keyValuePair.Value.Position) < 2)
                        {
                            closestBoats[i] = SpawnPoint.Zero;
                            invalid = true;
                        }
                    }

                    if (invalid) continue;
                    itemsToRemove.Add(keyValuePair);
                }

                // If closest point is not an existing boat, free.
                CVehicle boat = keyValuePair.Key;
                if (boat.Exists())
                {
                    boat.NoLongerNeeded();
                }

                Blip blip = keyValuePair.Value;
                if (blip.Exists())
                {
                    blip.Delete();
                }
            }

            foreach (KeyValuePair<CVehicle, Blip> keyValuePair in itemsToRemove)
            {
                this.copBoats.Remove(keyValuePair);
            }

            // Create new boats.
            for (int i = 0; i < 2; i++)
            {
                if (closestBoats[i].Position == Vector3.Zero)
                {
                    continue;
                }

                CVehicle boat = new CVehicle("PREDATOR", closestBoats[i].Position, EVehicleGroup.Police);
                if (boat.Exists())
                {
                    boat.Heading = closestBoats[i].Heading;
                }

                Blip blip = Blip.AddBlip(closestBoats[i].Position);
                if (blip != null && blip.Exists())
                {
                    blip.Icon = BlipIcon.Activity_BoatTour;
                    blip.Name = "LCPD Boat Patrol";
                }

                this.copBoats.Add(new KeyValuePair<CVehicle, Blip>(boat, blip));
            }
        }

        /// <summary>
        /// Called when an entity has been disposed.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="onEnd">Whether call was made due to ending.</param>
        private void ContentManager_OnEntityBeingDisposed(CEntity entity, bool onEnd)
        {
            CVehicle vehicle = entity as CVehicle;
            vehicle.ReleaseOwnership(this);
        }
    }
}