namespace LCPD_First_Response.LCPDFR
{
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Simplifies playing audio by providing functions that parse the parameter into audio strings.
    /// </summary>
    internal class AudioHelper
    {

        public enum EPursuitCallInReason
        {
            Pursuit,
            FootPursuit,
            Shooting,
            Backup,
            Assaulted,
            ShotAt,
            Other
        }

        /// <summary>
        /// The path where the audio files are at.
        /// </summary>
        private const string AudioFilesPath = "lcpdfr\\Audio\\";

        /// <summary>
        /// The audio database.
        /// </summary>
        private static AudioDatabase audioDatabase;

        /// <summary>
        /// Whether the audio helper is busy and new audio is rejected.
        /// </summary>
        private static bool isBusy;

        /// <summary>
        /// A list of police stations with helipads where helicopters can be dispatched from
        /// </summary>
        private static readonly string[] helicopterBases = new string[4] { "FRANCIS_INTERNATIONAL", "FRANCIS_INTERNATIONAL", "LOWER_EASTON", "EAST_HOLLAND" };

        /// <summary>
        /// Initializes static members of the <see cref="AudioHelper"/> class.
        /// </summary>
        static AudioHelper()
        {
            audioDatabase = new AudioDatabase(Path.Combine(GTA.Game.InstallFolder, AudioFilesPath));
            SoundEngine.SetGeneralAudioVolume(-1000);
        }

        /// <summary>
        /// Gets a value indicating whether the audio player is busy.
        /// </summary>
        public static bool IsBusy
        {
            get
            {
                return isBusy;
            }
        }

        /// <summary>
        /// Creates a random intro audio message string which can be used prior to describing crimes.
        /// <param name="reportedBy">Whether the crime was reported by 'units' or by 'civilians'</param>
        /// </summary>
        /// <returns>The audio message string.</returns>
        public static string CreateIntroAudioMessage(API.EIntroReportedBy reportedBy)
        {
            StringBuilder message = new StringBuilder();

            int randomValue = Common.GetRandomValue(0, 10);
            string intro = "THIS_IS_CONTROL ";

            if (randomValue > 5)
            {
                intro = "ATTENTION_ALL_UNITS ";
            }

            string err = "ERR_ERRR ";

            string unitsOrCiviliansReport = "INS_UNITS_REPORT ";
            if (reportedBy == API.EIntroReportedBy.Civilians) unitsOrCiviliansReport = "INS_CIVILIANS_REPORT ";

            switch (randomValue)
            {
                case 0:
                    message.Append("INS_AVAILABLE_UNITS_RESPOND_TO ");
                    if (Common.GetRandomBool(0, 3, 1))
                    {
                        message.Append(err);
                    }

                    break;

                case 1:
                    if (Common.GetRandomBool(0, 3, 1)) message.Append("THIS_IS_CONTROL ");
                    message.Append("INS_I_NEED_A_UNIT_FOR ");
                    if (Common.GetRandomBool(0, 3, 1))
                    {
                        message.Append(err);
                    }

                    break;

                case 2:
                    message.Append("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR ");
                    break;

                case 3:
                    message.Append(unitsOrCiviliansReport);
                    break;

                case 4:
                    message.Append(unitsOrCiviliansReport);
                    break;

                case 5:
                    message.Append(intro);
                    message.Append("ASSISTANCE_REQUIRED ");
                    message.Append("FOR ");
                    if (Common.GetRandomBool(0, 3, 1))
                    {
                        message.Append(err);
                    }
                    break;

                case 6:
                    message.Append("THIS_IS_CONTROL ");
                    message.Append(unitsOrCiviliansReport);
                    break;

                case 7:
                    message.Append(intro);
                    message.Append("INS_WEVE_GOT ");
                    break;

                case 8:
                    message.Append(intro);
                    message.Append("INS_WE_HAVE_A_REPORT_OF_ERRR ");
                    break;
                 
                case 9:
                    message.Append("ASSISTANCE_REQUIRED FOR ");
                    break;
            }

            return message.ToString();
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The action.</param>
        public static void PlayActionSound(string action)
        {
            PlayAction(action, false);
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="preferredLocation">The name of the preferred location of the audio file, which will be used first when there are multiple files with the same name.</param>
        public static void PlayActionSound(string action, string preferredLocation)
        {
            PlayAction(action, false, preferredLocation);
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="waitTillEnd">Whether the function returns when the sound has finished.</param>
        public static void PlayActionSound(string action, bool waitTillEnd)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            PlayAction(action, waitTillEnd);
            isBusy = false;
        }

        /// <summary>
        /// Plays <paramref name="speech"/> from <paramref name="ped"/> and adds scanner noise to it in a new thread.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="speech">The speech.</param>
        public static void PlaySpeechInScannerFromPed(CPed ped, string speech)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            Thread playThread = new Thread(() =>
            {
                PlayScannerIntroSound();
                PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
                if (playNoiseLoop == null)
                {
                    Log.Debug("PlayActionInScanner: Failed to play noise loop", "AudioHelper");
                    isBusy = false;
                    return;
                }

                // Hack: Since we can't call natives from other threads, we invoke it by using the delayed caller
                DelayedCaller.Call(delegate { ped.SayAmbientSpeech(speech); }, 1);

                // Block thread until speech is no longer playing
                System.Func<bool> isSpeechPlayingLogic = () => ped.IsAmbientSpeechPlaying;
                while (true)
                {
                    if (!DelayedCaller.InvokeOnMainThread(delegate { return isSpeechPlayingLogic(); }, 10))
                    {
                        playNoiseLoop.Stop();
                        PlayScannerOutroSound();
                        isBusy = false;
                        break;
                    }

                    Thread.Sleep(20);
                }
            }) { IsBackground = true };
            playThread.Start();
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/> and adds scanner noise to it in a new thread.
        /// </summary>
        /// <param name="action">The action.</param>
        public static void PlayActionInScanner(string action)
        {
            PlayActionInScanner(action, string.Empty);
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/> and adds scanner noise to it in a new thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="preferredLocation">The name of the preferred location of the audio file, which will be used first when there are multiple files with the same name.</param>
        public static void PlayActionInScanner(string action, string preferredLocation)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            Thread playThread = new Thread(() =>
            {
                PlayScannerIntroSound();
                PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
                if (playNoiseLoop == null)
                {
                    Log.Debug("PlayActionInScanner: Failed to play noise loop", "AudioHelper");
                    isBusy = false;
                    return;
                }

                PlayAction(action, true, preferredLocation);
                playNoiseLoop.Stop();
                PlayScannerOutroSound();
                isBusy = false;
            }) { IsBackground = true };
            playThread.Start();
        }

        public static void PlayPositionInThread(Vector3 position)
        {
            string streetName = World.GetStreetName(position);
            bool useArea = true;

            // Try to get a streetname
            if (!string.IsNullOrWhiteSpace(streetName))
            {
                string[] splitStreets = Regex.Split(streetName, ", ");
                foreach (string splitStreet in splitStreets)
                {
                    string formattedStreet = splitStreet.Replace(" ", "_");
                    string audioFilePath = audioDatabase.GetAudioFileForAction(formattedStreet.ToUpper());

                    if (!string.IsNullOrWhiteSpace(audioFilePath))
                    {
                        useArea = false;
                        streetName = formattedStreet;
                        PlayAction("ON", true);
                        if (Common.GetRandomBool(0, 4, 1)) PlayAction("ERR_ERRR", true);
                        PlayAction(streetName.ToUpper(), true);
                        return;
                    }
                }
            }

            if (useArea)
            {
                string areaName = AreaHelper.GetAreaName(position);
                if (areaName == "UNKNOWN")
                {
                    // Thread safe
                    DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);
                    return;
                }
                else
                {
                    if (areaName != "AT_SEA")  PlayAction("IN_01", true, "PoliceScanner");
                    if (Common.GetRandomBool(0, 4, 1)) PlayAction("ERR_ERRR", true);
                    PlayAction(areaName, true);
                }
            }  
        }

        /// <summary>
        /// Plays all actions in <paramref name="action"/> and replaces POSITION with <paramref name="position"/> while playing.
        /// </summary>
        /// <param name="action">The string containing all actions. Use a whitespace to separate.</param>
        /// <param name="position">The position to use.</param>
        public static void PlayActionInScannerUsingPosition(string action, Vector3 position)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            Thread playThread = new Thread(() =>
                {
                string streetName = World.GetStreetName(position);
                bool useArea = true;

                // Try to get a streetname
                if (!string.IsNullOrWhiteSpace(streetName))
                {
                    string[] splitStreets = Regex.Split(streetName, ", ");
                    foreach (string splitStreet in splitStreets)
                    {
                        string formattedStreet = splitStreet.Replace(" ", "_");
                        string audioFilePath = audioDatabase.GetAudioFileForAction(formattedStreet.ToUpper());

                        if (!string.IsNullOrWhiteSpace(audioFilePath))
                        {
                            useArea = false;
                            streetName = formattedStreet;
                            break;
                        }
                    }
                }

                // Get area name
                string areaName = AreaHelper.GetAreaName(position);
                if (areaName == "UNKNOWN")
                {
                    // Thread safe
                    DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                    isBusy = false;
                    return;
                }

                PlayScannerIntroSound();
                PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
                if (playNoiseLoop == null)
                {
                    Log.Debug("PlayActionInScanner: Failed to play noise loop", "AudioHelper");
                    isBusy = false;
                    return;
                }

                // Play the different actions
                Regex regex = new Regex(" ");
                string[] actions = regex.Split(action);
                foreach (string s in actions)
                {
                    // If action is POSITION, play area name (33% chance to be preceded by ERR)
                    if (s == "POSITION")
                    {
                        if (Common.GetRandomBool(0, 3, 1))
                        {
                            PlayAction("ERR_ERRR", true);
                        }

                        if (useArea)
                        {
                            PlayAction(areaName, true);
                        }
                        else
                        {
                            PlayAction(streetName.ToUpper(), true);
                        }
                    }
                    // If action is IN_OR_ON_POSITION, play area name preceded by appropriate prefix (33% chance to be preceded by ERR)
                    else if (s == "IN_OR_ON_POSITION")
                    {
                        if (useArea)
                        {
                            if (areaName != "AT_SEA") PlayAction("IN_01", true, "PoliceScanner");
                        }
                        else
                        {
                            PlayAction("ON", true);
                        }

                        if (Common.GetRandomBool(0, 3, 1))
                        {
                            PlayAction("ERR_ERRR", true);
                        }

                        if (useArea)
                        {
                            PlayAction(areaName, true);
                        }
                        else
                        {
                            PlayAction(streetName.ToUpper(), true);
                        }
                    }
                    else
                    {
                        PlayAction(s, true);
                    }
                }

                playNoiseLoop.Stop();
                PlayScannerOutroSound();
                isBusy = false;
            }) { IsBackground = true };
            playThread.Start();
        }

