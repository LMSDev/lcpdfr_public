namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;

    // * Super sorry I haven't got around to commenting this yet :( * //

    /// <summary>
    /// This task will make officers on foot search for a suspect
    /// </summary>
    internal class TaskCopSearchForPedOnFoot : PedTask
    {
        /// <summary>
        /// The last action the cop was executing
        /// </summary>
        private ETaskSearchForPedOnFootAction action;

        /// <summary>
        /// The target
        /// </summary>
        private CPed target;

        /// <summary>
        /// Timer to execute the chase logic
        /// </summary>
        private Timer processTimer;

        /// <summary>
        /// Blip for debug
        /// </summary>
        private ArrowCheckpoint blip;

        /// <summary>
        /// How many 100ms have passed since the search at the <paramref name="startPosition"/> was started
        /// </summary>
        private int timeSinceSearchStarted;

        /// <summary>
        /// The last known position of the suspect
        /// </summary>
        private Vector3 lastKnownPosition;

        /// <summary>
        /// Where the ped will start searching, this is based off of the <paramref name="lastKnownPosition"/>
        /// </summary>
        private Vector3 startPosition;

        private bool isSearching;

        private static List<Vector3> searchedPositions = new List<Vector3>();

        private PlaceToSearch searchPlace;

        private bool firstRun;

        private int stillFor = 0;

        private int timeToReapply;

        private bool alternateGoToAssigned;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopSearchForPedOnFoot"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="lastKnownPosition">
        /// The last known position of the target
        /// </param>
        public TaskCopSearchForPedOnFoot(CPed target, Vector3 lastKnownPosition)
            : base(ETaskID.CopSearchForPedOnFoot)
        {
            this.target = target;
            this.processTimer = new Timer(100);
            this.lastKnownPosition = lastKnownPosition;
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (ped.Exists())
            {
                ped.Task.ClearAll();
            }

            if (this.searchPlace != null)
            {
                this.searchPlace.SetAsSearched();
            }

            if (this.target.Exists()) this.target.Wanted.OfficersSearchingOnFoot--;

            this.SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);
            this.firstRun = true;
            ped.Task.ClearAll();
            this.GenerateStartPosition(ped);
            this.action = ETaskSearchForPedOnFootAction.GoToStartPosition;
            this.target.Wanted.OfficersSearchingOnFoot++;

            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
            {
                ped.Intelligence.TaskManager.Abort(ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.GetInVehicle)); 
            }

            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedInVehicle))
            {
                ped.Intelligence.TaskManager.Abort(ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopSearchForPedInVehicle));
            }
        }

        private PlaceToSearch GenerateAndAddPlaceToSearchInInterior(Vector3 position, int interiorID, float around = 0f, int attempts = 0)
        {
            position = this.target.GetSafePositionAlternate(position.Around(10.0f));
            Vector3 pos = position;

            for (int i = 0; i < 200; i++)
            {
                position = pos.Around(50.0f);

                if (Native.Natives.GetInteriorAtCoords(position) != interiorID)
                {
                    continue;
                }
            }

            // Log.Debug("Interior PlaceToSearch Added - it is " + position.DistanceTo2D(this.lastKnownPosition) + " units away.", this);
            return new PlaceToSearch(this.target.GetSafePositionAlternate(position), this.target, PlaceToSearch.EPlaceToSearchType.OnFoot);
        }

        private PlaceToSearch GenerateAndAddPlaceToSearchInExterior(Vector3 position, float around = 0f, int attempts = 0)
        {

            if (attempts > 50)
            {
                Log.Debug("(Bad) Exterior PlaceToSearch Returned Null - it is " + position.DistanceTo2D(this.lastKnownPosition) + " units away.", this);
                return null;
            }

            position = this.target.GetSafePositionAlternate(position.Around(around));

            bool failed = false;
            bool failedSA = false;

            int count = 0;
            foreach (PlaceToSearch place in this.target.Wanted.PlacesToSearch)
            {
                if (place.Type != PlaceToSearch.EPlaceToSearchType.OnFoot || place.Interior > 0)
                {
                    continue;
                }

                count++;
            }

            if (count > 0)
            {
                foreach (PlaceToSearch pts in this.target.Wanted.PlacesToSearch)
                {
                    if (pts.Type != PlaceToSearch.EPlaceToSearchType.OnFoot)
                    {
                        continue;
                    }

                    if (this.target.SearchArea != null)
                    {
                        if (position.DistanceTo2D(this.target.SearchArea.GetPosition()) > this.target.SearchArea.Size)
                        {
                            failedSA = true;
                            break;
                        }
                    }

                    if (position.DistanceTo(pts.Position) < 30.0f)
                    {
                        // Too close to already assigned position
                        failed = true;
                        break;
                    }
                }
            }

            if (failedSA)
            {
                return GenerateAndAddPlaceToSearchInExterior(position, around - 20.0f, attempts += 1);
            }

            if (failed)
            {
                return GenerateAndAddPlaceToSearchInExterior(position, around + 30.0f, attempts += 1);
            }

            return new PlaceToSearch(position, this.target, PlaceToSearch.EPlaceToSearchType.OnFoot); ;
        }

        private void GenerateStartPosition(CPed ped)
        {
            int count = 0;
            foreach (PlaceToSearch place in this.target.Wanted.PlacesToSearch)
            {
                if (place.Type != PlaceToSearch.EPlaceToSearchType.OnFoot)
                {
                    continue;
                }

                count++;
            }

            if (this.target.Wanted.PlacesToSearch.Count > 0)
            {
                foreach (PlaceToSearch place in this.target.Wanted.PlacesToSearch)
                {
                    if (place.Type != PlaceToSearch.EPlaceToSearchType.OnFoot)
                    {
                        continue;
                    }

                    if (!place.HasCopBeenAssigned && !place.Searched)
                    {
                        // Assign this cop to search it
                        place.AssignCop(ped);
                        searchPlace = place;
                        startPosition = place.Position;
                        this.action = ETaskSearchForPedOnFootAction.GoToStartPosition;
                        break;
                    }
                }
            }
            else
            {

                // generate a list of places to search

                // Are any interiors nearby?
                int lastKnownInterior = Native.Natives.GetInteriorAtCoords(lastKnownPosition);
                int targetInterior = Native.Natives.GetInteriorAtCoords(this.target.Position);

                if (lastKnownInterior != 0)
                {
                    // Last known position is an interior
                    for (int i = 0; i < 25; i++)
                    {
                        PlaceToSearch placeToSearch = GenerateAndAddPlaceToSearchInInterior(this.lastKnownPosition, lastKnownInterior);
                    }

                    // Log.Debug("Interior found: " + lastKnownInterior, this);
                }

                if (targetInterior != 0 && targetInterior != lastKnownInterior)
                {
                    // Target position is an interior
                    for (int i = 0; i < 25; i++)
                    {
                        PlaceToSearch placeToSearch = GenerateAndAddPlaceToSearchInInterior(this.target.Position, targetInterior);
                    }
                    // Log.Debug("Interior found: " + targetInterior, this);
                }

                List<int> interiorsToBeSearched = new List<int>();

                for (int i = 0; i < 100; i++)
                {
                    Vector3 interiorPosition = ped.GetSafePositionAlternate(this.lastKnownPosition.Around(Convert.ToSingle(i * 2)));
                    int closeInterior = Native.Natives.GetInteriorAtCoords(interiorPosition);

                    if (closeInterior != 0 && closeInterior != lastKnownInterior && closeInterior != targetInterior && !interiorsToBeSearched.Contains(closeInterior))
                    {
                        // There's an interior nearby so we'll need to search that too.  Poor cops!
                        for (int y = 0; y < 25; y++)
                        {
                            // Get 25 positions to search in it and add them.
                            PlaceToSearch placeToSearch = GenerateAndAddPlaceToSearchInInterior(interiorPosition, closeInterior);
                        }
                        interiorsToBeSearched.Add(closeInterior);
                    }
                }

                // Yay, interiors are done.  Now to search the exterior world - this is much easier!

                float maxSearchRange = 200.0f;

                if (this.target.SearchArea != null)
                {
                    maxSearchRange = this.target.SearchArea.Size;
                }

                for (int i = 0; i < 50; i++)
                {
                    PlaceToSearch placeToSearch = GenerateAndAddPlaceToSearchInExterior(lastKnownPosition.Around(maxSearchRange / 2));
                }

                int totalSearchPlaces = 0;
                int searchPlacesNearLastPosition = 0;

                foreach (PlaceToSearch placeToSearch in this.target.Wanted.PlacesToSearch)
                {
                    if (placeToSearch != null)
                    {
                        if (placeToSearch.Type != PlaceToSearch.EPlaceToSearchType.OnFoot)
                        {
                            continue;
                        }

                        totalSearchPlaces++;

                        if (placeToSearch.Position.DistanceTo(lastKnownPosition) < 10.0f)
                        {
                            searchPlacesNearLastPosition++;
                        }
                    }
                }

                if (searchPlacesNearLastPosition < 5)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 nearPosition = ped.GetSafePositionAlternate(this.lastKnownPosition.Around(Convert.ToSingle(i * 2)));
                        new PlaceToSearch(nearPosition, this.target, PlaceToSearch.EPlaceToSearchType.OnFoot);
                        totalSearchPlaces++;
                    }
                }

                Log.Debug("Completed generating positions to search.  Total places to be searched: " + totalSearchPlaces, this);
            }
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            // Abort task if target ped does no longer exist
            if (this.target == null || !this.target.Exists())
            {
                this.MakeAbortable(ped);
                return;
            }

            if (!this.target.IsAliveAndWell)
            {
                this.MakeAbortable(ped);
                return;
            }

            if (this.target.Wanted.OfficersVisual > 0)
            {
                this.MakeAbortable(ped);
                return;
            }

            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.ChasePedOnFoot))
            {
                ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.ChasePedOnFoot).MakeAbortable(ped);
            }

            if (this.processTimer.CanExecute())
            {
                //ped.Debug = action.ToString() + " (" +stillFor + ")";

                //TextHelper.PrintFormattedHelpBox(string.Format("ETaskSearchState: {0}~n~PlaceToSearch: {1}~n~isSearching: {2}~n~timeSinceSearch: {3}~n~Places Left: {4}~n~TimeToGet: {5}~n~TimeLeft: {6}", action, ptss, isSearching.ToString(),timeSinceSearchStarted, placesLeftToSearch, time, timeleft));
                if (this.action != ETaskSearchForPedOnFootAction.Wander)
                {
                    if (startPosition != null)
                    {
                        if (startPosition == Vector3.Zero)
                        {
                            this.action = ETaskSearchForPedOnFootAction.Wander;
                            this.firstRun = true;
                        }
                    }
                }

                if (this.action == ETaskSearchForPedOnFootAction.GoToStartPosition)
                {
                    if (stillFor > 25)
                    {
                        // They've been standing still for too long - I guess they can't get where they need to go or something.
                        if (alternateGoToAssigned)
                        {
                            // Hopeless
                            MakeAbortable(ped);
                            return;
                        }

                        Vector3 pavement = World.GetNextPositionOnPavement(startPosition);
                        Vector3 street = World.GetNextPositionOnStreet(startPosition);

                        startPosition = pavement;

                        if (street.DistanceTo(startPosition) < pavement.DistanceTo(startPosition))
                        {
                            startPosition = street;
                        }

                        if (startPosition == Vector3.Zero)
                        {
                            // Not much more we can do for this guy sadly, just apply a dummy task
                            this.action = ETaskSearchForPedOnFootAction.Wander;
                            this.firstRun = true;
                            return;
                        }

                        if (searchPlace != null)
                        {
                            searchPlace.Remove();
                        }

                        PlaceToSearch place = new PlaceToSearch(startPosition, this.target, PlaceToSearch.EPlaceToSearchType.OnFoot);
                        place.AssignCop(ped);
                        searchPlace = place;

                        this.firstRun = true;
                        stillFor = 0;
                        alternateGoToAssigned = true;
                    }

                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexControlMovement) || ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimplePlayRandomAmbients) || this.firstRun || timeToReapply > 50 && ped.Speed == 0)
                    {
                        if (firstRun)
                        {
                            ped.Task.ClearAll();
                        }

                        ped.SetPathfinding(true, true, true);
                        ped.SetNextDesiredMoveState(Native.EPedMoveState.Run);
                        ped.Task.RunTo(startPosition, false);
                        timeToReapply = 0;
                        firstRun = false;
                    }
                    else
                    {
                        if (ped.Position.DistanceTo(startPosition) < 5.0f)
                        {
                            isSearching = true;
                            action = ETaskSearchForPedOnFootAction.SearchPosition;
                            firstRun = true;
                        }
                    }

                    if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHitWall))
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
                        {
                            ped.Task.ClearAll();
                            ped.Task.WanderAround();
                        }
                    }
                    else
                    {
                        timeToReapply++;
                    }

                    if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill) || ped.Speed == 0)
                    {
                        stillFor++;
                        timeToReapply++;
                    }
                }

                if (this.action == ETaskSearchForPedOnFootAction.SearchPosition)
                {
                    timeSinceSearchStarted++;

                    if (firstRun)
                    {
                        ped.Task.ClearAll();
                        ped.SetPathfinding(true, true, true);
                        if (Game.CurrentEpisode == GameEpisode.GTAIV) ped.APed.TaskSearchForPedOnFoot(target.APed);
                        firstRun = false;
                    }
                    else
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSearchForPedOnFoot))
                        {
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
                            {
                                ped.Task.WanderAround();
                                if (ped.Model != "M_M_FATCOP_01") ped.SetAnimGroup("move_cop_search");
                            }
                            else
                            {
                                if (Common.GetRandomBool(0, 100, 1))
                                {
                                    if (!ped.IsAmbientSpeechPlaying) ped.SayAmbientSpeech("SCARE_HIDING_SUSPECT");
                                }
                            }
                        }
                    }

                    if (timeSinceSearchStarted > 100)
                    {
                        // Done.
                        searchPlace.SetAsSearched();
                        MakeAbortable(ped);
                    }
                    else
                    {
                        if (ped.Position.DistanceTo2D(this.target.Position) < 10.0f)
                        {
                            // They're really close as far as 2D distance is concerned, so to make it fairer on the cops
                            // as far as things like stairs and heights are concerned, we'll give them a little bit of help here

                            this.action = ETaskSearchForPedOnFootAction.Investigate;
                            this.firstRun = true;
                            return;
                        }
                    }
                }

                if (this.action == ETaskSearchForPedOnFootAction.Investigate)
                {
                    if (firstRun)
                    {
                        ped.Task.ClearAll();
                        ped.SetPathfinding(true, true, true);
                        AdvancedHookManaged.AGame.InitializeTaskCombatBustPed();
                        ped.Task.FightAgainst(this.target);
                        ped.SenseRange = 20.0f;
                        this.firstRun = false;
                    }
                    else
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombat))
                        {
                            // Just go back to normal
                            this.action = ETaskSearchForPedOnFootAction.SearchPosition;
                            this.firstRun = true;
                            return;
                        }
                    }
                }

                if (this.action == ETaskSearchForPedOnFootAction.Wander)
                {
                    if (firstRun)
                    {
                        ped.Task.ClearAll();
                        ped.SetPathfinding(true, true, true);
                        if (Game.CurrentEpisode == GameEpisode.GTAIV) ped.APed.TaskSearchForPedOnFoot(target.APed);
                        firstRun = false;
                    }
                    else
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSearchForPedOnFoot))
                        {
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWander))
                            {
                                ped.Task.WanderAround();
                            }
                            else
                            {
                                if (Common.GetRandomBool(0, 100, 1))
                                {
                                    if (!ped.IsAmbientSpeechPlaying) ped.SayAmbientSpeech("SCARE_HIDING_SUSPECT");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskCopSearchForPedOnFoot"; }
        }
    }

    /// <summary>
    /// Describes the different actions.
    /// </summary>
    internal enum ETaskSearchForPedOnFootAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        Unknown,

        /// <summary>
        /// Run to the start position of the search
        /// </summary>
        GoToStartPosition,

        /// <summary>
        /// Search the position
        /// </summary>
        SearchPosition,

        /// <summary>
        /// Applies the ComplexInvestigate task like single player wanted level
        /// </summary>
        Investigate,

        /// <summary>
        /// Just makes them wander about like the useless fucks they are
        /// </summary>
        Wander
    }

    internal class PlaceToSearch
    {
        public CPed Cop;
        public CPed Target;
        public Vector3 Position;
        public ETaskSearchForPedOnFootAction Action;
        public bool Searched;
        public bool HasCopBeenAssigned;
        public int Interior;
        public EPlaceToSearchType Type;
        private ArrowCheckpoint blip;

        public enum EPlaceToSearchType
        {
            OnFoot,
            InVehicle
        }

        public PlaceToSearch(Vector3 position, CPed target, EPlaceToSearchType type)
        {
            this.Position = position;
            this.Target = target;
            this.Target.Wanted.PlacesToSearch.Add(this);
            this.Interior = Native.Natives.GetInteriorAtCoords(position);
            // this.blip = new ArrowCheckpoint(position, delegate { });
            this.Type = type;
            /*
            if (this.blip != null)
            {
                if (this.Interior != 0)
                {
                    blip.ArrowColor = System.Drawing.Color.Gray;
                    blip.BlipColor = BlipColor.Grey;
                }
                else
                {
                    if (this.Type == EPlaceToSearchType.InVehicle)
                    {
                        blip.ArrowColor = System.Drawing.Color.LightYellow;
                        blip.BlipColor = BlipColor.LightYellow;
                    }
                    else
                    {
                        blip.ArrowColor = System.Drawing.Color.White;
                        blip.BlipColor = BlipColor.White;
                    }
                }
                blip.BlipDisplay = BlipDisplay.ArrowAndMap;
            }
            */
        }

        public void AssignCop(CPed cop)
        {
            this.Cop = cop;
            this.Action = ETaskSearchForPedOnFootAction.GoToStartPosition;
            /*
            if (this.blip != null)
            {
                if (this.Type == EPlaceToSearchType.InVehicle)
                {
                    this.blip.ArrowColor = System.Drawing.Color.Gold;
                    this.blip.BlipColor = BlipColor.Yellow;
                }
                else
                {
                    this.blip.ArrowColor = System.Drawing.Color.Red;
                    this.blip.BlipColor = BlipColor.DarkRed;
                }
            }
            */
            HasCopBeenAssigned = true;

            DelayedCaller.Call(delegate { if (cop != null && cop.Exists()) cop.SayAmbientSpeech("SPLIT_UP_AND_SEARCH"); }, Common.GetRandomValue(0, 5000));
        }

        public void SetTarget(CPed target)
        {
            this.Target = target;
        }

        public void Remove()
        {
            /*
            if (blip != null)
            {
                blip.Delete();
            }
             * */
        }

        public void SetAsSearched()
        {
            /*
            if (blip != null)
            {
                blip.Delete();
            }
            */
            this.Searched = true;
        }
    }
}