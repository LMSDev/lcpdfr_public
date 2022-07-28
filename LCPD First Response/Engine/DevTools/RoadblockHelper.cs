namespace LCPD_First_Response.Engine.DevTools
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Helper tool for easier saving of coordinates for roadblocks.
    /// </summary>
    internal class RoadblockHelper
    {
        /// <summary>
        /// The file.
        /// </summary>
        private StreamWriter file;

        /// <summary>
        /// The vehicles created.
        /// </summary>
        private List<CVehicle> vehicles;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoadblockHelper"/> class.
        /// </summary>
        public RoadblockHelper()
        {
            this.vehicles = new List<CVehicle>();
        }

        /// <summary>
        /// Deletes all spawned vehicles.
        /// </summary>
        public void Delete()
        {
            foreach (CVehicle cVehicle in vehicles)
            {
                if (cVehicle.Exists())
                {
                    cVehicle.Delete();
                }
            }
        }

        /// <summary>
        /// Saves the vehicle position.
        /// </summary>
        public void SaveVehicle()
        {
            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                CVehicle vehicle = CPlayer.LocalPlayer.Ped.CurrentVehicle;

                GTA.Vector3 position = vehicle.Position;
                float heading = vehicle.Heading;

                // Use old DataFile format of LCPDFR 0.95
                // <SPAWNDATA POSITION=724.69, 1463.83, 14.85; HEADING=346.92; />
                string line = string.Format("<SPAWNDATA POSITION={0}, {1}, {2}; HEADING={3} />", position.X, position.Y, position.Z, heading);
                this.file.WriteLine(line);
                Log.Debug("SaveVehicle: Saved line: " + line, "RoadblockHelper");
            }
        }

        /// <summary>
        /// Spaws all saved vehicles.
        /// </summary>
        public void Spawn()
        {
            this.Stop();

            Legacy.DataFile dataFile = Legacy.FileParser.ParseDataFile(File.ReadAllText("roadblocks.data"));
            Legacy.DataSet dataSet = dataFile.DataSets[0];
            SpawnPoint[] coordinates = new SpawnPoint[dataSet.Tags.Length];

            int i = 0;
            foreach (var tag in dataSet.Tags)
            {
                string position = tag.GetAttributesValueByName<string>("POSITION");
                string heading = tag.GetAttributesValueByName<string>("HEADING");

                GTA.Vector3 vector3 = Legacy.FileParser.ParseVector3(position);
                coordinates[i] = new SpawnPoint(Convert.ToSingle(heading), vector3);
                i++;
            }

            foreach (var spawnPoint in coordinates)
            {
                CVehicle vehicle = new CVehicle("POLICE2", spawnPoint.Position, EVehicleGroup.Police);
                if (vehicle.Exists())
                {
                    vehicle.Heading = spawnPoint.Heading;
                    vehicle.AllowSirenWithoutDriver = true;
                    vehicle.SirenActive = true;
                    this.vehicles.Add(vehicle);
                }
            }

            this.Start();
        }

        /// <summary>
        /// Starts the roadblock helper.
        /// </summary>
        public void Start()
        {
            this.file = new StreamWriter("roadblocks.data", true);
            Log.Debug("Start: Roadblock helper started", "RoadblockHelper");
        }

        /// <summary>
        /// Stops the roadblock helper.
        /// </summary>
        public void Stop()
        {
           this.file.Close();
           Log.Debug("Start: Roadblock helper stopped", "RoadblockHelper");
        }
    }
}