        /// <summary>
        /// Plays <paramref name="speech"/> from <paramref name="ped"/> and adds scanner intro and outro to it in a new thread.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="speech">The speech.</param>
        public static void PlaySpeechInScannerNoNoiseFromPed(CPed ped, string speech)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            Thread playThread = new Thread(() =>
            {
                PlayScannerIntroSound();

                // Hack: Since we can't call natives from other threads, we invoke it by using the delayed caller
                DelayedCaller.Call(delegate { ped.SayAmbientSpeech(speech); }, 1);

                // Block thread until speech is no longer playing
                System.Func<bool> isSpeechPlayingLogic = () => ped.IsAmbientSpeechPlaying;
                while (true)
                {
                    if (!DelayedCaller.InvokeOnMainThread(delegate { return isSpeechPlayingLogic(); }, 10))
                    {
                        PlayScannerOutroSound();
                        isBusy = false;
                        break;
                    }

                    Thread.Sleep(20);
                }
            }) { IsBackground = true };
            playThread.Start();
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/> and adds scanner intro and outro to it in a new thread.
        /// </summary>
        /// <param name="action">
        /// The action.
        /// </param>
        public static void PlayActionInScannerNoNoise(string action)
        {
            PlayActionInScannerNoNoise(action, string.Empty);
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/> and adds scanner intro and outro to it in a new thread.
        /// </summary>
        /// <param name="action">
        /// The action.
        /// </param>
        /// <param name="preferredLocation">
        /// The name of the preferred location of the audio file, which will be used first when there are multiple files with the same name.
        /// </param>
        public static void PlayActionInScannerNoNoise(string action, string preferredLocation)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            Thread playThread = new Thread(() =>
            {
                PlayScannerIntroSound();
                PlayAction(action, true, preferredLocation);
                PlayScannerOutroSound();
                isBusy = false;
            }) { IsBackground = true };
            playThread.Start();
        }
        
        /// <summary>
        /// Plays <paramref name="path"/> and returns an instance of <see cref="PlaybackControl"/> to control.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The controller.</returns>
        public static PlaybackControl PlayFile(string path)
        {
            string fileName = path;
            fileName = Path.Combine(AudioFilesPath, fileName);
            return SoundEngine.PlayExternalSound(fileName);
        }

        /// <summary>
        /// Plays the sound of a police backup requested.
        /// </summary>
        public static void PlayPoliceBackupRequested()
        {
            PlaySpeechInScannerFromPed(LCPDFRPlayer.LocalPlayer.Ped, "REQUEST_BACKUP");
        }

        /// <summary>
        /// Plays audio of the player requesting a NOOSE team
        /// </summary>
        public static void PlayNooseBackupRequested()
        {
            string speech = LCPDFRPlayer.LocalPlayer.Ped.VoiceData.DefaultNooseBackupSpeech;
            PlaySpeechInScannerFromPed(LCPDFRPlayer.LocalPlayer.Ped, speech);
        }

        /// <summary>
        /// Plays audio of the player requesting air support
        /// </summary>
        public static void PlayAirSupportRequested()
        {
            PlaySpeechInScannerFromPed(LCPDFRPlayer.LocalPlayer.Ped, "WANTED_LEVEL_INC_TO_3");
        }

        /// <summary>
        /// Plays audio of the player requesting an ambulance
        /// </summary>
        public static void PlayAmbulanceRequested()
        {
            if (LCPD_First_Response.LCPDFR.Scripts.Hardcore.playerInjured)
            {
                PlaySpeechInScannerFromPed(CPlayer.LocalPlayer.Ped, "OFFICER_DOWN");
            }
            else
            {
                PlaySpeechInScannerFromPed(CPlayer.LocalPlayer.Ped, "PED_SHOT");
            }
        }

