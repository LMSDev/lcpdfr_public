using GTA.Native;

namespace LCPD_First_Response.Engine.Scripting.Native
{
    using GTA;

    // Note: Please don't use the natives directly, but wrap into them a property/function in a class
    class Natives
    {
        public static void AddStringToNewsScrollBar(string text)
        {
            Function.Call("ADD_STRING_TO_NEWS_SCROllBAR", text);
        }

        public static bool AreEnemyPedsInArea(Ped ped, float radius)
        {
            return Function.Call<bool>("ARE_ENEMY_PEDS_IN_AREA", ped, ped.Position.X, ped.Position.Y, ped.Position.Z, radius);
        }

        public static void AttachCamToPed(Camera camera, Ped ped)
        {
            Function.Call("ATTACH_CAM_TO_PED", camera, ped);
        }

        public static void BlockCharAmbientAnims(Ped ped, bool block)
        {
            Function.Call("BLOCK_CHAR_AMBIENT_ANIMS", ped, block);
        }

        public static void BlockCharGestureAnims(Ped ped, bool block)
        {
            Function.Call("BLOCK_CHAR_GESTURE_ANIMS", ped, block);
        }

        public static void ChangeCharSitIdleAnim(Ped ped, string animationSet, string animationName)
        {
            Function.Call("CHANGE_CHAR_SIT_IDLE_ANIM", ped, animationSet, animationName, 1);
        }

        public static void ClearAreaOfCars(Vector3 position, float radius)
        {
            Function.Call("CLEAR_AREA_OF_CARS", position.X, position.Y, position.Z, radius);
        }

        public static void ClearAreaOfChars(Vector3 position, float radius)
        {
            Function.Call("CLEAR_AREA_OF_CHARS", position.X, position.Y, position.Z, radius);
        }

        public static void ClearCharLastDamageEntity(Ped ped)
        {
            Function.Call("CLEAR_CHAR_LAST_DAMAGE_ENTITY", ped);
        }

        public static void ClearCharLastWeaponDamage(Ped ped)
        {
            Function.Call("CLEAR_CHAR_LAST_WEAPON_DAMAGE", ped);
        }

        public static void ClearCharProp(Ped ped, int a0)
        {
            Function.Call("CLEAR_CHAR_PROP", ped, a0);
        }

        public static void ClearCharProps(Ped ped)
        {
            Function.Call("CLEAR_ALL_CHAR_PROPS", ped);
        }

        public static void ClearHelp()
        {
            Function.Call("CLEAR_HELP");
        }

        public static bool CreateEmergencyServicesCarReturnDriver(Model name, Model pedModel, Vector3 pos, ref Ped driver, ref Ped passenger, ref Vehicle vehicle)
        {
            RequestModel(name.Hash);
            RequestModel(pedModel.Hash);
            Pointer pntDriver = typeof(Ped);
            Pointer pntPassenger = typeof(Ped);
            Pointer pntVehicle = typeof(Vehicle);
            bool response = Function.Call<bool>("CREATE_EMERGENCY_SERVICES_CAR_RETURN_DRIVER", name.Hash, pos.X, pos.Y, pos.Z, pntVehicle, pntDriver, pntPassenger);
            if (response)
            {
                driver = (Ped)pntDriver.Value;
                passenger = (Ped)pntPassenger.Value;
                vehicle = (Vehicle)pntVehicle.Value;
            }
            else
            {
                return false;
            }

            return true;
        }

        public static void CreateEmergencyServicesCarThenWalk(Model model, Vector3 position)
        {
            RequestModel(model.Hash);
            Function.Call("CREATE_EMERGENCY_SERVICES_CAR_THEN_WALK", model, position.X, position.Y, position.Z);
        }

        public static void DamageChar(Ped ped, int amount)
        {
            Function.Call("DAMAGE_CHAR", ped, amount);
        }

        public static void DisablePoliceScanner()
        {
            Function.Call("DISABLE_POLICE_SCANNER");
        }

        public static void DisplayHUD(bool display)
        {
            Function.Call("DISPLAY_HUD", display);
        }

        public static bool DoesCarHaveRoof(Vehicle vehicle)
        {
            return Function.Call<bool>("DOES_CAR_HAVE_ROOF", vehicle);
        }

