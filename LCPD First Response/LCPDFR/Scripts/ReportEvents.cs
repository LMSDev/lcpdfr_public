namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Collections.Generic;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    /// <summary>
    /// Reporting events to dispatch
    /// </summary>
    [ScriptInfo("ReportEvents", true)]
    internal class ReportEvents : GameScript
    {
        /// <summary>
        /// A list of queued events.
        /// </summary>
        private static List<Event> queuedEvents = new List<Event>();

        /// <summary>
        /// The event the tick process is currently processing.
        /// </summary>
        private static Event currentEvent = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportEvents"/> class.
        /// </summary>
        public ReportEvents()
        {
            // EventOfficerDown.EventRaised += new EventOfficerDown.EventRaisedEventHandler(EventOfficerDown_EventRaised);
            EventPedDead.EventRaised += new EventPedDead.EventRaisedEventHandler(EventPedDead_EventRaised);
            // EventVisualLost.EventRaised += new EventVisualLost.EventRaisedEventHandler(EventVisualLost_EventRaised);
            // EventCriminalEnteredVehicle.EventRaised += new EventCriminalEnteredVehicle.EventRaisedEventHandler(EventCriminalEnteredVehicle_EventRaised);
            // EventCriminalLeftVehicle.EventRaised += new EventCriminalLeftVehicle.EventRaisedEventHandler(EventCriminalLeftVehicle_EventRaised);
            // EventCriminalEscaped.EventRaised += new EventCriminalEscaped.EventRaisedEventHandler(EventCriminalEscaped_EventRaised);
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.CallInPursuit) && !CPlayer.LocalPlayer.Ped.IsAiming && !CPlayer.LocalPlayer.IsTargettingAnything)
            {
                if (currentEvent == null)
                {
                    if (Hardcore.playerInjured)
                    {
                        if (!CPlayer.LocalPlayer.Ped.IsInVehicle)
                        {
                            if (!CPlayer.LocalPlayer.Ped.IsAmbientSpeechPlaying)
                            {
                                /*
                                foreach (CPed reactionPed in Pools.PedPool.GetAll())
                                {
                                    if (reactionPed.Exists())
                                    {
                                        if (reactionPed.Intelligence.CanSeePed(CPlayer.LocalPlayer.Ped))
                                        {
                                            if (!reactionPed.IsInVehicle)
                                            {
                                                if (!reactionPed.IsRequiredForMission && !reactionPed.IsInCombat && !reactionPed.IsInjured && reactionPed.IsAliveAndWell)
                                                {
                                                    if (reactionPed.RelationshipGroup == GTA.RelationshipGroup.Civillian_Male || reactionPed.RelationshipGroup == GTA.RelationshipGroup.Civillian_Female || reactionPed.RelationshipGroup == GTA.RelationshipGroup.Cop || reactionPed.RelationshipGroup == GTA.RelationshipGroup.Medic || reactionPed.RelationshipGroup == GTA.RelationshipGroup.Fireman)
                                                    {
                                                        if (reactionPed.Intelligence.IsFreeForAction(Engine.Scripting.Tasks.EPedActionPriority.AmbientTask))
                                                        {
                                                            if (reactionPed.RelationshipGroup == GTA.RelationshipGroup.Cop)
                                                            {
                                                                if (!reactionPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexWanderCop))
                                                                {
                                                                    reactionPed.SetDefensiveArea(CPlayer.LocalPlayer.Ped.Position, 50.0f);
                                                                    reactionPed.SayAmbientSpeech("OFFICER_DOWN");
                                                                    
                                                                    break;
                                                                }     
                                                            }
                                                            else if (reactionPed.RelationshipGroup == GTA.RelationshipGroup.Fireman || reactionPed.RelationshipGroup == GTA.RelationshipGroup.Medic)
                                                            {
                                                                if (!reactionPed.Intelligence.TaskManager.IsTaskActive(ETaskID.TreatPed))
                                                                {
                                                                    TaskTreatPed taskTreatPed = new TaskTreatPed(CPlayer.LocalPlayer.Ped);
                                                                    taskTreatPed.AssignTo(reactionPed, ETaskPriority.MainTask);
                                                                    break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                reactionPed.Task.TurnTo(CPlayer.LocalPlayer.Ped);
                                                                DelayedCaller.Call(delegate { reactionPed.SayAmbientSpeech("SURPRISED"); }, Common.GetRandomValue(2000, 3000));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                 *
                                } */
                            }

                        }
                    }
                }


                // If key to call in an event is down
                foreach (Event e in queuedEvents)
                {
                    if (e is EventPedDead)
                    {
                        EventPedDead pedEvent = (EventPedDead)e;
                        if (pedEvent.Ped != null && pedEvent.Ped.Exists())
                        {
                            if (CPlayer.LocalPlayer.Ped.Intelligence.CanSeePed(pedEvent.Ped))
                            {
                                if (pedEvent.Ped.PedGroup == EPedGroup.Criminal)
                                {
                                    AudioHelper.PlaySpeechInScannerFromPed(CPlayer.LocalPlayer.Ped, "KILLED_SUSPECT");
                                    // speech = "KILLED_SUSPECT";
                                    DelayedCaller.Call(delegate { AudioHelper.PlayDispatchAcknowledgeSuspectDown(); }, 4500);
                                }
                                else if (pedEvent.Ped.PedGroup == EPedGroup.Cop)
                                {
                                    DelayedCaller.Call(delegate { PoliceScanner.ReportOfficerDown(pedEvent.Ped, false); }, 5000);
                                    AudioHelper.PlaySpeechInScannerFromPed(CPlayer.LocalPlayer.Ped, "OFFICER_DOWN");
                                    // speech = "OFFICER_DOWN";
                                    DelayedCaller.Call(delegate { AudioHelper.PlayDispatchAcknowledgeOfficerDown(CPlayer.LocalPlayer.Ped.Position); }, 4500);
                                }
                                else
                                {
                                    AudioHelper.PlaySpeechInScannerFromPed(CPlayer.LocalPlayer.Ped, "PED_SHOT");
                                    // speech = "PED_SHOT";
                                    GTA.Vector3 position = pedEvent.Ped.Position;
                                    DelayedCaller.Call(delegate { AudioHelper.PlayDispatchAcknowledgeCivilianDown(position); }, 6000);
                                }

                                LCPDFRPlayer.LocalPlayer.IsReporting = true;
                                DelayedCaller.Call(delegate { LCPDFRPlayer.LocalPlayer.IsReporting = false; }, 4000);
                                currentEvent = e;
                                break;

                            }
                        }
                    }
                }

                if (currentEvent != null)
                {
                    queuedEvents.Remove(currentEvent);
                    currentEvent = null;
                }
            }
        }

        /// <summary>
        /// Called when a ped dies.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventPedDead_EventRaised(EventPedDead @event)
        {
            // Do nothing when not on duty
            if (!Globals.IsOnDuty)
            {
                return;
            }

            if (@event.Ped != null && @event.Ped.Exists())
            {
                // If ped was in player chase, allow player to report them as being killed.
                if (@event.Ped.PedData.CurrentChase != null)
                {
                    if (@event.Ped.PedData.CurrentChase.IsPlayersChase)
                    {
                        if (@event.Ped.PedGroup == EPedGroup.Criminal)
                        {
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALL_IN_PURSUIT_DEATH"), true);
                            queuedEvents.Add(@event);
                        }
                    }
                }
                else
                {
                    // If not a pursuit suspect, let's find out what it was
                    if (@event.Ped.HasBeenDamagedBy(CPlayer.LocalPlayer.Ped))
                    {
                        // If not a criminal, report a civlian down by player
                        if (@event.Ped.PedGroup != EPedGroup.Criminal)
                        {
                            Log.Debug("EventPedDead_EventRaised: Civilian killed by player", "PoliceScanner");
                            Stats.UpdateStat(Stats.EStatType.AccidentalKills, 1, @event.Ped.Position);

                            // If the player can see the ped, prompt them to report it.
                            if (CPlayer.LocalPlayer.Ped.Intelligence.CanSeePed(@event.Ped))
                            {
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALL_IN_CIVILIAN_DEATH"), true);
                            }
                        }
                        else if (@event.Ped.PedGroup == EPedGroup.Cop)
                        {
                            // If the ped is a cop
                            Log.Debug("EventPedDead_EventRaised: Player killed a cop", "PoliceScanner");
                            if (CPlayer.LocalPlayer.Ped.Intelligence.CanSeePed(@event.Ped))
                            {
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALL_IN_COP_DEATH"), true);
                            }
                        }
                        else
                        {
                            // Any other ped
                            // If the player can see the ped, prompt them to report it.
                            if (CPlayer.LocalPlayer.Ped.Intelligence.CanSeePed(@event.Ped))
                            {
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALL_IN_CIVILIAN_DEATH"), true);
                            }
                        }
                    }

                    // If the ped is the player
                    if (@event.Ped.PedGroup == EPedGroup.Player)
                    {
                        Log.Debug("EventPedDead_EventRaised: Player has been killed", "PoliceScanner");
                        Stats.UpdateStat(Stats.EStatType.OfficersKilled, 1, @event.Ped.Position);
                        DelayedCaller.Call(delegate { AudioHelper.PlayDispatchAcknowledgeOfficerDown(CPlayer.LocalPlayer.Ped.Position); }, 2000);
                    }
                    else
                    {
                        // Queue up event if not player's death
                        queuedEvents.Add(@event);
                    }
                }
            }
        }
    }
}
