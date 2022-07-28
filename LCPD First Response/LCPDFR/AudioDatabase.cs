namespace LCPD_First_Response.LCPDFR
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LCPD_First_Response.Engine;

    /// <summary>
    /// The audio database of LCPDFR, storing references to audio files for actions.
    /// </summary>
    internal class AudioDatabase
    {
        /// <summary>
        /// The items.
        /// </summary>
        private List<AudioDatabaseItem> items;

        /// <summary>
        /// The path.
        /// </summary>
        private string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDatabase"/> class.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        public AudioDatabase(string path)
        {
            this.path = path;
            this.items = new List<AudioDatabaseItem>();

            // Scan audio files
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            Log.Debug("AudioDatabase: Preparing: " + path, "AudioDatabase");

            FileInfo[] fileInfo = directoryInfo.GetFiles("*.wav", SearchOption.AllDirectories);
            foreach (FileInfo info in fileInfo)
            {
                string fileName = info.FullName;
                this.AddToDatabase(fileName);
            }

            Log.Debug("AudioDatabase: Database ready. " + this.items.Count + " sounds added", "AudioDatabase");
        }

        /// <summary>
        /// Gets the path of an audio file for <paramref name="action"/>. If there is more than one file for <paramref name="action"/>, a random one is chosen.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The audio file path.</returns>
        public string GetAudioFileForAction(string action)
        {
            return GetAudioFileForAction(action, string.Empty);
        }

        /// <summary>
        /// Gets the path of an audio file for <paramref name="action"/>. If there is more than one file for <paramref name="action"/>, a random one is chosen.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="preferredLocation">The name of the preferred location of the audio file, which will be used first when there are multiple files with the same name.</param>
        /// <returns>The audio file path.</returns>
        public string GetAudioFileForAction(string action, string preferredLocation)
        {
            // Get all suitable files
            AudioDatabaseItem[] items = this.GetAllItemsWithAction(action);
            if (items.Length == 0)
            {
                Log.Warning("GetAudioFileForAction: No file found for: " + action, "AudioDatabase");
                return string.Empty;
            }

            // Ensure preferred location is not null, otherwise maybe the player voice instance is null for some reason
            if (preferredLocation == null)
            {
                Log.Warning("GetAudioFileForAction: Location parameter is NULL for " + action, this);
            }
            else
            {
                // If there is a preferred location given, use it
                if (preferredLocation != string.Empty)
                {
                    // Add all items that have preferredLocation in their path
                    AudioDatabaseItem[] tempItems = items.Where(audioDatabaseItem => audioDatabaseItem.Path.Contains(preferredLocation)).ToArray();

                    // Select random one and return
                    return Common.GetRandomCollectionValue<AudioDatabaseItem>(tempItems).Path;
                }
            }

            AudioDatabaseItem item = Common.GetRandomCollectionValue<AudioDatabaseItem>(items);
            return item.Path;
        }

        /// <summary>
        /// Adds <paramref name="path"/> to the database.
        /// </summary>
        /// <param name="path">The full name of the item.</param>
        private void AddToDatabase(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            // Strip number at the end because the indicate another variant for the same message, e.g. ARREST_PED_01.
            // Numbers always consist of two digits and are in the last part of the string
            Regex regex = new Regex("_");
            string[] split = regex.Split(name);

            // Get last part
            string lastSplit = split[split.Length - 1];
            if (lastSplit.Length == 2)
            {
                char letter = lastSplit[0];
                if (char.IsDigit(letter))
                {
                    letter = lastSplit[1];
                    if (char.IsDigit(letter))
                    {
                        // Both last letters are a digit, remove them
                        name = name.Replace("_" + lastSplit, string.Empty);
                    }
                }
            }

            AudioDatabaseItem audioDatabaseItem = new AudioDatabaseItem(name, path);
            this.items.Add(audioDatabaseItem);
        }

        /// <summary>
        /// Returns all items that have <paramref name="action"/> as action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The items.</returns>
        private AudioDatabaseItem[] GetAllItemsWithAction(string action)
        {
            List<AudioDatabaseItem> tempItems = new List<AudioDatabaseItem>();
            foreach (AudioDatabaseItem audioDatabaseItem in this.items)
            {
                if (audioDatabaseItem.Action == action)
                {
                    tempItems.Add(audioDatabaseItem);
                }
                else
                {
                    // Since we have cut off the number, but might want to explicitly play one special file, we check for the number here again
                    if (action.StartsWith(audioDatabaseItem.Action))
                    {
                        // So if the requested file starts with the same name as the cut string in the database does, check the full path where the number is still in
                        if (audioDatabaseItem.Path.Contains(action))
                        {
                            tempItems.Add(audioDatabaseItem);
                        }
                    }
                }
            }

            return tempItems.ToArray();
        }

        /// <summary>
        /// An item in the audio database.
        /// </summary>
        internal class AudioDatabaseItem
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AudioDatabaseItem"/> class.
            /// </summary>
            /// <param name="action">
            /// The action.
            /// </param>
            /// <param name="path">
            /// The path.
            /// </param>
            public AudioDatabaseItem(string action, string path)
            {
                this.Action = action;
                this.Path = path;
            }

            /// <summary>
            /// Gets the action of the audio file.
            /// </summary>
            public string Action { get; private set; }

            /// <summary>
            /// Gets the full path of the audio file.
            /// </summary>
            public string Path { get; private set; }
        }
    }
}