        public static void DrawColouredCylinder(Vector3 position, float unknown, float unknown2, System.Drawing.Color color)
        {
            Function.Call("DRAW_COLOURED_CYLINDER", position.X, position.Y, position.Z, unknown, unknown2, color.R, color.G, color.B, color.A);
        }

        public static void DrawCorona(Vector3 position, float size, int unknown, float unknown2, System.Drawing.Color color)
        {
            Function.Call("DRAW_CORONA", position.X, position.Y, position.Z, size, unknown, unknown2, color.R, color.G, color.B);
        }

        public static void DrawRect(float x, float y, float width, float height, System.Drawing.Color color)
        {
            Function.Call("DRAW_RECT", x, y, width, height, color.R, color.G, color.B, color.A);
        }

        public static void EnablePoliceScanner()
        {
            Function.Call("ENABLE_POLICE_SCANNER");
        }

        public static void FlashBlip(GTA.Blip blip, bool value)
        {
            Function.Call("FLASH_BLIP", blip, value);
        }

        public static bool GetClosestCarNodeWithHeading(GTA.Vector3 position, ref GTA.Vector3 closestNode, ref float heading)
        {
            Pointer f0 = typeof(float), f1 = typeof(float), f2 = typeof(float), f3 = typeof(float);

            bool retValue = Function.Call<bool>("GET_CLOSEST_CAR_NODE_WITH_HEADING", position.X, position.Y, position.Z, f0, f1, f2, f3);
            closestNode = new Vector3((float)f0.Value, (float)f1.Value, (float)f2.Value);
            heading = (float)f3.Value;
            return retValue;
        }

        public static int GetCharSpeed(GTA.Ped ped)
        {
            GTA.Native.Pointer speed = new GTA.Native.Pointer(typeof(float));
            Function.Call("GET_CHAR_SPEED", ped, speed);
            return speed;
        }

        public static int GetInteriorAtCoords(GTA.Vector3 position)
        {
            GTA.Native.Pointer interior = new GTA.Native.Pointer(typeof(int));
            Function.Call("GET_INTERIOR_AT_COORDS", position.X, position.Y, position.Z, interior);
            return interior;
        }

        public static int GetNetworkIDFromPed(GTA.Ped ped)
        {
            Pointer pointer = new Pointer(typeof(int));
            Function.Call("GET_NETWORK_ID_FROM_PED", ped, pointer);
            return (int)pointer.Value;
        }

        public static int GetNetworkIDFromVehicle(GTA.Vehicle vehicle)
        {
            Pointer pointer = new Pointer(typeof (int));
            Function.Call("GET_NETWORK_ID_FROM_VEHICLE", vehicle, pointer);
            return (int) pointer.Value;
        }

        public static int GetNumberOfInstancesOfStreamedScript(string name)
        {
            return Function.Call<int>("GET_NUMBER_OF_INSTANCES_OF_STREAMED_SCRIPT", name);
        }

        public static void GetPedFromNetworkID(int networkID, ref GTA.Ped ped)
        {
            Pointer pointer = new Pointer(typeof(GTA.Ped));
            Function.Call("GET_PED_FROM_NETWORK_ID", networkID, pointer);
            ped = pointer.Value as GTA.Ped;
        }

        public static GTA.Vehicle GetVehiclePlayerWouldEnter()
        {
            Pointer vehiclePointer = typeof(Vehicle);
            Function.Call("GET_VEHICLE_PLAYER_WOULD_ENTER", Game.LocalPlayer, vehiclePointer);
            Vehicle vehicle = (Vehicle)vehiclePointer.Value;
            return vehicle;
        }

        public static int GetSoundID()
        {
            return Function.Call<int>("GET_SOUND_ID");
        }

        public static bool GetTextInputActive()
        {
            return Function.Call<bool>("GET_TEXT_INPUT_ACTIVE");
        }

        public static void GetVehicleFromNetworkID(int networkID, ref GTA.Vehicle vehicle)
        {
            Pointer pointer = new Pointer(typeof(GTA.Vehicle));
            Function.Call("GET_VEHICLE_FROM_NETWORK_ID", networkID, pointer);
            vehicle = pointer.Value as GTA.Vehicle;
        }

