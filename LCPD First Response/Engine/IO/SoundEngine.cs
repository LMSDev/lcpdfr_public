namespace LCPD_First_Response.Engine.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Media;
    using System.Threading;
    using LCPD_First_Response.Engine.Scripting.Native;

    using SlimDX.DirectSound;
    using SlimDX.Multimedia;
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Allows to play sounds ingame, either from ingame resources or from the harddisk.
    /// </summary>
    internal static class SoundEngine
    {
        /// <summary>
        /// The sound ID of the sound playing.
        /// </summary>
        private static int soundID;

        /// <summary>
        /// The list of sounds playing.
        /// </summary>
        private static List<int> sounds; 

        /// <summary>
        /// The audio volume.
        /// </summary>
        private static int volume;

        /// <summary>
        /// Initializes static members of the <see cref="SoundEngine"/> class.
        /// </summary>
        static SoundEngine()
        {
            sounds = new List<int>();
        }

        /// <summary>
        /// Plays a sound from the given path. This doesn't play any game sound, but sound files on your hdd.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The soundbuffer.
        /// </returns>
        public static PlaybackControl PlayExternalSound(string path)
        {
            return PlayExternalSound(path, false);
        }

        /// <summary>
        /// Plays a sound from the given path. This doesn't play any game sound, but sound files on your hdd.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="loop">
        /// Whether sound should be looped.
        /// </param>
        /// <returns>
        /// The soundbuffer.
        /// </returns>
        public static PlaybackControl PlayExternalSound(string path, bool loop)
        {
            if (!File.Exists(path))
            {
                string installFolder = GTA.Game.InstallFolder;
                path = Path.Combine(installFolder, path);
                if (!File.Exists(path))
                {
                    Log.Warning("PlayExternalSound: Failed to play " + path + ". File not found", "SoundEngine");
                    return null;
                }
            }

            Log.Debug("PlayExternalSound: Playing " + path, "SoundEngine");

            // Create direct sound object
            DirectSound directSound = new DirectSound();
            directSound.SetCooperativeLevel(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle, CooperativeLevel.Priority);

            // Read wave file
            Stream waveFileStream;
            try
            {
                waveFileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            }
            catch (Exception)
            {
                Log.Warning("PlayExternalSound: IO error while opening wave stream from " + path + ". File already in use?", "SoundEngine");
                return null;
            }

            BinaryReader reader = new BinaryReader(waveFileStream);

            int chunkID = reader.ReadInt32();
            int fileSize = reader.ReadInt32();
            int riffType = reader.ReadInt32();
            int fmtID = reader.ReadInt32();
            int fmtSize = reader.ReadInt32();
            int fmtCode = reader.ReadInt16();
            int channels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            int fmtAvgBPS = reader.ReadInt32();
            int fmtBlockAlign = reader.ReadInt16();
            int bitDepth = reader.ReadInt16();

            WaveFormat format = new WaveFormat();
            format.BitsPerSample = (short)bitDepth;
            format.BlockAlignment = (short)fmtBlockAlign;
            format.Channels = (short)channels;
            format.FormatTag = WaveFormatTag.Pcm;
            format.SamplesPerSecond = sampleRate;
            format.AverageBytesPerSecond = format.SamplesPerSecond * format.BlockAlignment;

            reader.Close();
            waveFileStream.Close();

            SoundBufferDescription desc2 = new SoundBufferDescription();
            desc2.Format = format;
            desc2.Flags = BufferFlags.ControlPositionNotify | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume;
            desc2.SizeInBytes = fileSize;

            SecondarySoundBuffer secondarySoundBuffer = new SecondarySoundBuffer(directSound, desc2);

            byte[] bytes2 = new byte[desc2.SizeInBytes];
            Stream stream = File.Open(path, FileMode.Open);
            secondarySoundBuffer.Tag = false;
            Thread fillBuffer = new Thread(() =>
            {
                int bytesRead = stream.Read(bytes2, 0, desc2.SizeInBytes);
                secondarySoundBuffer.Write<byte>(bytes2, 0, LockFlags.None);
                if (loop)
                {
                    secondarySoundBuffer.Play(0, PlayFlags.Looping);
                }
                else
                {
                    secondarySoundBuffer.Play(0, PlayFlags.None);
                }

                while (secondarySoundBuffer.Status.HasFlag(BufferStatus.Playing))
                {
                    Thread.Sleep(1);
                }

                stream.Close();
                stream.Dispose();
                secondarySoundBuffer.Tag = true;
            });
            fillBuffer.Start();

            // Dont return until actually playing
            while (!secondarySoundBuffer.Status.HasFlag(BufferStatus.Playing))
            {
                Thread.Sleep(1);
            }

            // Adjust volume
            secondarySoundBuffer.Volume = volume;

            return new PlaybackControl(secondarySoundBuffer);
        }

        /// <summary>
        /// Plays the sound.
        /// </summary>
        /// <param name="bankName">The name of the audio bank.</param>
        /// <param name="fileName">The file name.</param>
        public static void PlaySound(string bankName, string fileName)
        {
            // If there's already a sound playing, skip
            if (!Natives.HasSoundFinished(soundID))
            {
                Log.Debug("PlaySound: There's already a sound playing", "SoundEngine");
                return;
            }
            else
            {
                // Free old sound
                if (soundID != 0)
                {
                    Natives.ReleaseSoundID(soundID);
                    Natives.MissionAudioBankNoLongerNeeded();
                }
            }

            if (Natives.RequestMissionAudioBank(bankName))
            {
                soundID = Natives.GetSoundID();

                string bankAndFileName = bankName;
                bankAndFileName = bankAndFileName.Replace("/", "_");
                bankAndFileName = bankAndFileName.Replace("\\", "_");
                bankAndFileName += "_" + fileName;
                Natives.PlaySoundFrontend(soundID, bankAndFileName);
            }
        }

        /// <summary>
        /// Plays the sound.
        /// </summary>
        /// <param name="soundName">The sound name.</param>
        public static void PlaySound(string soundName)
        {
            // If there's already a sound playing, skip
            if (!Natives.HasSoundFinished(soundID))
            {
                Log.Debug("PlaySound: There's already a sound playing", "SoundEngine");
                return;
            }
            else
            {
                // Free old sound
                if (soundID != 0)
                {
                    Natives.ReleaseSoundID(soundID);
                    Natives.MissionAudioBankNoLongerNeeded();
                }
            }

            soundID = Natives.GetSoundID();
            Natives.PlaySoundFrontend(soundID, soundName);
        }

        /// <summary>
        /// Plays the sound from an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="soundName">The sound name.</param>
        public static void PlaySoundFromObject(GTA.Object o, string soundName)
        {
            // If there's already a sound playing, skip
            if (!Natives.HasSoundFinished(soundID))
            {
                Log.Debug("PlaySoundFromObject: There's already a sound playing", "SoundEngine");
                return;
            }
            else
            {
                // Free old sound
                if (soundID != 0)
                {
                    Natives.ReleaseSoundID(soundID);
                }
            }

            soundID = Natives.GetSoundID();
            Natives.PlaySoundFromObject(soundID, soundName, o);
        }

        /// <summary>
        /// Plays the sound from a ped.
        /// </summary>
        /// <param name="p">The ped.</param>
        /// <param name="soundName">The sound name.</param>
        public static void PlaySoundFromPed(CPed p, string soundName)
        {
            // If there's already a sound playing, skip
            if (!Natives.HasSoundFinished(soundID))
            {
                Log.Debug("PlaySoundFromObject: There's already a sound playing", "SoundEngine");
                return;
            }
            else
            {
                // Free old sound
                if (soundID != 0)
                {
                    Natives.ReleaseSoundID(soundID);
                }
            }

            soundID = Natives.GetSoundID();
            Natives.PlaySoundFromPed(soundID, soundName, p);
        }

        /// <summary>
        /// Plays the sound from a vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="soundName">The sound name.</param>
        /// <returns>The sound ID.</returns>
        public static int PlaySoundFromVehicle(CVehicle vehicle, string soundName)
        {
            // If there's already a sound playing, skip
            if (!Natives.HasSoundFinished(soundID))
            {
                Log.Debug("PlaySoundFromObject: There's already a sound playing", "SoundEngine");
                return -1;
            }
            else
            {
                // Free old sound
                if (soundID != 0)
                {
                    Natives.ReleaseSoundID(soundID);
                }
            }

            soundID = Natives.GetSoundID();
            Natives.PlaySoundFromVehicle(soundID, soundName, vehicle);
            return soundID;
        }

        /// <summary>
        /// Plays the sound from a vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="soundName">The sound name.</param>
        /// <param name="allowMultipleSounds">Whether multiple sounds are supported.</param>
        /// <returns>The sound ID.</returns>
        public static int PlaySoundFromVehicle(CVehicle vehicle, string soundName, bool allowMultipleSounds)
        {
            if (!allowMultipleSounds)
            {
                return PlaySoundFromVehicle(vehicle, soundName);
            }
            else
            {
                soundID = Natives.GetSoundID();
                Natives.PlaySoundFromVehicle(soundID, soundName, vehicle);
                sounds.Add(soundID);
                return soundID;
            }
        }

        /// <summary>
        /// Stops the sound with <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id.</param>
        public static void StopSound(int id)
        {
            Natives.StopSound(id);
            Natives.ReleaseSoundID(id);

            if (sounds.Contains(id))
            {
                sounds.Remove(id);
            }
        }

        /// <summary>
        /// Sets the general audio volume affecting ALL sounds played.
        /// </summary>
        /// <param name="volume">The volume.</param>
        public static void SetGeneralAudioVolume(int volume)
        {
            SoundEngine.volume = volume;
        }
    }
}