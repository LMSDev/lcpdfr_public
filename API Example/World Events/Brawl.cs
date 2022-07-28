namespace API_Example.World_Events
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.LCPDFR.API;

    /// <summary>
    /// A random world event where two guys will fight each other.
    /// </summary>
    public class Brawl : WorldEvent
    {
        /// <summary>
        /// The first guy.
        /// </summary>
        private LPed firstGuy;

        /// <summary>
        /// The second guy.
        /// </summary>
        private LPed secondGuy;

        /// <summary>
        /// Whether the brawl has been spotted by the player.
        /// </summary>
        private bool spottedByPlayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Brawl"/> class. Don't put any logic here, but use <see cref="Initialize"/> instead.
        /// This is because the scenario manager will create instances of this class to call <see cref="CanStart"/>.
        /// </summary>
        public Brawl() : base("Brawl")
        {
        }

        /// <summary>
        /// Checks whether the world event can start at the position depending on available peds.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <returns>
        /// True if yes, otherwise false.
        /// </returns>
        public override bool CanStart(Vector3 position)
        {
            // Look for peds meeting our requirements. Note that it is also completely fine to just always return true here and create the peds yourself.
            // But since we don't want to stress the game too much I'm using this approach for all world events, so they can only occur if the game has
            // created ambient peds anyway.
            foreach (Ped ped in World.GetAllPeds())
            {
                if (ped != null && ped.Exists())
                {
                    if (ped.Position.DistanceTo(position) < 30f)
                    {
                        LPed tempPed = LPed.FromGTAPed(ped);
                        if (!Functions.DoesPedHaveAnOwner(tempPed) && !tempPed.IsPlayer && tempPed.IsAliveAndWell && !tempPed.IsOnStreet
                            && !ped.isInVehicle())
                        {
                            if (this.firstGuy == null)
                            {
                                this.firstGuy = tempPed;
                            }
                            else
                            {
                                this.secondGuy = tempPed;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the world event can be disposed now, most likely because player got too far away.
        /// Returning true from here will call <see cref="End"/>.
        /// </summary>
        /// <returns>True if yes, false otherwise.</returns>
        public override bool CanBeDisposedNow()
        {
            return LPlayer.LocalPlayer.Ped.Position.DistanceTo(this.firstGuy.Position) > 120;
        }

        /// <summary>
        /// Called right after a world event was started.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // We want to keep exclusive ownership of the peds, so no other script can use them or consider them for its actions. 
            // Note that actions with high priority (such as player arresting a ped) can bypass this exclusive ownership and will call "PedLeftScript"
            // before transferring ownership. Make sure to clean up everything there.
            Functions.SetPedIsOwnedByScript(this.firstGuy, this, true);
            Functions.SetPedIsOwnedByScript(this.secondGuy, this, true);

            this.firstGuy.BlockPermanentEvents = true;
            this.secondGuy.BlockPermanentEvents = true;
            this.firstGuy.Task.AlwaysKeepTask = true;
            this.secondGuy.Task.AlwaysKeepTask = true;
            this.firstGuy.AttachBlip();
            this.secondGuy.AttachBlip();

            // Random chance of meele weapons.
            if (Common.GetRandomBool(0, 10, 1))
            {
                this.firstGuy.DefaultWeapon = Weapon.Melee_BaseballBat;
                this.firstGuy.EquipWeapon();
            }

            if (Common.GetRandomBool(0, 10, 1))
            {
                this.secondGuy.DefaultWeapon = Weapon.Melee_BaseballBat;
                this.secondGuy.EquipWeapon();
            }


            this.firstGuy.Task.FightAgainst(this.secondGuy);
            this.secondGuy.Task.FightAgainst(this.firstGuy);
        }

        /// <summary>
        /// Processes the main logic.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!this.spottedByPlayer)
            {
                if (this.firstGuy.Exists())
                {
                    this.spottedByPlayer = this.firstGuy.MakeCameraFocus(3500);
                }
            }

            // Check if both guys have ceased to exist, are dead or have been arrested. If it's true for at least one, end this world event.
            int guysDisposed = 0;
            if (!this.firstGuy.Exists() || !this.firstGuy.IsAliveAndWell || this.firstGuy.HasBeenArrested)
            {
                guysDisposed++;
            }

            if (!this.secondGuy.Exists() || !this.secondGuy.IsAliveAndWell || this.secondGuy.HasBeenArrested)
            {
                guysDisposed++;
            }

            if (guysDisposed > 0)
            {
                this.End();
            }
        }

        /// <summary>
        /// Called when a world event should be disposed. This is also called when <see cref="WorldEvent.CanBeDisposedNow"/> returns false.
        /// </summary>
        public override void End()
        {
            base.End();

            // If we are still the owner of the ped, so it hasn't been arrested, remove blip.
            if (Functions.IsStillControlledByScript(this.firstGuy, this))
            {
                if (this.firstGuy.Exists())
                {
                    this.firstGuy.DeleteBlip();
                }
            }

            if (Functions.IsStillControlledByScript(this.secondGuy, this))
            {
                if (this.secondGuy.Exists())
                {
                    this.secondGuy.DeleteBlip();
                }
            }

            // Automatically releases the peds and safe to call even though we might not own them anymore.
            Functions.SetPedIsOwnedByScript(this.firstGuy, this, false);
            Functions.SetPedIsOwnedByScript(this.secondGuy, this, false);
        }

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        public override void PedLeftScript(LPed ped)
        {
            base.PedLeftScript(ped);

            if (ped == this.firstGuy)
            {
                this.firstGuy.Task.ClearAll();
            }

            if (ped == this.secondGuy)
            {
                this.secondGuy.Task.ClearAll();
            }
        }
    }
}