        public static bool GetViewportPositionOfCoord(Vector3 viewportPosition, EViewportID viewportID, out Vector2 screenPosition)
        {
            Pointer x = new Pointer(typeof(float));
            Pointer y = new Pointer(typeof(float));

            bool ret = Function.Call<bool>("GET_VIEWPORT_POSITION_OF_COORD", viewportPosition.X, viewportPosition.Y, viewportPosition.Z, (int)viewportID, x, y);

            screenPosition.X = x;
            screenPosition.Y = y;
            return ret;
        }

        public static void HandleAudioAnimEvent(GTA.Ped ped, string audioEvent)
        {
            Function.Call("HANDLE_AUDIO_ANIM_EVENT", ped, audioEvent);
        }

        public static bool HasControlOfNetworkID(int id)
        {
            return Function.Call<bool>("HAS_CONTROL_OF_NETWORK_ID", id);
        }

        [System.Obsolete("Called a lot and fucking slow! Better wrap in adv hook")]
        public static bool HasCharSpottedChar(GTA.Ped ped, GTA.Ped ped2)
        {
            return Function.Call<bool>("HAS_CHAR_SPOTTED_CHAR", ped, ped2);
        }

        [System.Obsolete("Called a lot and fucking slow! Better wrap in adv hook")]
        public static bool HasCharSpottedCharInFront(GTA.Ped ped, GTA.Ped ped2)
        {
            return Function.Call<bool>("HAS_CHAR_SPOTTED_CHAR_IN_FRONT", ped, ped2);
        }

        public static bool HasModelLoaded(int hash)
        {
            return Function.Call<bool>("HAS_MODEL_LOADED", hash);
        }

        public static bool HasSoundFinished(int soundID)
        {
            return Function.Call<bool>("HAS_SOUND_FINISHED", soundID);
        }

        public static bool IsAmbientSpeechPlaying(GTA.Ped ped)
        {
            return Function.Call<bool>("IS_AMBIENT_SPEECH_PLAYING", ped);
        }

        public static bool IsBulletInArea(Vector3 position, float radius)
        {
            return Function.Call<bool>("IS_BULLET_IN_AREA", position.X, position.Y, position.Z, radius, true);
        }

        public static bool IsBigVehicle(GTA.Vehicle vehicle)
        {
            return Function.Call<bool>("IS_BIG_VEHICLE", vehicle);
        }

        public static bool IsCarInWater(GTA.Vehicle vehicle)
        {
            return Function.Call<bool>("IS_CAR_IN_WATER", vehicle);
        }

        public static bool IsCarStopped(GTA.Vehicle vehicle)
        {
            return Function.Call<bool>("IS_CAR_STOPPED", vehicle);
        }

        public static bool IsCharArmed(GTA.Ped ped, int slot)
        {
            return Function.Call<bool>("IS_CHAR_ARMED", ped, slot);
        }

        public static bool IsCharFacingChar(GTA.Ped ped, GTA.Ped ped2)
        {
            return Function.Call<bool>("IS_CHAR_FACING_CHAR", ped, ped2);
        }

        public static bool IsCharDucking(Ped ped)
        {
            return Function.Call<bool>("IS_CHAR_DUCKING", ped);
        }

        public static bool IsHelpMessageBeingDisplayed()
        {
            return Function.Call<bool>("IS_HELP_MESSAGE_BEING_DISPLAYED");
        }

        public static bool IsPlayerTargettingAnything(GTA.Player player)
        {
            return Function.Call<bool>("IS_PLAYER_TARGETTING_ANYTHING", player);
        }

        public static bool IsPlayerTargettingChar(GTA.Player player, GTA.Ped ped)
        {
            return Function.Call<bool>("IS_PLAYER_TARGETTING_CHAR", player, ped);
        }

        public static bool IsThisMachineTheServer()
        {
            return Function.Call<bool>("IS_THIS_MACHINE_THE_SERVER");
        }

        public static bool IsVehStuck(GTA.Vehicle vehicle, int stuckSinceMs, int unknown, int unknown2, int unknown3)
        {
            return Function.Call<bool>("IS_VEH_STUCK", vehicle, stuckSinceMs, unknown, unknown, unknown2);
        }

        public static bool IsUsingController()
        {
            return Function.Call<bool>("IS_USING_CONTROLLER");
        }

