namespace LCPD_First_Response.Engine.Input
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Speech.Recognition;
    using System.Speech.Synthesis;
    using System.Threading;

    /// <summary>
    /// Provides functions to utilize speech recognition.
    /// </summary>
    internal class SpeechRecognition : BaseComponent
    {
        /// <summary>
        /// The speech recognition engine.
        /// </summary>
        private SpeechRecognitionEngine speechRecognitionEngine;

        /// <summary>
        /// The registered phrases
        /// </summary>
        private Dictionary<string, System.Action> registeredPhrases;

        private bool hasStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognition"/> class.
        /// </summary>
        public SpeechRecognition()
        {
            // TODO: Make it based on LCPDFR language
            this.speechRecognitionEngine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
            this.speechRecognitionEngine.SpeechRecognized += this.speechRecognitionEngine_SpeechRecognized;
            this.speechRecognitionEngine.SetInputToDefaultAudioDevice();
            this.registeredPhrases = new Dictionary<string, Action>();

            Log.Debug("SpeechRecognition: Engine is ready, awaiting grammar", this);
        }

        /// <summary>
        /// Adds <paramref name="word"/> to the recognition engine.
        /// </summary>
        /// <param name="word">
        /// The word.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        public void AddGrammar(string word, System.Action callback)
        {
            this.registeredPhrases.Add(word, callback);

            GrammarBuilder grammarBuilder = new GrammarBuilder(word);
            Grammar grammar = new Grammar(grammarBuilder);

            this.speechRecognitionEngine.RequestRecognizerUpdate();
            this.speechRecognitionEngine.LoadGrammar(grammar);

            if (!this.hasStarted)
            {
                this.speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                this.hasStarted = true;
            }
        }

        private void speechRecognitionEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (this.registeredPhrases.ContainsKey(e.Result.Text))
            {
                this.registeredPhrases[e.Result.Text].Invoke();
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "SpeechRecognition";
            }
        }
    }
}