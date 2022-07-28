namespace LCPD_First_Response.Engine
{
    using System.Collections.Generic;
    using System.Linq;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// The purpose of this task is to consistently check memory every few seconds for potential issues. This includes validating our very own pools for possible issues,
    /// such as a handle being used twice as well as checking the game memory, especially for issues with CVehicleStruct.
    /// </summary>
    class MemoryValidator : SystemTaskTimed
    {
        private const int Interval = 10000;

        public MemoryValidator() : base(Interval)
        {

        }

        public override void Abort()
        {

        }

        public override void Process()
        {
            // Validate our pools.
            Pool<CPed> pedPool = Pools.PedPool;
            List<int> doubleHandles = new List<int>();
            var dict = new Dictionary<int, int>(pedPool.Count);
            foreach (CPed ped in pedPool)
            {
                if (dict.ContainsKey(ped.Handle))
                {
                    dict[ped.Handle]++;
                    if (!doubleHandles.Contains(ped.Handle))
                    {
                        doubleHandles.Add(ped.Handle);
                    }
                }
                else dict[ped.Handle] = 1;
            }

            if (doubleHandles.Count > 0)
            {
                Log.Warning("One or more handles appear twice in pool cache", "MemoryValidator");
                foreach (int doubleHandle in doubleHandles)
                {
                    CPed[] peds = pedPool.GetAll().Where(ped => ped.Handle == doubleHandle).ToArray();
                    CPed existingPed = peds.First(ped => ped.Exists());
                    int existingPeds = peds.Where(ped => ped.Exists()).ToArray().Length;

                    int occurences = peds.Length;
                    Log.Warning(string.Format("Handle {0} has {1} (valid: {2}) occurences in pool cache (Model: {3}/{4} Position: {5} PedGroup: {6} RequiredForMission {7}",
                        doubleHandle, occurences, existingPeds, existingPed.Model.ModelInfo.Name, existingPed.Model.ModelInfo.Hash, existingPed.Position, existingPed.PedGroup, 
                        existingPed.IsRequiredForMission), "MemoryValidator");

                    if (Main.DEBUG_MODE)
                    {
                        GUI.HelpBox.Print("One or more handles appear twice in pool cache -- attach debugger");
                    }
                }
            }

            // TODO: Validate CVehicleStruct, especially fragType
        }
    }
}