        public static void MarkModelAsNoLongerNeeded(Model model)
        {
            Function.Call("MARK_MODEL_AS_NO_LONGER_NEEDED", model.Hash);
        }

        public static void MissionAudioBankNoLongerNeeded()
        {
            Function.Call("MISSION_AUDIO_BANK_NO_LONGER_NEEDED");
        }

        public static void ModifyCharMoveState(GTA.Ped ped, EPedMoveState moveState)
        {
            Function.Call("MODIFY_CHAR_MOVE_STATE", ped, (int) moveState);
        }

        public static string NetworkGetHostServerName()
        {
            return Function.Call<string>("NETWORK_GET_HOST_SERVER_NAME");
        }        
        
        public static string NetworkGetServerName()
        {
            return Function.Call<string>("NETWORK_GET_SERVER_NAME");
        }

        /// <summary>
        /// Name musn't be longer than 32 chars
        /// </summary>
        /// <param name="name"></param>
        public static void NetworkSetServerName(string name)
        {
            Function.Call("NETWORK_SET_SERVER_NAME", name);
        }

        public static void PlaceCamBehindPed(GTA.Ped ped)
        {
            Function.Call("SET_CAM_BEHIND_PED", ped);
        }

        public static void PlaceCamInFrontOfPed(GTA.Ped ped)
        {
            Function.Call("SET_CAM_IN_FRONT_OF_PED", ped);
        }

        public static void PlaySoundFrontend(int soundID, string fileName)
        {
            Function.Call("PLAY_SOUND_FRONTEND", soundID, fileName);
        }

        public static void PlaySoundFromObject(int soundID, string soundName, GTA.Object o)
        {
            Function.Call("PLAY_SOUND_FROM_OBJECT", soundID, soundName, o);
        }

        public static void PlaySoundFromPed(int soundID, string soundName, GTA.Ped p)
        {
            Function.Call("PLAY_SOUND_FROM_PED", soundID, soundName, p);
        }

        public static void PlaySoundFromVehicle(int soundID, string soundName, GTA.Vehicle v)
        {
            Function.Call("PLAY_SOUND_FROM_VEHICLE", soundID, soundName, v);
        }


        public static void PrintStringWithLiteralStringNow(string text, int duration)
        {
            Function.Call("PRINT_STRING_WITH_LITERAL_STRING_NOW", "STRING", text, duration, 1);
        }

        public static void ReleaseSoundID(int soundID)
        {
            Function.Call("RELEASE_SOUND_ID", soundID);
        }

        public static void RemoveAllCharWeapons(GTA.Ped ped)
        {
            Function.Call("REMOVE_ALL_CHAR_WEAPONS", ped);
        }

        public static void RemoveCarWindow(GTA.Vehicle vehicle, GTA.VehicleWindow vehicleWindow)
        {
            Function.Call("REMOVE_CAR_WINDOW", vehicle, (int)vehicleWindow);
        }

        public static void RequestAnims(string anims)
        {
            Function.Call("REQUEST_ANIMS", anims);
        }

        public static bool RequestControlOfNetworkID(int id)
        {
            return Function.Call<bool>("REQUEST_CONTROL_OF_NETWORK_ID", id);
        }

        public static bool RequestMissionAudioBank(string name)
        {
            return Function.Call<bool>("REQUEST_MISSION_AUDIO_BANK", name);
        }

        public static void RequestModel(int hash)
        {
            Function.Call("REQUEST_MODEL", hash);
        }

        public static void SayAmbientSpeechWithVoice(GTA.Ped ped, string speech, string voice)
        {
            Function.Call("SAY_AMBIENT_SPEECH_WITH_VOICE", ped, speech, voice, 1, 1, 2);
        }

        public static void SetAnimGroupForChar(GTA.Ped ped, string animGroup)
        {
            Function.Call("SET_ANIM_GROUP_FOR_CHAR", ped, animGroup);
        }

        public static void SetCamAttachOffset(Camera camera, Vector3 offset)
        {
            Function.Call("SET_CAM_ATTACH_OFFSET", camera, offset.X, offset.Y, offset.Z);
        }

        public static void SetCameraControlsDisabledWithPlayerControls(bool disabledWithPlayerControl)
        {
            Function.Call("SET_CAMERA_CONTROLS_DISABLED_WITH_PLAYER_CONTROLS", disabledWithPlayerControl);
        }

