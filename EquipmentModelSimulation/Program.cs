using System;

namespace EquipmentModelSimulation
{
    class Program
    {
        private static readonly int MAX_RENDERS_PER_SECOND = 5;

        private static void Main(string[] args)
        {
            Arguments.ParseArgs(args);

            if (Arguments.ShowHelp)
            {
                Arguments.PrintHelp();
                return;
            }

            // Initialize the interface
            ConsoleGUI gui = new ConsoleGUI();

            gui.Log();
            gui.Log("Welcome to the Equipment Model simulation!");
            gui.Log("------------------------------------------");
            gui.Log();

            // Calculate the length of a singe time step
            double timeStep = 1.0 / Arguments.DataPointsPerSecond;

            Simulation simulation = new Simulation(
                simulateFrom: DateTime.UtcNow.AddSeconds(-Arguments.PopulateHistoryLength));

            // This variable stores time of the last render
            DateTime lastRender = DateTime.MinValue;
            DateTime loopStart;

            gui.Log();
            gui.Log("Connecting to the database...", false);

            using (DBConnection dbConnection = new DBConnection(
                Arguments.NumberOfSites,
                Arguments.Host,
                Arguments.Username,
                Arguments.Password,
                Arguments.ToplevelHierarchyPrefix))
            {
                dbConnection.ConnectOrThrow();
                gui.Log("Done");

                bool simulationHistoryDataWritten = false;

                gui.Log("Clearing previous history values...", false);

                // Clear all previous history data for the equipment properties
                dbConnection.ClearHistory();
                gui.Log("Done");

                gui.Log("Generating history data...", false);

                while (true)
                {
                    loopStart = DateTime.UtcNow;

                    // Run one simulation loop
                    simulation.RunLoop(timeStep);

                    // Case: History calculation finished
                    if (simulation.CurrentTimeReached)
                    {
                        // Case: History data has not been written to the database
                        if (!simulationHistoryDataWritten)
                        {
                            gui.Log("Done");

                            gui.Log("Writing history data...", false);

                            dbConnection.WriteSimulationHistoryData(simulation.History);
                            simulationHistoryDataWritten = true;

                            gui.Log("Done");
                        }

                        // Case: Enough time has been passed since the last render
                        // > Render again
                        if (DateTime.UtcNow.Subtract(lastRender).TotalMilliseconds > 1000 / MAX_RENDERS_PER_SECOND)
                        {
                            gui.DrawSystem(simulation);
                            lastRender = DateTime.UtcNow;
                        }

                        // Update the current values in the database to match the simulation values
                        dbConnection.WriteSimulationCurrentValues(simulation);

                        var nextPointInFuture = simulation.IsNextStepAfter(DateTime.UtcNow, timeStep, Arguments.WriteToFutureSeconds);

                        if (nextPointInFuture)
                        {
                            // Get loop duration
                            var loopDuration = (DateTime.UtcNow - loopStart).TotalMilliseconds;

                            // Sleep for a while, max out to zero
                            var sleepTime = System.Math.Max(timeStep * 1000 - loopDuration, 0);

                            System.Threading.Thread.Sleep((int)sleepTime);
                        }
                    }
                }
            }
        }
    }
}
