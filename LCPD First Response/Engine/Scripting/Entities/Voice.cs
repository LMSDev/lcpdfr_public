namespace LCPD_First_Response.Engine.Scripting.Entities
{  
    /// <summary>
    /// Voice class for peds.
    /// </summary>
    internal class Voice
    {
        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The model.
        /// </summary>
        private string model;

        private string name;
        private string stopSpeech;
        private string arrestSpeech;
        private string[] chaseSpeeches;
        private string reportCrimeSpeech;
        private string foundDrugsSpeech;

        /// <summary>
        /// Initializes a new instance of the <see cref="Voice"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public Voice(CPed ped)
        {
            this.ped = ped;
            this.Reset();
        }

        /// <summary>
        /// Gets the default noose speech string.
        /// </summary>
        public string DefaultNooseBackupSpeech
        {
            get { return "WANTED_LEVEL_INC_TO_5"; }
        }

        /// <summary>
        /// Resets the voice and sets up all strings again based on the current model.
        /// </summary>
        public void Reset()
        {
            this.model = this.ped.Model.ModelInfo.Name;
            this.SetCopVoice();
        }

        public string SpeechName
        {
            get
            {
                return this.name;
            }
        }

        public string StopSpeech
        {
            get
            {
                return this.stopSpeech;
            }
        }

        public string ArrestSpeech
        {
            get
            {
                return this.arrestSpeech;
            }
        }

        public string ReportSpeech
        {
            get
            {
                return this.reportCrimeSpeech;
            }
        }

        public string ChaseSpeech
        {
            get
            {
                return Common.GetRandomCollectionValue<string>(this.chaseSpeeches);
            }
        }

        public string FoundDrugsSpeech
        {
            get
            {
                return this.foundDrugsSpeech;
            }
        }

        /// <summary>
        /// Sets the voice of the cop based on <see cref="model"/>.
        /// </summary>
        private void SetCopVoice()
        {
            if (this.model == "M_Y_NHELIPILOT")
            {
                // Needs swat voice
                this.name = "M_Y_SWAT_WHITE";
            }
            else if (this.model == "M_Y_SWAT")
            {
                // Needs swat voice
                this.name = "M_Y_SWAT_WHITE";
            }
            else if (this.model == "M_Y_STROOPER")
            {
                // Needs state trooper voice
                int voiceRandom = Common.GetRandomValue(0, 2);

                if (voiceRandom == 0)
                {
                    this.name = "M_Y_STROOPER_WHITE_01";
                }
                else
                {
                    this.name = "M_Y_COP_WHITE";
                }
            }
            else if (this.model == "M_Y_COP_TRAFFIC")
            {
                int voiceRandom = Common.GetRandomValue(0, 3);
                if (voiceRandom == 0)
                {
                    this.name = "M_Y_COP_TRAFFIC_HISPANIC";
                }
                else if (voiceRandom == 1)
                {
                    this.name = "M_Y_COP_TRAFFIC_WHITE";
                }
                else
                {
                    this.name = "M_Y_COP_TRAFFIC_BLACK";
                }
            }
            else if (this.model == "M_M_FATCOP_01")
            {
                int modelIndex = this.ped.Skin.Component.Head.ModelIndex;
                string voiceString = modelIndex == 0 ? "M_M_FATCOP_01_WHITE" : "M_M_FATCOP_01_BLACK";
                this.name = voiceString;
            }
            else if (this.model == "M_Y_COP")
            {
                int modelIndex = this.ped.Skin.Component.Head.ModelIndex;
                string voiceString;

                if (modelIndex == 0)
                {
                    voiceString = Engine.Main.IsTbogt ? "M_Y_COP_HISPANIC" : "M_Y_COP_BLACK";
                }
                else if (modelIndex == 1)
                {
                    voiceString = Engine.Main.IsTbogt ? "M_Y_COP_TRAFFIC_HISPANIC" : "M_Y_COP_HISPANIC";
                }
                else if (modelIndex == 2)
                {
                    int voiceRandom = Common.GetRandomValue(0, 2);
                    voiceString = voiceRandom == 0 ? "M_Y_COP_WHITE" : "M_Y_COP_WHITE_02";
                }
                else
                {
                    int voiceRandom = Common.GetRandomValue(0, 2);
                    voiceString = voiceRandom == 0 ? "M_Y_COP_TRAFFIC_WHITE" : "M_Y_COP_BLACK_02";
                }

                this.name = voiceString;
            }
            else if (this.model == "M_M_FBI" || this.model == "M_Y_CIADLC_01" || this.model == "M_Y_CIADLC_02")
            {
                this.name = "M_Y_FIB";
            }
            else
            {
                this.name = "M_Y_COP_HISPANIC";
            }

            if (this.name == "M_Y_COP_BLACK")
            {
                this.stopSpeech = "SPOT_SUSPECT";
                this.arrestSpeech = "ARREST_PLAYER";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "JACKING_GENERIC_BACK", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "ARREST_PED";
            }
            else if (this.name == "M_Y_STROOPER_WHITE_01")
            {
                this.stopSpeech = "SPOT_SUSPECT";
                this.arrestSpeech = "ARREST_PLAYER";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "TARGET", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "FOUND_GUN";
            }
            else if (this.name == "M_Y_COP_WHITE_02")
            {
                this.stopSpeech = "SPOT_SUSPECT";
                this.arrestSpeech = "ARREST_PLAYER";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "DRAW_GUN", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "FOUND_GUN";
            }
            else if (this.name == "M_Y_COP_HISPANIC")
            {
                this.stopSpeech = "SPOT_SUSPECT";
                this.arrestSpeech = "PLACE_HANDS_ON_HEAD";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "DRAW_GUN", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "ARREST_PLAYER";
            }
            else if (this.name == "M_Y_COP_TRAFFIC_HISPANIC")
            {
                this.stopSpeech = "SPOT_CRIME";
                this.arrestSpeech = "VEHICLE_ATTACKED";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "JACKED_ON_STREET", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "ARREST_PED";
            }
            else if (this.name == "M_Y_COP_TRAFFIC_BLACK")
            {
                this.stopSpeech = "SPOT_CRIME";
                this.arrestSpeech = "ARREST_PLAYER";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "DRAW_GUN", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "ARREST_PED";
            }
            else if (this.name == "M_Y_COP_BLACK_02")
            {
                this.stopSpeech = "SPOT_CRIME";
                this.arrestSpeech = "ARREST_PED";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "ARREST_PLAYER";
            }
            else if (this.name == "M_M_FATCOP_01_BLACK")
            {
                this.stopSpeech = "SPOT_CRIME";
                this.arrestSpeech = "ARREST_PLAYER";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "SPOT_CRIME", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "FOUND_GUN";
            }
            else if (this.name == "M_M_FATCOP_01_WHITE")
            {
                this.stopSpeech = "SPOT_CRIME";
                this.arrestSpeech = "PLACE_HANDS_ON_HEAD";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "JACKED_ON_STREET", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "WANTED_LEVEL_INC_TO_1_NOGENDER";
            }
            else if (this.name == "M_Y_COP_TRAFFIC_WHITE")
            {
                this.stopSpeech = "SPOT_CRIME";
                this.arrestSpeech = "ARREST_PLAYER";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "JACKING_CAR_BACK";
            }
            else if (this.name == "M_Y_COP_WHITE")
            {
                this.stopSpeech = "DRAW_GUN";
                this.arrestSpeech = "PLACE_HANDS_ON_HEAD";
                this.chaseSpeeches = new string[] { "SUSPECT_IS_ON_FOOT", "CHASE_SOLO", "SURROUNDED" };
                this.reportCrimeSpeech = "WANTED_LEVEL_INC_TO_1";
                this.foundDrugsSpeech = "ARREST_PLAYER";
            }
            else if (this.name == "M_Y_SWAT_WHITE")
            {
                this.stopSpeech = "DRAW_GUN";
                this.arrestSpeech = "TARGET";
                this.chaseSpeeches = new string[] { "DRAW_GUN", "FIGHT", "JACKED_ON_STREET" };
                this.reportCrimeSpeech = null;
                this.foundDrugsSpeech = "CRASH_GENERIC";
            }
            else if (this.name == "M_Y_FIB")
            {
                this.stopSpeech = "DRAW_GUN";
                this.arrestSpeech = "ARREST_PLAYER";
                this.chaseSpeeches = new string[] { "CHASE_IN_GROUP", "SURROUNDED", "MOVE_IN" };
                this.reportCrimeSpeech = null;
                this.foundDrugsSpeech = "CRASH_GENERIC";
            }
        }
    }
}