        /// <summary>
        /// Plays the sound of an officer reporting a pursuit
        /// </summary>
        public static void PlayPursuitReported(EPursuitCallInReason reason)
        {
            object[] parameter = new object[1];
            parameter[0] = reason;

            if (reason == EPursuitCallInReason.Pursuit || CPlayer.LocalPlayer.Ped.VoiceData.ReportSpeech == null)
            {

                Thread t = new Thread(new ParameterizedThreadStart(PlayPursuitReportedThread));
                t.IsBackground = true;
                t.Start(parameter);
            }
            else
            {
                PlaySpeechInScannerFromPed(LCPDFRPlayer.LocalPlayer.Ped, CPlayer.LocalPlayer.Ped.VoiceData.ReportSpeech);
            }
        }

        /// <summary>
        /// Plays the sound of a NOOSE team being dispatched from <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayNooseBackupDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayNooseBackupDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of an air unit being dispatched to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayAirUnitDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayAirUnitDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of a police backup dispatched from <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayPoliceBackupDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayPoliceBackupDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of a police pursuit backup dispatched from <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayPolicePursuitBackupDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayPolicePursuitBackupDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of a police water unit being dispatched <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayPoliceWaterBackupDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayPoliceWaterBackupDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of a police water unit being dispatched <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayNooseWaterBackupDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayNooseWaterBackupDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of an ambulance being dispatched to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayAmbulanceDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayAmbulanceDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of a firetruck being dispatched to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayFiretruckDispatched(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayFiretruckDispatchedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of dispatch acknowledging a pursuit in <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayDispatchAcknowledgeReportedCrime(Vector3 position, AudioHelper.EPursuitCallInReason crime)
        {
            object[] parameter = new object[2];
            parameter[0] = position;
            parameter[1] = crime;

            Thread t = new Thread(new ParameterizedThreadStart(PlayDispatchAcknowledgeReportedCrimeThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of dispatch providing a location/heading update on a fleeing suspect
        /// </summary>
        /// <param name="suspect">The suspect.</param>
        public static void PlayDispatchChaseUpdateOnSuspect(CPed suspect)
        {
            object[] parameter = new object[1];
            parameter[0] = suspect;

            if (!suspect.Exists())
            {
                Log.Debug("PlayDispatchChaseUpdateOnSuspect: Suspect no longer valid", "AudioHelper");
            }

            Thread t = new Thread(new ParameterizedThreadStart(PlayDispatchChaseUpdateOnSuspectThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of dispatch providing a traffic alert for a high speed pursuit
        /// </summary>
        /// <param name="suspect">The suspect.</param>
        public static void PlayDispatchIssueTrafficAlertForSuspect(float speed, Vector3 position)
        {
            object[] parameter = new object[2];
            parameter[0] = speed;
            parameter[1] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayDispatchIssueTrafficAlertForSuspectThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the sound of dispatch acknowledging an officer has been attacked
        /// </summary>
        /// <param name="position">The position of the attack.</param>
        public static void PlayDispatchAcknowledgeOfficerAttacked(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayDispatchAcknowledgeOfficerAttackedThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays the suspect in custody sound.
        /// </summary>
        /// <param name="noise">Whether scanner noise should be enabled.</param>
        public static void PlaySuspectInCustody(bool noise)
        {
            if (noise)
            {
                PlayActionInScanner("WE_HAVE_THE_SUSPECT_IN_CUSTODY");
            }
            else
            {
                PlayActionSound("WE_HAVE_THE_SUSPECT_IN_CUSTODY");
            }
        }

        /// <summary>
        /// Plays the sound of dispatch acknwolding a suspect down.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayDispatchAcknowledgeSuspectDown()
        {
            Thread t = new Thread(new ThreadStart(PlayDispatchAcknowledgeSuspectDownThread));
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// Plays a suspect killed report from dispatch.
        /// </summary>
        /// <param name="noise">Whether scanner noise should be enabled.</param>
        public static void PlayDispatchAcknowledgeSuspectDownThread()
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            PlayScannerInSound();

            int randomIntro = Common.GetRandomValue(0, 2);
            string intro = "THIS_IS_CONTROL";

            if (Common.GetRandomBool(0, 2, 1))
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            PlayAction(intro, true);

            string[] speech = new string[] { "SUSPECT_IS_DOWN", "SUSPECT_NEUTRALIZED", "SUSPECT_TAKEN_DOWN" };
            PlayAction(Common.GetRandomCollectionValue<string>(speech), true);

            PlayScannerEndSound();
            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of dispatch acknwolding a civilian down in <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayDispatchAcknowledgeCivilianDown(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayDispatchAcknowledgeCivilianDownThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays a civilian down report from dispatch.
        /// </summary>
        /// <param name="noise">Whether scanner noise should be enabled.</param>
        private static void PlayDispatchAcknowledgeCivilianDownThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            PlayScannerInSound();

            int randomIntro = Common.GetRandomValue(0, 2);
            string intro = "THIS_IS_CONTROL";

            if (Common.GetRandomBool(0, 2, 1))
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            PlayAction(intro, true);

            string speech;

            if (Common.GetRandomBool(0, 2, 1))
            {
                string[] instructions = new string[] { "INS_I_NEED_A_MEDICAL_TEAM_TO_INVESTIGATE", "INS_I_NEED_A_MEDICAL_TEAM_FOR_ERRR", "INS_I_NEED_A_MEDIC_FOR" };
                speech = Common.GetRandomCollectionValue<string>(instructions);
            }
            else
            {
                speech = "INS_WE_HAVE_A_REPORT_OF_ERRR";
            }

            PlayAction(speech, true);
            PlayAction("A_AH", true);

            string[] description = new string[] { "CRIM_A_MEDICAL_EMERGENCY", "CRIM_AN_UNCONCIOUS_CIVILIAN", "CRIM_A_CIVILIAN_DOWN" };
            PlayAction(Common.GetRandomCollectionValue<string>(description), true);

            if (AreaHelper.GetAreaName(position) != "AT_SEA") PlayAction("IN_01", true, "PoliceScanner");
            PlayAction(AreaHelper.GetAreaName(position), true);

            PlayScannerEndSound();
            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of dispatch acknwolding an officer down in <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void PlayDispatchAcknowledgeOfficerDown(Vector3 position)
        {
            object[] parameter = new object[1];
            parameter[0] = position;

            Thread t = new Thread(new ParameterizedThreadStart(PlayDispatchAcknowledgeOfficerDownThread));
            t.IsBackground = true;
            t.Start(parameter);
        }

        /// <summary>
        /// Plays an officer down report from dispatch.
        /// </summary>
        /// <param name="noise">Whether scanner noise should be enabled.</param>
        private static void PlayDispatchAcknowledgeOfficerDownThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            PlayScannerInSound();

            int randomIntro = Common.GetRandomValue(0, 2);
            string intro = "ALL_UNITS_ALL_UNITS";

            if (Common.GetRandomBool(0, 2, 1))
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            PlayAction(intro, true);

            string speech;

            if (Common.GetRandomBool(0, 2, 1))
            {
                string[] instructions = new string[] { "INS_WEVE_GOT", "INS_WE_HAVE", "INS_I_NEED_A_MEDICAL_TEAM_TO_RESPOND_TO" };
                speech = Common.GetRandomCollectionValue<string>(instructions);
            }
            else
            {
                speech = "INS_WE_HAVE_A_REPORT_OF_ERRR";
            }

            PlayAction(speech, true);
            PlayAction("A_AH", true);
            PlayAction("CRIM_AN_OFFICER_DOWN", true);
            PlayAction("IN_01", true, "PoliceScanner");

            if (Common.GetRandomBool(0, 5, 1))
            {
                PlayAction("ERR_ERRR", true);
            }

            PlayAction(AreaHelper.GetAreaName(position), true);

            PlayScannerEndSound();
            isBusy = false;
        }

        /// <summary>
        /// Plays an officer down report from dispatch.
        /// </summary>
        private static void PlayDispatchAcknowledgeOfficerAttackedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            PlayScannerInSound();


            int randomIntro = Common.GetRandomValue(0, 4);
            string intro = "ALL_UNITS";

            if (randomIntro == 1)
            {
                intro = "THIS_IS_CONTROL";
            }
            else if (randomIntro == 2)
            {
                intro = "UNITS_PLEASE_BE_ADVISED";
            }
            else
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            PlayAction(intro, true);

            string speech;
            string[] instructions = new string[] { "INS_WEVE_GOT", "INS_WE_HAVE" };
            speech = Common.GetRandomCollectionValue<string>(instructions);
            PlayAction(speech, true);

            string[] crime = new string[] { "CRIM_AN_ASSAULT_ON_AN_OFFICER", "CRIM_AN_ATTACK_ON_AN_OFFICER", "CRIM_AN_OFFICER_STRUCK", "CRIM_AN_OFFICER_ASSAULT" };
            speech = Common.GetRandomCollectionValue<string>(crime);
            PlayAction(speech, true);
            PlayPositionInThread(position);
            PlayScannerEndSound();
            isBusy = false;
        }

        /// <summary>
        /// Plays the scanner intro noise.
        /// </summary>
        /// <returns>The controller.</returns>
        public static PlaybackControl PlayScannerIntroNoise()
        {
            string fileName = "Resident\\Scanner_Resident\\IN_NOISE_01.wav";
            fileName = Path.Combine(AudioFilesPath, fileName);
            return SoundEngine.PlayExternalSound(fileName);
        }

        /// <summary>
        /// Plays the scanner intro loop in a loop. Use <see cref="PlaybackControl.Stop"/> to end.
        /// </summary>
        /// <returns>The controller.</returns>
        public static PlaybackControl PlayScannerNoiseLoop()
        {
            string fileName = "Resident\\Scanner_Resident\\NOISE_LOOP_A.wav";
            fileName = Path.Combine(AudioFilesPath, fileName);
            PlaybackControl playbackControl = SoundEngine.PlayExternalSound(fileName, true);

            // We don't want the noise to be so loud
            playbackControl.Volume = -3000;
            return playbackControl;
        }

        /// <summary>
        /// Plays the scanner end noise.
        /// </summary>
        /// <returns>The controller.</returns>
        public static PlaybackControl PlayScannerEndNoise()
        {
            string fileName = "Resident\\Scanner_Resident\\END_NOISE_01.wav";
            fileName = Path.Combine(AudioFilesPath, fileName);
            return SoundEngine.PlayExternalSound(fileName);
        }

        /// <summary>
        /// Plays the scanner in sound, that is a short beep.
        /// </summary>
        /// <returns>The controller.</returns>
        public static PlaybackControl PlayScannerInSound()
        {
            string fileName = "Resident\\Scanner_Resident\\IN_01.wav";
            fileName = Path.Combine(AudioFilesPath, fileName);
            return SoundEngine.PlayExternalSound(fileName);
        }

        /// <summary>
        /// Plays the scanner end sound, that is a short beep.
        /// </summary>
        /// <returns>The controller.</returns>
        public static PlaybackControl PlayScannerEndSound()
        {
            string fileName = "Resident\\Scanner_Resident\\ENDING_03.wav";
            fileName = Path.Combine(AudioFilesPath, fileName);
            return SoundEngine.PlayExternalSound(fileName);
        }

        /// <summary>
        /// Plays the scanner intro sound, a short noise and a beep. Returns when finshed.
        /// </summary>
        public static void PlayScannerIntroSound()
        {
            PlaybackControl inSound = PlayScannerInSound();
            if (inSound == null)
            {
                Log.Debug("PlayScannerIntroSound: Failed to play scanner in sound", "AudioHelper");
                return;
            }

            while (inSound.IsPlaying)
            {
                Thread.Sleep(1);
            }

            PlaybackControl introNoise = PlayScannerIntroNoise();
            if (introNoise == null)
            {
                Log.Debug("PlayScannerIntroSound: Failed to play scanner intro noise", "AudioHelper");
                return;
            }

            while (introNoise.IsPlaying)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Plays the scanner outro sound, a short beep. Returns when finished.
        /// </summary>
        public static void PlayScannerOutroSound()
        {
            PlaybackControl endSound = PlayScannerEndSound();
            if (endSound == null)
            {
                Log.Debug("PlayScannerOutroSound: Failed to play scanner end sound", "AudioHelper");
                return;
            }

            while (endSound.IsPlaying || !endSound.IsDisposed)
            {
                Thread.Sleep(1);
            } 
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="waitTillEnd">Whether the function returns when the sound has finished.</param>
        private static void PlayAction(string action, bool waitTillEnd)
        {
            PlayAction(action, waitTillEnd, string.Empty);
        }

        /// <summary>
        /// Plays the audio file assigned to <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="waitTillEnd">Whether the function returns when the sound has finished.</param>
        /// <param name="preferredLocation">The name of the preferred location of the audio file, which will be used first when there are multiple files with the same name.</param>
        private static void PlayAction(string action, bool waitTillEnd, string preferredLocation)
        {
            // Check if action contains several speeches
            Regex regex = new Regex(" ");
            string[] actions = regex.Split(action);
            foreach (string s in actions)
            {
                string file = audioDatabase.GetAudioFileForAction(s, preferredLocation);
                if (string.IsNullOrEmpty(file))
                {
                    Log.Debug("PlayAction: Failed to find " + s, "AudioHelper");
                    return;
                }

                PlaybackControl playbackControl = SoundEngine.PlayExternalSound(file);
                if (playbackControl == null)
                {
                    Log.Debug("PlayAction: Failed to play " + s, "AudioHelper");
                    return;
                }

                if (waitTillEnd)
                {
                    while (playbackControl.IsPlaying)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }

        /// <summary>
        /// Plays the sound of an officer calling in a pursuit
        /// </summary>
        private static void PlayPursuitReportedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            string voice = DelayedCaller.InvokeOnMainThread(delegate { return LCPDFRPlayer.LocalPlayer.Voice; }, 1);

            // Vehicle pursuit
            PlayScannerInSound();
            PlayAction("CALL_IN_PURSUIT", true, voice);
            PlayScannerEndSound();
           
            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of dispatch acknowledging a pursuit being called in
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayDispatchAcknowledgeReportedCrimeThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            EPursuitCallInReason reason = (EPursuitCallInReason)para[1];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayPoliceBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possbible audios to play now
            int randomValue = Common.GetRandomValue(0, 6);

            string intro = "THIS_IS_CONTROL";
            if (randomValue > 3)
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            string err = "ERR_ERRR";
            randomValue = Common.GetRandomValue(0, 3);

            string[] crimes;
            string crime;

            if (reason == EPursuitCallInReason.FootPursuit || reason == EPursuitCallInReason.Pursuit)
            {
                crimes = new string[] {"CRIM_A_CRIMINAL_RESISTING_ARREST", "CRIM_A_SUSPECT_RESISTING_ARREST", "CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE"};     
            }
            else if (reason == EPursuitCallInReason.Backup)
            {
                crimes = new string[] { "CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", "CRIM_AN_OFFICER_IN_DANGER" }; 
            }
            else if (reason == EPursuitCallInReason.Shooting)
            {
                crimes = new string[] { "CRIM_SHOTS_FIRED", "CRIM_A_SHOOTING", "CRIM_A_SUSPECT_ARMED_AND_DANGEROUS" };
            }
            else if (reason == EPursuitCallInReason.ShotAt)
            {
                crimes = new string[] { "CRIM_SHOTS_FIRED_AT_AN_OFFICER", "CRIM_A_FIREARM_ATTACK_ON_AN_OFFICER" };
            }
            else if (reason == EPursuitCallInReason.Assaulted)
            {
                crimes = new string[] { "CRIM_AN_OFFICER_STRUCK", "CRIM_AN_ATTACK_ON_AN_OFFICER", "CRIM_AN_ASSAULT_ON_AN_OFFICER" };
            }
            else
            {
                crimes = new string[] { "CRIM_A_CIVILIAN_CAUSING_TROUBLE", "CRIM_SUSPICIOUS_ACTIVITY" };
            }

            crime = Common.GetRandomCollectionValue<string>(crimes);

            switch (randomValue)
            {
                case 0:
                    PlayAction("INS_UNITS_REPORT", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction(crime, true);

                    break;

                case 1:
                    if (Common.GetRandomBool(0, 3, 1))
                    {
                        PlayAction("INS_AVAILABLE_UNITS_RESPOND_TO", true);
                    }
                    else
                    {
                        PlayAction(intro, true);
                        PlayAction("INS_WEVE_GOT", true);
                    }

                    PlayAction(crime, true);
                    break;

                case 2:
                    PlayAction("INS_UNITS_REPORT", true);
                    PlayAction(crime, true);
                    break;
            }

            PlayPositionInThread(position);
            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of dispatch issuing a traffic alert on a speeding chase suspect
        /// </summary>
        /// <param name="parameter">The suspect.</param>
        private static void PlayDispatchIssueTrafficAlertForSuspectThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            float speed = (float)para[0];
            Vector3 position = (Vector3)para[1];

            if (speed == 0)
            {
                Log.Debug("PlayDispatchIssueTrafficAlertForSuspectThread: Speed was 0", "AudioHelper");
                isBusy = false;
                return;
            }
            string streetName = DelayedCaller.InvokeOnMainThread(delegate { return World.GetStreetName(position); }, 1);
            string areaName = "";

            // Street or area?  Default to area
            bool useArea = true;

            if (AreaHelper.GetAreaName(position) == "AT_SEA")
            {
                // Never use street names if at sea.
                streetName = null;
            }

            // Try to get a streetname
            if (streetName != null && !string.IsNullOrWhiteSpace(streetName))
            {
                string[] splitStreets = Regex.Split(streetName, ", ");

                foreach (string splitStreet in splitStreets)
                {
                    string formattedStreet = splitStreet.Replace(" ", "_");
                    string audioFilePath = audioDatabase.GetAudioFileForAction(formattedStreet.ToUpper());

                    if (audioFilePath != null && !string.IsNullOrWhiteSpace(audioFilePath))
                    {
                        // I presume we have a street, so lets use it
                        useArea = false;
                        streetName = formattedStreet;
                        break;
                    }
                }
            }

            // Set up shit and intro
            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayPoliceBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Main audio body
            if (Common.GetRandomBool(0, 2, 1))
            {
                PlayAction("ALL_UNITS_ALL_UNITS", true);
            }
            else
            {
                PlayAction("UNITS_PLEASE_BE_ADVISED", true);
            }

            if (Common.GetRandomBool(0, 2, 1))
            {
                PlayAction("INS_WE_HAVE", true);
            }
            else
            {
                PlayAction("INS_WEVE_GOT", true);
            }

            PlayAction("A", true);
            PlayAction("INS_TRAFFIC_ALERT_FOR", true);
            PlayAction("A", true);
            PlayAction("SUSPECT", true);


            // Speed
            if (speed >= 40)
            {
                PlayAction("DOING_90", true);
            }
            else if (speed >= 37f)
            {
                PlayAction("DOING_80", true);
            }
            else if (speed >= 32.5f)
            {
                PlayAction("DOING_70", true);
            }
            else
            {
                PlayAction("DOING_60", true);
            }

            // Location
            if (useArea)
            {
                // Area
                DelayedCaller.Call(delegate { Log.Warning("Getting area name", "AudioHelper"); }, 1);
                areaName = AreaHelper.GetAreaName(position);
                if (areaName == "UNKNOWN")
                {
                    // Thread safe
                    DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);
                    isBusy = false;
                    playNoiseLoop.Stop();
                    return;
                }
                else if (areaName != "AT_SEA")
                {
                    PlayAction("IN_01", true, "PoliceScanner");
                }

                PlayAction(areaName, true);
            }
            else
            {
                // Street
                PlayAction("ON", true);
                PlayAction(streetName.ToUpper(), true);
            }
   
            // Regardless of street or area, finish the scanner.
            playNoiseLoop.Stop();
            PlayScannerOutroSound();
            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of dispatch providing a location/heading update on a chase suspect
        /// </summary>
        /// <param name="parameter">The suspect.</param>
        private static void PlayDispatchChaseUpdateOnSuspectThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            CPed suspect = (CPed)para[0];

            if (!suspect.Exists()) return;

            Vector3 position = DelayedCaller.InvokeOnMainThread(delegate { return suspect.Position; }, 1);
            float heading = DelayedCaller.InvokeOnMainThread(delegate { return suspect.Heading; }, 1);
            string carColor = DelayedCaller.InvokeOnMainThread(delegate
            {
                if (suspect.CurrentVehicle != null && suspect.CurrentVehicle.Exists())
                {
                    return PoliceScanner.CarColSpeak(suspect.CurrentVehicle.Color.Index);
                }
                else
                {
                    return null;
                }
            }, 1);

            string carName = DelayedCaller.InvokeOnMainThread(delegate
            {
                if (suspect.CurrentVehicle != null && suspect.CurrentVehicle.Exists())
                {
                    return PoliceScanner.CarNameSpeak(suspect.CurrentVehicle);
                }
                else
                {
                    return null;
                }
            }, 1);

            bool inCar = false;

            if (carName != null && carColor != null)
            {
                inCar = true;
            }

            string streetName = DelayedCaller.InvokeOnMainThread(delegate { return World.GetStreetName(suspect.Position); }, 1);
            string areaName = "";
            string lastScannerAreaOrStreet = DelayedCaller.InvokeOnMainThread(delegate { return suspect.PedData.LastScannerAreaOrStreet; }, 1);
            string err = "ERR_ERRR";
            string headingSuspect; // E.g. SUSPECT_HEADED_NORTH
            string headingDirection; // E.g. NORTH.  You can add BOUND to this if needed

            // Set up shit and intro
            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayPoliceBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Get suspect heading
            headingSuspect = "SUSPECT_HEADED_NORTH";
            headingDirection = "NORTH";

            if (heading > 45 && heading < 135)
            {
                // Heading west
                headingSuspect = "SUSPECT_HEADED_WEST";
                headingDirection = "WEST";
            }
            else if (heading >= 135 && heading < 225)
            {
                // Heading south
                headingSuspect = "SUSPECT_HEADING_SOUTH";
                headingDirection = "SOUTH";
            }
            else if (heading >= 225 && heading < 315)
            {
                // Heading east
                headingSuspect = "SUSPECT_HEADED_EAST";
                headingDirection = "EAST";
            }


            if (inCar)
            {
                carName = carName.Trim();
                carColor = carColor.Trim();
            }

            DelayedCaller.Call(delegate { Log.Debug("carName " + carName, "AudioHelper"); }, 1);
            DelayedCaller.Call(delegate { Log.Debug("carColor " + carColor, "AudioHelper"); }, 1);
            DelayedCaller.Call(delegate { Log.Debug("headingSuspect " + headingSuspect, "AudioHelper"); }, 1);
            DelayedCaller.Call(delegate { Log.Debug("headingDirection " + headingDirection, "AudioHelper"); }, 1);
            DelayedCaller.Call(delegate { Log.Debug("heading " + heading, "AudioHelper"); }, 1);
            DelayedCaller.Call(delegate { Log.Debug("lastScannerAreaOrStreet " + lastScannerAreaOrStreet, "AudioHelper"); }, 1);

            // Play intro + heading
            int randomValue = Common.GetRandomValue(0, 6);
            string intro = "THIS_IS_CONTROL";
            if (randomValue > 3)
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            if (Common.GetRandomBool(0, 4, 1))
            {
                PlayAction(intro, true);
            }

            // Street or area?  Default to area
            bool useArea = true;

            if (AreaHelper.GetAreaName(position) == "AT_SEA") streetName = null;

            // Try to get a streetname
            if (streetName != null && !string.IsNullOrWhiteSpace(streetName))
            {
                string[] splitStreets = Regex.Split(streetName, ", ");

                foreach (string splitStreet in splitStreets)
                {
                    string formattedStreet = splitStreet.Replace(" ", "_");
                    string audioFilePath = audioDatabase.GetAudioFileForAction(formattedStreet.ToUpper());

                    //DelayedCaller.Call(delegate { Log.Warning("formattedStreet " + formattedStreet, "AudioHelper"); }, 1);
                    //DelayedCaller.Call(delegate { Log.Warning("audioFilePath " + audioFilePath, "AudioHelper"); }, 1);

                    if (audioFilePath != null && !string.IsNullOrWhiteSpace(audioFilePath))
                    {
                        // I presume we have a street, so lets play it
                        useArea = false;
                        streetName = formattedStreet;
                        //DelayedCaller.Call(delegate { Log.Warning("streetName " + streetName, "AudioHelper"); }, 1);
                        break;
                    }
                }
            }

            // Otherwise, get an area name
            if (useArea)
            {
                areaName = AreaHelper.GetAreaName(position);
                if (areaName == "UNKNOWN")
                {
                    // Thread safe
                    DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                    isBusy = false;
                    return;
                }

                #region Area

                if (lastScannerAreaOrStreet.Equals(areaName))
                {
                    // Suspect in the same area
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        // This just simply plays "he's still in area"
                        PlayAction("HES_STILL_IN", true);
                        if (areaName=="AT_SEA")
                        {
                            PlayAction("A_ERR", true);
                            PlayCarDescriptionFromString(carColor);
                            PlayCarDescriptionFromString(carName);
                        }
                        PlayAction(areaName, true);
                    }
                    else
                    {
                        // Otherwise, we have some new choices
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            PlayAction("LOOKS_LIKE_HES_GOING", true);
                        }
                        else
                        {
                            PlayAction("LOOKS_LIKE_HES_HEADING", true);
                        }

                        // Calculate heading
                        string direction = headingDirection;

                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            direction += "BOUND";
                        }

                        // Play heading
                        PlayAction(direction, true);

                        // 1/2 Chance to play area
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            if (areaName != "AT_SEA")
                            {
                                PlayAction("IN_01", true, "PoliceScanner");
                            }
                            PlayAction(areaName, true);


                            // 1/2 chance to play description
                            if (inCar)
                            {
                                // in car description
                                PlayAction("IN_01", true, "PoliceScanner");
                                PlayAction("A_ERR", true);
                                PlayCarDescriptionFromString(carColor);
                                PlayCarDescriptionFromString(carName);
                            }
                            else
                            {
                                // on foot description
                                PlayAction("ON_FOOT", true);
                            }
                        }
                        else
                        {
                            // If no area, 1/2 chance to give description
                            if (Common.GetRandomBool(0, 2, 1))
                            {
                                if (inCar)
                                {
                                    PlayAction("IN_01", true, "PoliceScanner");
                                    PlayAction("A_ERR", true);
                                    PlayCarDescriptionFromString(carColor);
                                    PlayCarDescriptionFromString(carName);
                                }
                                else
                                {
                                    PlayAction("ON_FOOT", true);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Suspect in a new area
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        // Option 1: Play Suspect headed x in y in/on z
                        PlayAction(headingSuspect, true);

                        // Always play area
                        if (areaName != "AT_SEA")
                        {
                            PlayAction("IN_01", true, "PoliceScanner");
                        }
                        PlayAction(areaName, true);

                        // 1/2 Chance to play description
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            if (inCar)
                            {
                                PlayAction("IN_01", true, "PoliceScanner");
                                PlayAction("A_ERR", true);
                                PlayCarDescriptionFromString(carColor);
                                PlayCarDescriptionFromString(carName);
                            }
                            else
                            {
                                PlayAction("ON_FOOT", true);
                            }
                        }
                    }
                    else
                    {
                        // Option 2: Play Looks like he's going x in/on y in z
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            PlayAction("LOOKS_LIKE_HES_GOING", true);
                        }
                        else
                        {
                            PlayAction("LOOKS_LIKE_HES_HEADING", true);
                        }

                        // Calculate heading
                        string direction = headingDirection;

                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            direction += "BOUND";
                        }

                        // Play heading
                        PlayAction(direction, true);

                        // 1/2 Chance to play description
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            if (inCar)
                            {
                                PlayAction("IN_01", true, "PoliceScanner");
                                PlayAction("A_ERR", true);
                                PlayCarDescriptionFromString(carColor);
                                PlayCarDescriptionFromString(carName);
                            }
                            else
                            {
                                PlayAction("ON_FOOT", true);
                            }
                        }

                        // Always play area
                        if (areaName != "AT_SEA") PlayAction("IN_01", true, "PoliceScanner");
                        PlayAction(areaName, true);
                    }
                }
             
                DelayedCaller.InvokeOnMainThread(delegate { suspect.PedData.LastScannerAreaOrStreet = areaName; return true; }, 1);

                #endregion
            }
            else
            {
                #region Street

                if (lastScannerAreaOrStreet.Equals(streetName.ToUpper()))
                {
                    // Suspect on the same street
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        // Option 1: He's still on z
                        PlayAction("HES_STILL_ON", true);
                        PlayAction(streetName.ToUpper(), true);
                    }
                    else
                    {
                        // Option 2: Looks like he's going x on z in y
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            PlayAction("LOOKS_LIKE_HES_GOING", true);
                        }
                        else
                        {
                            PlayAction("LOOKS_LIKE_HES_HEADING", true);
                        }

                        // Play heading
                        string direction = headingDirection;

                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            direction += "BOUND";
                        }

                        PlayAction(direction, true);

                        // Always play street
                        PlayAction("ON", true, "PoliceScanner");
                        PlayAction(streetName.ToUpper(), true);

                        // 1/2 Chance to play description
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            if (inCar)
                            {
                                // in car description
                                PlayAction("IN_01", true, "PoliceScanner");
                                PlayAction("A_ERR", true);
                                PlayCarDescriptionFromString(carColor);
                                PlayCarDescriptionFromString(carName);
                            }
                            else
                            {
                                // on foot description
                                PlayAction("ON_FOOT", true);
                            }
                        }
                    }
                }
                else
                {
                    // Suspect on a new street
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        // Option 1: Suspect headed x on z in y
                        PlayAction(headingSuspect, true);

                        // Always play street
                        PlayAction("ON", true, "PoliceScanner");
                        PlayAction(streetName.ToUpper(), true);

                        // 1/2 Chance to play description
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            if (inCar)
                            {
                                // in car description
                                PlayAction("IN_01", true, "PoliceScanner");
                                PlayAction("A_ERR", true);
                                PlayCarDescriptionFromString(carColor);
                                PlayCarDescriptionFromString(carName);
                            }
                            else
                            {
                                // on foot description
                                PlayAction("ON_FOOT", true);
                            }
                        }
                    }
                    else
                    {
                        // Option 2: Looks like he's going x on z in y
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            PlayAction("LOOKS_LIKE_HES_GOING", true);
                        }
                        else
                        {
                            PlayAction("LOOKS_LIKE_HES_HEADING", true);
                        }

                        // Play heading
                        string direction = headingDirection;

                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            direction += "BOUND";
                        }

                        PlayAction(direction, true);

                        // Always play street
                        PlayAction("ON", true, "PoliceScanner");
                        PlayAction(streetName.ToUpper(), true);

                        // 1/2 Chance to play description
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            if (inCar)
                            {
                                // in car description
                                PlayAction("IN_01", true, "PoliceScanner");
                                PlayAction("A_ERR", true);
                                PlayCarDescriptionFromString(carColor);
                                PlayCarDescriptionFromString(carName);
                            }
                            else
                            {
                                // on foot description
                                PlayAction("ON_FOOT", true);
                            }
                        }
                    }
                }
    
