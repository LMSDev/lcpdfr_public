namespace LCPD_First_Response.Engine.Scripting
{
    /// <summary>
    /// Represents an animation in GTA, consisting of the animation set and the animation name.
    /// </summary>
    internal class Animation
    {        
        /// <summary>
        /// Initializes a new instance of the <see cref="Animation"/> class.
        /// </summary>
        /// <param name="animationSetName">
        /// The animation set name.
        /// </param>
        /// <param name="animationName">
        /// The animation name.
        /// </param>
        public Animation(string animationSetName, string animationName)
        {
            this.AnimationSetName = animationSetName;
            this.AnimationName = animationName;
        }

        /// <summary>
        /// Gets the animation set name.
        /// </summary>
        public string AnimationSetName { get; private set; }

        /// <summary>
        /// Gets the animation name.
        /// </summary>
        public string AnimationName { get; private set; }
    }
}