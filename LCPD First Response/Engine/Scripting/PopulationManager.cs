namespace LCPD_First_Response.Engine.Scripting
{
    using System;
    using System.Data;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Native;

    using Timer = LCPD_First_Response.Engine.Timers.Timer;

    /// <summary>
    /// Provides functions to control population properties, for example density.
    /// </summary>
    class PopulationManager
    {
        private static Timer copDensityTimer;

        private static float copDensity;

        private static bool copSpawningDisabled;

        private static DateTime lastCopSpawningStateChange;

        public static void AllowAllSpawning()
        {
            AllowAllTraffic();
            SetPedDensity(1.0f);
        }

        public static void AllowAllTraffic()
        {
            SetParkedCarDensity(1.0f);
            SetRandomCarDensity(1.0f);
            SetAllowEmergencyServices(true);
            SetAllowGarbageTrucks(true);
            SetAllowRandomBoats(true);
            SetAllowRandomTrains(true);
        }

        public static void BlockAllSpawning()
        {
            BlockAllTraffic();
            SetPedDensity(0.0f);
        }

        public static void BlockAllTraffic()
        {
            SetParkedCarDensity(0.0f);
            SetRandomCarDensity(0.0f);
            SetAllowEmergencyServices(false);
            SetAllowGarbageTrucks(false);
            SetAllowRandomBoats(false);
            SetAllowRandomTrains(false);
        }

        public static void DisableCityServices()
        {
            SetAllowEmergencyServices(false);
            SetAllowGarbageTrucks(false);
            SetAllowRandomTrains(false);
        }

        public static void EnableCityServices()
        {
            SetAllowEmergencyServices(true);
            SetAllowGarbageTrucks(true);
            SetAllowRandomTrains(true);
        }

        public static void SetAllowEmergencyServices(bool value)
        {
            Game.AllowEmergencyServices = value;
        }

        public static void SetAllowGarbageTrucks(bool value)
        {
            Natives.SwitchGarbageTrucks(value);
        }

        public static void SetAllowRandomBoats(bool value)
        {
            Natives.SwitchRandomBoats(value);
        }

        public static void SetAllowRandomTrains(bool value)
        {
            // For some reason, the game code creates trains when this flag is set to false.
           Natives.SwitchRandomTrains(!value);
        }

        public static void SetParkedCarDensity(float density)
        {
            Natives.SetParkedCarDensityMultiplier(density);
        }

        public static void SetRandomCarDensity(float density)
        {
            Natives.SetRandomCarDensityMultiplier(density);
        }

        public static void SetPedDensity(float density)
        {
            Natives.SetPedDensityMultiplier(density);
        }

        public static void SetScenarioPedDensity(float density)
        {
            Natives.SetScenarioPedDensityMultiplier(density);
        }

        /// <summary>
        /// Sets the density of random cops.
        /// </summary>
        /// <param name="density">Can't exceed 1.0f.</param>
        public static void SetRandomCopsDensity(float density)
        {
            // Ensure value is 1.0f or below.
            if (density > 1.0f)
            {
                throw new ArgumentException("Value must be 1.0 or below.", "density");
            }
            copDensity = density;

            // Stop old timer.
            if (copDensityTimer == null)
            {
                lastCopSpawningStateChange = DateTime.Now;

                copDensityTimer = new Timer(250, CopDensityTimer_Callback);
                copDensityTimer.Start();
            }
        }

        private static void CopDensityTimer_Callback(params object[] parameter)
        {
            // Based on density, choose interval to disable cop spawning. (1.0 will be never disable, 0.5 will disable it for 5 seconds and allow it for 5 seconds as well, 0.0 will block it entirely).
            float timeDisabled = (-10.0f * copDensity) + 10.0f;
            float timeOn = 10.0f - timeDisabled;
            TimeSpan timeElapsed = DateTime.Now - lastCopSpawningStateChange;

            // If cops are spawning at the moment, check if we need to disable it.
            if (!copSpawningDisabled)
            {
                if (timeElapsed.TotalSeconds > timeOn)
                {
                    lastCopSpawningStateChange = DateTime.Now;
                    copSpawningDisabled = true;
                    Natives.SetCreateRandomCops(false);
                }
            }
            else
            {
                if (timeElapsed.TotalSeconds > timeDisabled)
                {
                    lastCopSpawningStateChange = DateTime.Now;
                    copSpawningDisabled = false;
                    Natives.SetCreateRandomCops(true);
                }
            }
        }
    }
}