                DelayedCaller.InvokeOnMainThread(delegate { suspect.PedData.LastScannerAreaOrStreet = streetName.ToUpper(); return true; }, 1);
                #endregion
            }

            // Regardless of street or area, finish the scanner.
            playNoiseLoop.Stop();
            PlayScannerOutroSound();
            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of a police backup dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayPolicePursuitBackupDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayPoliceBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possbible audios to play now
            int randomValue = Common.GetRandomValue(0, 6);

            string intro = "THIS_IS_CONTROL";
            if (randomValue > 3)
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            string err = "ERR_ERRR";

            switch (randomValue)
            {
                case 0:
                    PlayAction("THIS_IS_CONTROL", true);
                    PlayAction("INS_AVAILABLE_UNITS_RESPOND_TO", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_A_CRIMINAL_RESISTING_ARREST", true);

                    break;

                case 1:
                    PlayAction(intro, true);
                    PlayAction("INS_I_NEED_A_UNIT_FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_A_SUSPECT_RESISTING_ARREST", true);
                    break;

                case 2:
                    PlayAction("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR", true);
                    PlayAction("CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE", true);
                    break;

                case 3:
                    PlayAction("INS_ATTENTION_ALL_UNITS_WE_HAVE", true);
                    PlayAction("CRIM_A_SUSPECT_RESISTING_ARREST", true);
                    break;

                case 4:
                    PlayAction("INS_AVAILABLE_UNITS_RESPOND_TO", true);
                    PlayAction("CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE", true);
                    break;

                case 5:
                    PlayAction(intro, true);
                    PlayAction("ASSISTANCE_REQUIRED", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_A_CRIMINAL_RESISTING_ARREST", true);
                    break;

            }

            if (areaName != "AT_SEA") PlayAction("IN_01", true, "PoliceScanner");
            if (Common.GetRandomBool(0, 2, 1))
            {
                PlayAction(err, true);
            }

            PlayAction(areaName, true);
            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of an air unit being dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayAirUnitDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayAirUnitDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possible audios to play now
            int randomValue = Common.GetRandomValue(0, 7);
            int randomLocation = Common.GetRandomValue(0, 4);

            if (randomValue > 3)
            {
                string intro = "THIS_IS_CONTROL";
                PlayAction(intro, true);
            }

            if (randomValue > 5)
            {
                PlayAction("DFROM_DISPATCH_AIR_UNIT_FROM", true);
            }
            else if (randomValue > 2)
            {
                PlayAction("DFROM_DISPATCH_AIR_SQUAD_FROM", true);
            }
            else
            {
                PlayAction("DFROM_DISPATCH_AIR_UNITS_FROM", true);
            }

            string err = "ERR_ERRR";

            if (Common.GetRandomBool(0, 5, 1))
            {
                PlayAction(err, true);
            }

            PlayAction(helicopterBases[Common.GetRandomValue(0, helicopterBases.Length)], true);

            if (Common.GetRandomBool(0, 1, 0))
            {
                PlayAction("FOR", true);

                if (Common.GetRandomBool(0, 5, 1)) PlayAction(err, true);
                PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);

                if (Common.GetRandomBool(0, 2, 1))
                {
                    if (areaName != "AT_SEA") PlayAction("IN_01", true, "PoliceScanner");
                    if (Common.GetRandomBool(0, 5, 1)) PlayAction(err, true);
                    PlayAction(areaName, true);
                }
            }

            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of a NOOSE backup unit being dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayNooseBackupDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayNooseBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possible audios to play now
            int randomValue = Common.GetRandomValue(0, 3);

            if (randomValue > 1)
            {
                string intro = "THIS_IS_CONTROL";
                PlayAction(intro, true);
            }

            string err = "ERR_ERRR";

            switch (randomValue)
            {
                case 0:
                    PlayAction("DFROM_DISPATCH_A_NOOSE_TEAM_FROM", true);

                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction(areaName, true);
                    PlayAction("FOR", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 1:
                    PlayAction("DFROM_DISPATCH_A_NOOSE_TEAM_FROM", true);

                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction(areaName, true);
                    break;

                case 2:
                    PlayAction("DFROM_DISPATCH_SWAT_TEAM_FROM", true);
                    PlayAction(areaName, true);
                    PlayAction("FOR", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

            }

            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of a police backup dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayPoliceBackupDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayPoliceBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possbible audios to play now
            int randomValue = Common.GetRandomValue(0, 6);

            string intro = "THIS_IS_CONTROL";
            if (randomValue > 2)
            {
                intro = "ATTENTION_ALL_UNITS";
            }

            string err = "ERR_ERRR";

            switch (randomValue)
            {
                case 0:
                    PlayAction(intro, true);
                    PlayAction("INS_WE_HAVE", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);

                    break;

                case 1:
                    PlayAction(intro, true);
                    PlayAction("INS_I_NEED_A_UNIT_FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 2:
                    PlayAction("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 3:
                    PlayAction("INS_ATTENTION_ALL_UNITS_WE_HAVE", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 4:
                    PlayAction("INS_AVAILABLE_UNITS_RESPOND_TO", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 5:
                    PlayAction(intro, true);
                    PlayAction("ASSISTANCE_REQUIRED", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;
            }

            PlayAction("IN_01", true, "PoliceScanner");
            if (Common.GetRandomBool(0, 2, 1))
            {
                PlayAction(err, true);
            }

            PlayAction(areaName, true);
            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of a police boat being dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayPoliceWaterBackupDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayPoliceBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possbible audios to play now
            int randomValue = Common.GetRandomValue(0, 6);

            string intro = "THIS_IS_CONTROL";


            string err = "ERR_ERRR";

            switch (randomValue)
            {
                case 0:
                    PlayAction(intro, true);
                    PlayAction("DFROM_DISPATCH_A_POLICE_BOAT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    PlayAction("AT_SEA", true);
                    break;

                case 1:
                    PlayAction(intro, true);
                    PlayAction("DFROM_DISPATCH_A_POLICE_BOAT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 2:
                    PlayAction("DFROM_DISPATCH_A_POLICE_BOAT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 3:
                    PlayAction("INS_ATTENTION_ALL_UNITS_WE_HAVE", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    PlayAction("AT_SEA", true);
                    break;

                case 4:
                    PlayAction("INS_AVAILABLE_UNITS_RESPOND_TO", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    PlayAction("AT_SEA", true);
                    break;

                case 5:
                    PlayAction("DFROM_DISPATCH_A_POLICE_BOAT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    PlayAction("AT_SEA", true);
                    break;
            }

            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of a NOOSE water team being dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayNooseWaterBackupDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayPoliceBackupDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possbible audios to play now
            int randomValue = Common.GetRandomValue(0, 4);

            string intro = "THIS_IS_CONTROL";


            string err = "ERR_ERRR";

            switch (randomValue)
            {
                case 0:
                    PlayAction(intro, true);
                    PlayAction("DFROM_DISPATCH_A_NOOSE_WATER_UNIT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    PlayAction("AT_SEA", true);
                    break;

                case 1:
                    PlayAction(intro, true);
                    PlayAction("DFROM_DISPATCH_A_NOOSE_WATER_UNIT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 2:
                    PlayAction("DFROM_DISPATCH_A_NOOSE_WATER_UNIT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;


                case 3:
                    PlayAction("DFROM_DISPATCH_A_NOOSE_WATER_UNIT", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    PlayAction("AT_SEA", true);
                    break;
            }

            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of an ambulance being dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayAmbulanceDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayAmbulanceDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possbible audios to play now
            int randomValue = Common.GetRandomValue(0, 6);

            string intro = "THIS_IS_CONTROL";

            string err = "ERR_ERRR";

            switch (randomValue)
            {
                case 0:
                    PlayAction(intro, true);
                    PlayAction("INS_DISPATCH_AN_AMBULANCE_FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);

                    break;

                case 1:
                    PlayAction(intro, true);
                    PlayAction("INS_I_NEED_A_MEDICAL_TEAM_FOR_ERRR", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 2:
                    PlayAction("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_A_MEDICAL_EMERGENCY", true);
                    break;

                case 3:
                    PlayAction(intro, true);
                    PlayAction("INS_DISPATCH_A_PARAMEDIC_TEAM_TO", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 4:
                    PlayAction("INS_DISPATCH_A_MEDIC_TO", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 5:
                    PlayAction(intro, true);
                    PlayAction("ASSISTANCE_REQUIRED", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_A_MEDICAL_EMERGENCY", true);
                    break;
            }

            PlayAction("IN_01", true, "PoliceScanner");
            if (Common.GetRandomBool(0, 2, 1))
            {
                PlayAction(err, true);
            }

            PlayAction(areaName, true);
            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        /// <summary>
        /// Plays the sound of a firetruck being dispatched.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void PlayFiretruckDispatchedThread(object parameter)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            object[] para = parameter as object[];
            Vector3 position = (Vector3)para[0];

            // Get area name
            string areaName = AreaHelper.GetAreaName(position);
            if (areaName == "UNKNOWN")
            {
                // Thread safe
                DelayedCaller.Call(delegate { Log.Warning("Failed to find area name at " + position.ToString(), "AudioHelper"); }, 1);

                isBusy = false;
                return;
            }

            PlayScannerIntroSound();
            PlaybackControl playNoiseLoop = PlayScannerNoiseLoop();
            if (playNoiseLoop == null)
            {
                Log.Debug("PlayFiretruckDispatchedThread: Failed to play noise loop", "AudioHelper");
                isBusy = false;
                return;
            }

            // Lots of possbible audios to play now
            int randomValue = Common.GetRandomValue(0, 6);

            string intro = "THIS_IS_CONTROL";

            string err = "ERR_ERRR";

            switch (randomValue)
            {
                case 0:
                    PlayAction(intro, true);
                    PlayAction("INS_DISPATCH_AVAILABLE_FIRETRUCKS_FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);

                    break;

                case 1:
                    PlayAction(intro, true);
                    PlayAction("INS_I_NEED_A_FIREFIGHTING_TEAM_FOR", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 2:
                    PlayAction("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }
                    PlayAction("CRIM_A_FIRE", true);
                    break;

                case 3:
                    PlayAction(intro, true);
                    PlayAction("INS_DISPATCH_FIRE_FIGHTERS_IN_RESPONSE_TO", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 4:
                    PlayAction("INS_I_NEED_A_FIREFIGHTING_TEAM_FOR", true);
                    PlayAction("CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE", true);
                    break;

                case 5:
                    PlayAction(intro, true);
                    PlayAction("ASSISTANCE_REQUIRED", true);
                    PlayAction("FOR", true);
                    if (Common.GetRandomBool(0, 5, 1))
                    {
                        PlayAction(err, true);
                    }

                    PlayAction("CRIM_A_FIRE", true);
                    break;
            }

            PlayAction("IN_01", true, "PoliceScanner");
            if (Common.GetRandomBool(0, 2, 1))
            {
                PlayAction(err, true);
            }

            PlayAction(areaName, true);
            playNoiseLoop.Stop();
            PlayScannerOutroSound();

            isBusy = false;
        }

        private static void PlayCarDescriptionFromString(string description)
        {
            string[] split = Regex.Split(description, " ");
            foreach (string splitted in split)
            {
                if (splitted != null && audioDatabase.GetAudioFileForAction(splitted) != null)
                {
                    PlayAction(splitted, true);
                }
            }
        }
    }
}