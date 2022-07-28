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
    internal class TaskCopSearchForPedInVehicle : PedTask
    {
        /// <summary>
        /// The last action the cop was executing
        /// </summary>
        private ETaskCopSearchForPedInVehicleAction action;

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

        private bool investigated;

        private bool alternateGoToAssigned;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopSearchForPedInVehicle"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="lastKnownPosition">
        /// The last known position of the target
        /// </param>
        public TaskCopSearchForPedInVehicle(CPed target, Vector3 lastKnownPosition)
            : base(ETaskID.CopSearchForPedInVehicle)
        {
            this.target = target;
            this.processTimer = new Timer(100);
            this.lastKnownPosition = lastKnownPosition;

            if (this.target == null)
            {
                throw new ArgumentNullException("target");
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (ped.Exists())
            {
                if (ped.IsInVehicle)
                {
                    if (ped.CurrentVehicle.SirenManager != null)
                    {
                        ped.CurrentVehicle.SirenManager.UnmuteSiren();
                    }
                }

                ped.Task.ClearAll();
            }

            if (this.searchPlace != null)
            {
                this.searchPlace.SetAsSearched();
            }

            if (this.target != null && this.target.Exists())
            {
                this.target.Wanted.OfficersSearchingInAVehicle--;
            }

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
            this.GenerateStartPosition(ped);
            this.action = ETaskCopSearchForPedInVehicleAction.GoToStartPosition;
            this.target.Wanted.OfficersSearchingInAVehicle++;

            if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.CopSearchForPedOnFoot))
            {
                ped.Intelligence.TaskManager.Abort(ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.CopSearchForPedOnFoot));
            }

            if (ped.IsInVehicle)
            {
                if (ped.CurrentVehicle.SirenActive)
                {
                    ped.CurrentVehicle.SirenManager.MuteSiren();
                }
            }

            ped.Task.ClearAll();
        }

        private PlaceToSearch GenerateAndAddPlaceToSearch(Vector3 position, float around = 0f, int attempts = 0)
        {

            if (attempts > 50)
            {
                Log.Debug("(Bad) Vehicle PlaceToSearch Returned Null - it is " + position.DistanceTo2D(this.lastKnownPosition) + " units away.", this);
                return null;
            }

            position = World.GetNextPositionOnStreet(this.target.Position.Around(around));

            bool failed = false;
            bool failedSA = false;

            if (this.target.Wanted.PlacesToSearch.Count > 0)
            {
                foreach (PlaceToSearch pts in this.target.Wanted.PlacesToSearch)
                {
                    if (pts.Type != PlaceToSearch.EPlaceToSearchType.InVehicle)
                    {
                        continue;
                    }

                    if (this.target.SearchArea != null)
                    {
                        if (position.DistanceTo2D(this.target.SearchArea.GetPosition()) > 100.0f + this.target.SearchArea.Size)
                        {
                            failedSA = true;
                            break;
                        }
                    }

                    if (position.DistanceTo(pts.Position) < 100.0f)
                    {
                        // Too close to already assigned position
                        failed = true;
                        break;
                    }
                }
            }

            if (failedSA)
            {
                return GenerateAndAddPlaceToSearch(position, around - 20.0f, attempts += 1);
            }

            if (failed)
            {
                return GenerateAndAddPlaceToSearch(position, around + 30.0f, attempts += 1);
            }

            return new PlaceToSearch(position, this.target, PlaceToSearch.EPlaceToSearchType.InVehicle);
        }

        private void GenerateStartPosition(CPed ped)
        {
            int count = 0;
            foreach (PlaceToSearch place in this.target.Wanted.PlacesToSearch)
            {
                if (place.Type != PlaceToSearch.EPlaceToSearchType.InVehicle)
                {
                    continue;
                }

                count++;
            }

            if (count > 0)
            {
                foreach (PlaceToSearch place in this.target.Wanted.PlacesToSearch)
                {
                    if (place.Type != PlaceToSearch.EPlaceToSearchType.InVehicle)
                    {
                        continue;
                    }

                    if (!place.HasCopBeenAssigned && !place.Searched)
                    {
                        // Assign this cop to search it
                        place.AssignCop(ped);
                        searchPlace = place;
                        startPosition = place.Position;
                        if (ped.IsDriver) ped.Task.DriveTo(World.GetNextPositionOnStreet(startPosition), 10.0f, false);
                        this.action = ETaskCopSearchForPedInVehicleAction.GoToStartPosition; 
                        break;
                    }
                }
            }
            else
            {

                // generate a list of places to search

                float maxSearchRange = 300.0f;

                if (this.target.SearchArea != null)
                {
                    maxSearchRange = this.target.SearchArea.Size + 100.0f;
                }

                for (int i = 0; i < 50; i++)
                {
                    PlaceToSearch placeToSearch = GenerateAndAddPlaceToSearch(lastKnownPosition.Around(maxSearchRange / 2));
                }

                int totalSearchPlaces = 0;
                int searchPlacesNearLastPosition = 0;

                foreach (PlaceToSearch placeToSearch in this.target.Wanted.PlacesToSearch)
                {
                    if (placeToSearch != null)
                    {
                        if (placeToSearch.Type != PlaceToSearch.EPlaceToSearchType.InVehicle)
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
                        Vector3 nearPosition = World.GetNextPositionOnStreet(this.lastKnownPosition.Around(Convert.ToSingle(i * 4)));
                        new PlaceToSearch(nearPosition, this.target, PlaceToSearch.EPlaceToSearchType.InVehicle);
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

            if (!ped.IsInVehicle)
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
                //ped.Debug = action.ToString();

                //TextHelper.PrintFormattedHelpBox(string.Format("ETaskSearchState: {0}~n~PlaceToSearch: {1}~n~isSearching: {2}~n~timeSinceSearch: {3}~n~Places Left: {4}~n~TimeToGet: {5}~n~TimeLeft: {6}", action, ptss, isSearching.ToString(),timeSinceSearchStarted, placesLeftToSearch, time, timeleft));

                if (!ped.IsInVehicle)
                {
                    MakeAbortable(ped);
                    return;
                }

                if (!ped.IsDriver)
                {
                    if (!ped.CurrentVehicle.HasDriver)
                    {
                        MakeAbortable(ped);
                    }
                }

                if (ped.IsDriver)
                {
                    if (this.action == ETaskCopSearchForPedInVehicleAction.GoToStartPosition)
                    {
                        if (ped.Position.DistanceTo(startPosition) < 20.0f)
                        {
                            // Move on
                            this.action = ETaskCopSearchForPedInVehicleAction.SearchPosition;
                            this.firstRun = true;
                            return;
                        }

                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveBasic))
                        {
                            ped.Task.ClearAll();

                            if (ped.CurrentVehicle.SirenActive)
                            {
                                ped.CurrentVehicle.SirenManager.MuteSiren();
                            }

                            Game.LoadAllPathNodes = true;
                            ped.Task.DriveTo(World.GetNextPositionOnStreet(startPosition), 10.0f, false);
                        }
                    }

                    else if (this.action == ETaskCopSearchForPedInVehicleAction.SearchPosition)
                    {
                        timeSinceSearchStarted++;

                        if (firstRun)
                        {
                            ped.Task.ClearAll();
                            ped.Task.CruiseWithVehicle(ped.CurrentVehicle, 15.0f, false);
                            firstRun = false;
                        }
                        else
                        {
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveWander))
                            {
                                ped.Task.CruiseWithVehicle(ped.CurrentVehicle, 15.0f, false);
                            }
                        }

                        if (timeSinceSearchStarted > 400)
                        {
                            // Done.
                            searchPlace.SetAsSearched();
                            MakeAbortable(ped);
                        }
                        else
                        {
                            if (!investigated && ped.Position.DistanceTo2D(World.GetNextPositionOnStreet(this.target.Position)) < 15.0f)
                            {
                                // They're really close as far as 2D distance is concerned, so to make it fairer on the cops
                                // as far as things like stairs and heights are concerned, we'll give them a little bit of help here

                                this.action = ETaskCopSearchForPedInVehicleAction.Investigate;
                                this.firstRun = true;
                                return;
                            }
                        }
                    }
                    else if (this.action == ETaskCopSearchForPedInVehicleAction.Investigate)
                    {
                        if (firstRun)
                        {
                            ped.Task.ClearAll();
                            ped.Task.DriveTo(World.GetNextPositionOnStreet(this.target.Position), 15.0f, false);
                            this.firstRun = false;
                        }
                        else
                        {
                            if (ped.Position.DistanceTo2D(World.GetNextPositionOnStreet(this.target.Position)) < 5.0f)
                            {
                                // Just go back to normal
                                investigated = true;
                                this.action = ETaskCopSearchForPedInVehicleAction.SearchPosition;
                                this.firstRun = true;
                                return;
                            }
                        }
                    }
                    else if (this.action == ETaskCopSearchForPedInVehicleAction.Wander)
                    {
                        if (firstRun)
                        {
                            ped.Task.ClearAll();
                            ped.Task.CruiseWithVehicle(ped.CurrentVehicle, 15.0f, false);
                            this.firstRun = false;
                        }

                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveWander))
                        {
                            ped.Task.CruiseWithVehicle(ped.CurrentVehicle, 15.0f, false);
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
            get { return "TaskCopSearchForPedInVehicle"; }
        }
    }

    /// <summary>
    /// Describes the different actions.
    /// </summary>
    internal enum ETaskCopSearchForPedInVehicleAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        Unknown,

        /// <summary>
        /// Drive to the start position of the search
        /// </summary>
        GoToStartPosition,

        /// <summary>
        /// Search the position
        /// </summary>
        SearchPosition,

        /// <summary>
        /// Makes them use god given powers to go to the suspect (trololol)
        /// </summary>
        Investigate,

        /// <summary>
        /// Just makes them cruise around like the useless fucks they are
        /// </summary>
        Wander
    }
}