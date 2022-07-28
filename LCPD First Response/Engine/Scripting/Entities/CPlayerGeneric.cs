namespace LCPD_First_Response.Engine.Scripting.Entities
{
    /// <summary>
    /// The generic <see cref="CPlayer"/> class allowing to easily create and receive own player types.
    /// </summary>
    /// <typeparam name="T">The new player class type.</typeparam>
    internal class CPlayer<T> : CPlayer where T : CPlayer<T>, new()
    {
        /// <summary>
        /// The local player.
        /// </summary>
        private static T localPlayer;

        /// <summary>
        /// Gets the local player.
        /// </summary>
        public static new T LocalPlayer
        {
            get
            {
                if (localPlayer == null)
                {
                    // Create new instance of the extending type and clone all properties
                    localPlayer = new T();
                    localPlayer.CloneFrom(CPlayer.LocalPlayer);
                }

                return localPlayer;
            }
        }

        /// <summary>
        /// Gets the player ped.
        /// </summary>
        public override CPed Ped
        {
            get
            {
                return CPlayer.LocalPlayer.Ped;
            }
        }

        /// <summary>
        /// Clones the instance from <paramref name="player"/>.
        /// </summary>
        /// <param name="player">The player.</param>
        private void CloneFrom(CPlayer player)
        {
            if (this.AnimGroup != null)
            {
                this.AnimGroup = player.AnimGroup;
            }

            this.IgnoredByAI = player.IgnoredByAI;
            this.LastPedPulledOver = player.LastPedPulledOver;
            this.Voice = player.Voice;
        }
    }
}