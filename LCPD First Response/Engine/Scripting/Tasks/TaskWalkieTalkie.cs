namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.Engine.IO;

    /// <summary>
    /// Spawns a walkie talkie object and makes the ped use it with a speech of your choosing
    /// </summary>
    internal class TaskWalkieTalkie : PedTask
    {
        /// <summary>
        /// The speech to be played
        /// </summary>
        private string speech;

        /// <summary>
        /// Whether or not the script is waiting for the animation to finish
        /// </summary>
        private bool waitingForFinish;

        /// <summary>
        /// The walkie talkie object
        /// </summary>
        private GTA.Object radioObject;

        /// <summary>
        /// The time elasped of the walkie talkie animation
        /// </summary>
        private float animTimeElasped;

        /// <summary>
        /// Whether or not the ped is/has saying/said the speech
        /// </summary>
        private bool sayingSpeech;

        /// <summary>
        /// Ambient sounds played from the object
        /// </summary>
        private readonly string[] ambientSounds = new string[] {"MOBILE_TWO_WAY_GARBLED_SEQ", "MOBILE_TWO_WAY_GARBLED_GLITCH", "GARBLED_CHATTER_MT" };

        /// <summary>
        /// The walkie talkie model.
        /// </summary>
        private readonly GTA.Model radioModel = new GTA.Model("amb_walkietalkie");

        /// <summary>
        /// The active weapon of the ped before they used the walkie talkie
        /// </summary>
        private GTA.Weapon activeWeapon;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWalkieTalkie"/> class.
        /// </summary>
        /// <param name="speech">
        /// The speech to play.
        /// </param>
        public TaskWalkieTalkie(string speech)
            : base(ETaskID.WalkieTalkie)
        {
            this.speech = speech;

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWalkieTalkie"/> class.
        /// </summary>
        /// <param name="speechAction">
        /// The speech action.
        /// </param>
        public TaskWalkieTalkie(Action speechAction)
            : base(ETaskID.WalkieTalkie)
        {
            this.speech = "WALKIE_TALKIE";

            DelayedCaller.Call(delegate { speechAction(); }, this, 800);
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (ped.Exists())
            {
                ped.BlockGestures = false;
                Native.Natives.BlockCharAmbientAnims(ped, false);
                ped.BlockWeaponSwitching = false;
                ped.Weapons.Select(activeWeapon);
            }

            if (radioObject != null && radioObject.Exists())
            {
                radioObject.Delete();
            }

            SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!ped.Animation.isPlaying(new GTA.AnimationSet("missemergencycall"), "idle_answer_radio_a"))
            {
                // If the ped is not playing the animation
                if (waitingForFinish)
                {
                    // If we are waiting for the anim to finish, it has, so move on.

                    if (animTimeElasped > 0.75f)
                    {
                        // If the majority of the anim has played, we can move on.
                        MakeAbortable(ped);
                    }
                    else
                    {
                        // Otherwise, play the anim again.
                        waitingForFinish = false;
                    } 
                }
                else
                {
                    // Anim should be playing, apply it.
                    if (ped != CPlayer.LocalPlayer.Ped) ped.Task.Wait(4000);
                    ped.Task.PlayAnimSecondaryUpperBody("idle_answer_radio_a", "missemergencycall", 4.0f, false);
                    ped.Task.AlwaysKeepTask = true;
                }
            }
            else
            {
                if (ped.IsAmbientSpeechPlaying && !sayingSpeech)
                {
                    // If ambient speech is playing that we didn't set, cancel it and play ours
                    ped.CancelAmbientSpeech();
                    DelayedCaller.Call(
                        delegate
                        {
                            if (ped.Exists())
                            {
                                ped.SayAmbientSpeech(speech);
                            }
                        },
                        250);
                    sayingSpeech = true;
                }
                else
                {
                    // This updates the animation time
                    animTimeElasped = ped.Animation.GetCurrentAnimationTime(new GTA.AnimationSet("missemergencycall"), "idle_answer_radio_a");
                    if (!sayingSpeech)
                    {
                        // If our speech isn't playing
                        if (animTimeElasped > 0.6f)
                        {
                            // If the majority of the animation has elasped, the automatic speech should have been said by now
                            // Cancel anything that is playing, then play the one that we actually want
                            ped.CancelAmbientSpeech();
                            DelayedCaller.Call(
                                delegate
                                {
                                    if (ped.Exists())
                                    {
                                        ped.SayAmbientSpeech(speech);
                                    }
                                }, 
                                500);

                            // Set the script to recognise that our speech is playing.
                            DelayedCaller.Call(delegate { sayingSpeech = true; }, 450); 
                        }
                    }
                }

                if (radioObject == null || !radioObject.Exists())
                {
                    // Create the radio object
                    radioObject = GTA.World.CreateObject("amb_walkietalkie", ped.GetOffsetPosition(new GTA.Vector3(0f, 0f, -10f)));

                    if (radioObject != null && radioObject.Exists())
                    {
                        // If successful, give it to the ped
                        if (ped.Exists())
                        {
                            radioObject.AttachToPed(ped, GTA.Bone.RightHand, new GTA.Vector3(0f, 0f, 0f), new GTA.Vector3(0f, 0f, 0f));

                            // Save the ped's current weapon then remove it
                            activeWeapon = ped.Weapons.Current;
                            ped.Weapons.Select(GTA.Weapon.Unarmed);
                            Native.Natives.SetCurrentCharWeapon(ped, GTA.Weapon.Unarmed, true);
                            ped.BlockWeaponSwitching = true;

                            // Block ambient anims and gestures (not sure if this even works? :S)
                            ped.BlockGestures = true;
                            Native.Natives.BlockCharAmbientAnims(ped, true);

                            // If no speech set, play sound from radio
                            if (string.IsNullOrWhiteSpace(speech))
                            {
                                SoundEngine.PlaySoundFromObject(radioObject, Common.GetRandomCollectionValue<string>(ambientSounds));
                            }
                        }
                        else
                        {
                            if (radioObject.Exists())
                            {
                                radioObject.Delete();
                            }
                        }
                    }
                }

                // Set the script to recognise the animation has been played.
                waitingForFinish = true;
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskWalkieTalkie"; }
        }
    }
}