        public static void SetCarCanGoAgainstTraffic(GTA.Vehicle vehicle, bool value)
        {
            Function.Call("SET_CAR_CAN_GO_AGAINST_TRAFFIC", vehicle, value);
        }

        public static void SetCarCollision(GTA.Vehicle vehicle, bool value)
        {
            Function.Call("SET_CAR_COLLISION", vehicle, value);
        }

        public static void SetCharBleeding(GTA.Ped ped, bool value)
        {
            Function.Call("SET_CHAR_BLEEDING", ped, value);
        }

        public static void SetCharCollision(GTA.Ped ped, bool value)
        {
            Function.Call("SET_CHAR_COLLISION", ped, value);
        }

        public static void SetCharComponentVariation(Ped ped, int a0, int a1, int a2)
        {
            Function.Call("SET_CHAR_COMPONENT_VARIATION", ped, a0, a1, a2);
        }

        public static void SetCharCoordinatesDontWarpGang(Ped ped, Vector3 pos)
        {
            Function.Call("SET_CHAR_COORDINATES_DONT_WARP_GANG", ped, pos.X, pos.Y, pos.Z);
        }

        public static void SetCharDrownsInSinkingVehicle(Ped ped, bool value)
        {
            Function.Call("SET_CHAR_DROWNS_IN_SINKING_VEHICLE", ped, value);
        }

        public static void SetCharDrownsInWater(Ped ped, bool value)
        {
            Function.Call("SET_CHAR_DROWNS_IN_WATER", ped, value);
        }

        public static void SetCharReadyToBeExecuted(Ped ped, bool value)
        {
            Function.Call("SET_CHAR_READY_TO_BE_EXECUTED", ped, value);
        }

        public static void SetCharReadyToBeStunned(Ped ped, bool value)
        {
            Function.Call("SET_CHAR_READY_TO_BE_STUNNED", ped, value);
        }

        public static void SetCharShootRate(GTA.Ped ped, int value)
        {
            Function.Call("SET_CHAR_SHOOT_RATE", ped, value);
        }

        public static void SetCharWillUseCover(GTA.Ped ped, bool value)
        {
            Function.Call("SET_CHAR_WILL_USE_COVER", ped, value);
        }

        public static void SetCharWillLeaveCarInCombat(GTA.Ped ped, bool value)
        {
            Function.Call("SET_CHAR_WILL_LEAVE_CAR_IN_COMBAT", ped, value);
        }

        public static void SetCharWillTryToLeaveWater(Ped ped, bool value)
        {
            GTA.Native.Function.Call("SET_CHAR_WILL_TRY_TO_LEAVE_WATER", ped, value);
        }

        public static void SetCreateRandomCops(bool value)
        {
            GTA.Native.Function.Call("SET_CREATE_RANDOM_COPS", value);
        }

        public static void SetCurrentCharWeapon(GTA.Ped ped, GTA.Weapon weapon, bool unknown)
        {
            Function.Call("SET_CURRENT_CHAR_WEAPON", ped, (int)weapon, unknown);
        }

        public static void SetDontActivateRagdollFromPlayerImpact(GTA.Ped ped, bool value)
        {
            Function.Call("SET_DONT_ACTIVATE_RAGDOLL_FROM_PLAYER_IMPACT", ped, value);
        }

        public static void SetHeliBladesFullSpeed(GTA.Vehicle vehicle)
        {
            Function.Call("SET_HELI_BLADES_FULL_SPEED", vehicle);
        }

        public static void SetHeliStabiliser(GTA.Vehicle vehicle)
        {
            Function.Call("SET_HELI_STABILISER", vehicle);
        }

        public static void SetInterpFromGameToScript(bool interpolate, int time)
        {
            Function.Call("SET_INTERP_FROM_GAME_TO_SCRIPT", interpolate, time);
        }

        public static void SetInterpFromScriptToGame(bool interpolate, int time)
        {
            Function.Call("SET_INTERP_FROM_SCRIPT_TO_GAME", interpolate, time);   
        }

        public static void SetGameCameraControlsActive(bool active)
        {
            Function.Call("SET_GAME_CAMERA_CONTROLS_ACTIVE", active);
        }
        
