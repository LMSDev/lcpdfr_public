namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;

    using AdvancedHookManaged;
    using LCPD_First_Response.Engine.Scripting.Native;
    using GTA;

    static class CPedExtension
    {
        // Extension methods for CPed
        public static void ClearLastDamageEntity(this CPed ped)
        {
            Natives.ClearCharLastDamageEntity(ped);
            Natives.ClearCharLastWeaponDamage(ped);
        }

        /// <summary>
        /// Gets a safe position on a street for a ped to be spawned etc.  Can return Vector3.Zero so always check this.  Doesn't work in interiors.
        /// </summary>
        /// <param name="ped"></param>
        /// <returns>The position, always check against Vector3.Zero though</returns>
        public static Vector3 GetSafePosition(this CPed ped)
        {
            Vector3 pos = ped.Position;
            GTA.Native.Pointer x = new GTA.Native.Pointer(typeof(float));
            GTA.Native.Pointer y = new GTA.Native.Pointer(typeof(float));
            GTA.Native.Pointer z = new GTA.Native.Pointer(typeof(float));

            GTA.Native.Function.Call("GET_SAFE_POSITION_FOR_CHAR", pos.X, pos.Y, pos.Z, 1, x, y, z);

            return new Vector3(x, y, z);
        }

        public static Vector3 GetSafePositionAlternate(this CPed ped)
        {
            Vector3 pos = ped.Position;
            GTA.Native.Pointer x = new GTA.Native.Pointer(typeof(float));
            GTA.Native.Pointer y = new GTA.Native.Pointer(typeof(float));
            GTA.Native.Pointer z = new GTA.Native.Pointer(typeof(float));

            GTA.Native.Function.Call("GET_SAFE_PICKUP_COORDS", pos.X, pos.Y, pos.Z, x, y, z);

            return new Vector3(x, y, z);
        }

        public static Vector3 GetSafePositionAlternate(this CPed ped, Vector3 position)
        {
            Vector3 pos = position;
            GTA.Native.Pointer x = new GTA.Native.Pointer(typeof(float));
            GTA.Native.Pointer y = new GTA.Native.Pointer(typeof(float));
            GTA.Native.Pointer z = new GTA.Native.Pointer(typeof(float));

            GTA.Native.Function.Call("GET_SAFE_PICKUP_COORDS", pos.X, pos.Y, pos.Z, x, y, z);

            return new Vector3(x, y, z);
        }

        public static void DecisionLol(this CPed ped)
        {
            /*
             * finale1b.txt - 876:   |LOAD_CHAR_DECISION_MAKER(0, &Local[603]);
                finale1b.txt - 877:   |LOAD_CHAR_DECISION_MAKER(2, &Local[604]);
                finale1b.txt - 878:   |LOAD_COMBAT_DECISION_MAKER(10, &Local[606]);
                finale1b.txt - 879:   |LOAD_COMBAT_DECISION_MAKER(8, &Local[605]);
                finale1b.txt - 880:   |LOAD_COMBAT_DECISION_MAKER(8, &Local[607]);
                finale1b.txt - 881:   |LOAD_COMBAT_DECISION_MAKER(8, &Local[608]);
                finale1b.txt - 882:   |SET_DECISION_MAKER_ATTRIBUTE_SIGHT_RANGE(Local[606], 150);
                finale1b.txt - 883:   |SET_DECISION_MAKER_ATTRIBUTE_SIGHT_RANGE(Local[605], 150);
                finale1b.txt - 884:   |SET_DECISION_MAKER_ATTRIBUTE_SIGHT_RANGE(Local[607], 150);
                finale1b.txt - 885:   |SET_DECISION_MAKER_ATTRIBUTE_SIGHT_RANGE(Local[608], 150);
                finale1b.txt - 886:   |SET_DECISION_MAKER_ATTRIBUTE_WEAPON_ACCURACY(Local[606], 65);
                finale1b.txt - 887:   |SET_DECISION_MAKER_ATTRIBUTE_WEAPON_ACCURACY(Local[605], 65);
                finale1b.txt - 888:   |SET_DECISION_MAKER_ATTRIBUTE_WEAPON_ACCURACY(Local[607], 70);
                finale1b.txt - 889:   |SET_DECISION_MAKER_ATTRIBUTE_WEAPON_ACCURACY(Local[608], 60);
                finale1b.txt - 890:   |SET_DECISION_MAKER_ATTRIBUTE_TARGET_LOSS_RESPONSE(Local[606], 0);
                finale1b.txt - 891:   |SET_DECISION_MAKER_ATTRIBUTE_TARGET_LOSS_RESPONSE(Local[605], 0);
                finale1b.txt - 892:   |SET_DECISION_MAKER_ATTRIBUTE_TARGET_LOSS_RESPONSE(Local[607], 0);
                finale1b.txt - 893:   |SET_DECISION_MAKER_ATTRIBUTE_TARGET_LOSS_RESPONSE(Local[608], 0);
             * 
             *             bool response = Function.Call<bool>("CREATE_EMERGENCY_SERVICES_CAR_RETURN_DRIVER", name.Hash, pos.X, pos.Y, pos.Z, pntVehicle, pntDriver, pntPassenger);
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
             */
            /*

            GTA.Native.Pointer decisionMaker = new GTA.Native.Pointer(typeof(DecisionMaker));

            GTA.Native.Function.Call("LOAD_COMBAT_DECISION_MAKER", 8, decisionMaker);
            DecisionMaker decm = (DecisionMaker)decisionMaker.Value;
            decm.SightRange = 150;
            decm.TargetLossResponse = 0;
            decm.WeaponAccuracy = 80;
            decm.ApplyTo(ped);
            */

        }

        public static void HandleAudioAnimEvent(this CPed ped, string audioEvent)
        {
            Natives.HandleAudioAnimEvent(ped, audioEvent);
        }

        public static bool IsArmed(this CPed ped)
        {
            return ped.Weapons.Current.Slot != WeaponSlot.Unarmed;
        }

        public static bool IsInHelicopter(this CPed ped)
        {
            if (ped.IsInVehicle)
            {
                return ped.CurrentVehicle.Model.IsHelicopter;
            }
            return false;
        }

        public static void ModifyMoveState(this CPed ped, EPedMoveState moveState)
        {
            Natives.ModifyCharMoveState(ped, moveState);
        }

        public static void SayAmbientSpeech(this CPed ped, string phraseID, string voice)
        {
            Natives.SayAmbientSpeechWithVoice(ped, phraseID, voice);
        }

        public static void SetAnimGroup(this CPed ped, string animGroup)
        {
            Natives.RequestAnims(animGroup);
            Natives.SetAnimGroupForChar(ped, animGroup);
        }

        /// <summary>
        /// Warning: This is for the next goto task only, the ped doesn't matter!
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="moveState"></param>
        public static void SetNextDesiredMoveState(this CPed ped, EPedMoveState moveState)
        {
            Natives.SetNextDesiredMoveState(moveState);
        }

        public static void SetPedWontAttackPlayerWithoutWantedLevel(this CPed ped, bool value)
        {
            Natives.SetPedWontAttackPlayerWithoutWantedLevel(ped, value);
        }

        /// <summary>
        /// 0 = can't shoot at all, 100 = fast shooting
        /// </summary>
        /// <param name="ped">The ped</param>
        /// <param name="shootRate">The shoot rate</param>
        public static void SetShootRate(this CPed ped, int shootRate)
        {
            Natives.SetCharShootRate(ped, shootRate);
        }

        /// <summary>
        /// Does what it says on the tin
        /// </summary>
        /// <param name="ped">The ped</param>
        /// <param name="value">Whether or not they will use cover</param>
        public static void WillUseCover(this CPed ped, bool value)
        {
            Natives.SetCharWillUseCover(ped, value);
        }

        // Extension methods for pool

        /// <summary>
        /// Returns the ped for <paramref name="handle"/>.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="handle">The handle.</param>
        /// <returns>The ped.</returns>
        public static CPed AtPedHandle(this Pool<CPed> pool, int handle)
        {
            foreach (CEntity entity in pool.GetAll())
            {
                if (entity.Handle == handle)
                {
                    return (CPed)entity;
                }
            }

            return null;
        }

        public static CPed GetPedFromPool(this Pool<CPed> pool, APed ped)
        {
            foreach (CPed cPed in pool.GetAll())
            {
                if (cPed.Handle == ped.Get())
                {
                    return cPed;
                }
            }
            return null;
        }

        public static CPed GetPedFromPool(this Pool<CPed> pool, GTA.Ped ped)
        {
            if (ped == null) return null;
            foreach (CPed cPed in pool.GetAll())
            {
                if (cPed.Handle == ped.pHandle)
                {
                    return cPed;
                }
            }
            if (ped != null && ped.Exists())
            {
                // Negative handles might be an indicator for invalid/dummy peds
                if (ped.pHandle < 0)
                {
                    //Log.Warning("GetPedFromPool: Possible invalid/dummy handle: " + ped.pHandle, "CPedExtension");
                    return null;
                }

                try
                {
                    return new CPed(ped.pHandle);
                }
                catch (Exception)
                {
                    Log.Warning("GetPedFromPool: Invalid handle: " + ped.pHandle, "CPedExtension");
                }

            }
            return null;
        }
    }
}
