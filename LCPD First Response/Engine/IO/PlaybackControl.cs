namespace LCPD_First_Response.Engine.IO
{
    using System.Threading;

    using SlimDX.DirectSound;

    /// <summary>
    /// Provides several functions to control playback from a <see cref="SlimDX.DirectSound.SoundBuffer"/>.
    /// </summary>
    internal class PlaybackControl
    {
        /// <summary>
        /// The sound buffer.
        /// </summary>
        private SoundBuffer soundBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackControl"/> class.
        /// </summary>
        /// <param name="soundBuffer">
        /// The sound buffer.
        /// </param>
        public PlaybackControl(SoundBuffer soundBuffer)
        {
            this.soundBuffer = soundBuffer;
        }

        /// <summary>
        /// Gets a value indicating whether the soundbuffer has been disposed and all resources have been freed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return (bool)this.soundBuffer.Tag;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the sound is being played.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return this.soundBuffer.Status.HasFlag(BufferStatus.Playing);
            }
        }

        /// <summary>
        /// Gets or sets volume.
        /// </summary>
        public int Volume
        {
            get
            {
                return this.soundBuffer.Volume;
            }

            set
            {
                this.soundBuffer.Volume = value;
            }
        }

        /// <summary>
        /// Stops the playback.
        /// </summary>
        public void Stop()
        {
            this.soundBuffer.Stop();
        }
    }
}