        public static void SetMsgForLoadingScreen(string text)
        {
            Function.Call("SET_MSG_FOR_LOADING_SCREEN", text);
        }
            
        public static void SetNetworkIDCanMigrate(int id, bool value)
        {
            Function.Call("SET_NETWORK_ID_CAN_MIGRATE", id, value);
        }

        public static void SetNetworkIDExistsOnAllMachines(int id, bool value)
        {
            Function.Call("SET_NETWORK_ID_EXISTS_ON_ALL_MACHINES", id, value);
        }

        public static void SetNextDesiredMoveState(EPedMoveState moveState)
        {
            Function.Call("SET_NEXT_DESIRED_MOVE_STATE", (int)moveState);
        }

        public static void SetParkedCarDensityMultiplier(float value)
        {
            Function.Call("SET_PARKED_CAR_DENSITY_MULTIPLIER", value);
        }

        public static void SetPedAlpha(GTA.Ped ped, byte alpha)
        {
            Function.Call("SET_PED_ALPHA", ped, alpha);
        }

        public static void SetPedDensityMultiplier(float value)
        {
            Function.Call("SET_PED_DENSITY_MULTIPLIER", value);
        }

        public static void SetPedWontAttackPlayerWithoutWantedLevel(GTA.Ped ped, bool value)
        {
            Function.Call("SET_PED_WONT_ATTACK_PLAYER_WITHOUT_WANTED_LEVEL", ped, value);
        }

        public static void SetRadarZoom(int zoomLevel)
        {
            Function.Call("SET_RADAR_ZOOM", zoomLevel);
        }

        public static void SetRandomCarDensityMultiplier(float value)
        {
            Function.Call("SET_RANDOM_CAR_DENSITY_MULTIPLIER", value);
        }

        public static void SetScenarioPedDensityMultiplier(float value)
        {
            Function.Call("SET_SCENARIO_PED_DENSITY_MULTIPLIER", value);
        }

        public static void SetTextInputActive(bool value)
        {
            Function.Call("SET_TEXT_INPUT_ACTIVE", value);
        }

        public static void StopSound(int id)
        {
            Function.Call("STOP_SOUND", id);
        }
        public static void SwitchGarbageTrucks(bool value)
        {
            Function.Call("SWITCH_GARBAGE_TRUCKS", value);
        }

        public static void SwitchRandomBoats(bool value)
        {
            Function.Call("SWITCH_RANDOM_BOATS", value);
        }

        public static void SwitchRandomTrains(bool value)
        {
            Function.Call("SWITCH_RANDOM_TRAINS ", value);
        }

        public static void TaskAchieveHeading(GTA.Ped ped, float heading)
        {
            Function.Call("TASK_ACHIEVE_HEADING", ped, heading);
        }

        public static void TaskCarMission(GTA.Ped ped, GTA.Vehicle vehicle, int unknown, EVehicleDrivingStyle drivingStyle, float speed, int unknown2, int unknown3, int unknown4)
        {
            Function.Call("TASK_CAR_MISSION", ped, vehicle, unknown, (int)drivingStyle, speed, unknown2, unknown3, unknown4);
        }

        public static void TaskCarMission(GTA.Ped ped, GTA.Vehicle vehicle, Vector3 position, int unknown, float speed, int drivingStyle, int unknown2=1, int unknown3=1)
        {
            Function.Call("TASK_CAR_MISSION_COORS_TARGET", ped, vehicle, position.X, position.Y, position.Z, unknown, speed, drivingStyle, unknown2, unknown3);
        }

        public static void TaskCarTempAction(GTA.Ped ped, GTA.Vehicle vehicle, ECarTempActionType type, int duration)
        {
            Function.Call("TASK_CAR_TEMP_ACTION", ped, vehicle, (int)type, duration);
        }

        public static void TaskCharArrestChar(GTA.Ped ped, GTA.Ped pedToArrest)
        {
            Function.Call("TASK_CHAR_ARREST_CHAR", ped, pedToArrest);
        }

        public static void TaskCharSlideToCoord(GTA.Ped ped, GTA.Vector3 position, float heading, float speed)
        {
            Function.Call("TASK_CHAR_SLIDE_TO_COORD", ped, position.X, position.Y, position.Z, heading, speed);
        }

