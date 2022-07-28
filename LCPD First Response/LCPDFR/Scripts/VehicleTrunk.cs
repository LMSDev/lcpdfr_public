namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// The class responsible for handling the vehicle trunk logic.
    /// </summary>
    [ScriptInfo("VehicleTrunk", true)]
    internal class VehicleTrunk : GameScript
    {
        /// <summary>
        /// Whether player is currently searching.
        /// </summary>
        private bool isSearchingTrunk;

        private bool doorHelpShown;

        private bool removeHelpShown;

        private bool trunkHelpShown;

        private DateTime checkStart;

        /// <summary>
        /// The timer to get nearby vehicles only every half a second to improve performance.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleTrunk"/> class.
        /// </summary>
        public VehicleTrunk()
        {
            this.timer = new NonAutomaticTimer(500);
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.timer.CanExecute() && !KeyHandler.IsKeyDown(ELCPDFRKeys.OpenTrunk))
            {
                return;
            }

            // If not in PD, check if player is close to trunk of a vehicle
            if (!CPlayer.LocalPlayer.Ped.IsInVehicle && !LCPDFRPlayer.LocalPlayer.IsGrabbing)
            {
                // If close to police model, allow opening trunk or to open doors to get suspect out again
                if (!CPlayer.LocalPlayer.Ped.IsAiming && !CPlayer.LocalPlayer.IsTargettingAnything && !this.isSearchingTrunk && !CPlayer.LocalPlayer.Ped.IsGettingIntoAVehicle
                    && CPlayer.LocalPlayer.Ped.IsStandingStill)
                {
                    CVehicle vehicle = CPlayer.LocalPlayer.LastVehicle;
                    if (vehicle == null || !vehicle.Exists() || vehicle.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 4f)
                    {
                        vehicle = CPlayer.LocalPlayer.Ped.Intelligence.GetClosestVehicle(EVehicleSearchCriteria.NoCarsWithCopDriver, 4f);
                    }

                    if (vehicle != null && vehicle.Exists() && vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsVehicle) && !vehicle.Model.IsBike)
                    {                      
                        // Open door close to player
                        float vehicleWidth = vehicle.Model.GetDimensions().X;
                        float vehicleLength = 0;

                        // Adjust length for stockade
                        if (vehicle.Model.ModelInfo.Name.Contains("STOCKADE"))
                        {
                            vehicleLength = -vehicle.Model.GetDimensions().Y;
                        }

                        Vector3 leftRearDoorPosition = vehicle.GetOffsetPosition(new Vector3(-vehicleWidth / 2, vehicleLength / 2, 0));
                        Vector3 rightRearDoorPosition = vehicle.GetOffsetPosition(new Vector3(vehicleWidth / 2, vehicleLength / 2, 0));
                        Vector3 trunkPosition = vehicle.GetOffsetPosition(new Vector3(0, -vehicle.Model.GetDimensions().Y / 2, 0));

                        // Get closest door
                        List<KeyValuePair<int, float>> distances = new List<KeyValuePair<int, float>>();
                        distances.Add(new KeyValuePair<int, float>(1, CPlayer.LocalPlayer.Ped.Position.DistanceTo(leftRearDoorPosition)));
                        distances.Add(new KeyValuePair<int, float>(2, CPlayer.LocalPlayer.Ped.Position.DistanceTo(rightRearDoorPosition)));
                        distances.Add(new KeyValuePair<int, float>(0, CPlayer.LocalPlayer.Ped.Position.DistanceTo(trunkPosition)));
                        var dict = from entry in distances orderby entry.Value ascending select entry;
                        KeyValuePair<int, float> closest = dict.First();

                        // If close
                        if (closest.Value < 2f)
                        {
                            if (closest.Key == 0)
                            {
                                // Better position recognition
                                if (Common.IsNumberInRange(CPlayer.LocalPlayer.Ped.Heading, vehicle.Heading, 30f, 30f, 360) && closest.Value < 1.0f)
                                {
                                    if (!this.trunkHelpShown)
                                    {
                                        if ((DateTime.Now - this.checkStart).TotalSeconds > 2)
                                        {
                                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("VEHICLE_TRUNK_OPEN"));
                                            this.trunkHelpShown = true;
                                            DelayedCaller.Call(delegate { this.trunkHelpShown = false; }, this, 20000);
                                        }
                                    }

                                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.OpenTrunk))
                                    {
                                        CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
                                        CPlayer.LocalPlayer.Ped.Task.SlideToCoord(trunkPosition, vehicle.Heading, 0);
                                        this.isSearchingTrunk = true;
                                        DelayedCaller.Call(delegate { this.WaitForCorrectPosition(vehicle, 0); }, this, 500);
                                    }
                                }
                            }
                            else
                            {
                                // Only works for cop cars
                                if (vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                                {
                                    VehicleSeat seat = (VehicleSeat)closest.Key;
                                    VehicleDoor door = vehicle.GetDoorFromSeat(seat);

                                    if (vehicle.Door(door).Angle < 0.8f)
                                    {
                                        // If door isn't fully open (closed)
                                        if (!this.doorHelpShown)
                                        {
                                            if ((DateTime.Now - this.checkStart).TotalSeconds > 2)
                                            {
                                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("VEHICLE_DOOR_OPEN"));
                                                this.doorHelpShown = true;
                                                DelayedCaller.Call(delegate { this.doorHelpShown = false; }, this, 20000);
                                            }
                                        }

                                        // Handle key if they want to open it
                                        if (KeyHandler.IsKeyDown(ELCPDFRKeys.OpenTrunk))
                                        {
                                            Stats.UpdateStat(Stats.EStatType.DoorOpened, 1);
                                            CPlayer.LocalPlayer.Ped.Task.OpenPassengerDoor(vehicle, closest.Key);
                                        }
                                    }
                                    else
                                    {
                                        // If door is fully open
                                        if (vehicle.GetPedOnSeat(seat) != null && vehicle.GetPedOnSeat(seat).Exists())
                                        {
                                            CPed pedOnSeat = vehicle.GetPedOnSeat(seat);
                                            // If there is a ped on the seat, we can remove them.
                                            if (!removeHelpShown)
                                            {
                                                if (pedOnSeat.Exists() && pedOnSeat.PedData.Persona.Gender == Gender.Female && !LCPDFRPlayer.LocalPlayer.IsInTutorial)
                                                {
                                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("VEHICLE_DOOR_REMOVE_F"));
                                                }
                                                else if (pedOnSeat.Exists() && !LCPDFRPlayer.LocalPlayer.IsInTutorial)
                                                {
                                                    TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("VEHICLE_DOOR_REMOVE_M"));
                                                }
                                                removeHelpShown = true;
                                            }
                                            else
                                            {
                                                if (!TextHelper.IsHelpboxBeingDisplayed)
                                                {
                                                    if (pedOnSeat.Exists() && pedOnSeat.Gender == Gender.Female && !LCPDFRPlayer.LocalPlayer.IsInTutorial)
                                                    {
                                                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("VEHICLE_DOOR_REMOVE_F"));
                                                    }
                                                    else if (!LCPDFRPlayer.LocalPlayer.IsInTutorial)
                                                    {
                                                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("VEHICLE_DOOR_REMOVE_M"));
                                                    }
                                                }
                                            }

                                            // Handle key if they want to open it
                                            if (KeyHandler.IsKeyDown(ELCPDFRKeys.OpenTrunk))
                                            {
                                                if (pedOnSeat.Exists())
                                                {
                                                    CPlayer.LocalPlayer.Ped.APed.TaskCombatPullFromCarSubtask(pedOnSeat);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            /* 
                                             * TODO: Door closing (needs task though)
                                             * 
                                            if (CPlayer.LocalPlayer.Ped.Intelligence.CanSeePosition(leftRearDoorPosition) || CPlayer.LocalPlayer.Ped.Intelligence.CanSeePosition(rightRearDoorPosition))
                                            {
                                                // If not, close the door.
                                                // TODO: Close door task, LMS?
                                                foreach (CPed closePed in CPed.GetPedsAround(2.0f, EPedSearchCriteria.All, CPlayer.LocalPlayer.Ped.Position))
                                                {
                                                    if (closePed != null && closePed.Exists() && closePed.IsGettingIntoAVehicle)
                                                    {
                                                        // Only allow door to be closed if nobody is getting into it.
                                                        return;
                                                    }
                                                }

                                                vehicle.Door(door).Close();
                                            }
                                             * */
                                        }
                                    }                                   
                                }
                            }
                        }
                    }
                }
                else
                {
                    this.checkStart = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Called when the trunk should be opened.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        private void OpenTrunkCallback(CVehicle vehicle)
        {
            if (vehicle.Exists())
            {
                vehicle.Door(VehicleDoor.Trunk).Open();
                CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet("cop"), "copm_searchboot", 5f, AnimationFlags.Unknown05);

                if (vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                {
                    DelayedCaller.Call(delegate { this.CloseTrunkCallback(vehicle); }, this, Common.GetRandomValue(3000, 4000));
                }
                else
                {
                    DelayedCaller.Call(delegate { this.SearchTrunk(vehicle); }, this, Common.GetRandomValue(4000, 5000));
                }
            }
        }

        /// <summary>
        /// Waits for the correct position for the player before starting the animation.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="attempt">The number of attempts already made.</param>
        private void WaitForCorrectPosition(CVehicle vehicle, int attempt)
        {
            if (vehicle.Exists())
            {
                bool inPosition = !CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexUseSequence)
                    && Common.IsNumberInRange(CPlayer.LocalPlayer.Ped.Heading, vehicle.Heading, 10f, 10f, 360);

                if (attempt > 10 || inPosition)
                {
                    CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet("amb@bridgecops"), "open_boot", 5f, AnimationFlags.Unknown01);
                    DelayedCaller.Call(delegate { this.OpenTrunkCallback(vehicle); }, this, 800);
                }
                else
                {
                    DelayedCaller.Call(delegate { this.WaitForCorrectPosition(vehicle, attempt); }, this, 500);
                }
            }
        }

        /// <summary>
        /// Searches the vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        private void SearchTrunk(CVehicle vehicle)
        {
            // Search trunk of civilian car
            int randomValue = Common.GetRandomValue(0, 100);
            if (randomValue < 10 && !vehicle.Flags.HasFlag(EVehicleFlags.TrunkChecked))
            {
                vehicle.Flags |= EVehicleFlags.TrunkChecked;
                int type = Common.GetRandomValue(0, 2);
                if (type == 0)
                {
                    bool speech = Common.GetRandomBool(0, 2, 1);
                    DelayedCaller.Call(
                        delegate
                        {
                            CPlayer.LocalPlayer.Ped.SayAmbientSpeech(speech ? "FOUND_WEAPON_ON_PED" : "FOUND_GUN");
                            DelayedCaller.Call(delegate { TextHelper.PrintText(CultureHelper.GetText("TRUNK_FOUND_WEAPON"), 5000); }, this, 1000);
                        },
                        this,
                        3000);
                }
                else
                {
                    DelayedCaller.Call(
                        delegate
                        {
                            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("FOUND_STOLEN_CREDIT_CARDS");
                            DelayedCaller.Call(delegate { TextHelper.PrintText(CultureHelper.GetText("TRUNK_FOUND_CC"), 5000); }, this, 1000);
                        },
                        this,
                        3000);
                }
            }
            else
            {
                CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("kiss_my_ass", "gestures@male", 4.0f, false);
                TextHelper.PrintText(CultureHelper.GetText("TRUNK_FOUND_NOTHING"), 5000);
            }

            DelayedCaller.Call(delegate { this.CloseTrunkCallback(vehicle); }, this, 2000);
        }

        /// <summary>
        /// Called when the trunk should be closed.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        private void CloseTrunkCallback(CVehicle vehicle)
        {
            CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet("misscopbootsearch"), "close_boot", 5f, AnimationFlags.Unknown01);
            if (vehicle.Exists())
            {
                if (vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                {
                    // Equip weapons in police car
                    foreach (Weapon vehicleTrunkWeapon in Settings.VehicleTrunkWeapons)
                    {
                        CPlayer.LocalPlayer.Ped.Weapons[vehicleTrunkWeapon].Ammo += 100;
                    }

                    CPlayer.LocalPlayer.Ped.Health = 100;
                    CPlayer.LocalPlayer.Ped.Armor = 100;
                }
            }

            DelayedCaller.Call(delegate { this.CloseTrunkFinallyCallback(vehicle); }, this, 400);
        }

        /// <summary>
        /// Called when the trunk is finally is closed.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        private void CloseTrunkFinallyCallback(CVehicle vehicle)
        {
            this.isSearchingTrunk = false;
            if (vehicle.Exists())
            {
                vehicle.Door(VehicleDoor.Trunk).Close();
            }

            Stats.UpdateStat(Stats.EStatType.VehicleTrunkOpened, 1);
        }
    }
}