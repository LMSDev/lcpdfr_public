namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.LCPDFR.API;

    /// <summary>
    /// Responsible for starting ambient scenarios in the streets.
    /// </summary>
    [ScriptInfo("AmbientScenarioManager", true)]
    internal class AmbientScenarioManager : BaseScript
    {
        /// <summary>
        /// Currently running scenarios.
        /// </summary>
        private List<Scenario> currentScenarios;

        /// <summary>
        /// Scenarios registered.
        /// </summary>
        private Dictionary<string, Type> registeredScenarios;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbientScenarioManager"/> class.
        /// </summary>
        public AmbientScenarioManager()
        {
            this.RegisterConsoleCommands();

            this.currentScenarios = new List<Scenario>();
            this.registeredScenarios = new Dictionary<string, Type>();
            this.AllowAmbientScenarios = true;
            this.MaximumNumberOfScenarios = Settings.AmbientScenariosMaximum;

            // Register all scenarios
            this.registeredScenarios.Add("DrugDeal", typeof(ScenarioDrugdeal));
            this.registeredScenarios.Add("CopPullover", typeof(ScenarioCopPullover));
            this.registeredScenarios.Add("RandomPursuit", typeof(ScenarioRandomPursuit));
            this.registeredScenarios.Add("DrunkGuy", typeof(ScenarioDrunkGuy));
            this.registeredScenarios.Add("DrunkDriver", typeof(ScenarioDrunkDriver));
            //this.registeredScenarios.Add("PlayerAssault", typeof(ScenarioPlayerAssault));

            EventAmbientFootChase.EventRaised += this.EventAmbientFootChase_EventRaised;
        }

        /// <summary>
        /// Called when an officer starts chasing an ambient ped.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventAmbientFootChase_EventRaised(EventAmbientFootChase @event)
        {
            Log.Debug("EventAmbientFootChase_EventRaised: Officer chasing at " + @event.Cop.Position.ToString(), "AmbientScenarioManager");
            StartFootPursuitScenario(@event.Cop, @event.Suspect);
        }

        /// <summary>
        /// Gets or sets a value indicating whether ambient scenarios are allowed to start.
        /// </summary>
        public bool AllowAmbientScenarios { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of random scenarios.
        /// </summary>
        public int MaximumNumberOfScenarios { get; set; }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            EventAmbientFootChase.EventRaised -= this.EventAmbientFootChase_EventRaised;

            if (this.currentScenarios != null)
            {
                foreach (Scenario currentScenario in this.currentScenarios)
                {
                    currentScenario.MakeAbortable();
                }
            }
        }

        /// <summary>
        /// Called every tick to process all script logic.
        /// </summary>
        public override void Process()
        {
            if (this.AllowAmbientScenarios && this.currentScenarios.Count < this.MaximumNumberOfScenarios && !LCPDFRPlayer.LocalPlayer.IsBusy && !LCPDFRPlayer.LocalPlayer.IsInPoliceDepartment)
            {
                // Random chance to start new scenario
                int divisor = Common.EnsureValueIsNotZero<int>(Settings.AmbientScenariosMultiplier);
                int randomValue = Common.GetRandomValue(0, 1000000 / divisor);
                if (randomValue == 1)
                {
                    this.StartScenario();
                }
            }

            for (int i = 0; i < this.currentScenarios.Count; i++)
            {
                Scenario currentScenario = this.currentScenarios[i];
                if (currentScenario.Active)
                {
                    if (currentScenario is IAmbientScenario)
                    {
                        IAmbientScenario ambientScenario = (IAmbientScenario)currentScenario;

                        // If still in use, process. Remove otherwise
                        if (!ambientScenario.CanBeDisposedNow())
                        {
                            currentScenario.Process();
                        }
                        else
                        {
                            Log.Debug("Process: Scenario can be disposed: " + currentScenario.ComponentName, this);
                            this.currentScenarios.Remove(currentScenario);
                            i--;
                        }
                    }
                    else
                    {
                        Log.Warning("StartScenario: Scenario is not of type IAmbientScenario: " + currentScenario.ComponentName, this);
                    }
                }
                else
                {
                    this.currentScenarios.Remove(currentScenario);
                    i--;
                }
            }
        }

        /// <summary>
        /// Registers the scenario of type <paramref name="type"/> and <paramref name="name"/> so it can be started by the manager.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        public void RegisterScenario(Type type, string name)
        {
            this.registeredScenarios.Add(name, type);
        }

        /// <summary>
        /// Adds a running scenario to the collection. Does not initialize the scenario.
        /// </summary>
        /// <param name="scenario">The scenario.</param>
        public void AddRunningScenario(Scenario scenario)
        {
            this.currentScenarios.Add(scenario);
        }

        /// <summary>
        /// The callback of StartScenario.
        /// </summary>
        /// <param name="parameterCollection">The parameter.</param>
        [ConsoleCommand("StartScenario", false)]
        private void StartScenarioConsoleCallback(GTA.ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string param = parameterCollection[0];
                this.StartScenario(param, CPlayer.LocalPlayer.Ped.Position);
            }
            else
            {
                this.StartScenario();
            }
        }

        /// <summary>
        /// The callback of EndScenario.
        /// </summary>
        /// <param name="parameterCollection">The parameter.</param>
        [ConsoleCommand("EndScenario", false)]
        private void EndScenarioConsoleCallback(GTA.ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string name = parameterCollection[0];
                foreach (Scenario currentScenario in this.currentScenarios)
                {
                    if (currentScenario.ComponentName.Contains(name))
                    {
                        currentScenario.MakeAbortable();
                    }
                }
            }
            else
            {
                Log.Debug("EndScenarioConsoleCallback: No name given", this);
            }
        }

        /// <summary>
        /// Starts a new random scenario.
        /// </summary>
        private void StartScenario()
        {
            string randomName = Common.GetRandomCollectionValue<string>(this.registeredScenarios.Keys.ToArray());
            this.StartScenario(randomName, CPlayer.LocalPlayer.Ped.Position);
        }

        /// <summary>
        /// Starts <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the scenario.</param>
        /// <param name="position">The position.</param>
        private void StartScenario(string name, GTA.Vector3 position)
        {
            if (!this.registeredScenarios.ContainsKey(name))
            {
                Log.Warning("StartScenario: Failed to start \"" + name + "\": No such scenario", this);
                return;
            }

            Scenario scenario = null;
            object obj = Activator.CreateInstance(this.registeredScenarios[name]);

            if (obj is WorldEvent)
            {
                scenario = ((WorldEvent)obj).GetScenario();
            }

            if (obj is Scenario)
            {
                scenario = obj as Scenario;
            }

            if (scenario is IAmbientScenario)
            {
                IAmbientScenario ambientScenario = (IAmbientScenario)scenario;
                if (ambientScenario.CanScenarioStart(position))
                {
                    scenario.Initialize();
                    Log.Debug("Created scenario: " + name, this);
                    this.currentScenarios.Add(scenario);
                }
                else
                {
                    Log.Debug("Failed to create scenario: " + name, this);
                }
            }
            else
            {
                Log.Warning("StartScenario: Scenario is not of type IAmbientScenario: " + name, this);
            }
        }

        /// <summary>
        /// Starts an ambient foot pursuit.
        /// </summary>
        /// <param name="officer">The officer.</param>
        /// <param name="officer">The suspect.</param>
        private void StartFootPursuitScenario(CPed officer, CPed suspect)
        {
            if (Settings.ForceDisableUseFootChasesAsEvent)
            {
                return;
            }

            String name = "RandomPursuit";

            if (!this.registeredScenarios.ContainsKey(name))
            {
                Log.Warning("StartScenario: Failed to start \"" + name + "\": No such scenario", this);
                return;
            }

            ScenarioRandomPursuit scenario = new ScenarioRandomPursuit(officer, suspect);
            this.AddRunningScenario(scenario);
        }
    }
}