        public static void TaskCower(GTA.Ped ped)
        {
            Function.Call("TASK_COWER", ped);
        }

        public static void TaskClimb(GTA.Ped ped, int type)
        {
            Function.Call("TASK_CLIMB", ped, type);
        }

        public static void TaskDriveBy(GTA.Ped ped, GTA.Ped target, int unknown, float unknown2, float unknown3, float unknown4, float unknown5Distance, int unknown6, int unknown7, int unknown8)
        {
            Function.Call("TASK_DRIVE_BY", ped, target, unknown, unknown2, unknown3, unknown4, unknown5Distance, unknown6, unknown7, unknown8);
        }

        public static void TaskFleeCharAnyMeans(GTA.Ped ped, GTA.Ped pedToFleeFrom, float unknown, int unknown2, int unknown3, int unknown4, int unknown5, float unknown6)
        {
            Function.Call("TASK_FLEE_CHAR_ANY_MEANS", ped, pedToFleeFrom, unknown, unknown2, unknown3, unknown4, unknown5, unknown6);
        }

        public static void TaskGotoCharAiming(GTA.Ped ped, GTA.Ped pedToAimAt, float stopAndStartAimingDistance, float startAimingWhileRunningDistance)
        {
            Function.Call("TASK_GOTO_CHAR_AIMING", ped, pedToAimAt, stopAndStartAimingDistance, startAimingWhileRunningDistance);
        }

        public static void TaskGoToCoordWhileAiming(GTA.Ped ped, Vector3 position, EPedMoveState moveState, int unknown, int unknown2, int targetType, Vector3 aimingPosition, GTA.Ped aimingPedTarget)
        {
            Function.Call("TASK_GO_TO_COORD_WHILE_AIMING", ped, position.X, position.Y, position.Z, (int)moveState, unknown, unknown2, targetType, aimingPosition.X, aimingPosition.Y, aimingPosition.Z, 0);
        }

        public static void TaskHeliMission(GTA.Ped ped, GTA.Vehicle vehicle, int unknown, int unknown2, Vector3 position, int unknown4, float speed, int unknown5, float unknown6, int unknown7, int maxHeight)
        {
            Function.Call("TASK_HELI_MISSION", ped, vehicle, unknown, unknown2, position.X, position.Y, position.Z, unknown4, speed, unknown5, unknown6, unknown7, maxHeight);
        }

        public static void TaskJump(GTA.Ped ped, int type)
        {
            Function.Call("TASK_JUMP", ped, type);
        }

        public static void TaskLookAtChar(GTA.Ped ped, GTA.Ped pedToLookAt, int duration, EPedLookType lookType)
        {
            Function.Call("TASK_LOOK_AT_CHAR", duration, (int) lookType);
        }

        public static void TaskLookAtCoord(GTA.Ped ped, GTA.Vector3 position, int duration, EPedLookType lookType)
        {
            Function.Call("TASK_LOOK_AT_COORD", ped, position.X, position.Y, position.Z, duration, (int)lookType);
        }

        public static void TaskOpenPassengerDoor(GTA.Ped ped, GTA.Vehicle vehicle, GTA.VehicleSeat seat, int unknown)
        {
            Function.Call("TASK_OPEN_PASSENGER_DOOR", ped, vehicle, (int)seat, unknown);
        }

        public static void TaskPlayAnimSecondaryUpperBody(GTA.Ped ped, string animation, string animationSet, float unknown1, bool loop, int unknown2, int unknown3, int unknown4, int unknown5)
        {
            Function.Call("TASK_PLAY_ANIM_SECONDARY_UPPER_BODY", ped, animation, animationSet, unknown1, loop, unknown2, unknown3, unknown4, unknown5);
        }

        public static void TaskPlayAnimSecondaryUpperBody(GTA.Ped ped, string animation, string animationSet, bool loop)
        {
            Function.Call("TASK_PLAY_ANIM_SECONDARY_UPPER_BODY", ped, animation, animationSet, 8.0f, loop, 0, 0, 0, -1);
        }

        public static void TaskShuffleToNextCarSeat(GTA.Ped ped, GTA.Vehicle vehicle)
        {
            Function.Call("TASK_SHUFFLE_TO_NEXT_CAR_SEAT", ped, vehicle);
        }

        public static void TerminateAllScriptsWithThisName(string name)
        {
            Function.Call("TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME", name);
        }

        public static void WarpCharFromCar(GTA.Ped ped, Vector3 position)
        {
            Function.Call("WARP_CHAR_FROM_CAR_TO_COORD", ped, position.X, position.Y, position.Z);
        }

        public static void WarpCharIntoCharAsPassenger(GTA.Ped ped, GTA.Vehicle vehicle, GTA.VehicleSeat seat)
        {
            Function.Call("WARP_CHAR_INTO_CAR_AS_PASSENGER", ped, vehicle, (int)seat);
        }
    }

    /// <summary>
    /// Slow down = No longer speed up (so usually no brake lights)
    /// Slow down softly = Slow down softly (without skid marks) (so usually  brake lights)
    /// Slow down hardly = Slow down really hard (with skid marks) (so usually brake lights)
    /// </summary>
    enum ECarTempActionType
    {
        SlowDownSoftly = 1,
        /// <summary>
        /// This is probably used when player drives into a mission blip, to stop the car and deactive the player control. Turns on brake lights
        /// </summary>
        SlowDownHardPedNotUseable = 2,
        SlowDownSoftlyThenBackwards = 3,
        SlowDownHardTurnLeft = 4,
        SlowDownHardTurnRight = 5,
        /// <summary>
        /// Same (not as hard?) as 2, but doesn't turn on brake lights
        /// </summary>
        SlowDownHard = 6,
        SpeedUpLeft = 7,
        SpeedUpRight = 8,
        SpeedUpForwardsSoflty = 9,
        TurnRightSoftly = 10,
        TurnLeftSoftly = 11,
        /// <summary>
        /// Same as two
        /// </summary>
        SlowDownHardPedNotUseable2 = 12,
        SlowDownLeftSoftlyThenSpeedUpBackwardsLeft = 13,
        SlowDownRightSoftlyThenSpeedUpBackwardsRight = 14,
        /// <summary>
        /// Same as 2 but doesn't deactivate player control
        /// </summary>
        SlowDownHard2 = 15,
        /// <summary>
        /// Same as 2 but doesn't deactivate player control
        /// </summary>
        SlowDownHard3 = 16,
        /// <summary>
        /// Same as 2 but doesn't deactivate player control
        /// </summary>
        SlowDownHard4 = 17,
        /// <summary>
        /// Same as 2 but doesn't deactivate player control
        /// </summary>
        SlowDownHard5 = 18,
        SlowDownTurnLeft = 19,
        SlowDownSoftlyTurnLeft = 20,
        SlowDownSoftlyTurnRight = 21,
        SlowDownSoftlyThenBackwards2 = 22,
        SpeedUpForwards = 23,
        /// <summary>
        /// Same as 2 but doesn't deactivate player control
        /// </summary>
        SlowDownHard6 = 24,
        /// <summary>
        /// Same as 4 but gives player control back (so terminates task) as soon as the car is stopped
        /// </summary>
        SlowDownHardTurnLeftInstantEnd = 25,
        // Probably more...
    }

    /// <summary>
    /// The driving style for a vehicle.
    /// </summary>
    enum EVehicleDrivingStyle
    {
        /// <summary>
        /// Stops hard and keeps task running.
        /// </summary>
        Stop = 5,
        /// <summary>
        /// Parks the vehicle to the right.
        /// </summary>
        ParkToTheRight = 21,
    }

    enum EPedLookType
    {
        MoveHeadALittle = 0,
        MoveHeadAsMuchAsPossible = 4,
    }

    enum EPedMoveState
    {
        None,
        DontUse_1,
        Walk,
        Run,
        Sprint,
    }

    internal enum EJumpType
    {
        Stand,
        Front,
        Front2,
    }

    /// <summary>
    /// The different viewports.
    /// </summary>
    internal enum EViewportID
    {
        /// <summary>
        /// CViewportPrimaryOrtho, probably map icon.
        /// </summary>
        CViewportPrimaryOrtho = 1,

        /// <summary>
        /// The game viewport.
        /// </summary>
        CViewportGame = 2,

        /// <summary>
        /// The radar viewport.
        /// </summary>
        CViewportRadar = 3,
    }
}
